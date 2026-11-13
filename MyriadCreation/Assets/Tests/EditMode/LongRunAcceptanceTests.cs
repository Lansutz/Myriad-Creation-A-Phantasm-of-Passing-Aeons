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

        private GameWorld NewWorld(int seed = 4242)
        {
            var go = new GameObject("LongRunAcceptanceWorld");
            _objects.Add(go);
            GameWorld world = go.AddComponent<GameWorld>();
            world.mapWidth = 64;
            world.mapHeight = 32;
            world.randomSeed = seed;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(seed);
            return world;
        }

        [Test]
        public void RunTwoSeeds_15Days_NoNaN_AndHotspots()
        {
            string dir = Path.Combine(Application.dataPath, "../Logs");
            Directory.CreateDirectory(dir);
            var lines = new List<string> { "seed,day,total_ms,max_ms,memory_mb,realms,settlements,armies" };
            int[] seeds = { 4242, 7777 };
            var stopwatch = new Stopwatch();

            foreach (int seed in seeds)
            {
                GameWorld world = NewWorld(seed);
                double totalMs = 0d;
                double maxMs = 0d;
                for (int day = 0; day < 15; day++)
                {
                    stopwatch.Restart();
                    world.RunDiagnosticDays(1);
                    stopwatch.Stop();
                    double ms = stopwatch.Elapsed.TotalMilliseconds;
                    totalMs += ms;
                    maxMs = System.Math.Max(maxMs, ms);
                    Assert.IsFalse(double.IsNaN(ms), "seed " + seed + " 日耗时为 NaN");
                }
                double memoryMb = System.GC.GetTotalMemory(false) / (1024d * 1024d);
                lines.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2:F3},{3:F3},{4:F3},{5},{6},{7}",
                    seed, world.currentDay, totalMs, maxMs, memoryMb,
                    world.realms.Count, world.settlements.Count, world.armies.Count));
                Assert.Greater(totalMs, 0d, "seed " + seed + " 应有耗时");
                Assert.Less(maxMs, 5000d, "seed " + seed + " 单日不应卡死");
                Assert.Greater(world.realms.Count, 0, "seed " + seed + " 政权不应消失");
                Assert.Greater(world.SimulationScheduler.GetTimings().Count, 0, "seed " + seed + " 应有热点记录");
            }

            string file = Path.Combine(dir, "m05-multiseed-test.csv");
            File.WriteAllLines(file, lines, Encoding.UTF8);
            Assert.IsTrue(File.Exists(file), "多 seed 指标应导出");
            Assert.AreEqual(3, lines.Count, "表头 + 2 个 seed");
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

        /// <summary>
        /// P0.4 的日常档：一年长跑，检查早期/晚期日耗时漂移与内存增长。
        /// 100 年全量见 <see cref="RunHundredYears_TwoSeeds_ExportsLongHorizon"/>（Explicit，按需运行）。
        /// </summary>
        [Test]
        public void RunOneYear_ChecksEarlyLateDrift()
        {
            GameWorld world = NewWorld(4242);
            const int days = 365;
            var daily = new List<double>(days);
            var stopwatch = new Stopwatch();
            double startMemoryMb = System.GC.GetTotalMemory(true) / (1024d * 1024d);

            List<SimulationTimingEntry> midTimings = null;
            for (int day = 0; day < days; day++)
            {
                stopwatch.Restart();
                world.RunDiagnosticDays(1);
                stopwatch.Stop();
                double ms = stopwatch.Elapsed.TotalMilliseconds;
                Assert.IsFalse(double.IsNaN(ms), "日耗时为 NaN");
                daily.Add(ms);
                if (day == 29)
                {
                    midTimings = new List<SimulationTimingEntry>(world.SimulationScheduler.GetTimings());
                }
            }

            double earlyAvg = AverageRange(daily, 0, 30);
            double lateAvg = AverageRange(daily, days - 30, days);
            double maxMs = 0d;
            foreach (double ms in daily) maxMs = System.Math.Max(maxMs, ms);
            double endMemoryMb = System.GC.GetTotalMemory(false) / (1024d * 1024d);

            string dir = Path.Combine(Application.dataPath, "../Logs");
            Directory.CreateDirectory(dir);
            var lines = new List<string> { "day,elapsed_ms,characters,realms,settlements,armies,wars,events,processed_events" };
            for (int i = 0; i < daily.Count; i++)
            {
                lines.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1:F3},{2},{3},{4},{5},{6},{7},{8}",
                    i + 1, daily[i], world.GetCharacterManager().GetAllCharacters().Count,
                    world.realms.Count, world.settlements.Count, world.armies.Count, world.GetWars().Count,
                    world.PendingEventCount, world.LastProcessedEventCount));
            }
            File.WriteAllLines(Path.Combine(dir, "m05-yearly-test.csv"), lines, Encoding.UTF8);

            var hotspotLines = new List<string> { "phase,id,avg_ms,interval_days" };
            if (midTimings != null)
            {
                foreach (SimulationTimingEntry timing in midTimings)
                {
                    hotspotLines.Add(string.Format(CultureInfo.InvariantCulture,
                        "day30,{0},{1:F4},{2}", timing.id, timing.avgMs, timing.intervalDays));
                }
            }
            foreach (SimulationTimingEntry timing in world.SimulationScheduler.GetTimings())
            {
                hotspotLines.Add(string.Format(CultureInfo.InvariantCulture,
                    "day365,{0},{1:F4},{2}", timing.id, timing.avgMs, timing.intervalDays));
            }
            File.WriteAllLines(Path.Combine(dir, "m05-year-hotspots-test.csv"), hotspotLines, Encoding.UTF8);

            Assert.Less(lateAvg, earlyAvg * 4d + 5d, "晚期日均耗时应受控（允许 4 倍 + 5ms 抖动）");
            Assert.Less(maxMs, 5000d, "单日不应卡死");
            Assert.Less(endMemoryMb, startMemoryMb * 2.5d + 200d, "一年后内存增长应有界");
            Assert.Greater(world.currentYear, 0, "时间应推进");
        }

        /// <summary>
        /// P0.4 全量档：2 seed × 100 年。默认不跑（Explicit），用
        /// `-testFilter MyriadCreation.Tests.EditMode.LongRunAcceptanceTests.RunHundredYears_TwoSeeds_ExportsLongHorizon`
        /// 或 Unity Test Runner 显式执行。
        /// </summary>
        [Test]
        [Explicit("100 年长跑按需运行，默认套件不跑")]
        public void RunHundredYears_TwoSeeds_ExportsLongHorizon()
        {
            string dir = Path.Combine(Application.dataPath, "../Logs");
            Directory.CreateDirectory(dir);
            int[] seeds = { 4242, 7777 };
            const int years = 100;
            const int daysPerYear = 365;
            var summary = new List<string> { "seed,year,avg_ms,max_ms,memory_mb,realms,settlements,armies,wars" };
            var stopwatch = new Stopwatch();

            foreach (int seed in seeds)
            {
                GameWorld world = NewWorld(seed);
                var yearAvgs = new List<double>(years);
                for (int year = 0; year < years; year++)
                {
                    double totalMs = 0d;
                    double maxMs = 0d;
                    for (int day = 0; day < daysPerYear; day++)
                    {
                        stopwatch.Restart();
                        world.RunDiagnosticDays(1);
                        stopwatch.Stop();
                        double ms = stopwatch.Elapsed.TotalMilliseconds;
                        totalMs += ms;
                        maxMs = System.Math.Max(maxMs, ms);
                    }
                    double avgMs = totalMs / daysPerYear;
                    yearAvgs.Add(avgMs);
                    double memoryMb = System.GC.GetTotalMemory(false) / (1024d * 1024d);
                    summary.Add(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},{2:F3},{3:F3},{4:F3},{5},{6},{7},{8}",
                        seed, year + 1, avgMs, maxMs, memoryMb,
                        world.realms.Count, world.settlements.Count, world.armies.Count, world.GetWars().Count));
                }

                double early = AverageRange(yearAvgs, 0, 10);
                double late = AverageRange(yearAvgs, years - 10, years);
                Assert.Less(late, early * 3d + 5d, "seed " + seed + " 晚期十年日均耗时应受控");
                Assert.Greater(world.realms.Count, 0, "seed " + seed + " 百年后政权不应消失");
                Assert.Greater(world.currentDay, 0, "seed " + seed + " 时间应推进");
            }

            File.WriteAllLines(Path.Combine(dir, "m05-100y-test.csv"), summary, Encoding.UTF8);
            Assert.AreEqual(1 + seeds.Length * years, summary.Count, "表头 + 2 seed × 100 年");
        }

        private static double AverageRange(List<double> values, int start, int end)
        {
            if (values == null || values.Count == 0) return 0d;
            start = System.Math.Max(0, start);
            end = System.Math.Min(values.Count, end);
            if (end <= start) return 0d;
            double total = 0d;
            for (int i = start; i < end; i++) total += values[i];
            return total / (end - start);
        }
    }
}
