using NUnit.Framework;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P8.0：cadence 按累计天数判定，跳日（daysPerTick&gt;1）不漏月度/年度系统。</summary>
    public class SimulationSchedulerCadenceTests
    {
        [Test]
        public void Monthly_FiresWhenJumpingThirtyDays()
        {
            var scheduler = new SimulationScheduler();
            int count = 0;
            scheduler.Register("m", SimulationCadence.Monthly, 0, _ => count++);

            scheduler.TickBudgeted(31, 1, 30, 0f, null, out _, out _);
            Assert.AreEqual(1, count, "一次跳 30 天必须触发月度系统");
        }

        [Test]
        public void Monthly_AccumulatesSingleDays()
        {
            var scheduler = new SimulationScheduler();
            int count = 0;
            scheduler.Register("m", SimulationCadence.Monthly, 0, _ => count++);

            for (int day = 1; day <= 29; day++)
            {
                scheduler.TickBudgeted(day, 1, 1, 0f, null, out _, out _);
            }
            Assert.AreEqual(0, count, "29 天不应触发");
            scheduler.TickBudgeted(30, 1, 1, 0f, null, out _, out _);
            Assert.AreEqual(1, count, "第 30 天应触发");
        }

        [Test]
        public void Seasonal_FiresOnNinetyDayJump()
        {
            var scheduler = new SimulationScheduler();
            int count = 0;
            scheduler.Register("s", SimulationCadence.Seasonal, 0, _ => count++);

            scheduler.TickBudgeted(91, 1, 90, 0f, null, out _, out _);
            Assert.AreEqual(1, count, "一次跳 90 天必须触发季节系统");
        }

        [Test]
        public void YearlyLongTerm_FiresOnYearJump()
        {
            var scheduler = new SimulationScheduler();
            int count = 0;
            scheduler.Register("y", SimulationCadence.LongTerm, 0, _ => count++);

            scheduler.TickBudgeted(366, 2, 365, 0f, null, out _, out _);
            Assert.AreEqual(1, count, "一次跳 365 天必须触发年度系统");
        }
    }
}
