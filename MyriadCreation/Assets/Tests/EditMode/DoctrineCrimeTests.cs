using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.FaithHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P1.3：伦理教义决定行为是否入罪；Shunned/Accepted 都不入罪。</summary>
    public class DoctrineCrimeTests
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
            var go = new GameObject("DoctrineCrimeWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(97);
            return world;
        }

        private static ReligionDef ReligionOf(GameWorld world, RealmData realm)
        {
            FaithSystem faith = world.GetFaithSystem(realm.stateReligionId);
            Assert.IsNotNull(faith);
            Assert.IsTrue(ContentResolvers.Religions.TryGet(faith.religionId, out ReligionDef religion));
            return religion;
        }

        [Test]
        public void CrimeStance_MarksBehaviorCriminal()
        {
            var world = NewWorld();
            int realmId = -1;
            foreach (int key in world.realms.Keys) { realmId = key; break; }
            RealmData realm = world.realms[realmId];
            ReligionDef religion = ReligionOf(world, realm);
            var original = new List<string>(religion.selectedDoctrines);
            try
            {
                religion.selectedDoctrines.Add("doctrine_adultery_crime");
                Assert.IsTrue(world.IsDoctrineCriminal(realmId, DoctrineCrimeBehavior.Adultery));
                Assert.IsFalse(world.IsDoctrineCriminal(realmId, DoctrineCrimeBehavior.Witchcraft));
            }
            finally
            {
                religion.selectedDoctrines.Clear();
                religion.selectedDoctrines.AddRange(original);
            }
        }

        [Test]
        public void AcceptedStance_DoesNotCreateCrime()
        {
            var world = NewWorld();
            int realmId = -1;
            foreach (int key in world.realms.Keys) { realmId = key; break; }
            ReligionDef religion = ReligionOf(world, world.realms[realmId]);
            var original = new List<string>(religion.selectedDoctrines);
            try
            {
                religion.selectedDoctrines.Add("doctrine_homosexuality_accepted");
                Assert.IsFalse(world.IsDoctrineCriminal(realmId, DoctrineCrimeBehavior.Homosexuality));
            }
            finally
            {
                religion.selectedDoctrines.Clear();
                religion.selectedDoctrines.AddRange(original);
            }
        }
    }
}
