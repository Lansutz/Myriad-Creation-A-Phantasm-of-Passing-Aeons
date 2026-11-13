using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// M0.5 最小 headless 长跑台：固定 seed 跑 N 天，导出日耗时/内存/实体计数。
    /// 严格性能阈值留给后续基线快照，本测试先守住“能跑、时间在推进、无 NaN、有导出文件”。
    /// </summary>
    public class LongRunAcceptanceTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
            MapUnitRegistry.Clear();
            CharacterKnowledgeRegistry.Clear();
            RelationRegistry.Clear();
            ContractRegistry.Clear();
        }

        private GameWorld NewWorld()
        {
            var go = new GameObject("LongRunAcceptanceWorld");
            _objects.Add(go);
            GameWorld world = go.AddComponent<GameWorld>();
            world.mapWidth = 64;
            world.mapHeight = 32;
            world.randomSeed = 4242;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(4242);
            return world;
        }

        [Test]
        public void Run30Days_ExportsMetrics_AndAdvancesTime()
        {
            GameWorld world = NewWorld();
            int startDay = world.currentDay;
            int startYear = world.currentYear;
            var lines = new List<string>
            {
                "day,elapsed_ms,memory_mb,realms,settlements,armies,plans"
            };
            double maxMs = 0d;
            var stopwatch = new Stopwatch();

            for (int i = 0; i < 30; i++)
            {
                stopwatch.Restart();
                world.RunDiagnosticDays(1);
                stopwatch.Stop();
                double ms = stopwatch.Elapsed.TotalMilliseconds;
                maxMs = System.Math.Max(maxMs, ms);
                double memoryMb = System.GC.GetTotalMemory(false) / (1024d * 1024d);
                Assert.IsFalse(double.IsNaN(ms), "日耗时不应该是 NaN");
                Assert.IsFalse(double.IsNaN(memoryMb), "内存不应该是 NaN");
                lines.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1:F3},{2:F3},{3},{4},{5},{6}",
                    world.currentDay, ms, memoryMb, world.realms.Count,
                    world.settlements.Count, world.armies.Count, world.Plans.PlanCount));
            }

            string dir = Path.Combine(Application.dataPath, "../Logs");
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, "m05-longrun-test.csv");
            File.WriteAllLines(file, lines, Encoding.UTF8);

            Assert.IsTrue(File.Exists(file), "M0.5 指标 CSV 应导出");
            Assert.AreEqual(31, lines.Count, "30 天应有表头 + 30 行");
            Assert.Less(maxMs, 5000d, "小世界单日 tick 不应超过 5s（宽松上限，仅拦卡死）");
            Assert.IsTrue(world.currentDay != startDay || world.currentYear != startYear,
                "长跑后世界时间必须推进");
            Assert.Greater(world.realms.Count, 0, "长跑后政权不应消失");
        }
    }
}
