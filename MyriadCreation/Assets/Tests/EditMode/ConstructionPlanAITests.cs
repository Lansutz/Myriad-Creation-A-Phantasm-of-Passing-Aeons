using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Simulation.AI;
using MyriadCreation.Simulation.Planning;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P3.3 建造令的生产调用者：AI 按战/财政条件在统一 ConstructionPlanSystem 上立计划，
    /// 不再有“只有 API 没有调用者”的死路。
    /// </summary>
    public class ConstructionPlanAITests
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
            var go = new GameObject("ConstructionAIWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(83);
            return world;
        }

        private static RealmData FirstRealm(GameWorld world)
        {
            foreach (var realm in world.realms.Values) return realm;
            return null;
        }

        private static void RunAIDays(GameWorld world, RealmData realm, int days)
        {
            var controller = new AIController(realm.realmId, new AIPersonality());
            for (int i = 0; i < days; i++)
            {
                controller.DailyTick(world.realms, world.tiles, world.GetDiplomacyManager(),
                    world.Innovations, world.Characters, world);
            }
        }

        [Test]
        public void RichRealm_EventuallyIssuesConstructionPlan()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm, "前置：应有政权");
            realm.treasury = 10000f;

            RunAIDays(world, realm, 400);

            List<PlanSaveData> plans = world.Plans.CapturePlans();
            PlanSaveData construction = plans.Find(p => p.type == (int)PlanType.Construction);
            Assert.IsNotNull(construction, "有财政余量的政权应通过 AI 下建造令");
            Assert.IsTrue(construction.requirements.Exists(r => r.key == "treasury"),
                "建造计划必须带财政需求，不允许绕过统一计划系统直接落建筑");
        }

        [Test]
        public void BrokeRealm_DoesNotIssueConstructionPlan()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm, "前置：应有政权");
            realm.treasury = 0f;

            RunAIDays(world, realm, 400);

            List<PlanSaveData> plans = world.Plans.CapturePlans();
            Assert.IsFalse(plans.Exists(p => p.type == (int)PlanType.Construction),
                "国库为 0 时不得立项（CanBuildBuilding 的财政门禁必须生效）");
        }
    }
}
