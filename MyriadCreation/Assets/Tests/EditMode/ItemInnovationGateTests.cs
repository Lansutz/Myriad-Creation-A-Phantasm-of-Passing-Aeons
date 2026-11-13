using System.Linq;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>物品可以同时依赖多条革新：两条都满足才解锁；不再退化成单 requiredInnovation。</summary>
    public class ItemInnovationGateTests
    {
        private InnovationTree _tree;

        [SetUp]
        public void Setup()
        {
            _tree = new InnovationTree();
            _tree.RegisterInnovation(new InnovationDef { innovationId = 9501, innovationName = "甲工艺" });
            _tree.RegisterInnovation(new InnovationDef { innovationId = 9502, innovationName = "乙材料" });
        }

        private void Complete(int id)
        {
            Assert.IsTrue(_tree.StartResearch(1, id));
            _tree.DailyTick(1, 100000f);
            Assert.IsTrue(_tree.HasInnovation(1, id));
        }

        private static EquipmentDef MultiItem()
        {
            return new EquipmentDef
            {
                equipmentId = 9001,
                equipmentName = "双革新物品",
                requiredInnovation = 0,
                requiredInnovationTree = InnovationPrerequisiteNode.All(
                    InnovationPrerequisiteNode.Leaf(9501),
                    InnovationPrerequisiteNode.Leaf(9502)),
            };
        }

        [Test]
        public void ItemNeedsBothInnovations()
        {
            var item = MultiItem();
            Assert.IsFalse(_tree.AreItemInnovationsMet(1, item));
            Complete(9501);
            Assert.IsFalse(_tree.AreItemInnovationsMet(1, item), "只掌握甲工艺不应解锁");
            Complete(9502);
            Assert.IsTrue(_tree.AreItemInnovationsMet(1, item), "甲乙都掌握才解锁");
        }

        [Test]
        public void SingleInnovationStillWorks()
        {
            var item = new EquipmentDef { equipmentId = 9002, requiredInnovation = 9501 };
            Assert.IsFalse(_tree.AreItemInnovationsMet(1, item));
            Complete(9501);
            Assert.IsTrue(_tree.AreItemInnovationsMet(1, item));
        }
    }
}
