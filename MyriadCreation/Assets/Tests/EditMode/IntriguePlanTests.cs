using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Simulation.Planning;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P3.5 密谋：S19 地图知识是立项前提；目标死亡/失去线索/有军队戒备都会让计划失败。
    /// </summary>
    public class IntriguePlanTests
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
            var go = new GameObject("IntrigueWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(113);
            return world;
        }

        private static void PickTwoRealms(GameWorld world, out RealmData a, out RealmData b)
        {
            a = null;
            b = null;
            foreach (RealmData realm in world.realms.Values)
            {
                if (a == null) a = realm;
                else { b = realm; break; }
            }
        }

        [Test]
        public void Create_RequiresKnownTargetTile()
        {
            var world = NewWorld();
            PickTwoRealms(world, out RealmData plotterRealm, out RealmData targetRealm);
            Assert.IsNotNull(plotterRealm);
            Assert.IsNotNull(targetRealm, "前置：至少两个政权");
            CharacterData plotter = world.Characters.GetCharacter(plotterRealm.GetSupremeRulerId());
            CharacterData target = world.Characters.GetCharacter(targetRealm.GetSupremeRulerId());
            Assert.IsNotNull(plotter);
            Assert.IsNotNull(target);
            target.currentTile = 30;
            // 清空知识层，确保“未知”是测试前提而不是生成期播种的巧合。
            world.Knowledge.Restore(new List<RealmKnowledgeSaveData>(), world.tiles.Length);

            Assert.IsNull(world.CreateIntriguePlan(plotterRealm.realmId, plotter.characterId, target.characterId),
                "目标地块未知时不得凭空立项");

            world.Knowledge.MarkKnown(plotterRealm.realmId, 30);
            Plan plan = world.CreateIntriguePlan(plotterRealm.realmId, plotter.characterId, target.characterId);

            Assert.IsNotNull(plan, "知道目标位置后才允许立项");
            Assert.AreEqual(PlanType.Intrigue, plan.type);
            Assert.AreEqual(PlanState.Executing, plan.state, "立项后应进入执行态");
        }

        [Test]
        public void Execute_TargetDeathFailsPlan()
        {
            var world = NewWorld();
            PickTwoRealms(world, out RealmData plotterRealm, out RealmData targetRealm);
            CharacterData plotter = world.Characters.GetCharacter(plotterRealm.GetSupremeRulerId());
            CharacterData target = world.Characters.GetCharacter(targetRealm.GetSupremeRulerId());
            target.currentTile = 30;
            world.Knowledge.MarkKnown(plotterRealm.realmId, 30);
            Plan plan = world.CreateIntriguePlan(plotterRealm.realmId, plotter.characterId, target.characterId);
            Assert.IsNotNull(plan);

            target.deathDay = 1; // 目标身故
            world.Plans.DailyTick(world.currentDay, 1f);

            Assert.AreEqual(PlanState.Failed, plan.state, "目标死亡后密谋必须失败，而不是继续推进");
            StringAssert.Contains("target_dead", plan.resultCode ?? "");
        }

        [Test]
        public void Execute_UnguardedTargetCompletes()
        {
            var world = NewWorld();
            PickTwoRealms(world, out RealmData plotterRealm, out RealmData targetRealm);
            CharacterData plotter = world.Characters.GetCharacter(plotterRealm.GetSupremeRulerId());
            CharacterData target = world.Characters.GetCharacter(targetRealm.GetSupremeRulerId());
            target.currentTile = 30;
            world.Knowledge.MarkKnown(plotterRealm.realmId, 30);
            Plan plan = world.CreateIntriguePlan(plotterRealm.realmId, plotter.characterId, target.characterId);
            Assert.IsNotNull(plan);

            for (int day = 0; day < 60 && plan.state == PlanState.Executing; day++)
            {
                world.Plans.DailyTick(day + 1, 1f);
            }

            Assert.AreEqual(PlanState.Completed, plan.state, "无军队戒备时密谋应在期限内完成");
        }

        [Test]
        public void Execute_GuardedTargetFails()
        {
            var world = NewWorld();
            PickTwoRealms(world, out RealmData plotterRealm, out RealmData targetRealm);
            CharacterData plotter = world.Characters.GetCharacter(plotterRealm.GetSupremeRulerId());
            CharacterData target = world.Characters.GetCharacter(targetRealm.GetSupremeRulerId());
            target.currentTile = 30;
            world.Knowledge.MarkKnown(plotterRealm.realmId, 30);
            var guard = new Army { armyId = 9001, ownerRealmId = targetRealm.realmId, currentTileIndex = 30 };
            world.armies[guard.armyId] = guard;
            Plan plan = world.CreateIntriguePlan(plotterRealm.realmId, plotter.characterId, target.characterId);
            Assert.IsNotNull(plan);

            for (int day = 0; day < 60 && plan.state == PlanState.Executing; day++)
            {
                world.Plans.DailyTick(day + 1, 1f);
            }

            Assert.AreEqual(PlanState.Failed, plan.state, "目标身边有军队时应失败");
            StringAssert.Contains("guarded", plan.resultCode ?? "");
        }
    }
}
