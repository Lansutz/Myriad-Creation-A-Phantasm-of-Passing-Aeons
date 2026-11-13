using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P1.3：和平主义教义禁止宗教战争/圣战。</summary>
    public class DoctrinePacifismTests
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
            var go = new GameObject("DoctrinePacifismWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(101);
            return world;
        }

        private static (RealmData realm, ReligionDef religion) Pick(GameWorld world)
        {
            RealmData realm = null;
            foreach (var r in world.realms.Values) { realm = r; break; }
            FaithSystem faith = world.GetFaithSystem(realm.stateReligionId);
            Assert.IsNotNull(faith);
            Assert.IsTrue(ContentResolvers.Religions.TryGet(faith.religionId, out ReligionDef religion));
            return (realm, religion);
        }

        [Test]
        public void Pacifism_BlocksReligiousWarDeclaration()
        {
            var world = NewWorld();
            var (realm, religion) = Pick(world);
            int defender = -1;
            foreach (int key in world.realms.Keys)
            {
                if (key != realm.realmId) { defender = key; break; }
            }
            Assert.GreaterOrEqual(defender, 0, "前置：应有两个政权");
            var original = new List<string>(religion.selectedDoctrines);
            try
            {
                religion.selectedDoctrines.Add("doctrine_pacifism");
                Assert.IsTrue(world.IsStateFaithPacifist(realm.realmId), "国教应持和平主义");

                var cb = new CasusBelli
                {
                    cbId = 77,
                    type = GameEnums.CasusBelliType.ReligiousConflict,
                    holderRealmId = realm.realmId,
                    targetRealmId = defender,
                    expiryDay = -1
                };

                Assert.IsFalse(world.DeclareWarWithJustification(realm.realmId, defender, cb, null, "圣战"),
                    "和平主义国教不得发动宗教战争");
            }
            finally
            {
                religion.selectedDoctrines.Clear();
                religion.selectedDoctrines.AddRange(original);
            }
        }
    }
}
