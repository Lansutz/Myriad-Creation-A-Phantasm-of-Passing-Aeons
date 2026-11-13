using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P8.0：世界历法接线——年长改变后年份/季节仍正确推进。</summary>
    public class CalendarIntegrationTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private GameWorld NewWorld(int daysPerYear)
        {
            _go = new GameObject("CalendarWorld");
            var world = _go.AddComponent<GameWorld>();
            world.randomSeed = 131;
            world.mapWidth = 32;
            world.mapHeight = 16;
            world.SetupOptions = WorldSetupOptions.CreateDefault();
            world.SetupOptions.calendar.daysPerYear = daysPerYear;
            world.InitializeWorld();
            world.GenerateTerrain(131);
            return world;
        }

        [Test]
        public void SetupCalendar_PropagatesToWorld()
        {
            _go = new GameObject("CalendarWorld");
            var world = _go.AddComponent<GameWorld>();
            world.randomSeed = 131;
            world.mapWidth = 32;
            world.mapHeight = 16;
            world.SetupOptions = WorldSetupOptions.CreateDefault();
            world.SetupOptions.calendar.hoursPerDay = 12f;
            world.SetupOptions.calendar.daysPerYear = 400;
            world.InitializeWorld();
            world.GenerateTerrain(131);

            Assert.AreEqual(12f, world.calendar.hoursPerDay, 0.001f, "自转参数应传入世界历法");
            Assert.AreEqual(400, world.calendar.daysPerYear, "公转参数应传入世界历法");
            Assert.Greater(world.calendar.DiurnalRangeFactor, 1f);
        }

        [Test]
        public void ThreeHundredSixtyDayYear_AdvancesYearsAndSeasons()
        {
            var world = NewWorld(360);
            for (int day = 0; day < 360 * 10; day++) world.RunDiagnosticDays(1);

            Assert.AreEqual(11, world.currentYear, "10 个 360 天年应到第 11 年");
            Assert.GreaterOrEqual(world.currentDay, 1);
            Assert.LessOrEqual(world.currentDay, 360, "日号不得超过配置的年长");
            Assert.GreaterOrEqual(world.currentSeason, 0);
            Assert.LessOrEqual(world.currentSeason, 3);
        }

        [Test]
        [Explicit("100 年回归按需运行，默认套件不跑")]
        [Timeout(1800000)]
        public void HundredYears_BothYearLengthsStayConsistent()
        {
            foreach (int daysPerYear in new[] { 365, 360 })
            {
                var world = NewWorld(daysPerYear);
                for (int year = 0; year < 100; year++) world.RunDiagnosticDays(daysPerYear);

                Assert.AreEqual(101, world.currentYear, $"{daysPerYear} 天/年：100 年后应在第 101 年");
                Assert.GreaterOrEqual(world.currentDay, 1);
                Assert.LessOrEqual(world.currentDay, daysPerYear, $"{daysPerYear} 天/年：日号越界");
                Assert.IsNotNull(world.settlements);
                Assert.IsNotNull(world.realms);
                Object.DestroyImmediate(_go);
                _go = null;
            }
        }

        [Test]
        public void ThreeHundredSixtyFiveDayYear_AdvanceDaysPerTickDoesNotSkipYear()
        {
            var world = NewWorld(365);
            world.daysPerTick = 30;
            for (int tick = 0; tick < 12 * 3; tick++) world.RunDiagnosticDays(1);

            Assert.AreEqual(3, world.currentYear, "36 次 30 天跳日应推进约 3 年");
        }
    }
}
