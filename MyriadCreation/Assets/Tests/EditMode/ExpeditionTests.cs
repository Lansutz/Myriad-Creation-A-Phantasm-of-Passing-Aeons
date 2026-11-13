using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Planning;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P4.1 远征：路线移动、知识揭示、补给/拦截失败、计划完成、存档往返。
    /// </summary>
    public class ExpeditionTests
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
            var go = new GameObject("ExpeditionWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(151);
            return world;
        }

        private static RealmData FirstRealm(GameWorld world)
        {
            foreach (RealmData realm in world.realms.Values) return realm;
            return null;
        }

        private static int FindReachableTarget(GameWorld world, int start)
        {
            for (int i = world.tiles.Length - 1; i >= 0; i--)
            {
                if (!world.tiles[i].isLand || i == start) continue;
                if (ExpeditionSystem.FindRoute(world.tiles, world.mapWidth, world.mapHeight, start, i) != null) return i;
            }
            return -1;
        }

        [Test]
        public void CreateExplorationPlan_CreatesExpeditionAndCompletes()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            CharacterData ruler = world.Characters.GetCharacter(realm.GetSupremeRulerId());
            Assert.IsNotNull(ruler);
            int start = ruler.currentTile >= 0 ? ruler.currentTile : FindOwned(world, realm.realmId);
            int target = FindReachableTarget(world, start);
            Assert.GreaterOrEqual(target, 0, "前置：应有可达目标");

            Plan plan = world.CreateExplorationPlan(realm.realmId, ruler.characterId, target);

            Assert.IsNotNull(plan, "有路线时应立项");
            Assert.IsTrue(world.expeditions.ContainsKey(plan.targetId));
            Expedition expedition = world.expeditions[plan.targetId];
            Assert.Greater(expedition.route.Count, 1);

            for (int day = 0; day < expedition.route.Count + 10 && plan.state == PlanState.Executing; day++)
            {
                world.RunDiagnosticDays(1);
            }

            Assert.AreEqual(PlanState.Completed, plan.state, "远征抵达后计划应完成");
            Assert.AreEqual(ExpeditionState.Completed, expedition.state);
            Assert.IsTrue(world.Knowledge.IsKnown(realm.realmId, target), "远征必须揭示目标地块");
        }

        [Test]
        public void Expedition_WithoutSuppliesFails()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            CharacterData ruler = world.Characters.GetCharacter(realm.GetSupremeRulerId());
            int start = ruler.currentTile >= 0 ? ruler.currentTile : FindOwned(world, realm.realmId);
            int target = FindReachableTarget(world, start);
            Plan plan = world.CreateExplorationPlan(realm.realmId, ruler.characterId, target);
            Assert.IsNotNull(plan);
            Expedition expedition = world.expeditions[plan.targetId];
            expedition.supplies = 0f;

            world.RunDiagnosticDays(1);

            Assert.AreEqual(ExpeditionState.Failed, expedition.state);
            Assert.AreEqual(PlanState.Failed, plan.state);
            Assert.AreEqual("supplies", expedition.failReason);
        }

        [Test]
        public void Expedition_EnemyArmyIntercepts()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            CharacterData ruler = world.Characters.GetCharacter(realm.GetSupremeRulerId());
            int start = ruler.currentTile >= 0 ? ruler.currentTile : FindOwned(world, realm.realmId);
            int target = FindReachableTarget(world, start);
            Plan plan = world.CreateExplorationPlan(realm.realmId, ruler.characterId, target);
            Assert.IsNotNull(plan);
            Expedition expedition = world.expeditions[plan.targetId];
            int nextTile = expedition.route[1];
            world.armies[9001] = new Army { armyId = 9001, ownerRealmId = realm.realmId + 1, currentTileIndex = nextTile };

            world.RunDiagnosticDays(2);

            Assert.AreEqual(ExpeditionState.Failed, expedition.state);
            Assert.AreEqual("intercepted", expedition.failReason);
        }

        [Test]
        public void ExpeditionSave_RoundTrips()
        {
            var expedition = new Expedition
            {
                expeditionId = 3,
                ownerRealmId = 1,
                leaderCharacterId = 9,
                homeTileIndex = 5,
                currentTileIndex = 6,
                route = new List<int> { 5, 6, 7 },
                routeCursor = 1,
                supplies = 42f,
                state = ExpeditionState.Outbound
            };

            ExpeditionSaveData dto = ExpeditionSaveData.FromExpedition(expedition);
            Expedition restored = dto.ToExpedition();

            Assert.AreEqual(3, restored.expeditionId);
            Assert.AreEqual(3, restored.route.Count);
            Assert.AreEqual(1, restored.routeCursor);
            Assert.AreEqual(42f, restored.supplies, 0.001f);
            Assert.AreEqual(ExpeditionState.Outbound, restored.state);
        }

        [Test]
        public void ExpeditionPersistence_CapturesAndRestores()
        {
            var world = NewWorld();
            world.expeditions[7] = new Expedition
            {
                expeditionId = 7,
                ownerRealmId = 1,
                route = new List<int> { 1, 2, 3 },
                routeCursor = 2,
                supplies = 30f
            };

            List<ExpeditionSaveData> captured = ExpeditionPersistence.Capture(world);
            world.expeditions.Clear();
            int nextId = ExpeditionPersistence.Restore(world, captured);

            Assert.IsTrue(world.expeditions.ContainsKey(7));
            Assert.AreEqual(2, world.expeditions[7].routeCursor);
            Assert.AreEqual(8, nextId, "恢复后自增 id 应接在最大值之后");
        }

        private static int FindOwned(GameWorld world, int realmId)
        {
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (world.tiles[i].isLand && world.tiles[i].ownerRealmId == realmId) return i;
            }
            return -1;
        }
    }
}
