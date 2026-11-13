using NUnit.Framework;
using MyriadCreation.World.Presentation;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.World.Settlement;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>UI 点类型名必须按代码枚举：堡垒不能被隐藏，也不能发明“聚落”。</summary>
    public class SiteTypeLabelsTests
    {
        [Test]
        public void SettlementType_UsesLevelName()
        {
            var settlement = new SettlementData
            {
                siteMode = SiteMode.SedentarySettlement,
                settlementLevel = SettlementLevel.LevelIII,
            };
            Assert.AreEqual("城邑", SiteTypeLabels.TypeName(settlement));
            Assert.AreEqual("定居点", SiteTypeLabels.Group(settlement.siteMode));
        }

        [Test]
        public void Fort_IsFortress_WithPurpose()
        {
            var fort = new SettlementData
            {
                siteMode = SiteMode.FortifiedOutpost,
                fortifiedOutpostData = new FortifiedOutpostData { purpose = "关口" },
            };
            Assert.AreEqual("堡垒", SiteTypeLabels.TypeName(fort), "关口堡本质仍是堡垒");
            Assert.AreEqual("据点", SiteTypeLabels.Group(fort.siteMode), "堡垒属于据点，但据点不只有堡垒");
        }

        [Test]
        public void OutpostPort_IsPort_NotFortress()
        {
            var port = new SettlementData
            {
                siteMode = SiteMode.FortifiedOutpost,
                portTier = PortTier.DeepWaterPort,
                primaryFunction = SettlementFunction.Commercial,
            };
            Assert.AreEqual("港口", SiteTypeLabels.TypeName(port), "据点组里港口不是堡垒");
        }

        [Test]
        public void Camp_IsCamp()
        {
            var camp = new CampData();
            Assert.AreEqual("营地", SiteTypeLabels.TypeName(camp));
            Assert.AreEqual("营地", SiteTypeLabels.Group(camp.siteMode));
        }
    }
}
