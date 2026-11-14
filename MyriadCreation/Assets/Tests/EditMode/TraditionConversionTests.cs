using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P11.5：革新 → 传统固化 → 教义/法律转换；条件不满足不得凭空出现。
    /// </summary>
    public class TraditionConversionTests
    {
        private GameObject _go;
        private GameWorld _world;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private GameWorld NewWorld()
        {
            _go = new GameObject("TraditionWorld");
            var world = _go.AddComponent<GameWorld>();
            world.mapWidth = 16;
            world.mapHeight = 8;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(11);
            world.realms.Clear();
            world.cultures.Clear();
            return world;
        }

        private CultureData AddRealmCulture(GameWorld world, int realmId, int cultureId, float maturity)
        {
            world.realms[realmId] = new RealmData
            {
                realmId = realmId,
                primaryCultureId = cultureId,
                stateReligionId = world.FaithSystems.Count > 0 ? world.FaithSystems[0].faithId : -1
            };
            var culture = new CultureData { cultureId = cultureId, maturity = maturity };
            world.cultures[cultureId] = culture;
            return culture;
        }

        [Test]
        public void Tradition_SpawnsOnlyWhenInnovationAndMaturityMet()
        {
            GameWorld world = NewWorld();
            CultureData culture = AddRealmCulture(world, 1, 500, 0.9f);

            List<string> before = TraditionConversionSystem.TrySpawnTraditions(world, 1);
            CollectionAssert.DoesNotContain(before, "trad_agrarian_rites", "没有对应革新不得固化传统");

            world.Innovations.GrantInnovation(1, 100);
            List<string> after = TraditionConversionSystem.TrySpawnTraditions(world, 1);
            CollectionAssert.Contains(after, "trad_agrarian_rites");
            CollectionAssert.Contains(culture.traditionIds, "trad_agrarian_rites");
        }

        [Test]
        public void Tradition_RespectsMaturityGate()
        {
            GameWorld world = NewWorld();
            AddRealmCulture(world, 1, 500, 0.05f);
            world.Innovations.GrantInnovation(1, 100);

            List<string> added = TraditionConversionSystem.TrySpawnTraditions(world, 1);

            CollectionAssert.DoesNotContain(added, "trad_agrarian_rites", "成熟度不足不得固化");
        }

        [Test]
        public void Tradition_ConvertsToDoctrine()
        {
            GameWorld world = NewWorld();
            CultureData culture = AddRealmCulture(world, 1, 500, 0.9f);
            FaithSystem faith = world.GetFaithSystem(world.realms[1].stateReligionId);
            Assert.IsNotNull(faith, "测试世界应至少有一个 faith 运行态");
            culture.traditionIds.Add("trad_agrarian_rites");

            bool ok = TraditionConversionSystem.TryConvert(world, 1, "trad_agrarian_rites", 100, out string target);

            Assert.IsTrue(ok);
            Assert.AreEqual("doctrine_feast_communal", target);
            CollectionAssert.Contains(faith.doctrineIds, "doctrine_feast_communal");
        }

        [Test]
        public void Tradition_ConvertsToLaw_ThroughLawCommand()
        {
            GameWorld world = NewWorld();
            CultureData culture = AddRealmCulture(world, 1, 500, 0.9f);
            culture.traditionIds.Add("trad_craft_guild");
            var law = new RecordingLawCommand();

            bool ok = TraditionConversionSystem.TryConvert(world, 1, "trad_craft_guild", 120, out string target, law);

            Assert.IsTrue(ok);
            Assert.AreEqual("charter_guild", target);
            Assert.AreEqual("charter_guild", law.LastLawId);
            Assert.AreEqual(1, law.EnactCalls);
        }

        [Test]
        public void Conversion_RequiresTraditionInCulture()
        {
            GameWorld world = NewWorld();
            AddRealmCulture(world, 1, 500, 0.9f);

            Assert.IsFalse(TraditionConversionSystem.TryConvert(world, 1, "trad_craft_guild", 100, out _));
        }

        private sealed class RecordingLawCommand : ILawCommand
        {
            public int EnactCalls;
            public string LastLawId = "";
            public void EnsureKnownLaws(int realmId) { }
            public int TickPendingLaws(int realmId, int currentDay) => 0;
            public void TickDecreeExpiry(int realmId, int currentDay) { }
            public bool BeginEnactLaw(int realmId, string lawId, int proposerCharacterId, int currentDay, out string reason)
            {
                EnactCalls++;
                LastLawId = lawId;
                reason = "";
                return true;
            }
            public bool CancelPendingLaw(int realmId, string groupId, int requesterCharacterId, out string reason) { reason = ""; return true; }
            public bool EnactDecree(int realmId, string decreeId, int currentDay, out string reason) { reason = ""; return true; }
            public bool RevokeDecree(int realmId, string decreeId, out string reason) { reason = ""; return true; }
            public bool ForceSetLaw(int realmId, string groupId, string lawId, int currentDay, out string reason) { reason = ""; return true; }
        }
    }
}
