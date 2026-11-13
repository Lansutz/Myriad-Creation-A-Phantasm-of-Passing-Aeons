using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 补给/金库链路回归：0 号物产（粮食）是合法 id、"未指定"用 -1、付款与发货共用 funding 事实源。
    /// 审计 C 的 P0-1 / P0-2 / P0-4 对应。
    /// </summary>
    public class ProvisioningLedgerTests
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
                    60, new GoodsDef
                    {
                        goodsId = 60,
                        goodsName = "金",
                        category = GameEnums.GoodsCategory.Metal,
                        baseValue = 100f,
                        weight = 0.05f
                    }
                }
            };
        }

        private static RealmData NewRealm(float treasury = 100f)
        {
            return new RealmData { realmId = 1, realmName = "测试国", treasury = treasury };
        }

        [Test]
        public void FoodGoodsIdZero_IsValidInCargoAndVault()
        {
            var unit = new TransportUnit();
            unit.AddCargo(0, 5f);
            Assert.AreEqual(5f, unit.GetCargoAmount(0), 0.001f, "0 号粮食应能进背包");

            unit.AddCargo(-1, 5f);
            Assert.AreEqual(0f, unit.GetCargoAmount(-1), 0.001f, "-1 是无效 id，应被拒绝");

            var vault = new TreasuryVault();
            vault.Add(0, 5f);
            Assert.AreEqual(5f, vault.Get(0), 0.001f, "0 号粮食应能进金库");
            Assert.IsTrue(vault.TrySpend(0, 2f));
            Assert.AreEqual(3f, vault.Get(0), 0.001f);
        }

        [Test]
        public void TryFund_ZeroFoodId_PaysAndEquipsFood()
        {
            var goods = DefaultGoods();
            var realm = NewRealm();
            // 国库里要真的有粮（测试里 100 的 treasury 只是货币，粮食需另行入库）
            var nationalVault = TreasuryLedger.NationalVault(realm);
            nationalVault.Add(0, 100f);
            var cost = UnitProvisioningCost.ForCourier(10f, 0, 60);

            bool ok = UnitProvisioning.TryFund(null, goods, realm, null, UnitFundingSource.NationalTreasury, -1, cost,
                out var funding, out var reason);

            Assert.IsTrue(ok, "国库支付应成功：" + reason);
            Assert.AreEqual(0, funding.foodGoodsId, "实际支付的粮食 id 必须是 0");
            Assert.AreEqual(60, funding.currencyGoodsId);
            Assert.AreEqual(cost.money, funding.moneyPaid, 0.001f, "moneyPaid 应记账");
            Assert.AreEqual(cost.food, funding.foodPaid, 0.001f, "foodPaid 应记账");

            var vault = TreasuryLedger.NationalVault(realm);
            Assert.AreEqual(100f - cost.money, vault.CurrencyAmount(60), 0.01f, "国库货币应真的扣款");
            Assert.AreEqual(100f - cost.food, vault.Get(0), 0.01f, "国库粮食应真的扣款");

            var unit = new TransportUnit();
            UnitProvisioning.Equip(unit, cost, funding, goods);
            Assert.AreEqual(cost.food, unit.GetCargoAmount(0), 0.01f, "口粮应真的进背包");
            Assert.AreEqual(cost.money, unit.GetCargoAmount(60), 0.01f, "经费应真的进背包");
        }

        [Test]
        public void Equip_UsesFundingGoodsId_NotCallerCost()
        {
            var goods = DefaultGoods();
            // 模拟审计 P0-2：TryPay 在内部选中了 id，但调用方的原始 cost 仍是 -1
            var cost = new UnitProvisioningCost
            {
                food = 20f,
                money = 5f,
                foodGoodsId = -1,
                currencyGoodsId = -1
            };
            var funding = new UnitFunding
            {
                foodGoodsId = 0,
                currencyGoodsId = 60,
                foodPaid = 20f,
                moneyPaid = 5f
            };

            var unit = new TransportUnit();
            UnitProvisioning.Equip(unit, cost, funding, goods);

            Assert.AreEqual(20f, unit.GetCargoAmount(0), 0.01f, "发货必须用 funding 里的实际 id");
            Assert.AreEqual(5f, unit.GetCargoAmount(60), 0.01f);
            Assert.AreEqual(0, funding.foodGoodsId, "Equip 不得反向覆盖 funding 的物产 id");
            Assert.AreEqual(60, funding.currencyGoodsId);
        }

        [Test]
        public void TryFund_FailsWhenNoFoodGoodsDefined()
        {
            var cost = new UnitProvisioningCost
            {
                food = 10f,
                foodGoodsId = -1,
                currencyGoodsId = 60
            };
            var realm = NewRealm();

            bool ok = UnitProvisioning.TryFund(null, new Dictionary<int, GoodsDef>(), realm, null,
                UnitFundingSource.NationalTreasury, -1, cost, out _, out var reason);

            Assert.IsFalse(ok, "没有粮食物产时不能假装成功");
            StringAssert.Contains("粮食", reason);
        }

        [Test]
        public void ForCourier_ProvisionsEnoughForDistance()
        {
            var cost = UnitProvisioningCost.ForCourier(30f);

            // 每日需求 2（1 人 + 1 马），行程 ≥ 30 天，另加 3 天缓冲与 15% 余量
            Assert.GreaterOrEqual(cost.food, 2f * (30f + 3f), "口粮必须覆盖整个行程");
            Assert.Greater(cost.money, 0f);
            Assert.AreEqual(1, cost.horses);
            Assert.AreEqual(UnitProvisioningCost.InvalidGoodsId, cost.foodGoodsId, "未指定物产用 -1");
            Assert.AreEqual(UnitProvisioningCost.InvalidGoodsId, cost.currencyGoodsId);
        }

        [Test]
        public void GeneratedWorld_RegionalVault_SeedsFoodAndCurrency()
        {
            // 复核 R1 根因：生成局区域库只有粮没有货币，导致"国库有钱无粮 + 区域库有粮无钱"
            // 两条路都付不出。这里验证 InitializeTradeCenters 同时播粮和货币。
            var go = new GameObject("VaultSeedWorld");
            try
            {
                var world = go.AddComponent<GameWorld>();
                world.mapWidth = 16;
                world.mapHeight = 8;
                world.SetupOptions = MyriadCreation.World.Generation.WorldSetupOptions.CreateStandard();
                world.InitializeWorld();

                Assert.Greater(world.tradeCenters.Count, 0, "应生成区域库");
                TradeCenter first = null;
                foreach (var tc in world.tradeCenters.Values) { first = tc; break; }
                Assert.IsNotNull(first);
                Assert.Greater(first.inventory.GetValueOrDefault(0), 0f, "区域库应播粮食（0 号物产）");

                int currencyId = TreasuryLedger.SelectCurrencyGoodsId(world.goodsDefs);
                Assert.GreaterOrEqual(currencyId, 0, "默认内容应有货币物产");
                Assert.Greater(first.inventory.GetValueOrDefault(currencyId), 0f, "区域库应播货币");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PersonalFunding_FallsBackToRegionalVault_WhenNationalHasNoFood()
        {
            // 复核 R1：Personal 降级国库后，国库没有实物粮草 → 必须回退区域库，否则传令官发不出。
            var goods = DefaultGoods();
            var realm = NewRealm(treasury: 0f); // 国库无钱无粮
            var go = new GameObject("ProvisioningRegionWorld");
            try
            {
                var world = go.AddComponent<GameWorld>();
                var region = new TradeCenter { regionId = 0, inventoryCapacity = 10000f };
                region.inventory[0] = 100f;   // 城库粮草
                region.inventory[60] = 100f;  // 城库货币
                world.tradeCenters = new Dictionary<int, TradeCenter> { { 0, region } };

                var cost = UnitProvisioningCost.ForCourier(5f, 0, 60);
                bool ok = UnitProvisioning.TryFund(world, goods, realm, null, UnitFundingSource.Personal, 0, cost,
                    out var funding, out var reason);

                Assert.IsTrue(ok, "区域库有实物时个人出资应回退成功：" + reason);
                Assert.AreEqual(UnitFundingSource.CityTreasury, funding.source, "资金来源必须回写为实际付款方");
                Assert.Greater(funding.foodPaid, 0f);
                Assert.Less(region.inventory[0], 100f, "区域库粮草应真的被扣");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
