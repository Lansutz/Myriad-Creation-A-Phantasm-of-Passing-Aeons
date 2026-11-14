using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P10.5：8192 地块下经济日结不能击穿调度预算。
    /// 单产缓存 + 地区聚合消费后，平均 tick 应明显低于旧实现（旧实现约 28ms）。
    /// </summary>
    public class EconomyTickPerformanceTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void DailyTick_OnDefaultSizedWorld_StaysUnderBudget()
        {
            _go = new GameObject("EconomyPerfWorld");
            var world = _go.AddComponent<GameWorld>();
            world.mapWidth = 128;
            world.mapHeight = 64;
            world.SetupOptions = WorldSetupOptions.CreateDefault();
            world.InitializeWorld();
            world.GenerateTerrain(4242);

            for (int i = 0; i < 5; i++) world.GetEconomyManager().DailyTick();

            const int Samples = 20;
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < Samples; i++) world.GetEconomyManager().DailyTick();
            watch.Stop();
            double averageMs = watch.Elapsed.TotalMilliseconds / Samples;

            TestContext.WriteLine($"ECON avg={averageMs:F2}ms");
            Assert.Less(averageMs, 25.0, "8192 地块经济日结平均应低于 25ms");
        }
    }
}
