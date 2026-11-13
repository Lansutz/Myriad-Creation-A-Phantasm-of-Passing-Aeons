using NUnit.Framework;
using MyriadCreation.Core.Data;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P8.0：历法是日/年/季节的唯一来源。</summary>
    public class GameCalendarTests
    {
        [Test]
        public void Defaults_AreEarthLike()
        {
            var cal = new GameCalendar();
            Assert.AreEqual(365, cal.daysPerYear);
            Assert.AreEqual(4, cal.seasons);
            Assert.AreEqual(91, cal.DaysPerSeason, "365/4 向下取整为 91，不再写死 91");
            Assert.AreEqual(24f, cal.hoursPerDay);
            Assert.AreEqual(1, cal.ticksPerDay);
        }

        [Test]
        public void SeasonIndex_UsesConfiguredYearLength()
        {
            var cal = new GameCalendar { daysPerYear = 360, seasons = 4 };
            Assert.AreEqual(90, cal.DaysPerSeason);
            Assert.AreEqual(0, cal.SeasonIndex(1));
            Assert.AreEqual(0, cal.SeasonIndex(90));
            Assert.AreEqual(1, cal.SeasonIndex(91));
            Assert.AreEqual(3, cal.SeasonIndex(360));
        }

        [Test]
        public void AbsoluteDay_RoundTripsAcrossYearLengths()
        {
            foreach (int yearLength in new[] { 360, 365, 400 })
            {
                var cal = new GameCalendar { daysPerYear = yearLength };
                for (int day = 1; day <= yearLength; day += 37)
                {
                    int absolute = cal.AbsoluteDay(7, day);
                    Assert.AreEqual(7, cal.YearOfAbsoluteDay(absolute));
                    Assert.AreEqual(day, cal.DayOfYearOfAbsoluteDay(absolute));
                }
            }
        }

        [Test]
        public void Normalize_ClampsInvalidWorldParameters()
        {
            var cal = new GameCalendar { hoursPerDay = 0f, daysPerYear = 0, seasons = 0, ticksPerDay = 0, daysPerTick = 0 };
            cal.Normalize();
            Assert.GreaterOrEqual(cal.hoursPerDay, 1f);
            Assert.GreaterOrEqual(cal.daysPerYear, 30);
            Assert.GreaterOrEqual(cal.seasons, 1);
            Assert.GreaterOrEqual(cal.ticksPerDay, 1);
            Assert.GreaterOrEqual(cal.daysPerTick, 1);
        }

        [Test]
        public void DiurnalRangeFactor_ScalesWithRotation()
        {
            var earth = new GameCalendar { hoursPerDay = 24f };
            var fast = new GameCalendar { hoursPerDay = 12f };
            var slow = new GameCalendar { hoursPerDay = 48f };
            Assert.AreEqual(1f, earth.DiurnalRangeFactor, 0.001f);
            Assert.Greater(fast.DiurnalRangeFactor, 1f, "自转更快应缩小昼夜温差");
            Assert.Less(slow.DiurnalRangeFactor, 1f, "自转更慢应放大昼夜温差");
        }

        [Test]
        public void HourOfDay_UsesTicksPerDay()
        {
            var cal = new GameCalendar { ticksPerDay = 4, hoursPerDay = 24f };
            Assert.AreEqual(0f, cal.HourOfDay(0));
            Assert.AreEqual(6f, cal.HourOfDay(1));
            Assert.AreEqual(18f, cal.HourOfDay(3));
        }
    }
}
