using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P1.5 征兵：人口→军队、兵种革新门禁、人口不足拒绝。</summary>
    public class RecruitmentTests
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
            var go = new GameObject("RecruitmentWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(79);
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

        private static int FirstUnitId(GameWorld world)
        {
            foreach (int key in world.unitDefs.Keys)
            {
                if (!world.Innovations.IsUnlockReferenced(InnovationUnlockKind.Unit, key)) return key;
            }
            return -1;
        }

        private static void MakeUnitFree(GameWorld world, int unitId)
        {
            UnitDef def = world.unitDefs[unitId];
            def.manpowerCost = 100f;
            def.recruitCost = null;
            def.requiredInnovations = new List<int>();
            world.unitDefs[unitId] = def;
        }

        [Test]
        public void Recruit_CreatesArmyAndConsumesPopulation()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            int tile = FirstOwnedLandTile(world, realm.realmId);
            int unitId = FirstUnitId(world);
            Assert.GreaterOrEqual(tile, 0);
            Assert.GreaterOrEqual(unitId, 0);
            MakeUnitFree(world, unitId);
            TileData data = world.tiles[tile];
            data.populationBlocks = new List<PopulationBlock>
            {
                new PopulationBlock { count = 2000f, socialClass = GameEnums.SocialClass.Peasant, satisfaction = 60f }
            };
            world.tiles[tile] = data;
            SettlementPopulationSystem.RecomputeAll(world);

            bool ok = RecruitmentSystem.TryRecruit(world, realm.realmId, tile, unitId, 1f, out Army army, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.IsNotNull(army);
            Assert.AreEqual(realm.realmId, army.ownerRealmId);
            Assert.AreEqual(1f, army.unitCounts[unitId], 0.01f);
            float population = 0f;
            foreach (PopulationBlock block in world.tiles[tile].populationBlocks) population += block.count;
            Assert.Less(population, 2000f, "征兵应减少地块人口");
        }

        [Test]
        public void Recruit_RequiresUnitInnovationUnlock()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            int tile = FirstOwnedLandTile(world, realm.realmId);
            int unitId = FirstUnitId(world);
            MakeUnitFree(world, unitId);
            world.Innovations.RegisterInnovation(new InnovationDef
            {
                innovationId = 9010,
                unlockRefs = new List<InnovationUnlock> { InnovationUnlock.Of(InnovationUnlockKind.Unit, unitId) }
            });

            Assert.IsFalse(RecruitmentSystem.TryRecruit(world, realm.realmId, tile, unitId, 1f, out _, out string reason),
                "未解锁兵种不得征召");
            Assert.AreEqual("兵种尚未由革新解锁", reason);

            world.Innovations.GrantInnovation(realm.realmId, 9010);
            Assert.IsTrue(RecruitmentSystem.TryRecruit(world, realm.realmId, tile, unitId, 1f, out _, out _));
        }

        [Test]
        public void Recruit_FailsWithoutPopulation()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            int tile = FirstOwnedLandTile(world, realm.realmId);
            int unitId = FirstUnitId(world);
            MakeUnitFree(world, unitId);
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (world.tiles[i].ownerRealmId != realm.realmId) continue;
                TileData owned = world.tiles[i];
                owned.populationBlocks = new List<PopulationBlock>();
                world.tiles[i] = owned;
            }
            SettlementPopulationSystem.RecomputeAll(world);

            Assert.IsFalse(RecruitmentSystem.TryRecruit(world, realm.realmId, tile, unitId, 1f, out _, out string reason));
            Assert.AreEqual("可征召人口不足", reason);
        }
    }
}
