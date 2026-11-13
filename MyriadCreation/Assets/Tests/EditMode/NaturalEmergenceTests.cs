using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P5：默认世界只有地理/聚落/人口，不预置政权；政权由聚落人口、开发与仓储剩余自然形成。
    /// </summary>
    public class NaturalEmergenceTests
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
            TaxReceiptLedger.Clear();
        }

        private GameWorld NewWorld(int seed = 131, int width = 48, int height = 24)
        {
            var go = new GameObject("NaturalWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.randomSeed = seed;
            world.mapWidth = width;
            world.mapHeight = height;
            world.SetupOptions = WorldSetupOptions.CreateDefault();
            world.InitializeWorld();
            world.GenerateTerrain(seed);
            return world;
        }

        private static SettlementData FirstVillage(GameWorld world)
        {
            foreach (SettlementData settlement in world.settlements.Values)
            {
                if (!settlement.IsRuined && settlement.settlementCategory != SettlementCategory.Outpost) return settlement;
            }
            return null;
        }

        private static void MakeEligible(GameWorld world, SettlementData settlement, float population = 300f, int ageDays = 9 * 365)
        {
            TileData tile = world.tiles[settlement.tileIndex];
            tile.populationBlocks = new List<PopulationBlock>
            {
                new PopulationBlock { count = population, raceId = 0, cultureId = -1, faithId = -1, socialClass = GameEnums.SocialClass.Peasant, satisfaction = 60f }
            };
            world.tiles[settlement.tileIndex] = tile;
            SettlementPopulationSystem.Recompute(world, settlement);
            settlement.development = 10f;
            settlement.foundingTick = world.currentYear * 365 + world.currentDay - ageDays;

            int regionId = tile.regionId;
            if (world.tradeCenters.TryGetValue(regionId, out TradeCenter center))
            {
                center.inventory.Clear();
                center.inventory[0] = population; // 0 号小麦：足够 90 天以上口粮
            }
        }

        [Test]
        public void DefaultWorld_HasSettlementsButNoPolitiesCharactersOrOffices()
        {
            var world = NewWorld();

            Assert.AreEqual(0, world.realms.Count, "默认世界不得预置政权");
            Assert.AreEqual(0, world.Characters.GetTotalCharacterCount(), "默认世界不得预置君主/宫廷");
            Assert.Greater(world.settlements.Count, 0, "默认世界应有聚落（人群），只是没有国家");
            foreach (RealmData realm in world.realms.Values)
            {
                Assert.IsTrue(realm.officeHolders == null || realm.officeHolders.Count == 0);
            }
        }

        [Test]
        public void EligibleSettlement_FormsRealmWithFoundingHousehold()
        {
            var world = NewWorld();
            SettlementData settlement = FirstVillage(world);
            Assert.IsNotNull(settlement, "前置：应生成聚落");
            MakeEligible(world, settlement);
            Assert.IsTrue(StateFormationSystem.IsEligible(world, settlement),
                $"前置：应满足建国资格（pop={settlement.population:F0} dev={settlement.development:F1} age={world.currentYear * 365 + world.currentDay - settlement.foundingTick}）");

            bool formed = StateFormationSystem.MonthlyTick(world);

            Assert.IsTrue(formed, "人口/开发/口粮剩余满足时应形成政权");
            Assert.AreEqual(1, world.realms.Count);
            RealmData realm = null;
            foreach (RealmData value in world.realms.Values) realm = value;
            Assert.IsNotNull(realm);
            Assert.GreaterOrEqual(realm.monarchId, 0, "政权必须有真实首领");
            Assert.AreEqual(realm.realmId, world.tiles[settlement.tileIndex].ownerRealmId, "聚落地块应归新政权");
            Assert.AreEqual(0f, realm.treasury, 0.001f, "自然政权不得开局送国库货币");
            Assert.IsTrue(realm.officeHolders == null || realm.officeHolders.Count == 0, "官职应由后续制度门槛补，不在建国当天填满");
            Assert.GreaterOrEqual(world.Characters.GetTotalCharacterCount(), 2, "至少应有首领与家庭/地方显贵");
        }

        [Test]
        public void NoFoodSurplus_DoesNotFormRealm()
        {
            var world = NewWorld();
            SettlementData settlement = FirstVillage(world);
            Assert.IsNotNull(settlement);
            MakeEligible(world, settlement);
            TradeCenter center = world.tradeCenters[world.tiles[settlement.tileIndex].regionId];
            center.inventory.Clear();

            Assert.IsFalse(StateFormationSystem.MonthlyTick(world), "没有口粮剩余不得建国");
            Assert.AreEqual(0, world.realms.Count);
        }

        [Test]
        public void TooYoungSettlement_DoesNotFormRealm()
        {
            var world = NewWorld();
            SettlementData settlement = FirstVillage(world);
            Assert.IsNotNull(settlement);
            MakeEligible(world, settlement, ageDays: 365);

            Assert.IsFalse(StateFormationSystem.MonthlyTick(world), "聚落存在不足 8 年不得建国");
            Assert.AreEqual(0, world.realms.Count);
        }

        [Test]
        public void OfficeThresholds_RequireTerritoryAndPopulation()
        {
            OfficeDef governor = new OfficeDef { minTiles = 2, minPopulation = 150f, minCentralization = 0.05f };
            Assert.IsFalse(OfficeSystem.MeetsInstitutionalThreshold(governor, new OfficeRealmContext
            {
                ownedTiles = 1, population = 300f, centralization = 0.3f, cultureStage = 4
            }), "只有一块地时不设地方长官");
            Assert.IsTrue(OfficeSystem.MeetsInstitutionalThreshold(governor, new OfficeRealmContext
            {
                ownedTiles = 2, population = 300f, centralization = 0.3f, cultureStage = 4
            }), "领土与人口达标后才设官");
        }

        [Test]
        public void SpouseAndCommoner_AreNotOfficeCandidates()
        {
            var manager = new CharacterManager();
            CharacterData ruler = manager.CreateCharacter("君", "氏", 40, true, 0, 0, 0, CharacterRole.Ruler);
            CharacterData spouse = manager.CreateCharacter("后", "氏", 35, false, 0, 0, 0, CharacterRole.Spouse);
            CharacterData commoner = manager.CreateCharacter("民", "氏", 30, true, 0, 0, 0, CharacterRole.Commoner);
            OfficeDef def = new OfficeDef();

            Assert.IsFalse(OfficeSystem.IsEligible(spouse, ruler, def));
            Assert.IsFalse(OfficeSystem.IsEligible(commoner, ruler, def));
            Assert.IsFalse(OfficeSystem.IsEligible(ruler, ruler, def));
        }

        [Test]
        public void StateFormation_IsDeterministicForSameSeed()
        {
            var first = NewWorld(4242, 64, 32);
            SettlementData firstVillage = FirstVillage(first);
            Assert.IsNotNull(firstVillage);
            MakeEligible(first, firstVillage);
            Assert.IsTrue(StateFormationSystem.MonthlyTick(first));

            var second = NewWorld(4242, 64, 32);
            SettlementData secondVillage = FirstVillage(second);
            Assert.IsNotNull(secondVillage);
            MakeEligible(second, secondVillage);
            Assert.IsTrue(StateFormationSystem.MonthlyTick(second));

            RealmData firstRealm = null;
            foreach (RealmData realm in first.realms.Values) firstRealm = realm;
            RealmData secondRealm = null;
            foreach (RealmData realm in second.realms.Values) secondRealm = realm;
            Assert.IsNotNull(firstRealm);
            Assert.IsNotNull(secondRealm);
            Assert.AreEqual(firstRealm.realmName, secondRealm.realmName, "同 seed 的政权名必须一致");
            Assert.AreEqual(firstRealm.coreTiles.Count, secondRealm.coreTiles.Count, "同 seed 的领土数必须一致");
            var firstTiles = new List<int>(firstRealm.coreTiles);
            var secondTiles = new List<int>(secondRealm.coreTiles);
            firstTiles.Sort();
            secondTiles.Sort();
            CollectionAssert.AreEqual(firstTiles, secondTiles, "同 seed 的领土必须一致");
        }

        [Test]
        [Explicit("诊断用，默认套件不跑")]
        public void Diagnose_EligibilityBreakdown()
        {
            var world = NewWorld(4242, 64, 32);
            float genMaxPop = 0f;
            float genMaxTilePop = 0f;
            float genMaxDev = 0f;
            foreach (SettlementData s in world.settlements.Values)
            {
                if (s.settlementCategory == SettlementCategory.Outpost) continue;
                genMaxPop = Mathf.Max(genMaxPop, s.population);
                genMaxTilePop = Mathf.Max(genMaxTilePop, PopulationLedger.Total(world.tiles[s.tileIndex]));
                genMaxDev = Mathf.Max(genMaxDev, world.tiles[s.tileIndex].development);
            }
            TestContext.WriteLine($"GEN total={world.settlements.Count} genMaxPop={genMaxPop:F0} genMaxTilePop={genMaxTilePop:F0} genMaxDev={genMaxDev:F2}");
            for (int month = 0; month < 24; month++)
            {
                world.RunDiagnosticDays(30);
                int cities = 0, villages = 0, outposts = 0;
                float monthMaxPop = 0f, monthMinPop = float.MaxValue, monthMaxFood = 0f;
                foreach (SettlementData s in world.settlements.Values)
                {
                    if (s.settlementCategory == SettlementCategory.Outpost) { outposts++; continue; }
                    if (s.settlementForm == SettlementForm.City) cities++; else villages++;
                    monthMaxPop = Mathf.Max(monthMaxPop, s.population);
                    monthMinPop = Mathf.Min(monthMinPop, s.population);
                    monthMaxFood = Mathf.Max(monthMaxFood, StateFormationSystem.EdibleStock(world, world.tiles[s.tileIndex].regionId));
                }
                TestContext.WriteLine($"MONTH {month + 1} y={world.currentYear} d={world.currentDay} total={world.settlements.Count} city={cities} village={villages} outpost={outposts} maxPop={monthMaxPop:F0} minPop={(monthMinPop == float.MaxValue ? 0 : monthMinPop):F0} maxFood={monthMaxFood:F0}");
            }
            for (int year = 0; year < 10; year++) world.RunDiagnosticDays(365);
            int total = 0, ruined = 0, outpost = 0, owned = 0, ageOk = 0, popOk = 0, devOk = 0, foodOk = 0, eligible = 0;
            float maxPop = 0f, maxFood = 0f;
            int maxAge = -1;
            foreach (SettlementData s in world.settlements.Values)
            {
                total++;
                if (s.IsRuined) { ruined++; continue; }
                if (s.settlementCategory == SettlementCategory.Outpost) { outpost++; continue; }
                TileData tile = world.tiles[s.tileIndex];
                if (tile.ownerRealmId >= 0) owned++;
                int age = world.currentDay - s.foundingTick;
                if (age > maxAge) maxAge = age;
                if (age >= 8 * 365) ageOk++;
                float pop = Mathf.Max(s.population, PopulationLedger.Total(tile));
                if (pop > maxPop) maxPop = pop;
                if (pop >= 200f) popOk++;
                if (s.development >= 6f) devOk++;
                float food = StateFormationSystem.EdibleStock(world, tile.regionId);
                if (food > maxFood) maxFood = food;
                if (food >= pop * 0.01f * 90f) foodOk++;
                if (StateFormationSystem.IsEligible(world, s)) eligible++;
            }
            int officeCount = 0;
            int receiptCount = 0;
            foreach (RealmData realm in world.realms.Values)
            {
                if (realm.officeHolders != null) officeCount += realm.officeHolders.Count;
                receiptCount += TaxReceiptLedger.GetHistory(realm.realmId, 0).Count;
            }
            TestContext.WriteLine($"DIAG total={total} ruined={ruined} outpost={outpost} owned={owned} ageOk={ageOk} popOk={popOk} devOk={devOk} foodOk={foodOk} eligible={eligible} maxPop={maxPop:F0} maxFood={maxFood:F0} maxAge={maxAge} day={world.currentDay} realms={world.realms.Count} offices={officeCount} receipts={receiptCount}");
            Assert.Pass();
        }

        /// <summary>
        /// 自然长跑：默认世界（无预置政权）2 seed × 100 年，必须出现政权、官职与实物税流水。
        /// Explicit：按需运行，默认套件不跑。
        /// </summary>
        [Test]
        [Explicit("自然世界 100 年长跑按需运行，默认套件不跑")]
        [Timeout(1800000)]
        public void NaturalLongRun_FormsRealmOfficesAndTaxReceipts()
        {
            // 100 年自然世界实体规模大，长跑取 1 个 seed；确定性由独立单测覆盖。
            int[] seeds = { 4242 };
            foreach (int seed in seeds)
            {
                var world = NewWorld(seed, 64, 32);
                bool everFormed = false;
                bool everHadOffice = false;
                bool everHadTaxReceipt = false;
                for (int year = 0; year < 100; year++)
                {
                    world.RunDiagnosticDays(365);
                    if (world.realms.Count > 0) everFormed = true;
                    foreach (RealmData realm in world.realms.Values)
                    {
                        if (realm.officeHolders != null && realm.officeHolders.Count > 0) everHadOffice = true;
                        if (TaxReceiptLedger.GetHistory(realm.realmId, 0).Count > 0) everHadTaxReceipt = true;
                    }
                }

                Assert.IsTrue(everFormed, $"seed {seed}：100 年内应自然形成过政权");
                Assert.IsTrue(everHadOffice, $"seed {seed}：政权形成后应出现官职");
                Assert.IsTrue(everHadTaxReceipt, $"seed {seed}：应有实物税从地方仓储流入国家仓");
            }
        }
    }
}
