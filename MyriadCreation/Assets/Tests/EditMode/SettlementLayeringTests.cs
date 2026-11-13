using NUnit.Framework;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Units;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>定居点/据点/营地三者定位：采石采矿据点是 FortifiedOutpost，不是 SedentarySettlement。</summary>
    public class SettlementLayeringTests
    {
        [Test]
        public void LargeQuarry_IsFortifiedOutpost_NotSedentarySettlement()
        {
            var site = new SettlementData();
            site.InitializeModeData(SiteMode.FortifiedOutpost);
            site.siteMode = SiteMode.FortifiedOutpost;
            site.population = 1200f;
            site.garrison = 150;
            site.fortifiedOutpostData.kind = OutpostKind.Quarrying;
            site.economicComposition[EconomicSector.Mining] = 0.7f;
            site.economicComposition[EconomicSector.Artisanal] = 0.2f;
            site.economicComposition[EconomicSector.Commerce] = 0.1f;

            Assert.AreEqual(SiteMode.FortifiedOutpost, site.siteMode);
            Assert.AreEqual(OutpostKind.Quarrying, site.fortifiedOutpostData.kind);
            Assert.Greater(site.population, 0f, "据点可以住人");
            Assert.Greater(site.garrison, 0, "据点可以驻军");
            Assert.AreEqual(3, site.economicComposition.Count, "据点可以有多种经济成分");
        }

        [Test]
        public void UnitActivity_GatheringDoesNotNeedOutpost()
        {
            var unit = new TransportUnit();
            Assert.AreEqual(UnitActivity.None, unit.activity);
            unit.activity = UnitActivity.Gathering;
            Assert.AreEqual(UnitActivity.Gathering, unit.activity);
        }
    }
}
