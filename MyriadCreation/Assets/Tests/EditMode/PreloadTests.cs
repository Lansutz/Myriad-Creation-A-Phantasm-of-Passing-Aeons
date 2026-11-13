using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Simulation.AI;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// D39 预加载：任务按预算分帧执行，超预算留到下一帧；预热完成必须幂等。
    /// AI 首次日结耗时可观测，避免"预热了但不知道有没有效"。
    /// </summary>
    public class PreloadTests
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
            var go = new GameObject("PreloadWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(41);
            return world;
        }

        [Test]
        public void RunBudgeted_DefersRemainingTasksToNextFrame()
        {
            var service = new PreloadService();
            int ran = 0;
            for (int i = 0; i < 3; i++)
            {
                service.Enqueue("task" + i, () => { ran++; return true; });
            }
            double clock = 0.0;

            PreloadReport first = service.RunBudgeted(2.5, () => { clock += 1.0; return clock; });

            Assert.AreEqual(2, first.tasksCompleted, "预算内应执行两条");
            Assert.AreEqual(1, first.tasksDeferred, "超预算任务留到下帧");
            Assert.IsFalse(first.allDone);
            Assert.AreEqual(2, ran);

            PreloadReport second = service.RunBudgeted(0.0, () => { clock += 1.0; return clock; });

            Assert.AreEqual(1, second.tasksCompleted);
            Assert.IsTrue(second.allDone);
            Assert.AreEqual(3, ran);
            Assert.IsTrue(service.IsIdle);
        }

        [Test]
        public void Clear_ResetsCountersAndQueue()
        {
            var service = new PreloadService();
            service.Enqueue("a", () => true);
            service.RunBudgeted(0.0, () => 0.0);
            Assert.AreEqual(1, service.TasksCompleted);

            service.Clear();

            Assert.IsTrue(service.IsIdle);
            Assert.AreEqual(0, service.TasksCompleted);
            Assert.AreEqual(0, service.PendingCount);
        }

        [Test]
        public void WorldPreloadPlan_CoversContentAiAndQueries()
        {
            var world = NewWorld();
            int realmCount = world.realms.Count;
            Assert.Greater(realmCount, 0, "前置：应有政权");

            PreloadReport report = world.RunPreloadNow();

            Assert.IsTrue(report.allDone, "不限预算应跑完");
            Assert.IsTrue(world.IsPreloadComplete);
            Assert.GreaterOrEqual(report.tasksCompleted, realmCount + 2, "内容表 + 每政权 AI + AI 上下文 + 查询");
            Assert.AreEqual(realmCount, world.GetAIManager().GetAllControllers().Count, "每政权都应有 AI 控制器");

            PreloadReport again = world.RunPreloadNow();
            Assert.AreEqual(0, again.tasksCompleted, "完成后重复执行不应重建");
            Assert.IsTrue(again.allDone);
        }

        [Test]
        public void AIManager_RecordsFirstTickLatency()
        {
            var manager = new AIManager();
            Assert.AreEqual(-1f, manager.FirstTickMs, "未跑过时应为 -1");

            manager.DailyTick(new Dictionary<int, RealmData>(), null, null, null);

            Assert.GreaterOrEqual(manager.FirstTickMs, 0f, "首次日结应留下耗时");
            float recorded = manager.FirstTickMs;
            manager.DailyTick(new Dictionary<int, RealmData>(), null, null, null);
            Assert.AreEqual(recorded, manager.FirstTickMs, "只记录首次");
        }
    }
}
