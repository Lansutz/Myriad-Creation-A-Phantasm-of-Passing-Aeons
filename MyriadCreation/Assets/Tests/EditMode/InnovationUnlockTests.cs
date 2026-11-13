using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P1.4 革新解锁第一消费者：建筑受 unlockRefs 门禁；未被任何革新引用的建筑保持可用。
    /// </summary>
    public class InnovationUnlockTests
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
            var go = new GameObject("InnovationUnlockWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(71);
            return world;
        }

        private static RealmData FirstRealm(GameWorld world)
        {
            foreach (var realm in world.realms.Values) return realm;
            return null;
        }

        private static int FirstOwnedLandTile(GameWorld world, int realmId)
        {
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (world.tiles[i].isLand && world.tiles[i].ownerRealmId == realmId) return i;
            }
            return -1;
        }

        [Test]
        public void UnreferencedBuilding_StaysAvailable()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            var system = new BuildingSystem(world.tiles) { Innovations = world.Innovations };

            Assert.IsTrue(world.Innovations.IsUnlockGranted(realm.realmId, InnovationUnlockKind.Building, 99999),
                "未被任何革新引用的建筑不应被门禁挡住");
        }

        [Test]
        public void ReferencedBuilding_RequiresGrantedInnovation()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            int tile = FirstOwnedLandTile(world, realm.realmId);
            Assert.GreaterOrEqual(tile, 0, "前置：应有政权地块");
            realm.treasury = 10000f;
            var innovation = new InnovationDef
            {
                innovationId = 9001,
                unlockRefs = new List<InnovationUnlock> { InnovationUnlock.Of(InnovationUnlockKind.Building, 100) }
            };
            world.Innovations.RegisterInnovation(innovation);
            var system = new BuildingSystem(world.tiles) { Innovations = world.Innovations };

            Assert.IsTrue(world.Innovations.IsUnlockReferenced(InnovationUnlockKind.Building, 100));
            Assert.IsFalse(world.Innovations.IsUnlockGranted(realm.realmId, InnovationUnlockKind.Building, 100),
                "未完成革新时不得解锁");
            Assert.IsFalse(system.BuildBuilding(tile, 100, realm.realmId, realm), "建筑门禁应拒绝未解锁建筑");

            world.Innovations.GrantInnovation(realm.realmId, 9001);

            Assert.IsTrue(world.Innovations.IsUnlockGranted(realm.realmId, InnovationUnlockKind.Building, 100));
            Assert.IsTrue(system.BuildBuilding(tile, 100, realm.realmId, realm), "解锁后应允许建造");
        }
    }
}
