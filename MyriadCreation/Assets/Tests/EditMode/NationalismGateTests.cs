using NUnit.Framework;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 民族主义门槛：国界线 + 机械印刷出版 + 普遍公民权，三者缺一不可。
    /// </summary>
    public class NationalismGateTests
    {
        private InnovationTree _tree;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            _tree = new InnovationTree();
        }

        private void Complete(int innovationId)
        {
            var def = _tree.GetInnovation(innovationId);
            Assert.IsNotNull(def, "革新不存在：" + innovationId);
            if (_tree.HasInnovation(1, innovationId))
            {
                return;
            }
            foreach (int prerequisite in def.GetPrerequisiteIds())
            {
                Complete(prerequisite);
            }
            Assert.IsTrue(_tree.StartResearch(1, innovationId), "应可开始研究：" + def.innovationName);
            _tree.DailyTick(1, def.researchCost + 1f);
            Assert.IsTrue(_tree.HasInnovation(1, innovationId), "应完成研究：" + def.innovationName);
        }

        [Test]
        public void Nationalism_RequiresAllThreePrecisePrerequisites()
        {
            Complete(1101); // 国界线
            Complete(1110); // 机械印刷出版
            Assert.IsFalse(_tree.StartResearch(1, 1017), "缺普遍公民权时民族主义不可研究");

            Complete(1103); // 普遍公民权
            Assert.IsTrue(_tree.StartResearch(1, 1017), "三者齐备后民族主义应可研究");
        }

        [Test]
        public void Nationalism_NotAvailable_WhenOnlyTwoOfThree()
        {
            Complete(1101);
            Complete(1103);
            Assert.IsFalse(_tree.StartResearch(1, 1017), "缺机械印刷出版时民族主义不可研究");
        }
    }
}
