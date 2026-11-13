using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.UI.Presentation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>栖居点人口构成与构成面板模型。</summary>
    public class PopulationCompositionTests
    {
        private static PopulationComposition Sample()
        {
            var c = new PopulationComposition();
            c.Add(60f, raceId: 1, cultureId: 10, faithId: 20,
                EconomicClass.Freeholder, SocialStratum.Middle, OccupationFlags.Farmer,
                religionTraditionId: 101);
            c.Add(30f, raceId: 1, cultureId: 11, faithId: 21,
                EconomicClass.Tenant, SocialStratum.LowerMiddle, OccupationFlags.Artisan,
                religionTraditionId: 102);
            c.Add(10f, raceId: 2, cultureId: 10, faithId: 21,
                EconomicClass.HiredLaborer, SocialStratum.Lower, OccupationFlags.Laborer,
                religionTraditionId: 102);
            return c;
        }

        [Test]
        public void Composition_AggregatesAndFindsDominant()
        {
            var c = Sample();
            Assert.AreEqual(100f, c.Total, 0.01f);
            Assert.AreEqual(70f, c.ByCulture()[10], 0.01f);
            Assert.AreEqual(40f, c.ByFaith()[21], 0.01f);
            Assert.AreEqual(10, c.DominantCulture());
            Assert.AreEqual(20, c.DominantFaith(), "信仰 20 占 60，多于 21 的 40");
            Assert.AreEqual(101, c.DominantTradition(), "只有一条带传统细分");
        }

        [Test]
        public void Composition_FromBlocks()
        {
            var c = new PopulationComposition();
            c.AddBlocks(new[]
            {
                new PopulationBlock
                {
                    count = 40f, raceId = 1, cultureId = 3, faithId = 4,
                    economicClass = EconomicClass.Freeholder, stratum = SocialStratum.Middle,
                },
                new PopulationBlock
                {
                    count = 10f, raceId = 1, cultureId = 3, faithId = 5,
                    economicClass = EconomicClass.Freeholder, stratum = SocialStratum.Middle,
                },
            });
            Assert.AreEqual(50f, c.Total, 0.01f);
            Assert.AreEqual(3, c.DominantCulture());
            Assert.AreEqual(4, c.DominantFaith());
        }

        [Test]
        public void PanelModel_RowsSortedWithDominant()
        {
            var model = PopulationCompositionPanelModel.Build(
                Sample(), PopulationCompositionCategory.Culture,
                id => "文化" + id, id => "#112233", "测试城");

            Assert.AreEqual(100f, model.total, 0.01f);
            Assert.AreEqual(2, model.rows.Count);
            Assert.AreEqual(10, model.rows[0].id);
            Assert.AreEqual(70f, model.rows[0].percent, 0.01f);
            Assert.IsTrue(model.rows[0].isDominant);
            Assert.IsFalse(model.rows[1].isDominant);
            Assert.AreEqual("文化10", model.rows[0].name);
        }

        [Test]
        public void PanelModel_ClassAndOccupationTabs()
        {
            var classes = PopulationCompositionPanelModel.Build(
                Sample(), PopulationCompositionCategory.EconomicClass,
                id => ((EconomicClass)id).ToString(), _ => "#fff");
            Assert.AreEqual(3, classes.rows.Count);

            var occupations = PopulationCompositionPanelModel.Build(
                Sample(), PopulationCompositionCategory.Occupation,
                id => ((OccupationFlags)id).ToString(), _ => "#fff");
            Assert.GreaterOrEqual(occupations.rows.Count, 3);

            var traditions = PopulationCompositionPanelModel.Build(
                Sample(), PopulationCompositionCategory.ReligionTradition,
                id => "传统" + id, _ => "#fff");
            Assert.AreEqual(2, traditions.rows.Count, "20/21 回退到 faith，101 为传统");
        }
    }
}
