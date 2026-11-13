using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 前置表达式：All（全部满足）/Any（多选一）/可嵌套；旧 prerequisites 字段仍兼容。
    /// 以及革新的结构化解锁目标（InnovationUnlock）与具体物品分离。
    /// </summary>
    public class InnovationPrerequisiteExpressionTests
    {
        private InnovationTree _tree;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            _tree = new InnovationTree();
        }

        private void Complete(int id)
        {
            var def = _tree.GetInnovation(id);
            Assert.IsNotNull(def, "革新不存在：" + id);
            Assert.IsTrue(_tree.StartResearch(1, id), "应可开始：" + id);
            _tree.DailyTick(1, def.researchCost + 1f);
            Assert.IsTrue(_tree.HasInnovation(1, id), "应完成：" + id);
        }

        private InnovationDef Register(int id, InnovationPrerequisiteNode tree)
        {
            var def = new InnovationDef
            {
                innovationId = id,
                innovationName = "测试前置 " + id,
                field = InnovationField.Governance,
                era = 0,
                researchCost = 1f,
                prerequisiteTree = tree,
            };
            _tree.RegisterInnovation(def);
            return def;
        }

        private void RegisterLeaf(int id)
        {
            _tree.RegisterInnovation(new InnovationDef
            {
                innovationId = id,
                innovationName = "叶子 " + id,
                field = InnovationField.Governance,
                era = 0,
                researchCost = 1f,
            });
        }

        [Test]
        public void AllOfAnyAndAll_RequiresBothBranches()
        {
            RegisterLeaf(9101);
            RegisterLeaf(9102);
            RegisterLeaf(9103);
            RegisterLeaf(9104);
            var def = Register(9200, InnovationPrerequisiteNode.All(
                InnovationPrerequisiteNode.Any(InnovationPrerequisiteNode.Leaf(9101), InnovationPrerequisiteNode.Leaf(9102)),
                InnovationPrerequisiteNode.All(InnovationPrerequisiteNode.Leaf(9103), InnovationPrerequisiteNode.Leaf(9104))));

            Complete(9101);
            Complete(9103);
            Assert.IsFalse(_tree.ArePrerequisitesMet(null, 1, def), "缺 9104 时不应满足");

            Complete(9104);
            Assert.IsTrue(_tree.ArePrerequisitesMet(null, 1, def), "任一 OR 分支 + 全部 AND 分支应满足");
        }

        [Test]
        public void AnyOfAll_AllowsEitherNestedBranch()
        {
            RegisterLeaf(9111);
            RegisterLeaf(9112);
            RegisterLeaf(9113);
            var def = Register(9201, InnovationPrerequisiteNode.Any(
                InnovationPrerequisiteNode.All(InnovationPrerequisiteNode.Leaf(9111), InnovationPrerequisiteNode.Leaf(9112)),
                InnovationPrerequisiteNode.Leaf(9113)));

            Complete(9111);
            Assert.IsFalse(_tree.ArePrerequisitesMet(null, 1, def), "第一个 AND 分支不完整");
            Complete(9113);
            Assert.IsTrue(_tree.ArePrerequisitesMet(null, 1, def), "第二个叶子分支满足");
        }

        [Test]
        public void NestedTree_SurvivesJsonRoundTrip()
        {
            var def = new InnovationDef
            {
                innovationId = 9300,
                innovationName = "序列化测试",
                field = InnovationField.Governance,
                era = 0,
                researchCost = 1f,
                prerequisiteTree = InnovationPrerequisiteNode.All(
                    InnovationPrerequisiteNode.Leaf(9101),
                    InnovationPrerequisiteNode.Any(
                        InnovationPrerequisiteNode.Leaf(9102),
                        InnovationPrerequisiteNode.Leaf(9103))),
                unlockRefs = new List<InnovationUnlock>
                {
                    InnovationUnlock.Of(InnovationUnlockKind.Good, 12),
                    InnovationUnlock.Keyed(InnovationUnlockKind.Unit, "chariot"),
                },
            };

            string json = JsonUtility.ToJson(def);
            var restored = JsonUtility.FromJson<InnovationDef>(json);

            Assert.IsNotNull(restored);
            Assert.IsNotNull(restored.prerequisiteTree);
            Assert.AreEqual(InnovationPrerequisiteKind.All, restored.prerequisiteTree.kind);
            Assert.AreEqual(2, restored.prerequisiteTree.children.Count);
            Assert.AreEqual(InnovationPrerequisiteKind.Any, restored.prerequisiteTree.children[1].kind);
            Assert.AreEqual(2, restored.unlockRefs.Count);
            Assert.AreEqual(InnovationUnlockKind.Good, restored.unlockRefs[0].kind);
            Assert.AreEqual(12, restored.unlockRefs[0].id);
            Assert.AreEqual("chariot", restored.unlockRefs[1].key);
        }

    }
}
