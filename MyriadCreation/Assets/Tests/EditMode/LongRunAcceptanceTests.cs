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
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    [System.Serializable]
    public class M05BaselineSnapshot
    {
        public int seed;
        public int days;
        public double avgMs;
        public double maxMs;
        public double memoryMb;
        public int realms;
        public int settlements;
        public int armies;
        public string[] hotspotIds = new string[0];
        public double[] hotspotMs = new double[0];
    }

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
            double totalMs = 0d;
            double lastMemoryMb = 0d;
            var stopwatch = new Stopwatch();

            for (int i = 0; i < 30; i++)
            {
                stopwatch.Restart();
                world.RunDiagnosticDays(1);
                stopwatch.Stop();
                double ms = stopwatch.Elapsed.TotalMilliseconds;
                maxMs = System.Math.Max(maxMs, ms);
                totalMs += ms;
                double memoryMb = System.GC.GetTotalMemory(false) / (1024d * 1024d);
                lastMemoryMb = memoryMb;
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

            var hotspotLines = new List<string> { "id,avg_ms,interval_days,order,lane" };
            var timings = world.SimulationScheduler.GetTimings();
            foreach (SimulationTimingEntry timing in timings)
            {
                hotspotLines.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1:F4},{2},{3},{4}",
                    timing.id, timing.avgMs, timing.intervalDays, timing.order, timing.lane));
            }
            string hotspotFile = Path.Combine(dir, "m05-hotspots-test.csv");
            File.WriteAllLines(hotspotFile, hotspotLines, Encoding.UTF8);

            var baseline = new M05BaselineSnapshot
            {
                seed = 4242,
                days = 30,
                avgMs = totalMs / 30d,
                maxMs = maxMs,
                memoryMb = lastMemoryMb,
                realms = world.realms.Count,
                settlements = world.settlements.Count,
                armies = world.armies.Count
            };
            int topCount = System.Math.Min(5, timings.Count);
            baseline.hotspotIds = new string[topCount];
            baseline.hotspotMs = new double[topCount];
            for (int i = 0; i < topCount; i++)
            {
                baseline.hotspotIds[i] = timings[i].id;
                baseline.hotspotMs[i] = timings[i].avgMs;
            }
            string baselineFile = Path.Combine(dir, "m05-baseline-test.json");
            File.WriteAllText(baselineFile, JsonUtility.ToJson(baseline, true));

            Assert.IsTrue(File.Exists(file), "M0.5 指标 CSV 应导出");
            Assert.IsTrue(File.Exists(baselineFile), "M0.5 基线快照应导出");
            Assert.Greater(baseline.avgMs, 0d, "基线平均日耗时应大于 0");
            Assert.Greater(baseline.memoryMb, 0d, "基线内存应大于 0");
            Assert.IsTrue(File.Exists(hotspotFile), "M0.5 热点 CSV 应导出");
            Assert.AreEqual(31, lines.Count, "30 天应有表头 + 30 行");
            Assert.Greater(timings.Count, 0, "应有调度项耗时记录");
            Assert.Less(maxMs, 5000d, "小世界单日 tick 不应超过 5s（宽松上限，仅拦卡死）");
            Assert.IsTrue(world.currentDay != startDay || world.currentYear != startYear,
                "长跑后世界时间必须推进");
            Assert.Greater(world.realms.Count, 0, "长跑后政权不应消失");
        }
    }
}
