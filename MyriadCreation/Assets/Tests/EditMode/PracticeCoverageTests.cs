using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Enums;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P10.1：社会实践必须覆盖全部革新领域，否则“实践 50% 才可立项”会把整条科技链锁死；
    /// AI 只能从实践达标的革新里选题。
    /// </summary>
    public class PracticeCoverageTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
        }

        [Test]
        public void SocialClasses_CoverEveryInnovationField()
        {
            var tree = new InnovationTree();
            foreach (GameEnums.SocialClass socialClass in Enum.GetValues(typeof(GameEnums.SocialClass)))
            {
                SettlementSimulationSystem.AddClassPractice(tree, 1, socialClass, 1000f);
            }

            var missing = Enum.GetValues(typeof(InnovationField))
                .Cast<InnovationField>()
                .Where(field => tree.GetPractice(1, field) <= 0f)
                .ToList();
            Assert.IsEmpty(missing, "以下领域没有实践来源：" + string.Join(", ", missing));
        }

        [Test]
        public void ResearchableList_RespectsPracticeGate()
        {
            _go = new GameObject("PracticeFilterWorld");
            var world = _go.AddComponent<GameWorld>();
            var tree = new InnovationTree();
            InnovationDef def = tree.GetInnovation(100);
            Assert.IsNotNull(def);

            Assert.IsFalse(tree.GetResearchableInnovations(1, world).Any(x => x.innovationId == 100),
                "没有实践时不应出现在可立项列表");

            tree.AddPractice(1, def.field, InnovationTree.GetRequiredPractice(def) * 0.5f);
            Assert.IsTrue(tree.GetResearchableInnovations(1, world).Any(x => x.innovationId == 100),
                "实践达到 50% 后应可立项");
        }
    }
}
