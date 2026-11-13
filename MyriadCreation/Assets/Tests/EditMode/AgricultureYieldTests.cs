using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P3.0 农业—食物链：耕作者 × 肥力 × NPP × 开发度 → 地区库存 → 口粮消费 → 饥荒。
    /// 只验证机制方向与守恒，不锁定会被平衡调整的具体系数。
    /// </summary>
    public class AgricultureYieldTests
    {
        private static TileData NewTile(float fertility, float npp, float development, int peasants, int nobles = 0)
        {
            var blocks = new List<PopulationBlock>();
            if (peasants > 0)
            {
                blocks.Add(new PopulationBlock { count = peasants, socialClass = GameEnums.SocialClass.Peasant });
            }
            if (nobles > 0)
            {
                blocks.Add(new PopulationBlock { count = nobles, socialClass = GameEnums.SocialClass.NobilityClergy });
            }
            return new TileData
            {
                exists = true,
                isLand = true,
                regionId = 0,
                tileIndex = 0,
                stability = 50f,
                order = 40f,
                fertility = fertility,
                npp = npp,
                development = development,
                buildingLevels = new int[6],
                populationBlocks = blocks
            };
        }

        private static EconomyManager NewEconomy(TileData[] tiles, Dictionary<int, TradeCenter> centers)
        {
            var goods = new Dictionary<int, GoodsDef>
            {
                {
                    0, new GoodsDef
                    {
                        goodsId = 0,
                        goodsName = "粮食",
                        category = GameEnums.GoodsCategory.Food,
                        baseValue = 1f,
                        weight = 0.1f,
                        edible = true
                    }
                }
            };
            return new EconomyManager(tiles, centers, goods, new CurrencySystem(), new TaxSystem());
        }

        // ---------- 1. 纯函数：输入方向 ----------

        [Test]
        public void Yield_IsZeroWithoutFarmers()
        {
            Assert.AreEqual(0f, AgricultureYield.Compute(NewTile(0.8f, 0.8f, 0.5f, 0, nobles: 100)), 0.0001f,
                "只有贵族没有耕作者时不得凭空产出食物");
        }

        [Test]
        public void Yield_IsZeroOnBarrenOrNonLandTile()
        {
            Assert.AreEqual(0f, AgricultureYield.Compute(NewTile(0f, 0.8f, 0.5f, 100)), 0.0001f,
                "贫瘠土地（肥力 0）不得产生作物");
            Assert.AreEqual(0f, AgricultureYield.Compute(NewTile(0.02f, 0.8f, 0.5f, 100)), 0.0001f,
                "肥力阈值以下视为不可耕作");

            TileData sea = NewTile(0.8f, 0.8f, 0.5f, 100);
            sea.isLand = false;
            Assert.AreEqual(0f, AgricultureYield.Compute(sea), 0.0001f, "非陆地不得产出作物");

            TileData voidTile = NewTile(0.8f, 0.8f, 0.5f, 100);
            voidTile.exists = false;
            Assert.AreEqual(0f, AgricultureYield.Compute(voidTile), 0.0001f, "未存在地块不得产出作物");
        }

        [Test]
        public void Yield_IncreasesWithFarmersFertilityAndDevelopment()
        {
            float baseline = AgricultureYield.Compute(NewTile(0.4f, 0.5f, 0.2f, 100));
            Assert.Greater(AgricultureYield.Compute(NewTile(0.4f, 0.5f, 0.2f, 200)), baseline,
                "更多耕作者应产出更多");
            Assert.Greater(AgricultureYield.Compute(NewTile(0.8f, 0.5f, 0.2f, 100)), baseline,
                "更高肥力应产出更多");
            Assert.Greater(AgricultureYield.Compute(NewTile(0.4f, 0.5f, 0.8f, 100)), baseline,
                "更高开发度应产出更多");
        }

        [Test]
        public void Yield_ScalesWithNpp()
        {
            float poor = AgricultureYield.Compute(NewTile(0.5f, 0.1f, 0.3f, 100));
            float rich = AgricultureYield.Compute(NewTile(0.5f, 0.9f, 0.3f, 100));
            Assert.Greater(rich, poor, "NPP 高（潜在植被生产力高）的地块应产出更多");

            float legacy = AgricultureYield.Compute(NewTile(0.5f, 0f, 0.3f, 100));
            Assert.Greater(legacy, 0f, "旧档 npp=0 应回退中性值，而不是归零");
        }

        // ---------- 2. 集成：产出进入库存并被口粮消费 ----------

        [Test]
        public void FertileCity_ProducesFoodIntoRegionalInventory()
        {
            TileData[] tiles = { NewTile(0.6f, 0.5f, 0.1f, 100) };
            var center = new TradeCenter { regionId = 0, inventoryCapacity = 10000f };
            EconomyManager economy = NewEconomy(tiles, new Dictionary<int, TradeCenter> { { 0, center } });

            float yield = AgricultureYield.Compute(tiles[0]);
            economy.DailyTick();

            Assert.Greater(yield, 1f, "100 农民在中等肥力下应能覆盖 100 人的日粮（1.0）");
            Assert.AreEqual(yield - 1f, center.inventory.GetValueOrDefault(0), 0.001f,
                "入账应为产出减口粮（100 人 × 0.01），不得重复计账");
            Assert.AreEqual(50f, tiles[0].stability, 0.001f, "有粮时不应触发饥荒惩罚");
        }

        [Test]
        public void Drought_ReducesYieldBelowNeed_AndTriggersFamine()
        {
            TileData[] tiles = { NewTile(0.5f, 0.5f, 0.1f, 100) };
            var center = new TradeCenter { regionId = 0, inventoryCapacity = 10000f };
            EconomyManager economy = NewEconomy(tiles, new Dictionary<int, TradeCenter> { { 0, center } });

            float yield = AgricultureYield.Compute(tiles[0]);
            Assert.Less(yield, 1f, "肥力 0.5 的旱年应产不足 100 人的日粮");

            economy.DailyTick();

            Assert.AreEqual(0f, center.inventory.GetValueOrDefault(0), 0.001f, "产出不足日需时库存不得卡住剩余");
            Assert.Less(tiles[0].stability, 50f, "旱灾减产应通过库存缺口触发饥荒惩罚");
        }

        [Test]
        public void Production_DoesNotBypassInventoryCapacity()
        {
            TileData[] tiles = { NewTile(1f, 1f, 1f, 1000) };
            var center = new TradeCenter { regionId = 0, inventoryCapacity = 1f };
            center.inventory[0] = 1f; // 已满仓
            EconomyManager economy = NewEconomy(tiles, new Dictionary<int, TradeCenter> { { 0, center } });

            // 直接走生产入口：DailyTick 会先按地区建筑重算容量，掩盖这里的容量约束。
            economy.ProduceResources();

            Assert.AreEqual(1f, center.GetTotalInventory(), 0.001f, "农业产出同样受仓储容量约束，不得超容");
        }
    }
}
