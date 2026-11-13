using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;
using UnityEngine;
using TerrainSeaLandGenerator = MyriadCreation.World.Terrain.SeaLandGenerator;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// SeaLandGenerator 重算路径确定性回归（主测生产在用副本 MyriadCreation.World.Terrain.SeaLandGenerator）：
    /// 旧实现 RecalculateAll 的破碎化分支用未播种的 UnityEngine.Random，同一 seed 全量重算不可复现；
    /// 改为按 seed 派生的确定性散列后，同 seed 必同、异 seed 应异，且不污染 UnityEngine.Random 全局状态。
    /// </summary>
    public class SeaLandRecalcDeterminismTests
    {
        internal const int MapWidth = 32;
        internal const int MapHeight = 16;
        internal const int BaseInputSeed = 424242;
        internal const int RecalcSeedA = 12345;
        internal const int RecalcSeedB = 999;

        internal static WorldConfig CreateConfig()
        {
            var config = ScriptableObject.CreateInstance<WorldConfig>();
            config.wrapX = false;
            config.wrapY = false;
            config.seaLevel = 0.5f;
            config.landAmount = 0.5f;
            config.landFragment = 1f;
            config.coastFragment = 1f;
            config.oceanBuffer = 0.35f;
            return config;
        }

        internal static TileData[] CreateBaseTiles()
        {
            var input = new System.Random(BaseInputSeed);
            var tiles = new TileData[MapWidth * MapHeight];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new TileData
                {
                    exists = true,
                    isLand = false,
                    elevation01 = (float)input.NextDouble(),
                    oceanTier = GameEnums.OceanTier.None,
                    seaConnectId = -1,
                    waterAdjacentWeight = 0f
                };
            }
            return tiles;
        }

        /// <summary>破碎噪声取 1，最大化 landFragment/coastFragment 的触发概率，让 RNG 差异可观测。</summary>
        internal static float[] CreateFragmentNoise()
        {
            var noise = new float[MapWidth * MapHeight];
            for (int i = 0; i < noise.Length; i++)
            {
                noise[i] = 1f;
            }
            return noise;
        }

        /// <summary>
        /// 在全新地块副本上跑一次 RecalculateAll；seed 为 null 时走既有 4 参构造（默认 seed）。
        /// </summary>
        internal static TileData[] Recalculate(TileData[] baseTiles, float[] fragmentNoise, int? seed)
        {
            TileData[] tiles = (TileData[])baseTiles.Clone();
            WorldConfig config = CreateConfig();
            try
            {
                TerrainSeaLandGenerator terrain = seed.HasValue
                    ? new TerrainSeaLandGenerator(config, tiles, MapWidth, MapHeight, seed.Value)
                    : new TerrainSeaLandGenerator(config, tiles, MapWidth, MapHeight);
                terrain.SetFragmentNoise(fragmentNoise);
                terrain.RecalculateAll();
                return tiles;
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        internal static int CountTileDifferences(TileData[] a, TileData[] b)
        {
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].isLand != b[i].isLand
                    || a[i].elevation01 != b[i].elevation01
                    || a[i].isCoast != b[i].isCoast
                    || a[i].oceanTier != b[i].oceanTier
                    || a[i].oceanDepth01 != b[i].oceanDepth01
                    || a[i].waterAdjacentWeight != b[i].waterAdjacentWeight
                    || a[i].seaConnectId != b[i].seaConnectId)
                {
                    diff++;
                }
            }
            return diff;
        }

        [Test]
        public void RecalculateAll_SameSeed_IsDeterministic()
        {
            TileData[] baseTiles = CreateBaseTiles();
            float[] noise = CreateFragmentNoise();

            TileData[] first = Recalculate(baseTiles, noise, RecalcSeedA);
            TileData[] second = Recalculate(baseTiles, noise, RecalcSeedA);

            Assert.AreEqual(0, CountTileDifferences(first, second),
                "同一 seed 两次全量重算的 isLand/elevation01/海岸/海域连通必须完全一致");
            Assert.Greater(CountTileDifferences(baseTiles, first), 0,
                "重算应实际改写地块，否则确定性断言无意义");
        }

        [Test]
        public void RecalculateAll_DifferentSeeds_Differ()
        {
            TileData[] baseTiles = CreateBaseTiles();
            float[] noise = CreateFragmentNoise();

            TileData[] withSeedA = Recalculate(baseTiles, noise, RecalcSeedA);
            TileData[] withSeedB = Recalculate(baseTiles, noise, RecalcSeedB);

            Assert.Greater(CountTileDifferences(withSeedA, withSeedB), 0,
                "不同 seed 的重算结果必须不同，防止 seed 被忽略（仍走 UnityEngine.Random）");
        }

        [Test]
        public void RecalculateAll_DefaultSeed_IsDeterministic()
        {
            TileData[] baseTiles = CreateBaseTiles();
            float[] noise = CreateFragmentNoise();

            TileData[] first = Recalculate(baseTiles, noise, null);
            TileData[] second = Recalculate(baseTiles, noise, null);

            Assert.AreEqual(0, CountTileDifferences(first, second),
                "不传 seed 的既有 4 参构造调用也必须可复现（默认 seed=0）");
        }
    }

    /// <summary>
    /// 世界 seed 接入回归：GameWorld.RecalculateAll() 必须用当前 world.randomSeed 驱动 SeaLandGenerator 重算路径。
    /// 生产默认破碎噪声全 0（破碎化不触发），因此测试向生成器注入噪声，使 seed 差异可观测。
    /// </summary>
    public class SeaLandWorldSeedRecalcTests
    {
        private const int WorldSeedA = 777;
        private const int WorldSeedB = 778;

        /// <summary>
        /// 构造一个最小 GameWorld（32x16），覆盖为固定测试地形、注入破碎噪声，然后走生产路径
        /// world.RecalculateAll() 全量重算，返回重算后的地块快照。
        /// </summary>
        private static TileData[] RecalculateWorld(int worldSeed)
        {
            var go = new GameObject("SeaLandWorldSeedTest");
            try
            {
                var world = go.AddComponent<GameWorld>();
                world.mapWidth = SeaLandRecalcDeterminismTests.MapWidth;
                world.mapHeight = SeaLandRecalcDeterminismTests.MapHeight;
                world.randomSeed = worldSeed;
                world.SetupOptions = WorldSetupOptions.CreateStandard();
                world.InitializeWorld();

                var input = new System.Random(SeaLandRecalcDeterminismTests.BaseInputSeed);
                for (int i = 0; i < world.tiles.Length; i++)
                {
                    TileData tile = world.tiles[i];
                    tile.exists = true;
                    tile.isLand = false;
                    tile.elevation01 = (float)input.NextDouble();
                    tile.isCoast = false;
                    tile.oceanTier = GameEnums.OceanTier.None;
                    tile.oceanDepth01 = 0f;
                    tile.waterAdjacentWeight = 0f;
                    tile.seaConnectId = -1;
                    world.tiles[i] = tile;
                }

                // 生产流程未注入破碎噪声（默认全 0 → 破碎化不触发）；注入后 seed 才能通过重算路径观测
                world.GetSeaLandGenerator().SetFragmentNoise(SeaLandRecalcDeterminismTests.CreateFragmentNoise());

                world.RecalculateAll();
                return (TileData[])world.tiles.Clone();
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void WorldRecalculateAll_SameSeed_IsDeterministic()
        {
            TileData[] first = RecalculateWorld(WorldSeedA);
            TileData[] second = RecalculateWorld(WorldSeedA);

            Assert.AreEqual(0, SeaLandRecalcDeterminismTests.CountTileDifferences(first, second),
                "同一 world.randomSeed 的两个世界 RecalculateAll 后应逐地块一致");
        }

        [Test]
        public void WorldRecalculateAll_DifferentSeeds_Differ()
        {
            TileData[] withSeedA = RecalculateWorld(WorldSeedA);
            TileData[] withSeedB = RecalculateWorld(WorldSeedB);

            Assert.Greater(SeaLandRecalcDeterminismTests.CountTileDifferences(withSeedA, withSeedB), 0,
                "不同 world.randomSeed 的世界 RecalculateAll 结果必须不同（确保世界 seed 真的接入了重算路径）");
        }
    }
}
