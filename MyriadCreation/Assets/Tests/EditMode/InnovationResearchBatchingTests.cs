using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.AI;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P10.4 批处理口径回归：日结研究系统必须按 deltaDays 折算。
    /// 旧实现每 tick 只加一次 rate，daysPerTick=90 时长跑只有 1/90 的速度。
    /// </summary>
    public class InnovationResearchBatchingTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            ContentRegistry.Initialize();
        }

        private static InnovationDef PickPlainInnovation(InnovationTree innovations)
        {
            foreach (KeyValuePair<int, InnovationDef> pair in innovations.GetAllInnovations())
            {
                InnovationDef def = pair.Value;
                if (def == null || def.GetPrerequisiteIds().Count > 0) continue;
                if (def.researchCost < 100) continue;
                return def;
            }
            return null;
        }

        [Test]
        public void DailyResearchSchedule_ScalesProgressWithDeltaDays()
        {
            var innovations = new InnovationTree();
            InnovationDef def = PickPlainInnovation(innovations);
            Assert.IsNotNull(def, "内容里应至少有一个无前置、成本 ≥100 的革新可用于测试");

            const int realmId = 7;
            var realms = new Dictionary<int, RealmData>
            {
                [realmId] = new RealmData { realmId = realmId, coreTiles = new HashSet<int> { 0 } }
            };
            var tiles = new[] { new TileData { exists = true, development = 0f } };

            var ai = new AIManager();
            ai.CreateController(realmId, new AIPersonality { researchMultiplier = 1f, fixedArchetype = true });

            Assert.IsTrue(innovations.StartResearch(realmId, def.innovationId),
                "无世界语义的 StartResearch 不受实践门槛限制，应能立项");

            var scheduler = new SimulationScheduler();
            InnovationResearchSchedule.Register(scheduler, innovations, ai, () => realms, () => tiles);

            scheduler.TickBudgeted(90, 1, 90, 0f, null, out _, out _);

            // 基础速率 0.5/天 × (1 + 开发度 0) × 个性 1.0；跳 90 天应推进 45 点。
            // GetResearchProgress 返回 0..1 的完成度，乘研究成本还原为点数。
            float progress = innovations.GetResearchProgress(realmId) * def.researchCost;
            Assert.AreEqual(45f, progress, 0.01f,
                "日结研究必须按 deltaDays 折算；只加一次 rate 会让大跳步长跑慢 90 倍");
        }
    }
}
