using NUnit.Framework;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.Politics;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>B2.2：头衔共持（Sole/Senior/CoRuler/Elector/Regent + 机构持有）。</summary>
    public class TitleHoldingTests
    {
        [Test]
        public void NewTitleDef_HasEmptyHolders()
        {
            var def = new TitleDefinition { titleId = "title_test" };
            Assert.IsNotNull(def.holders);
            Assert.AreEqual(0, def.holders.Count);
        }

        [Test]
        public void SoleAndCoRuler_CanCoexist()
        {
            var def = new TitleDefinition { titleId = "title_test" };
            def.holders.Add(new TitleHolding { characterId = 1, role = TitleHolderRole.Sole, share = 1f, sinceDay = 10 });
            def.holders.Add(new TitleHolding { characterId = 2, role = TitleHolderRole.CoRuler, share = 0.5f, sinceDay = 20 });

            Assert.AreEqual(2, def.holders.Count);
            Assert.AreEqual(TitleHolderRole.Sole, def.holders[0].role);
            Assert.AreEqual(TitleHolderRole.CoRuler, def.holders[1].role);
            Assert.AreEqual(0.5f, def.holders[1].share);
        }

        [Test]
        public void InstitutionCanHoldTitle()
        {
            var holding = new TitleHolding { institutionId = 7, role = TitleHolderRole.Elector, share = 1f };
            Assert.IsTrue(holding.IsInstitutionHolding);
            Assert.AreEqual(1, holding.sinceDay == -1 ? 1 : 0);
        }

        [Test]
        public void HoldingActiveWindow()
        {
            var holding = new TitleHolding { characterId = 3, sinceDay = 10, endDay = 40 };
            Assert.IsTrue(holding.IsActive(20));
            Assert.IsFalse(holding.IsActive(40));
            Assert.IsTrue(new TitleHolding { characterId = 4, sinceDay = 10 }.IsActive(9999));
        }
    }
}
