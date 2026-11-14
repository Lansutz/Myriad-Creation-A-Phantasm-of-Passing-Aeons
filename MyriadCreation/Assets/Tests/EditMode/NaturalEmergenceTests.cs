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

        private static void MakeEligible(GameWorld world, SettlementData settlement, float population = 1200f, int ageDays = 210 * 365)
        {
            TileData tile = world.tiles[settlement.tileIndex];
            tile.populationBlocks = new List<PopulationBlock>
            {
                new PopulationBlock { count = population, raceId = 0, cultureId = -1, faithId = -1, socialClass = GameEnums.SocialClass.Peasant, satisfaction = 60f }
            };
            world.tiles[settlement.tileIndex] = tile;
            SettlementPopulationSystem.Recompute(world, settlement);
            settlement.development = 20f;
            // P8.1：政权要求城市形态；测试直接给城市，不依赖 200 年年龄门。
            settlement.settlementForm = SettlementForm.City;
            settlement.hasMarket = true;
            settlement.foundingTick = world.calendar.AbsoluteDay(world.currentYear, world.currentDay) - ageDays;

            int regionId = tile.regionId;
            if (world.tradeCenters.TryGetValue(regionId, out TradeCenter center))
            {
                center.inventory.Clear();
                center.inventory[0] = population * 3f; // 0 号小麦：足够 180 天以上口粮
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

        /// <summary>P8.1：取消年龄门后，刚出现（age=0）的城市只要物质条件齐备就能建国。</summary>
        [Test]
        public void ZeroAgeCity_CanFormRealm()
        {
            var world = NewWorld();
            SettlementData settlement = FirstVillage(world);
            Assert.IsNotNull(settlement);
            MakeEligible(world, settlement, ageDays: 0);

            Assert.IsTrue(StateFormationSystem.MonthlyTick(world), "城市不应因“年龄不足”被挡在建国之外");
            Assert.AreEqual(1, world.realms.Count);
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
        public void Diagnose_FixedCity()
        {
            var world = NewWorld(4242, 48, 24);
            SettlementData tracked = null;
            float bestCap = -1f;
            foreach (SettlementData s in world.settlements.Values)
            {
                if (s.IsRuined || s.settlementCategory == SettlementCategory.Outpost) continue;
                float cap = CarryingCapacitySystem.CalculateCarryingCapacity(world.tiles[s.tileIndex], world.tradeCenters);
                if (cap > bestCap) { bestCap = cap; tracked = s; }
            }
            TestContext.WriteLine($"TRACK {tracked?.settlementName} tile{tracked?.tileIndex} cap0={bestCap:F0}");
            int lastFamine = 0;
            for (int year = 1; year <= 200; year++)
            {
                for (int d = 0; d < 365; d++) world.RunDiagnosticDays(1);
                if (year > 15) break;
                if (year < 5) continue;
                TileData tile = world.tiles[tracked.tileIndex];
                float pop = PopulationLedger.Total(tile);
                var blockInfo = new System.Text.StringBuilder();
                if (tile.populationBlocks != null)
                    foreach (PopulationBlock b in tile.populationBlocks) blockInfo.Append($"{b.socialClass}:{b.count:F0} ");
                bool hasCenter = world.tradeCenters.TryGetValue(tile.regionId, out var center);
float regionPop = 0f, regionAgri = 0f;
for (int t = tile.regionId * 16; t < Mathf.Min(world.tiles.Length, tile.regionId * 16 + 16); t++)
{
    regionPop += PopulationLedger.Total(world.tiles[t]);
    regionAgri += AgricultureYield.Compute(world.tiles[t]);
}
var invInfo = new System.Text.StringBuilder();
if (center != null) foreach (var kv in center.inventory) invInfo.Append($"{kv.Key}:{kv.Value:F0} ");
TestContext.WriteLine($"Y{year} pop{pop:F0} agri{AgricultureYield.Compute(tile):F2} food{StateFormationSystem.EdibleStock(world, tile.regionId):F0} ownFamine{world.FamineByTile[tracked.tileIndex]}");
                int famineDelta = world.FamineEventCount - lastFamine;
                lastFamine = world.FamineEventCount;
                TestContext.WriteLine($"Y{year} pop{pop:F0} cap{CarryingCapacitySystem.CalculateCarryingCapacity(tile, world.tradeCenters):F0} fert{tile.fertility:F2}/{EcosystemSystem.PotentialFertility(tile):F2} temp{tile.annualTemp:F1} precip{tile.annualPrecipMm:F0} npp{tile.npp:F2} biome{tile.biome} river{tile.isRiver} dev{tile.development:F2} food{StateFormationSystem.EdibleStock(world, tile.regionId):F0} ownFamine{(tracked.tileIndex < world.FamineByTile.Length ? world.FamineByTile[tracked.tileIndex] : -1)}");
            }
            Assert.Pass();
        }

        [Test]
        [Explicit("诊断用，默认套件不跑")]
        public void Diagnose_FertilityBaseline()
        {
            var world = NewWorld(4242, 48, 24);
            var values = new List<float>();
            var biomes = new Dictionary<GameEnums.BiomeType, int>();
            float precipSum = 0f, tempSum = 0f, nppSum = 0f;
            foreach (TileData tile in world.tiles)
            {
                if (!tile.exists || !tile.isLand) continue;
                values.Add(tile.fertility);
                precipSum += tile.annualPrecipMm;
                tempSum += tile.annualTemp;
                nppSum += tile.npp;
                biomes.TryGetValue(tile.biome, out int c);
                biomes[tile.biome] = c + 1;
            }
            values.Sort();
            int n = values.Count;
            float Pct(float p) => values[Mathf.Clamp(Mathf.RoundToInt(p * (n - 1)), 0, n - 1)];
            int good = 0, best = 0;
            foreach (float f in values) { if (f >= 0.4f) good++; if (f >= 0.6f) best++; }
            TestContext.WriteLine($"FERT n={n} min={Pct(0f):F2} p25={Pct(0.25f):F2} p50={Pct(0.5f):F2} p75={Pct(0.75f):F2} p90={Pct(0.9f):F2} max={Pct(1f):F2} ge40={good} ge60={best} precip={precipSum / n:F0} temp={tempSum / n:F1} npp={nppSum / n:F2}");
            foreach (var pair in biomes) TestContext.WriteLine($"BIOME {pair.Key}={pair.Value}");
            TestContext.WriteLine($"SETTLEMENTS {world.settlements.Count}");
            int shown = 0;
            foreach (SettlementData s in world.settlements.Values)
            {
                if (shown++ >= 8) break;
                TileData tile = world.tiles[s.tileIndex];
                TestContext.WriteLine($"S {s.settlementName} tile{s.tileIndex} fert{tile.fertility:F2} npp{tile.npp:F2} dev{tile.development:F2} pop{PopulationLedger.Total(tile):F0} cap{CarryingCapacitySystem.CalculateCarryingCapacity(tile, world.tradeCenters):F0} stab{tile.stability:F0} order{tile.order:F0}");
            }
            Assert.Pass();
        }

        [Test]
        [Explicit("诊断用，默认套件不跑")]
        public void Diagnose_After220()
        {
            var world = NewWorld(4242, 48, 24);
            int lastFamine = 0;
            for (int chunk = 0; chunk < 5; chunk++)
            {
                for (int year = 0; year < 50; year++) world.RunDiagnosticDays(365);
                int famineDelta = world.FamineEventCount - lastFamine;
                lastFamine = world.FamineEventCount;
                int cities = 0, outposts = 0, capOk = 0, overCap = 0;
                float chunkMaxPop = 0f, chunkMaxFood = 0f, cityPopSum = 0f, tilePopSum = 0f;
                float fertSum = 0f, devSum = 0f, agriSum = 0f, foodSum = 0f, capSum = 0f;
                foreach (SettlementData s in world.settlements.Values)
                {
                    if (s.IsRuined) continue;
                    if (s.settlementCategory == SettlementCategory.Outpost) { outposts++; continue; }
                    cities++;
                    TileData tile = world.tiles[s.tileIndex];
                    float pop = Mathf.Max(s.population, PopulationLedger.Total(tile));
                    cityPopSum += pop;
                    fertSum += tile.fertility;
                    devSum += tile.development;
                    agriSum += AgricultureYield.Compute(tile);
                    float cap = CarryingCapacitySystem.CalculateCarryingCapacity(tile, world.tradeCenters);
                    capSum += cap;
                    if (cap >= 800f) capOk++;
                    if (pop > cap) overCap++;
                    chunkMaxPop = Mathf.Max(chunkMaxPop, pop);
                    chunkMaxFood = Mathf.Max(chunkMaxFood, StateFormationSystem.EdibleStock(world, tile.regionId));
                }
                foreach (TileData tile in world.tiles)
                {
                    if (tile.exists && tile.isLand) tilePopSum += PopulationLedger.Total(tile);
                }
                int hungryRegions = 0, emptyRegions = 0;
                float[] regionPop = new float[world.tiles.Length];
                foreach (TileData tile in world.tiles)
                {
                    if (!tile.exists || !tile.isLand || tile.populationBlocks == null) continue;
                    if (tile.regionId < 0 || tile.regionId >= regionPop.Length) continue;
                    regionPop[tile.regionId] += PopulationLedger.Total(tile);
                }
                foreach (TradeCenter center in world.tradeCenters.Values)
                {
                    float stock = StateFormationSystem.EdibleStock(world, center.regionId);
                    foodSum += stock;
                    if (stock <= 0.01f) emptyRegions++;
                    int rid = center.regionId;
                    if (rid >= 0 && rid < regionPop.Length && stock < regionPop[rid] * 0.01f * 7f) hungryRegions++;
                }
                float inv = cities > 0 ? 1f / cities : 0f;
                var topCities = new List<SettlementData>();
                foreach (SettlementData s in world.settlements.Values)
                {
                    if (s.IsRuined || s.settlementCategory == SettlementCategory.Outpost) continue;
                    topCities.Add(s);
                }
                topCities.Sort((a, b) => Mathf.Max(b.population, PopulationLedger.Total(world.tiles[b.tileIndex]))
                    .CompareTo(Mathf.Max(a.population, PopulationLedger.Total(world.tiles[a.tileIndex]))));
                System.Text.StringBuilder topNow = new System.Text.StringBuilder();
                for (int t = 0; t < topCities.Count && t < 3; t++)
                {
                    SettlementData s = topCities[t];
                    TileData tile = world.tiles[s.tileIndex];
                    int rid = tile.regionId;
                    float rp = rid >= 0 && rid < regionPop.Length ? regionPop[rid] : 0f;
                    topNow.Append($"[{s.settlementName} pop{Mathf.Max(s.population, PopulationLedger.Total(tile)):F0} cap{CarryingCapacitySystem.CalculateCarryingCapacity(tile, world.tradeCenters):F0} fert{tile.fertility:F2} stock{StateFormationSystem.EdibleStock(world, rid):F0} rpop{rp:F0}] ");
                }
                TestContext.WriteLine($"TOP{(chunk + 1) * 50} {topNow}");
                TestContext.WriteLine($"AT{(chunk + 1) * 50} cities={cities} outposts={outposts} maxPop={chunkMaxPop:F0} cityPop={cityPopSum:F0} allPop={tilePopSum:F0} food={foodSum:F0} agri={agriSum:F1} eat={tilePopSum * 0.01f:F1} fert={fertSum * inv:F2} dev={devSum * inv:F2} cap={capSum * inv:F0} capOk={capOk} overCap={overCap} famine={famineDelta} hungry={hungryRegions} empty={emptyRegions} regions={world.tradeCenters.Count} realms={world.realms.Count}");
            }
            int total = world.settlements.Count, eligible = 0, popOk = 0, devOk = 0, ageOk = 0, foodOk = 0;
            float maxPop = 0f, maxFood = 0f, maxDev = 0f;
            System.Text.StringBuilder top = new System.Text.StringBuilder();
            int shown = 0;
            foreach (SettlementData s in world.settlements.Values)
            {
                if (s.IsRuined || s.settlementCategory == SettlementCategory.Outpost) continue;
                TileData tile = world.tiles[s.tileIndex];
                float pop = Mathf.Max(s.population, PopulationLedger.Total(tile));
                float food = StateFormationSystem.EdibleStock(world, tile.regionId);
                int age = world.currentYear * 365 + world.currentDay - s.foundingTick;
                maxPop = Mathf.Max(maxPop, pop); maxFood = Mathf.Max(maxFood, food); maxDev = Mathf.Max(maxDev, s.development);
                if (pop >= 800f) popOk++;
                if (s.development >= 15f) devOk++;
                if (age >= 200 * 365) ageOk++;
                if (food >= pop * 0.01f * 180f) foodOk++;
                if (StateFormationSystem.IsEligible(world, s)) eligible++;
                float siteCap = CarryingCapacitySystem.CalculateCarryingCapacity(tile, world.tradeCenters);
                if (shown++ < 8) top.Append($"{s.settlementName}:pop{pop:F0},cap{siteCap:F0},fert{tile.fertility:F2},dev{s.development:F0},age{age / 365},food{food:F0},own{tile.ownerRealmId}; ");
            }
            TestContext.WriteLine($"D220 day={world.currentDay} settlements={total} eligible={eligible} popOk={popOk} devOk={devOk} ageOk={ageOk} foodOk={foodOk} maxPop={maxPop:F0} maxFood={maxFood:F0} maxDev={maxDev:F0} realms={world.realms.Count}");
            TestContext.WriteLine("TOP " + top);
            Assert.Pass();
        }

        [Test]
        [Explicit("河流/城市动力学诊断，默认套件不跑")]
        [Timeout(1800000)]
        public void Diagnose_RiverAndCityDynamics()
        {
            var world = NewWorld(4242, 128, 64);
            world.daysPerTick = 90;
            int initialRivers = 0, initialHigh = 0;
            foreach (TileData tile in world.tiles) { if (!tile.isRiver) continue; initialRivers++; if (tile.riverStreamOrder >= 2) initialHigh++; }
            int m0 = 0, p0 = 0, t0 = 0, a0 = 0, c0 = 0;
            foreach (SettlementData st in world.settlements.Values)
            {
                if (st.IsRuined) continue;
                if (st.hasMarket) m0++;
                if (st.portTier >= PortTier.RiverPort) p0++;
                if (st.tradePower >= SettlementEvolutionSystem.CityTradePowerThreshold) t0++;
                if ((st.primaryFunction & SettlementFunction.Administrative) != 0) a0++;
                if ((st.primaryFunction & SettlementFunction.Commercial) != 0) c0++;
            }
            TestContext.WriteLine($"DIAG0 rivers={initialRivers} order2={initialHigh} settlements={world.settlements.Count} market={m0} port={p0} tradeHigh={t0} admin={a0} commercial={c0}");
            for (int year = 0; year < 500; year++) world.RunDiagnosticDays(world.calendar.daysPerYear);
            int rivers = 0, highOrder = 0, noWater = 0;
            foreach (TileData tile in world.tiles)
            {
                if (!tile.isRiver) continue;
                rivers++;
                if (tile.riverStreamOrder >= 2) highOrder++;
            }
            int maxPressure = 0;
            int cityNoWater = 0;
            foreach (SettlementData settlement in world.settlements.Values)
            {
                if (settlement.IsRuined) continue;
                int support = SettlementEvolutionSystem.CountWaterSupport(world.tiles, world.mapWidth, world.mapHeight, settlement.tileIndex, 1);
                if (support == 0) { noWater++; if (settlement.settlementForm == SettlementForm.City) cityNoWater++; }
                if (settlement.environmentalPressureDays > maxPressure) maxPressure = settlement.environmentalPressureDays;
            }
            TestContext.WriteLine($"DIAG rivers={rivers} order2={highOrder} avulsions={world.RiverAvulsionCount} settlements={world.settlements.Count} noWater={noWater} cityNoWater={cityNoWater} maxPressure={maxPressure} cities={world.CityFoundedCount} collapsed={world.CityCollapsedCount} firstCity={world.FirstCityYear} firstRealm={world.FirstRealmYear}");
            Assert.Pass();
        }

        /// <summary>
        /// P8.1 观测：默认世界以 daysPerTick 批处理长跑，记录第一座城市与第一个政权出现的年份。
        /// 年份是涌现结果，不是门槛或配置常量；断言只要求机制在观测窗口内确实产生了城市与政权。
        /// </summary>
        [Test]
        [Explicit("7000 年观测长跑按需运行，默认套件不跑")]
        [Timeout(3600000)]
        public void NaturalLongRun_ObservesFirstCityAndRealmYears()
        {
            var world = NewWorld(4242, 48, 24);
            world.daysPerTick = 90;
            const int windowYears = 7000;
            while (world.currentYear < windowYears)
            {
                world.RunDiagnosticDays(world.calendar.daysPerYear);
            }

            int cities = 0;
            foreach (SettlementData settlement in world.settlements.Values)
            {
                if (!settlement.IsRuined && settlement.settlementForm == SettlementForm.City) cities++;
            }
            TestContext.WriteLine($"OBS firstCity={world.FirstCityYear} firstRealm={world.FirstRealmYear} worldYear={world.currentYear} settlements={world.settlements.Count} cities={cities} realms={world.realms.Count} founded={world.CityFoundedCount} collapsed={world.CityCollapsedCount} avulsions={world.RiverAvulsionCount} lastCause={world.LastCityDeclineCause}");

            Assert.GreaterOrEqual(world.FirstCityYear, 0, "观测窗口内应出现第一座城市");
            Assert.GreaterOrEqual(world.FirstRealmYear, 0, "观测窗口内应出现第一个政权");
        }

        /// <summary>
        /// 自然长跑：默认世界（无预置政权）1 seed × 220 年，必须出现政权、官职与实物税流水。
        /// Explicit：按需运行，默认套件不跑。
        /// </summary>
        [Test]
        [Explicit("自然世界 220 年长跑按需运行，默认套件不跑")]
        [Timeout(1800000)]
        public void NaturalLongRun_FormsRealmOfficesAndTaxReceipts()
        {
            // 220 年自然世界实体规模大，长跑取 1 个 seed；确定性由独立单测覆盖。
            int[] seeds = { 4242 };
            foreach (int seed in seeds)
            {
                var world = NewWorld(seed, 48, 24);
                bool everFormed = false;
                bool everHadOffice = false;
                bool everHadTaxReceipt = false;
                for (int year = 0; year < 220; year++)
                {
                    world.RunDiagnosticDays(365);
                    if (world.realms.Count > 0) everFormed = true;
                    foreach (RealmData realm in world.realms.Values)
                    {
                        if (realm.officeHolders != null && realm.officeHolders.Count > 0) everHadOffice = true;
                        if (TaxReceiptLedger.GetHistory(realm.realmId, 0).Count > 0) everHadTaxReceipt = true;
                    }
                }

                Assert.IsTrue(everFormed, $"seed {seed}：220 年内应自然形成过政权");
                Assert.IsTrue(everHadOffice, $"seed {seed}：政权形成后应出现官职");
                Assert.IsTrue(everHadTaxReceipt, $"seed {seed}：应有实物税从地方仓储流入国家仓");
            }
        }
    }
}
