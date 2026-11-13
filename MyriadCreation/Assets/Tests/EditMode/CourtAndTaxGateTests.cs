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
    /// 过渡期测试（P5.2 之前）：世界生成不再自动造宫廷/填官职；
    /// 成熟的宫廷必须在测试里显式播种，官职才由内容表补缺。
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

        private static void SeedMatureCourt(GameWorld world, RealmData realm)
        {
            world.Characters.CreateInitialCourtiers(world.realms, world.currentYear);
            OfficeSystem.FillVacancies(realm, world.Characters, world.currentYear * 365 + world.currentDay);
        }

        [Test]
        public void NewRealm_HasNoAutoCourtOrOffices()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm);
            int extra = 0;
            foreach (CharacterData character in world.Characters.GetCharactersByRealm(realm.realmId))
            {
                if (character.role != CharacterRole.Ruler && character.role != CharacterRole.Spouse) extra++;
            }
            Assert.AreEqual(0, extra, "世界生成不得再凭空造 3 名宫廷人物");
            Assert.IsTrue(realm.officeHolders == null || realm.officeHolders.Count == 0, "生成结束不得自动填官职");
        }

        [Test]
        public void MatureRealm_OfficesFilledIncludingTaxation()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm);
            SeedMatureCourt(world, realm);
            Assert.IsTrue(realm.officeHolders != null && realm.officeHolders.Count > 0, "显式播种宫廷后才应补缺");
            Assert.IsTrue(OfficeSystem.HasModuleHolder(realm, OfficeSystem.ModuleTaxation, world.Characters, out _),
                "有可任职人物后应出现持 taxation 职权的官员");
        }

        [Test]
        public void TaxGate_NoCollectorMeansZeroRevenue()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm);
            SeedMatureCourt(world, realm);
            float normal = world.GetEconomyManager().SettleTaxes(realm.realmId);
            Assert.Greater(normal, 0f, "有征收者时应能算出正税收");

            float blocked = world.GetEconomyManager().SettleTaxes(realm.realmId, 1f, hasCollector: false);
            Assert.AreEqual(0f, blocked, 0.001f, "无征税官时税收必须为 0，而不是税率归零");
        }

        [Test]
        public void RemoveCollector_ZeroTax_ThenRefillRestores()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm);
            SeedMatureCourt(world, realm);

            realm.officeHolders.Clear();
            Assert.IsFalse(OfficeSystem.HasModuleHolder(realm, OfficeSystem.ModuleTaxation, world.Characters, out _),
                "移除后不应再有征税官");
            Assert.AreEqual(0f, world.GetEconomyManager().SettleTaxes(realm.realmId, 1f, hasCollector: false), 0.001f);

            int filled = OfficeSystem.FillVacancies(realm, world.Characters, world.currentYear * 365 + world.currentDay);

            Assert.Greater(filled, 0, "有候选时应补缺");
            Assert.IsTrue(OfficeSystem.HasModuleHolder(realm, OfficeSystem.ModuleTaxation, world.Characters, out _),
                "补缺后应恢复征税权");
        }

        [Test]
        public void FirstDayTax_AppearsInBudgetReport()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            SeedMatureCourt(world, realm);
            world.RunDiagnosticDays(1);

            RealmBudgetReport report = world.GetRealmBudgetReport(realm.realmId);
            Assert.Greater(report.income, 0f, "有征收者时第一日应有税收入账");
            Assert.IsTrue(report.lines.Exists(l => l.reason == "tax"), "税收应留下流水");
        }
    }
}
