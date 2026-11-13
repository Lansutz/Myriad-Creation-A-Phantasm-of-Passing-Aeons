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
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 初始开图性能回归（task-50）：破碎化接入后，1024×512 的 GenerateTerrain 曾从历史 ~4.8s 涨到 ~85.8s。
    /// 根因是 SeaLandGenerator 热路径每次邻接遍历都分配 int[6]+迭代器（Boehm GC 全堆扫描放大），
    /// 优化为构造期扁平邻居表 + 预分配缓冲复用 + 破碎化后的局部邻接重算。
    /// 本测试用宽松上限（8s）拦住回到 80s 级的回归，并校验同 seed 两次生成逐地块一致（语义未变）。
    /// 不在测试内做严格性能断言（CI 抖动），实际耗时通过 Debug.Log / TestContext 输出给 Lead。
    /// </summary>
    public class SeaLandPerformanceTests
    {
        private const int MapWidth = 1024;
        private const int MapHeight = 512;
        private const int WorldSeed = 424242;
        private const long LooseBudgetMs = 8000L;

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
        }

        [Test]
        public void GenerateTerrain_1024x512_UnderLooseBudget_AndDeterministic()
        {
            long firstMs;
            long firstHash;
            int firstLand;
            int firstSea;

            GameWorld firstWorld = CreateWorld();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            firstWorld.GenerateTerrain(WorldSeed);
            stopwatch.Stop();
            firstMs = stopwatch.ElapsedMilliseconds;
            firstHash = ComputeTerrainHash(firstWorld.tiles);
            firstLand = firstWorld.GetLandTileCount();
            firstSea = firstWorld.GetSeaTileCount();
            Assert.Greater(firstLand, 0, "前置：1024×512 开图后应存在陆地");
            LogMeasurement("第一次", firstMs, firstLand, firstSea);
            Object.DestroyImmediate(firstWorld.gameObject);
            firstWorld = null;
            // 释放第一张 1024×512 的大世界，避免第二次生成时内存叠加（TileData 是 class，524k 对象）。
            System.GC.Collect();

            long secondMs;
            long secondHash;
            int secondLand;
            var secondWorld = CreateWorld();
            stopwatch.Restart();
            secondWorld.GenerateTerrain(WorldSeed);
            stopwatch.Stop();
            secondMs = stopwatch.ElapsedMilliseconds;
            secondHash = ComputeTerrainHash(secondWorld.tiles);
            secondLand = secondWorld.GetLandTileCount();
            LogMeasurement("第二次", secondMs, secondLand, secondWorld.GetSeaTileCount());
            Object.DestroyImmediate(secondWorld.gameObject);

            Assert.Less(firstMs, LooseBudgetMs,
                "1024×512 GenerateTerrain 超过宽松上限 8s（历史 ~4.8s，破碎化回归时曾 85.8s）");
            Assert.Less(secondMs, LooseBudgetMs,
                "同 seed 第二次 1024×512 GenerateTerrain 超过宽松上限 8s");
            Assert.LessOrEqual(secondMs, System.Math.Max(firstMs * 4L, 5000L),
                "同 seed 两次生成的耗时应在同一量级（容忍 CI 抖动/首次 JIT 预热）");
            Assert.AreEqual(firstHash, secondHash,
                "同 seed 两次 1024×512 生成的地形必须逐地块一致（isLand/高程/海岸/海洋层级/海域连通）");
            Assert.AreEqual(firstLand, secondLand, "同 seed 两次生成的陆地数应一致");
        }

        private GameWorld CreateWorld()
        {
            var go = new GameObject("SeaLandPerfWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = MapWidth;
            world.mapHeight = MapHeight;
            world.randomSeed = WorldSeed;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            return world;
        }

        private static void LogMeasurement(string label, long elapsedMs, int land, int sea)
        {
            string line = $"[SeaLandPerf] 1024x512 GenerateTerrain {label}: {elapsedMs} ms (land={land}, sea={sea})";
            Debug.Log((object)line);
            TestContext.WriteLine(line);
        }

        /// <summary>
        /// 地形哈希：覆盖 SeaLand 重算会写入的全部字段，用于同 seed 两次生成的逐地块一致性校验
        /// （TileData 是 class，逐个 524k 对象深拷贝代价高，哈希等价于逐项比较且内存友好）。
        /// </summary>
        private static long ComputeTerrainHash(TileData[] tiles)
        {
            unchecked
            {
                long hash = 1469598103934665603L;
                for (int i = 0; i < tiles.Length; i++)
                {
                    TileData tile = tiles[i];
                    hash = (hash ^ (tile.exists ? 1 : 0)) * 1099511628211L;
                    hash = (hash ^ (tile.isLand ? 1 : 0)) * 1099511628211L;
                    hash = (hash ^ (int)tile.oceanTier) * 1099511628211L;
                    hash = (hash ^ (tile.isCoast ? 1 : 0)) * 1099511628211L;
                    hash = (hash ^ tile.seaConnectId) * 1099511628211L;
                    hash = (hash ^ tile.elevation01.GetHashCode()) * 1099511628211L;
                    hash = (hash ^ tile.oceanDepth01.GetHashCode()) * 1099511628211L;
                    hash = (hash ^ tile.waterAdjacentWeight.GetHashCode()) * 1099511628211L;
                }
                return hash;
            }
        }
    }
}
