using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P4.0 宫廷人物供给与征税门禁：开国有可任职的人，官职能填上，税收有征收者。
    /// </summary>
    public class CourtAndTaxGateTests
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
        }

        private GameWorld NewWorld()
        {
            var go = new GameObject("CourtWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(131);
            return world;
        }

        private static RealmData FirstRealm(GameWorld world)
        {
            foreach (RealmData realm in world.realms.Values) return realm;
            return null;
        }

        [Test]
        public void NewRealm_HasCourtiersBeyondRulerAndSpouse()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm);
            int count = 0;
            foreach (CharacterData character in world.Characters.GetCharactersByRealm(realm.realmId))
            {
                if (character.role != CharacterRole.Ruler && character.role != CharacterRole.Spouse) count++;
            }
            Assert.GreaterOrEqual(count, 3, "开局至少应有 3 名可任职宫廷人物");
        }

        [Test]
        public void NewRealm_OfficesFilledIncludingTaxation()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm);
            Assert.IsTrue(realm.officeHolders != null && realm.officeHolders.Count > 0, "生成结束应填充官职");
            Assert.IsTrue(OfficeSystem.HasModuleHolder(realm, OfficeSystem.ModuleTaxation, world.Characters, out _),
                "开局应有持 taxation 职权的官员");
        }

        [Test]
        public void TaxGate_NoCollectorMeansZeroRevenue()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm);
            float normal = world.GetEconomyManager().SettleTaxes(realm.realmId);
            Assert.Greater(normal, 0f, "有征收者时应能算出正税收");

            float blocked = world.GetEconomyManager().SettleTaxes(realm.realmId, 1f, hasCollector: false);
            Assert.AreEqual(0f, blocked, 0.001f, "无征税官时税收必须为 0，而不是税率归零");
        }

        [Test]
        public void FirstDayTax_AppearsInBudgetReport()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            world.RunDiagnosticDays(1);

            RealmBudgetReport report = world.GetRealmBudgetReport(realm.realmId);
            Assert.Greater(report.income, 0f, "开局第一日应有税收入账");
            Assert.IsTrue(report.lines.Exists(l => l.reason == "tax"), "税收应留下流水");
        }
    }
}
