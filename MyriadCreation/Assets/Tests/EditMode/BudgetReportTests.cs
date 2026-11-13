using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P1.2 收支报告：只汇总实际国库流水，收入/支出/净额与窗口过滤正确，不写模拟状态。
    /// </summary>
    public class BudgetReportTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
            TreasuryLedger.ClearHistory();
        }

        private GameWorld NewWorld()
        {
            var go = new GameObject("BudgetReportWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(61);
            return world;
        }

        private static RealmData FirstRealm(GameWorld world)
        {
            foreach (var realm in world.realms.Values) return realm;
            return null;
        }

        [Test]
        public void TaxIncome_AppearsInReport()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm, "前置：应有政权");
            // P5：生成期不再自动造宫廷/填官职，税收必须有人征。显式播种成熟宫廷后再跑一天。
            world.Characters.CreateInitialCourtiers(world.realms, world.currentYear);
            OfficeSystem.FillVacancies(realm, world.Characters, world.currentYear * 365 + world.currentDay);

            world.RunDiagnosticDays(1);
            RealmBudgetReport report = world.GetRealmBudgetReport(realm.realmId);

            Assert.Greater(report.income, 0f, "税收入应进入报告");
            Assert.IsTrue(report.lines.Exists(l => l.reason == "tax"), "应有一条 tax 流水线");
            Assert.AreEqual(report.income - report.expense, report.net, 0.01f);
            Assert.AreEqual(realm.treasury, report.treasury, 0.01f);
        }

        [Test]
        public void Expenses_AndWindowFiltering_Work()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm);
            int today = world.currentYear * 365 + world.currentDay;
            TreasuryLedger.Record(realm.realmId, today, -120f, "construction");
            TreasuryLedger.Record(realm.realmId, today - 1000, -500f, "old");

            RealmBudgetReport report = world.GetRealmBudgetReport(realm.realmId, 365);

            Assert.AreEqual(120f, report.expense, 0.01f, "窗口内支出应被统计");
            Assert.IsFalse(report.lines.Exists(l => l.reason == "old"), "窗口外流水应被过滤");
            Assert.AreEqual(-120f, report.net, 0.01f, "净额 = 收入 - 支出");
        }
    }
}
