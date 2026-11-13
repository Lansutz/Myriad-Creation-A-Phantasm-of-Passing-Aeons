using NUnit.Framework;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>帧预算调度：超预算的条目留到下一帧。</summary>
    public class SimulationSchedulerBudgetTests
    {
        [Test]
        public void Budget_DefersRemainingEntries()
        {
            var scheduler = new SimulationScheduler();
            double now = 0;
            int a = 0, b = 0, c = 0;
            scheduler.Register("a", SimulationCadence.Daily, 0, _ => { now += 0.01; a++; });
            scheduler.Register("b", SimulationCadence.Daily, 1, _ => { now += 0.01; b++; });
            scheduler.Register("c", SimulationCadence.Daily, 2, _ => { now += 0.01; c++; });

            scheduler.TickBudgeted(1, 1, 1, 5f, () => now, out int executed, out int deferred);
            Assert.AreEqual(1, executed);
            Assert.AreEqual(2, deferred);
            Assert.AreEqual(1, a);
            Assert.AreEqual(0, b);
            Assert.AreEqual(0, c);

            scheduler.TickBudgeted(2, 1, 1, 1000f, () => now, out executed, out deferred);
            Assert.AreEqual(0, deferred);
            Assert.AreEqual(1, b);
            Assert.AreEqual(1, c);
        }

        [Test]
        public void Governor_RaisesBudgetUnderBacklog_AndClamps()
        {
            var governor = new SimulationBudgetGovernor { CurrentBudgetMs = 8f };
            float raised = governor.NextBudget(16f, pendingEntries: 3, deferredLastTick: 0);
            Assert.Greater(raised, 8f);
            for (int i = 0; i < 20; i++) governor.NextBudget(20f, 5, 5);
            Assert.LessOrEqual(governor.CurrentBudgetMs, governor.MaxBudgetMs);
            for (int i = 0; i < 30; i++) governor.NextBudget(8f, 0, 0);
            Assert.GreaterOrEqual(governor.CurrentBudgetMs, governor.MinBudgetMs);
        }

        [Test]
        public void ProvinceQualityPreset_SetsTargets()
        {
            var config = new MapGenerationConfig();
            config.ProvinceQuality = MapGenerationConfig.ProvinceQualityTier.Performance;
            config.ApplyProvinceQualityPreset();
            Assert.AreEqual(5000, config.ProvinceCount);
            Assert.AreEqual(1, config.ProvinceLloydIterations);

            config.ProvinceQuality = MapGenerationConfig.ProvinceQualityTier.Quality;
            config.ApplyProvinceQualityPreset();
            Assert.AreEqual(8000, config.ProvinceCount);
            Assert.AreEqual(3, config.ProvinceLloydIterations);
        }

        [Test]
        public void NoClock_ExecutesAll()
        {
            var scheduler = new SimulationScheduler();
            int count = 0;
            scheduler.Register("a", SimulationCadence.Daily, 0, _ => count++);
            scheduler.Register("b", SimulationCadence.Daily, 1, _ => count++);
            scheduler.TickBudgeted(1, 1, 1, 0f, null, out int executed, out int deferred);
            Assert.AreEqual(2, executed);
            Assert.AreEqual(0, deferred);
            Assert.AreEqual(2, count);
        }
    }
}
