using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// D43 国库单源：货币唯一可支付余额是 realm.treasury；
    /// currencySystem.vault 里的货币物产是储备，不是第二本账；粮草仍从 vault 货物扣。
    /// </summary>
    public class TreasurySingleSourceTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
        }

        private GameWorld NewWorld()
        {
            var go = new GameObject("TreasuryWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(53);
            return world;
        }

        private static RealmData FirstRealm(GameWorld world)
        {
            foreach (var realm in world.realms.Values) return realm;
            return null;
        }

        [Test]
        public void MoneyPayment_DebitsTreasury_NotVaultBalance()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm, "前置：应有政权");
            int currencyId = TreasuryLedger.SelectCurrencyGoodsId(world.goodsDefs);
            Assert.GreaterOrEqual(currencyId, 0, "前置：应有货币物产");
            TreasuryVault vault = world.GetEconomyManager().GetNationalVault(realm);
            vault.Add(currencyId, 9999f);
            realm.treasury = 100f;

            var cost = new UnitProvisioningCost { money = 50f, currencyGoodsId = currencyId, foodGoodsId = -1 };
            bool paid = TreasuryLedger.TryPay(realm, world, UnitFundingSource.NationalTreasury, 0, world.goodsDefs, cost, -1, out _, out string reason);

            Assert.IsTrue(paid, reason);
            Assert.AreEqual(50f, realm.treasury, 0.01f, "货币付款应扣 realm.treasury");
            Assert.AreEqual(9999f, vault.CurrencyAmount(currencyId), 0.01f, "vault 里的货币储备不应被当成第二本账");
        }

        [Test]
        public void RichVault_CannotCoverTreasuryShortfall()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm);
            int currencyId = TreasuryLedger.SelectCurrencyGoodsId(world.goodsDefs);
            Assert.GreaterOrEqual(currencyId, 0);
            TreasuryVault vault = world.GetEconomyManager().GetNationalVault(realm);
            vault.Add(currencyId, 9999f);
            realm.treasury = 10f;

            var cost = new UnitProvisioningCost { money = 50f, currencyGoodsId = currencyId, foodGoodsId = -1 };
            bool paid = TreasuryLedger.TryPay(realm, world, UnitFundingSource.NationalTreasury, 0, world.goodsDefs, cost, -1, out _, out string reason);

            Assert.IsFalse(paid, "vault 富余不能替代国库余额");
            Assert.AreEqual(10f, realm.treasury, 0.01f, "失败付款不得扣款");
            Assert.AreEqual("国库不足", reason);
        }

        [Test]
        public void FoodPayment_SpendsVaultGoods()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm);
            int foodId = TreasuryLedger.SelectFoodGoodsId(world.goodsDefs);
            Assert.GreaterOrEqual(foodId, 0, "前置：应有粮食物产");
            TreasuryVault vault = world.GetEconomyManager().GetNationalVault(realm);
            vault.Add(foodId, 100f);
            realm.treasury = 100f;

            var cost = new UnitProvisioningCost { money = 0f, food = 10f, foodGoodsId = foodId, currencyGoodsId = -1 };
            bool paid = TreasuryLedger.TryPay(realm, world, UnitFundingSource.NationalTreasury, 0, world.goodsDefs, cost, -1, out _, out string reason);

            Assert.IsTrue(paid, reason);
            Assert.AreEqual(90f, vault.Get(foodId), 0.01f, "粮草应从 vault 货物扣");
            Assert.AreEqual(100f, realm.treasury, 0.01f, "纯粮草付款不应动货币国库");
        }
    }
}
