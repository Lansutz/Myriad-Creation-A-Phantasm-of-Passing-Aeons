using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P9/M3：政权没有相应社会实践就不能立项；实践达到需求的 50% 才可立项。
    /// 纯单元路径（world 为空）不读实践池，仍供旧测试/调试使用。
    /// </summary>
    public class RealmPracticeGateTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private GameWorld NewWorld()
        {
            _go = new GameObject("PracticeGateWorld");
            return _go.AddComponent<GameWorld>();
        }

        [Test]
        public void StartResearch_RequiresHalfPractice()
        {
            var world = NewWorld();
            var tree = new InnovationTree();
            InnovationDef def = tree.GetInnovation(100);
            Assert.IsNotNull(def);

            Assert.IsFalse(tree.StartResearch(1, 100, world), "没有实践不得立项");

            float half = InnovationTree.GetRequiredPractice(def) * 0.5f;
            tree.AddPractice(1, def.field, half * 0.9f);
            Assert.IsFalse(tree.StartResearch(1, 100, world), "不足 50% 仍不得立项");

            tree.AddPractice(1, def.field, half * 0.2f);
            Assert.IsTrue(tree.StartResearch(1, 100, world), "达到 50% 应可立项");
        }

        [Test]
        public void Practice_IsPerFieldAndPerRealm()
        {
            var tree = new InnovationTree();
            tree.AddPractice(1, InnovationField.Agriculture, 10f);

            Assert.AreEqual(10f, tree.GetPractice(1, InnovationField.Agriculture), 0.001f);
            Assert.AreEqual(0f, tree.GetPractice(1, InnovationField.Mining), 0.001f);
            Assert.AreEqual(0f, tree.GetPractice(2, InnovationField.Agriculture), 0.001f);
        }

        [Test]
        public void RealmMastery_IsMaxAcrossCharacters()
        {
            var knowledge = new InnovationKnowledgeSystem();
            knowledge.SetMastery(10, 100, 1);
            knowledge.SetMastery(11, 100, 3);
            knowledge.SetMastery(12, 100, 2);

            Assert.AreEqual(3, knowledge.GetRealmMastery(new[] { 10, 11, 12 }, 100));
            Assert.AreEqual(0, knowledge.GetRealmMastery(new[] { 12 }, 999));
        }

        [Test]
        public void Crafting_RecordsPracticeForRecipeField()
        {
            var tree = new InnovationTree();
            tree.AddPractice(3, InnovationField.Craft, 5f);
            Assert.AreEqual(5f, tree.GetPractice(3, InnovationField.Craft), 0.001f);
        }
    }
}
