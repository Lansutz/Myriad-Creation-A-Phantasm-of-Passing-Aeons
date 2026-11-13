using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Biome;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 破碎化后派生属性一致性回归（task-51 / verifier P1-2）：初始开图 ApplyInitialSeaLandFragmentation 会改写
    /// isLand/isCoast/elevation01，受影响地块的 annualTemp/annualPrecipMm/biome/fertility 必须按生成公式补算
    /// （海地块 fertility=0、新陆地不再残留旧海洋群系）；同 seed 确定；读档路径不重算、沿用存档值。
    /// </summary>
    public class FragmentationClimateConsistencyTests
    {
        private const int MapWidth = 96;
        private const int MapHeight = 48;
        private const int WorldSeed = 20251101;
        private const float DefaultLandFragment = 0.4f;
        private const float DefaultCoastFragment = 0.3f;
        private const string SaveName = "__fragmentation_climate_test";

        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<WorldConfig> _configs = new List<WorldConfig>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _objects.Clear();
            foreach (WorldConfig config in _configs)
            {
                if (config != null)
                {
                    Object.DestroyImmediate(config);
                }
            }
            _configs.Clear();
            MapUnitRegistry.Clear();
            CharacterKnowledgeRegistry.Clear();
            RelationRegistry.Clear();
            ContractRegistry.Clear();
            SaveSystem.DeleteSave(SaveName);
        }

        // ===== 工具 =====

        /// <summary>走生产主路径开图：InitializeWorld（GeographyOnly，避免省份/聚落之外的开销）→ GenerateTerrain。</summary>
        private GameWorld CreateGeneratedWorld(float landFragment, float coastFragment, int seed)
        {
            GameObject go = new GameObject("FragmentationClimateWorld");
            _objects.Add(go);
            GameWorld world = go.AddComponent<GameWorld>();
            world.mapWidth = MapWidth;
            world.mapHeight = MapHeight;
            world.randomSeed = seed;
            world.SetupOptions = WorldSetupOptions.Create(InitialContentMode.GeographyOnly);

            WorldConfig config = WorldConfig.CreateRuntimeInstance();
            config.wrapX = false;
            config.wrapY = false;
            config.landFragment = landFragment;
            config.coastFragment = coastFragment;
            _configs.Add(config);
            world.config = config;

            world.InitializeWorld();
            _configs.Add(world.config);
            world.GenerateTerrain(seed);
            return world;
        }

        /// <summary>与 PlanetTerrainGenerator 生成期一致的 |lat|；供测试按当前字段独立重算 biome。</summary>
        private static float GetLatAbs(int index, int width, int height)
        {
            int y = index / width;
            float latitude = 90f - (float)y / (float)height * 180f;
            return Mathf.Abs(latitude);
        }

        /// <summary>
        /// 破碎化实际改动地块：两世界（启用 vs 关闭破碎化）中 isLand 翻转或高程变化 &gt; 0.01 者。
        /// 不把 isCoast 差异计入：关闭破碎化世界不会跑破碎化收尾的邻接重算，isCoast 对比会混入实现细节噪声；
        /// isLand/高程差异已精确对应破碎化改写的地块。
        /// </summary>
        private static List<int> CollectChangedTiles(GameWorld enabled, GameWorld disabled)
        {
            var changed = new List<int>();
            for (int i = 0; i < enabled.tiles.Length; i++)
            {
                TileData a = enabled.tiles[i];
                TileData b = disabled.tiles[i];
                if (!a.exists)
                {
                    continue;
                }
                if (a.isLand != b.isLand
                    || Mathf.Abs(a.elevation01 - b.elevation01) > 0.01f)
                {
                    changed.Add(i);
                }
            }
            return changed;
        }

        // ===== 1) 破碎化 affected 地块：biome/fertility 按当前字段一致，海地 fertility=0 =====

        [Test]
        public void FragmentationChangedTiles_HaveConsistentDerivedClimate()
        {
            GameWorld enabled = CreateGeneratedWorld(DefaultLandFragment, DefaultCoastFragment, WorldSeed);
            GameWorld disabled = CreateGeneratedWorld(0f, 0f, WorldSeed);

            List<int> changed = CollectChangedTiles(enabled, disabled);
            Assert.Greater(changed.Count, 0,
                "前置：默认破碎化应至少改写一个地块（否则一致性断言无意义）");

            int newLandCount = 0;
            foreach (int index in changed)
            {
                TileData tile = enabled.tiles[index];
                float latAbs = GetLatAbs(index, enabled.mapWidth, enabled.mapHeight);
                GameEnums.BiomeType expectedBiome = HoldridgeBiomeClassifier.Classify(
                    tile.annualTemp, tile.annualPrecipMm, tile.elevation01, tile.isLand, tile.isCoast,
                    tile.isRiver, tile.slopeDegree, latAbs);
                Assert.AreEqual(expectedBiome, tile.biome,
                    $"破碎化 changed 地块 #{index} 的 biome 必须与按当前字段重新 Classify 的结果一致（旧实现会残留破碎前/海洋群系）");

                if (!tile.isLand)
                {
                    Assert.AreEqual(0f, tile.fertility, 0f,
                        $"changed 海地块 #{index} 的 fertility 必须为 0");
                    continue;
                }

                float expectedFertility = HoldridgeBiomeClassifier.CalculateFertility(
                    tile.annualTemp, tile.annualPrecipMm, tile.elevation01, tile.slopeDegree, tile.biome);
                Assert.AreEqual(expectedFertility, tile.fertility, 1e-5f,
                    $"changed 陆地地块 #{index} 的 fertility 必须按当前字段重算");

                if (!disabled.tiles[index].isLand)
                {
                    newLandCount++;
                    Assert.AreNotEqual(GameEnums.BiomeType.EndorheicLake, tile.biome,
                        $"新陆地 #{index} 不应保留旧海洋（EndorheicLake）群系");
                }
            }

            Assert.Greater(newLandCount, 0,
                "前置：本 seed 应至少有 1 个 changed 地块由海变陆，用于验证旧海洋群系已被替换");
        }

        // ===== 2) 同 seed 确定性 =====

        [Test]
        public void GeneratedWorld_SameSeed_DerivedClimateIsDeterministic()
        {
            GameWorld first = CreateGeneratedWorld(DefaultLandFragment, DefaultCoastFragment, WorldSeed);
            GameWorld second = CreateGeneratedWorld(DefaultLandFragment, DefaultCoastFragment, WorldSeed);

            for (int i = 0; i < first.tiles.Length; i++)
            {
                TileData a = first.tiles[i];
                TileData b = second.tiles[i];
                Assert.AreEqual(a.exists, b.exists, $"地块 #{i} exists 应一致");
                Assert.AreEqual(a.isLand, b.isLand, $"地块 #{i} isLand 应一致");
                Assert.AreEqual(a.elevation01, b.elevation01, 0f, $"地块 #{i} elevation01 应一致");
                Assert.AreEqual(a.annualTemp, b.annualTemp, 0f, $"地块 #{i} annualTemp 应一致");
                Assert.AreEqual(a.annualPrecipMm, b.annualPrecipMm, 0f, $"地块 #{i} annualPrecipMm 应一致");
                Assert.AreEqual(a.biome, b.biome, $"地块 #{i} biome 应一致");
                Assert.AreEqual(a.fertility, b.fertility, 0f, $"地块 #{i} fertility 应一致");
            }
        }

        // ===== 3) 读档不重算：沿用存档值（含 sentinel 否定性前置） =====

        [Test]
        public void LoadedWorld_KeepsSavedDerivedClimate_WithoutRecompute()
        {
            GameWorld world = CreateGeneratedWorld(DefaultLandFragment, DefaultCoastFragment, WorldSeed);
            int tileCount = world.tiles.Length;
            float[] temps = new float[tileCount];
            float[] precips = new float[tileCount];
            GameEnums.BiomeType[] biomes = new GameEnums.BiomeType[tileCount];
            float[] fertilities = new float[tileCount];
            for (int i = 0; i < tileCount; i++)
            {
                temps[i] = world.tiles[i].annualTemp;
                precips[i] = world.tiles[i].annualPrecipMm;
                biomes[i] = world.tiles[i].biome;
                fertilities[i] = world.tiles[i].fertility;
            }

            int sentinel = -1;
            for (int i = 0; i < tileCount; i++)
            {
                if (world.tiles[i].exists && world.tiles[i].isLand)
                {
                    sentinel = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(sentinel, 0, "前置：生成世界应至少有一个陆地地块");
            TileData sentinelTile = world.tiles[sentinel];
            sentinelTile.annualTemp = 1234.5f;
            sentinelTile.annualPrecipMm = 777.5f;
            sentinelTile.biome = GameEnums.BiomeType.TemperateGrassland;
            sentinelTile.fertility = 0.5f;
            world.tiles[sentinel] = sentinelTile;

            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "前置：存盘应成功");
            GameWorld loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "读档应成功");
            _objects.Add(loaded.gameObject);

            // 否定性前置：sentinel 若被读档路径重算，会被生成公式覆盖为正常值。
            Assert.AreEqual(1234.5f, loaded.tiles[sentinel].annualTemp, 0f,
                "读档不得重算 annualTemp（应沿用存档值）");
            Assert.AreEqual(777.5f, loaded.tiles[sentinel].annualPrecipMm, 0f,
                "读档不得重算 annualPrecipMm（应沿用存档值）");
            Assert.AreEqual(GameEnums.BiomeType.TemperateGrassland, loaded.tiles[sentinel].biome,
                "读档不得重算 biome（应沿用存档值）");
            Assert.AreEqual(0.5f, loaded.tiles[sentinel].fertility, 0f,
                "读档不得重算 fertility（应沿用存档值）");

            for (int i = 0; i < tileCount; i++)
            {
                if (i == sentinel)
                {
                    continue;
                }
                Assert.AreEqual(temps[i], loaded.tiles[i].annualTemp, 1e-4f,
                    $"地块 #{i} 读档后 annualTemp 应沿用存档值（不得重算）");
                Assert.AreEqual(precips[i], loaded.tiles[i].annualPrecipMm, 1e-4f,
                    $"地块 #{i} 读档后 annualPrecipMm 应沿用存档值（不得重算）");
                Assert.AreEqual(biomes[i], loaded.tiles[i].biome,
                    $"地块 #{i} 读档后 biome 应沿用存档值（不得重算）");
                Assert.AreEqual(fertilities[i], loaded.tiles[i].fertility, 1e-4f,
                    $"地块 #{i} 读档后 fertility 应沿用存档值（不得重算）");
            }
        }
    }
}
