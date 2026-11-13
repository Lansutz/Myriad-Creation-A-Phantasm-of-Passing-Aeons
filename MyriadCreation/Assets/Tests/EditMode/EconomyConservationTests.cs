using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 市场/仓储守恒与城市缺粮后果回归：对应审计 C 的 P1-2（部分入账丢货、商队货物蒸发）与 P1-3（缺粮无后果）。
    /// </summary>
    public class EconomyConservationTests
    {
        private static Dictionary<int, GoodsDef> DefaultGoods()
        {
            return new Dictionary<int, GoodsDef>
            {
                {
                    0, new GoodsDef
                    {
                        goodsId = 0,
                        goodsName = "粮食",
                        category = GameEnums.GoodsCategory.Food,
                        baseValue = 1f,
                        weight = 0.1f
                    }
                },
                {
                    3, new GoodsDef
                    {
                        goodsId = 3,
                        goodsName = "盐",
                        category = GameEnums.GoodsCategory.Food,
                        baseValue = 3f,
                        weight = 0.05f
                    }
                }
            };
        }

        private static EconomyManager NewEconomy(TileData[] tiles, Dictionary<int, TradeCenter> centers)
        {
            return new EconomyManager(tiles, centers, DefaultGoods(), new CurrencySystem(), new TaxSystem());
        }

        // ---------- 1. TradeCenter.AddGoods 部分入账语义 ----------

        [Test]
        public void TradeCenter_PartialStorage_ReturnsActuallyAcceptedAmount()
        {
            var center = new TradeCenter { inventoryCapacity = 100f };

            float accepted = center.AddGoodsReturningAccepted(0, 150f);

            Assert.AreEqual(100f, accepted, 0.001f, "容量 100 时 150 只能实际入库 100");
            Assert.AreEqual(100f, center.GetTotalInventory(), 0.001f, "总库存应等于容量上限，不得超容");
            Assert.AreEqual(100f, center.inventory.GetValueOrDefault(0), 0.001f, "0 号粮食是合法 id，入账应记在 0 号上");
        }

        [Test]
        public void TradeCenter_BoolAddGoods_KeepsWholeOrderContract()
        {
            var center = new TradeCenter { inventoryCapacity = 100f };

            Assert.IsTrue(center.AddGoods(0, 60f), "整单入库应返回 true");
            Assert.IsFalse(center.AddGoods(0, 60f), "部分入库保持旧契约：返回 false（但已收下的部分仍在库里）");
            Assert.AreEqual(100f, center.GetTotalInventory(), 0.001f, "返回 false 时也只收下剩余容量，不得超容");
        }

        // ---------- 2. 商队结算守恒 ----------

        [Test]
        public void CaravanArrival_ConservesGoods_WhenDestinationStorageIsFull()
        {
            var source = new TradeCenter { regionId = 0, inventoryCapacity = 1000f };
            source.inventory[0] = 150f;
            var cargo = new Dictionary<int, float>();
            Assert.IsTrue(source.RemoveGoods(0, 150f), "发车时来源库应全额扣减");
            cargo[0] = 150f;

            var destination = new TradeCenter { regionId = 1, inventoryCapacity = 100f };
            float accepted = EconomyManager.DeliverCargo(destination, cargo);

            Assert.AreEqual(100f, accepted, 0.001f, "目的地只装得下 100");
            Assert.AreEqual(100f, destination.GetTotalInventory(), 0.001f);
            Assert.IsTrue(cargo.ContainsKey(0), "装不下的部分必须留在商队 cargo");
            Assert.AreEqual(50f, cargo[0], 0.001f, "剩余 50 不得蒸发");
            Assert.AreEqual(150f, destination.GetTotalInventory() + cargo[0], 0.001f,
                "守恒：来源扣减 150 = 入库 100 + 商队剩余 50");

            destination.inventoryCapacity = 200f;
            float acceptedAgain = EconomyManager.DeliverCargo(destination, cargo);
            Assert.AreEqual(50f, acceptedAgain, 0.001f, "容量腾出后剩余货物应可继续入库");
            Assert.AreEqual(150f, destination.GetTotalInventory(), 0.001f);
            Assert.AreEqual(0, cargo.Count, "全部入库后 cargo 应清空");
        }

        [Test]
        public void CaravanLoss_ReturnsCargoToOrigin_AndReportsUnstorableAsLoss()
        {
            var origin = new TradeCenter { regionId = 0, inventoryCapacity = 100f };
            origin.inventory[0] = 90f; // 只剩 10 的容量
            var economy = NewEconomy(
                new TileData[0],
                new Dictionary<int, TradeCenter> { { 0, origin } });
            var caravan = new Caravan { caravanId = 7, fromRegionId = 0, toRegionId = 1 };
            caravan.cargo[0] = 50f;

            float lost = economy.ReturnCaravanCargo(caravan, "贸易路线不存在");

            Assert.AreEqual(100f, origin.inventory.GetValueOrDefault(0), 0.001f, "能装下的 10 应退回起点区域库");
            Assert.AreEqual(40f, lost, 0.001f, "装不下的 40 必须明确记为损失，不得静默消失");
            Assert.AreEqual(0, caravan.cargo.Count, "商队被移除后 cargo 应清空（已退回或已记损失）");
        }

        [Test]
        public void CaravanLoss_FallsBackToNearestCenter_WhenOriginMissing()
        {
            var nearest = new TradeCenter { regionId = 1, inventoryCapacity = 1000f };
            var economy = NewEconomy(
                new TileData[0],
                new Dictionary<int, TradeCenter> { { 1, nearest } });
            var caravan = new Caravan { caravanId = 9, fromRegionId = 0, toRegionId = 2 };
            caravan.cargo[0] = 30f;

            float lost = economy.ReturnCaravanCargo(caravan, "起点区域库不存在");

            Assert.AreEqual(0f, lost, 0.001f, "起点缺失时应退回最近可用区域库，不应记损失");
            Assert.AreEqual(30f, nearest.inventory.GetValueOrDefault(0), 0.001f, "在途货物应落到最近区域库");
        }

        [Test]
        public void CaravanStuck_RetiresAfterGraceDays_AndLossIsReported()
        {
            // 目的地仓库被塞满（无入库空间），商队到货后应进入滞留计时，宽限期满退役且货物可追踪（退回区域库）
            var origin = new TradeCenter { regionId = 0, inventoryCapacity = 10000f };
            var destination = new TradeCenter { regionId = 1, inventoryCapacity = 10000f };
            destination.inventory[99] = 10000f;
            origin.tradeRoutes.Add(new TradeRoute { fromRegionId = 0, toRegionId = 1 });
            var economy = NewEconomy(
                new TileData[0],
                new Dictionary<int, TradeCenter> { { 0, origin }, { 1, destination } });
            var caravan = new Caravan { caravanId = 7, fromRegionId = 0, toRegionId = 1, isMoving = false };
            caravan.cargo[0] = 50f;
            economy.RegisterCaravan(caravan);

            for (int day = 0; day < 4; day++)
            {
                economy.DailyTick();
            }

            Assert.AreEqual(1, economy.GetActiveCaravans().Count, "退役宽限期内商队应仍在等待入库");
            Assert.AreEqual(50f, caravan.cargo[0], 0.001f, "等待入库期间货物不得蒸发");
            Assert.AreEqual(0f, origin.inventory.GetValueOrDefault(0), 0.001f, "退役前货物不应提前退回");

            economy.DailyTick(); // 第 5 天：连续无法入库 → 退役

            Assert.AreEqual(0, economy.GetActiveCaravans().Count, "连续 5 天无法入库必须退役商队，避免永久占位阻塞发车");
            Assert.AreEqual(0, caravan.cargo.Count, "退役后 cargo 应清空（退回区域库或明确记损失）");
            Assert.AreEqual(50f, origin.inventory.GetValueOrDefault(0), 0.001f, "在途货物应退回起点区域库，不得蒸发");
            Assert.AreEqual(10050f, origin.GetTotalInventory() + destination.GetTotalInventory(), 0.001f,
                "退役后总量守恒：区域库 50 + 目的地 10000 = 出发前 10000 + 在途 50");
        }

        [Test]
        public void InitiateTrades_RespectsCaravanCap()
        {
            // 场景 1：已有 19 支商队 → 只能再发 1 支；两条路线共 2 次发车机会，循环内复查必须拦住第 21 支
            var origin = new TradeCenter { regionId = 0, inventoryCapacity = 10000f };
            origin.inventory[0] = 1000f;
            origin.localSupply[0] = 1f; // 有可供货物，且产地库存 > 发车门槛
            var destination = new TradeCenter { regionId = 1, inventoryCapacity = 10000f };
            origin.tradeRoutes.Add(new TradeRoute { fromRegionId = 0, toRegionId = 1, currentEfficiency = 1f });
            origin.tradeRoutes.Add(new TradeRoute { fromRegionId = 0, toRegionId = 1, currentEfficiency = 1f });
            var economy = NewEconomy(
                new TileData[0],
                new Dictionary<int, TradeCenter> { { 0, origin }, { 1, destination } });
            for (int i = 0; i < 19; i++)
            {
                economy.RegisterCaravan(new Caravan { caravanId = -1 - i });
            }

            economy.InitiateTrades();

            Assert.AreEqual(20, economy.GetActiveCaravans().Count, "上限内只能再发 1 车，循环内复查必须拦住第 21 车");
            Assert.AreEqual(900f, origin.inventory.GetValueOrDefault(0), 0.001f, "只应扣 1 车货（100），第二车不得发出");

            // 场景 2：已达上限 → 一支也不发
            var origin2 = new TradeCenter { regionId = 0, inventoryCapacity = 10000f };
            origin2.inventory[0] = 1000f;
            origin2.localSupply[0] = 1f;
            var destination2 = new TradeCenter { regionId = 1, inventoryCapacity = 10000f };
            origin2.tradeRoutes.Add(new TradeRoute { fromRegionId = 0, toRegionId = 1, currentEfficiency = 1f });
            var economy2 = NewEconomy(
                new TileData[0],
                new Dictionary<int, TradeCenter> { { 0, origin2 }, { 1, destination2 } });
            for (int i = 0; i < 20; i++)
            {
                economy2.RegisterCaravan(new Caravan { caravanId = -100 - i });
            }

            economy2.InitiateTrades();

            Assert.AreEqual(20, economy2.GetActiveCaravans().Count, "已达上限时不得再发车");
            Assert.AreEqual(1000f, origin2.inventory.GetValueOrDefault(0), 0.001f, "已达上限时不得扣货发车");
        }

        [Test]
        public void InitiateTrades_SkipsDispatchWhenDestinationIsFull()
        {
            // 目的地满仓：发车前容量预检应跳过该路线——不发车、不占商队名额、不扣货源
            var origin = new TradeCenter { regionId = 0, inventoryCapacity = 10000f };
            origin.inventory[0] = 1000f;
            origin.localSupply[0] = 1f;
            var destination = new TradeCenter { regionId = 1, inventoryCapacity = 100f };
            destination.inventory[99] = 100f; // 目的地已满仓（可用容量 0）
            origin.tradeRoutes.Add(new TradeRoute { fromRegionId = 0, toRegionId = 1, currentEfficiency = 1f });
            var economy = NewEconomy(
                new TileData[0],
                new Dictionary<int, TradeCenter> { { 0, origin }, { 1, destination } });
            economy.RegisterCaravan(new Caravan { caravanId = -1 });
            economy.RegisterCaravan(new Caravan { caravanId = -2 });

            economy.InitiateTrades();

            Assert.AreEqual(2, economy.GetActiveCaravans().Count, "目的地满仓时不得发车，active 数不变（不占商队名额）");
            Assert.AreEqual(1000f, origin.inventory.GetValueOrDefault(0), 0.001f, "容量预检失败时不得扣发货源");

            destination.inventory[99] = 0f; // 腾出容量
            economy.InitiateTrades();

            Assert.AreEqual(3, economy.GetActiveCaravans().Count, "目的地有余量后应恢复发车");
            Assert.AreEqual(900f, origin.inventory.GetValueOrDefault(0), 0.001f, "恢复发车应只扣 1 车货（100）");
        }

        // ---------- 3. 城市缺粮后果 ----------

        private static TileData[] NewSingleCityWorld()
        {
            return new[]
            {
                new TileData
                {
                    exists = true,
                    isLand = true,
                    regionId = 0,
                    tileIndex = 0,
                    stability = 60f,
                    order = 40f,
                    fertility = 1f,
                    development = 1f,
                    buildingLevels = new int[2],
                    populationBlocks = new List<PopulationBlock>
                    {
                        new PopulationBlock { count = 100f, socialClass = GameEnums.SocialClass.Peasant }
                    }
                }
            };
        }

        [Test]
        public void CityWithoutFood_GetsFamineConsequences_OnDailyTick()
        {
            TileData[] tiles = NewSingleCityWorld();
            var center = new TradeCenter { regionId = 0, inventoryCapacity = 10000f };
            var economy = NewEconomy(tiles, new Dictionary<int, TradeCenter> { { 0, center } });

            economy.DailyTick();

            Assert.Less(tiles[0].stability, 60f, "缺粮必须让稳定度下降");
            Assert.Less(tiles[0].order, 40f, "缺粮必须让秩序下降");
            Assert.AreEqual(100f, tiles[0].populationBlocks[0].count, 0.001f,
                "口径 a：人口惩罚归 GameWorld Famine 事件，本系统轻罚不得再扣人口");
            Assert.AreEqual(0f, center.GetTotalInventory(), 0.001f, "缺粮时不得凭空产生库存");
        }

        [Test]
        public void CityWithFood_DoesNotGetFamineConsequences_OnDailyTick()
        {
            TileData[] tiles = NewSingleCityWorld();
            var center = new TradeCenter { regionId = 0, inventoryCapacity = 10000f };
            center.inventory[0] = 100f;
            var economy = NewEconomy(tiles, new Dictionary<int, TradeCenter> { { 0, center } });

            economy.DailyTick();

            Assert.AreEqual(60f, tiles[0].stability, 0.001f, "有粮时不应扣稳定度");
            Assert.AreEqual(40f, tiles[0].order, 0.001f, "有粮时不应扣秩序");
            Assert.AreEqual(100f, tiles[0].populationBlocks[0].count, 0.001f, "有粮时人口不应减少");
            Assert.AreEqual(99f, center.inventory.GetValueOrDefault(0), 0.001f, "有粮时应正常扣掉当日口粮（100 人 * 0.01）");
        }

        [Test]
        public void CityWithHalfDayFood_ConsumesRemainingBeforeFaminePenalty()
        {
            // 库存为日需的 40%：必须先吃光剩余库存再罚，否则库存会永远卡在 0<stock<need
            TileData[] tiles = NewSingleCityWorld();
            var center = new TradeCenter { regionId = 0, inventoryCapacity = 10000f };
            center.inventory[0] = 0.4f; // 日需 = 100 * 0.01 = 1.0
            var economy = NewEconomy(tiles, new Dictionary<int, TradeCenter> { { 0, center } });

            economy.DailyTick();

            Assert.AreEqual(0f, center.inventory.GetValueOrDefault(0), 0.001f, "库存不足日需时应先吃光剩余库存（避免死循环）");
            Assert.AreEqual(59.8f, tiles[0].stability, 0.001f, "半日饥荒应触发轻量稳定度惩罚（−0.2）");
            Assert.AreEqual(39.9f, tiles[0].order, 0.001f, "半日饥荒应触发轻量秩序惩罚（−0.1）");
            Assert.AreEqual(100f, tiles[0].populationBlocks[0].count, 0.001f, "口径 a：缺粮轻罚只动稳定度/秩序，人口归 GameWorld Famine 事件");

            economy.DailyTick();

            Assert.AreEqual(0f, center.inventory.GetValueOrDefault(0), 0.001f, "次日仍无粮，库存不得卡在部分值");
            Assert.Less(tiles[0].stability, 59.8f, "持续缺粮应继续轻量惩罚");
        }

        [Test]
        public void FaminePenalty_DoesNotTouchPopulation()
        {
            // 口径 (a)：人口后果归 GameWorld Famine 事件，本系统缺粮只做 stability/order 轻罚
            TileData[] tiles = NewSingleCityWorld();
            var center = new TradeCenter { regionId = 0, inventoryCapacity = 10000f };
            var economy = NewEconomy(tiles, new Dictionary<int, TradeCenter> { { 0, center } });

            economy.DailyTick();

            Assert.AreEqual(100f, tiles[0].populationBlocks[0].count, 0.001f, "口径 a：本系统不得再扣人口（避免与 Famine 事件双罚）");
            Assert.AreEqual(59.8f, tiles[0].stability, 0.001f, "只保留轻量稳定度惩罚（−0.2）");
            Assert.AreEqual(39.9f, tiles[0].order, 0.001f, "只保留轻量秩序惩罚（−0.1）");
        }
    }
}
