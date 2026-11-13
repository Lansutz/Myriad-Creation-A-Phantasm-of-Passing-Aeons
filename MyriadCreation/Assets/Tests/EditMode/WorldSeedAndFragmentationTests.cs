using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;
using TerrainSeaLandGenerator = MyriadCreation.World.Terrain.SeaLandGenerator;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 破碎化生效（seed 驱动噪声）+ world.randomSeed 入档回归（SaveVersion 16，task-37）。
    ///
    /// 旧实现 SeaLandGenerator 构造函数把 _fragmentNoise 初始化为全 0，生产又没有 SetFragmentNoise 调用，
    /// 导致 landFragment/coastFragment 两条分支永远不触发（死路径）；且 randomSeed 不入档，读档后重算恒用旧 seed。
    /// 本测试验证：构造函数按 seed 生成确定性噪声、landFragment&gt;0 时重算结果随 seed 变化、0 时保持关闭语义、
    /// 存档往返后 seed 保留且重算可复现、旧档缺字段回退默认 42 不崩。
    /// </summary>
    public class WorldSeedAndFragmentationTests
    {
        private const int MapWidth = 32;
        private const int MapHeight = 16;
        private const int BaseInputSeed = 424242;
        private const int RecalcSeedA = 12345;
        private const int RecalcSeedB = 999;
        private const string SaveName = "__world_seed_fragmentation_test";

        private static readonly FieldInfo FragmentNoiseField =
            typeof(TerrainSeaLandGenerator).GetField("_fragmentNoise", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
            MapUnitRegistry.Clear();
            CharacterKnowledgeRegistry.Clear();
            RelationRegistry.Clear();
            ContractRegistry.Clear();
            SaveSystem.DeleteSave(SaveName);
        }

        // ===== 工具 =====

        private static WorldConfig CreateConfig(float landFragment, float coastFragment)
        {
            var config = ScriptableObject.CreateInstance<WorldConfig>();
            config.wrapX = false;
            config.wrapY = false;
            config.seaLevel = 0.5f;
            config.landAmount = 0.5f;
            config.landFragment = landFragment;
            config.coastFragment = coastFragment;
            config.oceanBuffer = 0.35f;
            return config;
        }

        private static TileData[] CreateBaseTiles()
        {
            var rng = new System.Random(BaseInputSeed);
            var tiles = new TileData[MapWidth * MapHeight];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new TileData
                {
                    tileIndex = i,
                    exists = true,
                    isLand = false,
                    elevation01 = (float)rng.NextDouble(),
                    oceanTier = GameEnums.OceanTier.None,
                    seaConnectId = -1,
                    waterAdjacentWeight = 0f
                };
            }
            return tiles;
        }

        /// <summary>深拷贝地块（TileData 是 struct，数组 Clone 为浅拷贝；重算会就地改字段，必须逐项复制）。</summary>
        private static TileData[] CloneTiles(TileData[] source)
        {
            var copy = new TileData[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                TileData s = source[i];
                copy[i] = new TileData
                {
                    tileIndex = s.tileIndex,
                    exists = s.exists,
                    isLand = s.isLand,
                    elevation01 = s.elevation01,
                    isCoast = s.isCoast,
                    oceanTier = s.oceanTier,
                    oceanDepth01 = s.oceanDepth01,
                    waterAdjacentWeight = s.waterAdjacentWeight,
                    seaConnectId = s.seaConnectId,
                    slopeDegree = s.slopeDegree,
                    annualTemp = s.annualTemp,
                    annualPrecipMm = s.annualPrecipMm,
                    airHumidityPct = s.airHumidityPct,
                    soilHumidityPct = s.soilHumidityPct,
                    diurnalTempRange = s.diurnalTempRange,
                    accumulatedTemp = s.accumulatedTemp,
                    frostFreeDays = s.frostFreeDays,
                    climateZone = s.climateZone,
                    biome = s.biome,
                    fertility = s.fertility,
                    movementCost = s.movementCost,
                    isRiver = s.isRiver
                };
            }
            return copy;
        }

        private static float[] ReadFragmentNoise(TerrainSeaLandGenerator generator)
        {
            Assert.IsNotNull(generator, "海陆生成器不应为 null");
            Assert.IsNotNull(FragmentNoiseField,
                "找不到 SeaLandGenerator._fragmentNoise（反射契约变化，需同步测试）");
            return (float[])FragmentNoiseField.GetValue(generator);
        }

        /// <summary>在独立地块副本上按 seed 构造生成器并全量重算（不注入噪声 → 用构造函数 seed 噪声）。</summary>
        private static TileData[] RecalculateForSeed(TileData[] baseTiles, int seed, float landFragment, float coastFragment)
        {
            TileData[] tiles = CloneTiles(baseTiles);
            WorldConfig config = CreateConfig(landFragment, coastFragment);
            try
            {
                var generator = new TerrainSeaLandGenerator(config, tiles, MapWidth, MapHeight, seed);
                generator.RecalculateAll();
                return tiles;
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        private static int CountLandDifferences(TileData[] a, TileData[] b)
        {
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].isLand != b[i].isLand) diff++;
            }
            return diff;
        }

        private static bool ContainsNonZero(float[] values, float threshold)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] > threshold) return true;
            }
            return false;
        }

        private GameWorld NewWorld(int width, int height)
        {
            var go = new GameObject("WorldSeedTestWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = width;
            world.mapHeight = height;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            return world;
        }

        // ===== 1) 构造函数噪声：同 seed 一致、异 seed 不同、不再全 0 =====

        [Test]
        public void Constructor_SameSeed_ProducesIdenticalFragmentNoise()
        {
            var config = CreateConfig(0.4f, 0.3f);
            try
            {
                var first = new TerrainSeaLandGenerator(config, CreateBaseTiles(), MapWidth, MapHeight, RecalcSeedA);
                var second = new TerrainSeaLandGenerator(config, CreateBaseTiles(), MapWidth, MapHeight, RecalcSeedA);
                float[] noiseA = ReadFragmentNoise(first);
                float[] noiseB = ReadFragmentNoise(second);

                Assert.AreEqual(MapWidth * MapHeight, noiseA.Length, "破碎噪声数组长度应等于地块数");
                Assert.AreEqual(noiseA.Length, noiseB.Length, "同参数构造的噪声长度应一致");
                for (int i = 0; i < noiseA.Length; i++)
                {
                    Assert.AreEqual(noiseA[i], noiseB[i], 0f,
                        "同一 seed 构造的破碎噪声应逐项一致（i=" + i + "）");
                }
                Assert.IsTrue(ContainsNonZero(noiseA, 0.001f),
                    "构造函数应填入非零破碎噪声，否则 landFragment/coastFragment 仍是死路径");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Constructor_DifferentSeeds_ProduceDifferentFragmentNoise()
        {
            var config = CreateConfig(0.4f, 0.3f);
            try
            {
                var first = new TerrainSeaLandGenerator(config, CreateBaseTiles(), MapWidth, MapHeight, RecalcSeedA);
                var second = new TerrainSeaLandGenerator(config, CreateBaseTiles(), MapWidth, MapHeight, RecalcSeedB);
                float[] noiseA = ReadFragmentNoise(first);
                float[] noiseB = ReadFragmentNoise(second);

                int diff = 0;
                for (int i = 0; i < noiseA.Length; i++)
                {
                    if (Mathf.Abs(noiseA[i] - noiseB[i]) > 1e-6f) diff++;
                }
                Assert.Greater(diff, 0, "不同 seed 构造的破碎噪声应不同（噪声必须由 seed 驱动）");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        // ===== 2) 破碎化真发生：landFragment>0 时重算结果随 seed 不同；=0 时保持关闭语义 =====

        [Test]
        public void RecalculateAll_LandFragmentEnabled_ResultDependsOnSeed()
        {
            TileData[] baseTiles = CreateBaseTiles();

            TileData[] withSeedA = RecalculateForSeed(baseTiles, RecalcSeedA, landFragment: 1f, coastFragment: 1f);
            TileData[] withSeedB = RecalculateForSeed(baseTiles, RecalcSeedB, landFragment: 1f, coastFragment: 1f);

            Assert.Greater(CountLandDifferences(baseTiles, withSeedA), 0,
                "全量重算应实际改写海陆，否则断言无意义");
            Assert.Greater(CountLandDifferences(withSeedA, withSeedB), 0,
                "landFragment>0 时不同 seed 的重算海陆结果必须不同（证明破碎化不再是死路径）");
        }

        [Test]
        public void RecalculateAll_FragmentationDisabled_IsSeedIndependent()
        {
            TileData[] baseTiles = CreateBaseTiles();

            TileData[] withSeedA = RecalculateForSeed(baseTiles, RecalcSeedA, landFragment: 0f, coastFragment: 0f);
            TileData[] withSeedB = RecalculateForSeed(baseTiles, RecalcSeedB, landFragment: 0f, coastFragment: 0f);

            Assert.AreEqual(0, CountLandDifferences(withSeedA, withSeedB),
                "landFragment/coastFragment=0 时应保持既有语义：破碎化不参与，异 seed 海陆结果一致");
        }

        // ===== 3) SaveVersion 16：randomSeed 存档往返 + 同 seed 重算可复现 =====

        [Test]
        public void SaveLoad_PreservesRandomSeed_AndRecalcIsReproducible()
        {
            const int worldSeed = 20251101;
            var world = NewWorld(MapWidth, MapHeight);
            world.randomSeed = worldSeed;
            world.GenerateTerrain(worldSeed);
            Assert.AreEqual(worldSeed, world.randomSeed, "前置：GenerateTerrain 后 world.randomSeed 应为生成 seed");

            // 用可由 JSON 精确往返的二进制小数做输入，避免 float 序列化精度差异影响“同 seed 重算一致”判定。
            var rng = new System.Random(BaseInputSeed);
            for (int i = 0; i < world.tiles.Length; i++)
            {
                TileData tile = world.tiles[i];
                tile.exists = true;
                tile.isLand = false;
                tile.elevation01 = (float)rng.Next(129) / 128f;
                tile.isCoast = false;
                tile.oceanTier = GameEnums.OceanTier.None;
                tile.oceanDepth01 = 0f;
                tile.waterAdjacentWeight = 0f;
                tile.seaConnectId = -1;
                world.tiles[i] = tile;
            }
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "前置：存盘应成功");

            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "读档应成功");
            _objects.Add(loaded.gameObject);
            Assert.AreEqual(worldSeed, loaded.randomSeed, "读档后 world.randomSeed 必须保留");

            // 地块高程必须精确往返（否则下面的重算一致性断言会受序列化精度影响）
            Assert.AreEqual(world.tiles.Length, loaded.tiles.Length, "读档前后地块数应一致");
            for (int i = 0; i < world.tiles.Length; i++)
            {
                Assert.AreEqual(world.tiles[i].elevation01, loaded.tiles[i].elevation01, 0f,
                    "地块高程应精确往返（i=" + i + "）");
            }

            // 同 seed + 同地块输入 → 破碎噪声与全量重算海陆结果都应一致
            world.RecalculateAll();
            loaded.RecalculateAll();

            float[] originalNoise = ReadFragmentNoise(world.GetSeaLandGenerator());
            float[] loadedNoise = ReadFragmentNoise(loaded.GetSeaLandGenerator());
            Assert.AreEqual(originalNoise.Length, loadedNoise.Length, "读档前后海陆生成器噪声长度应一致");
            for (int i = 0; i < originalNoise.Length; i++)
            {
                Assert.AreEqual(originalNoise[i], loadedNoise[i], 0f,
                    "同 seed 下读档前后破碎噪声应逐项一致（i=" + i + "）");
            }
            Assert.AreEqual(0, CountLandDifferences(world.tiles, loaded.tiles),
                "读档前后同 seed 全量重算的海陆结果应一致");
        }

        [Test]
        public void LoadGame_LegacySaveWithoutRandomSeed_FallsBackToDefault()
        {
            var world = NewWorld(24, 12);
            world.GenerateTerrain(8888);
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "前置：存盘应成功");

            // 模拟 SaveVersion 15 旧档：randomSeed 字段缺省（JsonUtility 对缺失 int 反序列化为 0）。
            string path = Path.Combine(SaveSystem.GetSaveDirectory(), SaveName + ".sav");
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            Assert.IsNotNull(data, "前置：存档应可解析");
            data.randomSeed = 0;
            data.version = GameConstants.SaveVersion - 1;
            File.WriteAllText(path, JsonUtility.ToJson(data, true));

            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "旧档缺 randomSeed 字段不应读档失败");
            _objects.Add(loaded.gameObject);
            Assert.AreEqual(42, loaded.randomSeed, "旧档 randomSeed 缺省 0 → 回退默认世界 seed 42");
        }

        // ===== 4) 初始开图即应用破碎化（task-42）：GameWorld.GenerateTerrain 生成段接入 SeaLand 全量重算 =====

        private const int WorldGenSeedA = 20251101;
        private const int WorldGenSeedB = 20251102;

        /// <summary>
        /// 走生产主路径 GameWorld.GenerateTerrain(seed) 开图，返回地形快照（深拷贝）后立刻销毁世界。
        /// 每个世界 InitializeWorld 都会清理全局注册表，避免两个快照互相污染。
        /// </summary>
        private static TileData[] GenerateTerrainSnapshot(int width, int height, int seed,
            System.Action<GameWorld> configureBeforeGenerate = null)
        {
            var go = new GameObject("WorldGenSnapshotWorld");
            try
            {
                var world = go.AddComponent<GameWorld>();
                world.mapWidth = width;
                world.mapHeight = height;
                world.SetupOptions = WorldSetupOptions.CreateStandard();
                world.InitializeWorld();
                configureBeforeGenerate?.Invoke(world);
                world.GenerateTerrain(seed);
                return CloneTiles(world.tiles);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static int CountTerrainDifferences(TileData[] a, TileData[] b)
        {
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                TileData x = a[i];
                TileData y = b[i];
                if (x.exists != y.exists
                    || x.isLand != y.isLand
                    || x.isCoast != y.isCoast
                    || x.oceanTier != y.oceanTier
                    || x.elevation01 != y.elevation01
                    || x.oceanDepth01 != y.oceanDepth01
                    || x.waterAdjacentWeight != y.waterAdjacentWeight
                    || x.seaConnectId != y.seaConnectId)
                {
                    diff++;
                }
            }
            return diff;
        }

        [Test]
        public void GenerateTerrain_DifferentSeeds_ProduceDifferentLandMaps()
        {
            TileData[] withSeedA = GenerateTerrainSnapshot(24, 12, WorldGenSeedA);
            TileData[] withSeedB = GenerateTerrainSnapshot(24, 12, WorldGenSeedB);

            Assert.Greater(CountLandDifferences(withSeedA, withSeedB), 0,
                "默认配置下不同 seed 开图的海陆分布应不同（初始生成也由世界 seed 驱动）");
        }

        [Test]
        public void GenerateTerrain_SameSeed_IsDeterministic()
        {
            TileData[] first = GenerateTerrainSnapshot(24, 12, WorldGenSeedA);
            TileData[] second = GenerateTerrainSnapshot(24, 12, WorldGenSeedA);

            Assert.AreEqual(0, CountTerrainDifferences(first, second),
                "同一 seed 两次开图的地形（海陆/高程/海岸/海洋层级/海域连通）应逐地块一致");
        }

        [Test]
        public void GenerateTerrain_DefaultConfig_AppliesFragmentation()
        {
            const int width = 64;
            const int height = 32;
            TileData[] withFragmentation = GenerateTerrainSnapshot(width, height, WorldGenSeedA);
            TileData[] withoutFragmentation = GenerateTerrainSnapshot(width, height, WorldGenSeedA,
                world =>
                {
                    world.config.landFragment = 0f;
                    world.config.coastFragment = 0f;
                });

            Assert.Greater(CountLandDifferences(withFragmentation, withoutFragmentation), 0,
                "默认配置（landFragment=0.4/coastFragment=0.3）开图应真实应用破碎化：与关闭破碎的地形必须不同");
        }

        [Test]
        public void SaveLoad_LoadedTerrain_MatchesSavedPerTile()
        {
            var world = NewWorld(MapWidth, MapHeight);
            world.GenerateTerrain(WorldGenSeedA);
            Assert.Greater(world.GetLandTileCount(), 0, "前置：开图应有陆地");
            TileData[] savedTiles = CloneTiles(world.tiles);
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "前置：存盘应成功");

            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "读档应成功");
            _objects.Add(loaded.gameObject);
            Assert.AreEqual(savedTiles.Length, loaded.tiles.Length, "读档前后地块数应一致");

            int landDiff = 0;
            int coastDiff = 0;
            int oceanTierDiff = 0;
            int seaConnectDiff = 0;
            int elevationDiff = 0;
            for (int i = 0; i < savedTiles.Length; i++)
            {
                if (savedTiles[i].isLand != loaded.tiles[i].isLand) landDiff++;
                if (savedTiles[i].isCoast != loaded.tiles[i].isCoast) coastDiff++;
                if (savedTiles[i].oceanTier != loaded.tiles[i].oceanTier) oceanTierDiff++;
                if (savedTiles[i].seaConnectId != loaded.tiles[i].seaConnectId) seaConnectDiff++;
                if (Mathf.Abs(savedTiles[i].elevation01 - loaded.tiles[i].elevation01) > 1e-4f) elevationDiff++;
            }

            Assert.AreEqual(0, landDiff, "读档不应二次破碎化：海陆应逐地块一致");
            Assert.AreEqual(0, coastDiff, "读档不应改写海岸标记（二次重算会翻转）");
            Assert.AreEqual(0, oceanTierDiff, "读档不应改写海洋层级");
            Assert.AreEqual(0, seaConnectDiff, "读档不应重算海域连通");
            Assert.AreEqual(0, elevationDiff, "高程应在序列化精度内一致（大偏移说明读档触发了二次重算）");
            Assert.AreEqual(world.GetLandTileCount(), loaded.GetLandTileCount(), "读档前后陆地数应一致");
        }

        // ===== 5) 首个 Daily tick 不二次改写初始地形（task-49） =====

        [Test]
        public void GenerateTerrain_ThenDirtyRecalc_DoesNotRewriteTerrain()
        {
            var world = NewWorld(MapWidth, MapHeight);
            world.GenerateTerrain(WorldGenSeedA);

            Assert.IsFalse(world.SimulationScheduler.Dirty.IsDirty(WorldMapSchedule.DirtyKey),
                "生成完成后应清掉调度器 world.terrain dirty key，否则首个 Daily tick 会再触发一次全表重算");
            TileData[] generated = CloneTiles(world.tiles);

            // 等价于首个 Daily tick：WorldMapSchedule → WorldMapSimulationSystem.RecalculateDirty()
            world.RecalculateDirty();

            Assert.AreEqual(0, CountTerrainDifferences(generated, world.tiles),
                "生成后走 dirty 重算路径不应二次改写地形（修复前：全表 terrain dirty + 已设置的 _cachedSeaThreshold 会把高程/海陆再算一遍）");
        }

        [Test]
        public void PaintTerrain_ThenDirtyRecalc_OnlyRewritesMarkedNeighbourhood()
        {
            var world = NewWorld(MapWidth, MapHeight);
            world.GenerateTerrain(WorldGenSeedA);

            // 用平整的高地合成地形做 fixture：避免生成后"相对化高程再减阈值"导致邻格 isLand 翻转，
            // 从而触发全局海域连通重算（那是 dirty 路径的另一既有行为，与本回归关注点无关）。
            for (int i = 0; i < world.tiles.Length; i++)
            {
                TileData tile = world.tiles[i];
                tile.exists = true;
                tile.isLand = true;
                tile.elevation01 = 0.9f;
                tile.isCoast = false;
                tile.oceanTier = GameEnums.OceanTier.Land;
                tile.oceanDepth01 = 0f;
                tile.waterAdjacentWeight = 0f;
                tile.seaConnectId = -1;
                world.tiles[i] = tile;
            }

            int centre = (MapHeight / 2) * MapWidth + MapWidth / 2;
            var expectedAffected = new HashSet<int> { centre };
            // WMS.MarkTileDirty 带一环邻格，SeaLandGenerator.RecalculateDirty 再带一环 → 期望影响 2 环内。
            ExpandNeighbourhood(world, expectedAffected);
            ExpandNeighbourhood(world, expectedAffected);

            // 地图编辑器路径：PaintTerrain 改高程并重新标脏
            world.PaintTerrain(centre, 0.95f);
            Assert.IsTrue(world.SimulationScheduler.Dirty.IsDirty(WorldMapSchedule.DirtyKey),
                "PaintTerrain 之后必须重新标脏 world.terrain（编辑器重算路径不能被初始生成清理破坏）");
            TileData[] beforeRecalc = CloneTiles(world.tiles);

            world.RecalculateDirty();

            int changedInside = 0;
            for (int i = 0; i < beforeRecalc.Length; i++)
            {
                if (expectedAffected.Contains(i) && TerrainTileDiffers(beforeRecalc[i], world.tiles[i]))
                {
                    changedInside++;
                }
            }
            Assert.Greater(changedInside, 0, "标脏地块及其邻域在 dirty 重算后应发生实际变化（dirty 机制仍生效）");
            Assert.AreEqual(0, CountTerrainDifferencesOutside(beforeRecalc, world.tiles, expectedAffected),
                "dirty 重算只应影响被标脏地块的邻域，其它地块必须逐项不变");
        }

        // ===== 6) 破碎化 changed 地块的派生气候补算（task-55） =====

        [Test]
        public void GenerateTerrain_FragmentationChangedTiles_RefreshAirHumidityAndClimateZone()
        {
            const int width = 64;
            const int height = 32;
            var go = new GameObject("FragmentationHumidityWorld");
            try
            {
                var world = go.AddComponent<GameWorld>();
                world.mapWidth = width;
                world.mapHeight = height;
                world.SetupOptions = WorldSetupOptions.CreateStandard();
                world.InitializeWorld();
                world.GenerateTerrain(WorldGenSeedA);

                var recomputed = new List<int>(world.LastFragmentationRecomputedTiles);
                Assert.Greater(recomputed.Count, 0,
                    "前置：默认破碎化应至少补算一个地块的派生气候，才能验证湿度/气候带重算");

                int humidityMismatch = 0;
                int climateZoneMismatch = 0;
                foreach (int i in recomputed)
                {
                    TileData tile = world.tiles[i];
                    float expectedHumidity = Mathf.Clamp01(tile.annualPrecipMm / 2000f);
                    if (Mathf.Abs(tile.airHumidityPct - expectedHumidity) > 1e-4f)
                    {
                        humidityMismatch++;
                    }
                    GameEnums.ClimateZone expectedZone =
                        MyriadCreation.World.Terrain.PlanetTerrainGenerator.ClassifyClimateZone(tile.annualTemp, tile.annualPrecipMm);
                    if (tile.climateZone != expectedZone)
                    {
                        climateZoneMismatch++;
                    }
                }

                Assert.AreEqual(0, humidityMismatch,
                    "破碎化补算地块的 airHumidityPct 必须按新 annualPrecipMm 同口径重算（task-55）");
                Assert.AreEqual(0, climateZoneMismatch,
                    "破碎化补算地块的 climateZone 必须按新温度/降水重算（task-55）");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
        [Test]
        public void RecalculateHydrology_ThenDirtyRecalc_DoesNotRewriteTerrain()
        {
            var world = NewWorld(MapWidth, MapHeight);
            world.GenerateTerrain(WorldGenSeedA);
            world.RecalculateHydrology();

            Assert.IsFalse(world.SimulationScheduler.Dirty.IsDirty(WorldMapSchedule.DirtyKey),
                "水文重算后应直接全量重算并清掉 terrain dirty（task-55），否则首个 Daily tick 会二次改写地形");
            TileData[] afterHydrology = CloneTiles(world.tiles);
            world.RecalculateDirty();
            Assert.AreEqual(0, CountTerrainDifferences(afterHydrology, world.tiles),
                "水文重算后的 dirty 重算路径不应再改写地形（terrain dirty 已清空）");
        }

        private static void ExpandNeighbourhood(GameWorld world, HashSet<int> indices)
        {
            var expanded = new HashSet<int>(indices);
            TerrainSeaLandGenerator generator = world.GetSeaLandGenerator();
            foreach (int index in indices)
            {
                foreach (int neighbour in generator.GetNeighbourIndices(index))
                {
                    expanded.Add(neighbour);
                }
            }
            indices.Clear();
            indices.UnionWith(expanded);
        }

        private static bool TerrainTileDiffers(TileData x, TileData y)
        {
            return x.exists != y.exists
                || x.isLand != y.isLand
                || x.isCoast != y.isCoast
                || x.oceanTier != y.oceanTier
                || x.elevation01 != y.elevation01
                || x.oceanDepth01 != y.oceanDepth01
                || x.waterAdjacentWeight != y.waterAdjacentWeight
                || x.seaConnectId != y.seaConnectId;
        }

        private static int CountTerrainDifferencesOutside(TileData[] before, TileData[] after, HashSet<int> allowed)
        {
            int diff = 0;
            for (int i = 0; i < before.Length; i++)
            {
                if (!allowed.Contains(i) && TerrainTileDiffers(before[i], after[i]))
                {
                    diff++;
                }
            }
            return diff;
        }
    }
}
