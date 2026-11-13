using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P5.3 机制化税收：税率只看政权自己的 taxSystem；税基是地方仓储里的真实货物，
    /// 没有行政区划持有人、没有库存、没有市场就没有税；货币只从真实售货产生。
    /// </summary>
    public class TaxSingleSourceTests
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
            TreasuryLedger.ClearHistory();
        }

        private GameWorld NewWorld()
        {
            var go = new GameObject("TaxWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(47);
            return world;
        }

        private static RealmData FirstRealm(GameWorld world)
        {
            foreach (var realm in world.realms.Values) return realm;
            return null;
        }

        private static void ZeroRates(TaxSystem tax)
        {
            tax.agriculturalTax = 0f;
            tax.headTax = 0f;
            tax.tradeTax = 0f;
            tax.miningTax = 0f;
            tax.craftTax = 0f;
            tax.livestockTax = 0f;
            tax.luxuryTax = 0f;
            tax.saltMonopolyTax = 0f;
            tax.wartimeSpecialTax = 0f;
        }

        private static CharacterData SeedOfficial(GameWorld world, RealmData realm)
        {
            world.Characters.CreateInitialCourtiers(world.realms, world.currentYear);
            CharacterData official = null;
            foreach (CharacterData character in world.Characters.GetCharactersByRealm(realm.realmId))
            {
                if (character.role != CharacterRole.Ruler && character.role != CharacterRole.Spouse)
                {
                    official = character;
                    break;
                }
            }
            Assert.IsNotNull(official, "前置：应有可任职人物");
            foreach (AdminDivision division in realm.adminDivisions)
            {
                division.holderCharacterId = official.characterId;
            }
            return official;
        }

        private static TradeCenter CapitalCenter(GameWorld world, RealmData realm)
        {
            foreach (int tileIndex in realm.coreTiles)
            {
                int regionId = world.tiles[tileIndex].regionId;
                if (world.tradeCenters.TryGetValue(regionId, out TradeCenter center)) return center;
            }
            return null;
        }

        private static TaxCollectionResult Collect(GameWorld world, RealmData realm, float multiplier = 1f)
        {
            return world.GetEconomyManager().CollectTaxes(
                realm.realmId, world.Characters, world.currentYear * 365 + world.currentDay, multiplier);
        }

        [Test]
        public void RealmTaxSystem_IsTheOnlyRateSource()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm);
            SeedOfficial(world, realm);
            TradeCenter center = CapitalCenter(world, realm);
            Assert.IsNotNull(center);
            ZeroRates(realm.taxSystem);
            center.inventory.Clear();
            center.inventory[0] = 1000f;

            TaxCollectionResult zeroed = Collect(world, realm);
            Assert.AreEqual(0f, zeroed.goodsCollected, 0.001f, "政权税率为 0 时不得回退到全局税率");

            realm.taxSystem.agriculturalTax = 0.5f;
            TaxCollectionResult taxed = Collect(world, realm);
            Assert.Greater(taxed.goodsCollected, 0f, "政权自己的税率应生效");
        }

        [Test]
        public void HigherRate_CollectsMoreFromSameStock()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            SeedOfficial(world, realm);
            TradeCenter center = CapitalCenter(world, realm);
            ZeroRates(realm.taxSystem);
            center.inventory.Clear();
            center.inventory[0] = 1000f;
            realm.taxSystem.agriculturalTax = 0.1f;
            float low = Collect(world, realm).goodsCollected;

            center.inventory[0] = 1000f;
            realm.taxSystem.agriculturalTax = 0.4f;
            float high = Collect(world, realm).goodsCollected;

            Assert.Greater(high, low, "税率高应征得更多实物");
        }

        [Test]
        public void NoAdministrativeHolder_CollectsNothing()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            SeedOfficial(world, realm);
            TradeCenter center = CapitalCenter(world, realm);
            ZeroRates(realm.taxSystem);
            realm.taxSystem.agriculturalTax = 0.3f;
            center.inventory.Clear();
            center.inventory[0] = 1000f;

            realm.adminDivisions.Clear();
            TaxCollectionResult none = Collect(world, realm);

            Assert.AreEqual(0f, none.goodsCollected, 0.001f, "没有行政区划持有人就没有征收资格");
            Assert.AreEqual(1000f, center.inventory[0], 0.001f, "没征收时地方仓储不得下降");
        }

        [Test]
        public void FoodReserve_ProtectsPopulationFromTax()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            SeedOfficial(world, realm);
            TradeCenter center = CapitalCenter(world, realm);
            ZeroRates(realm.taxSystem);
            realm.taxSystem.agriculturalTax = 0.5f;
            realm.taxSystem.reserveDays = 90;

            float population = 0f;
            foreach (int tileIndex in realm.coreTiles)
            {
                int regionId = world.tiles[tileIndex].regionId;
                if (regionId != world.tiles[center.centerTileIndex].regionId) continue;
                population += PopulationLedger.Total(world.tiles[tileIndex]);
            }
            float onlyThirtyDays = population * 0.01f * 30f;
            foreach (TradeCenter other in world.tradeCenters.Values) other.inventory.Clear();
            center.inventory[0] = onlyThirtyDays;

            TaxCollectionResult result = Collect(world, realm);
            Assert.AreEqual(0f, result.goodsCollected, 0.001f, "口粮不足 90 天时不得征走食物");
        }

        [Test]
        public void Money_OnlyAppearsWithMintedCoinAndMarketDemand()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            SeedOfficial(world, realm);
            TradeCenter center = CapitalCenter(world, realm);
            ZeroRates(realm.taxSystem);
            realm.taxSystem.luxuryTax = 0.3f;

            int luxuryId = -1;
            foreach (KeyValuePair<int, GoodsDef> pair in world.goodsDefs)
            {
                if (pair.Value.category == GameEnums.GoodsCategory.Luxury)
                {
                    luxuryId = pair.Key;
                    break;
                }
            }
            Assert.GreaterOrEqual(luxuryId, 0, "前置：内容表应有奢侈品");
            center.localDemand[luxuryId] = 10000f;

            center.inventory.Clear();
            center.inventory[luxuryId] = 1000f;
            realm.currencySystem.currentStage = GameEnums.CurrencyStage.Barter;
            TaxCollectionResult barter = Collect(world, realm);
            Assert.Greater(barter.goodsCollected, 0f);
            Assert.AreEqual(0f, barter.moneyCollected, 0.001f, "物物交换阶段不得凭空产生货币税");

            center.inventory[luxuryId] = 1000f;
            realm.currencySystem.currentStage = GameEnums.CurrencyStage.MintedCoin;
            TaxCollectionResult minted = Collect(world, realm);
            Assert.Greater(minted.moneyCollected, 0f, "有铸币与市场需求的非食用货物才能卖出换钱");
            Assert.Greater(realm.treasury, 0f);
        }
    }
}
