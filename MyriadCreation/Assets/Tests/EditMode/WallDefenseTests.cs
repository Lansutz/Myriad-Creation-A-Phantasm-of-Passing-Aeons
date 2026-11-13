using NUnit.Framework;
using MyriadCreation.World.Settlement;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>城墙长度与守军匹配：墙越长需求越高，守军不足会摊薄防御。</summary>
    public class WallDefenseTests
    {
        private static SettlementData City(int population, int garrison, int layers = 1)
        {
            var city = new SettlementData
            {
                siteMode = SiteMode.SedentarySettlement,
                settlementLevel = SettlementLevel.LevelIII,
                settlementForm = SettlementForm.City,
                cityForm = CityForm.Circular,
                population = population,
                garrison = garrison,
                fortification = 5f,
                fortificationLevel = FortificationLevels.StoneWall,
            };
            var wall = city.AddBlock(SettlementBlockKind.Wall, null, FortificationLevels.StoneWall);
            wall.wallLayerCount = layers;
            wall.wallLayout = WallLayout.Ring;
            return city;
        }

        [Test]
        public void WallLength_ScalesWithSettlementAndPopulation()
        {
            var small = City(2000, 400);
            var big = City(50000, 400);
            var smallSnap = WallDefense.Evaluate(small);
            var bigSnap = WallDefense.Evaluate(big);

            Assert.IsTrue(smallSnap.hasWall);
            Assert.Greater(smallSnap.wallLengthMeters, 1000f);
            Assert.Greater(bigSnap.wallLengthMeters, smallSnap.wallLengthMeters,
                "人口更大的城，墙更长");
            Assert.Greater(bigSnap.requiredGarrison, smallSnap.requiredGarrison);
        }

        [Test]
        public void UnderGarrison_ReducesEffectiveDefense()
        {
            var full = WallDefense.Evaluate(City(10000, 2000));
            var thin = WallDefense.Evaluate(City(10000, 100));

            Assert.Less(thin.coverage, 0.5f);
            Assert.Less(thin.effectiveFortification, full.effectiveFortification);
            Assert.Greater(thin.unguardedLengthMeters, 0f, "墙长超过守军覆盖，出现空段");
        }

        [Test]
        public void MultipleCircuits_NeedMoreGarrison()
        {
            var single = WallDefense.Evaluate(City(20000, 1000, 1));
            var triple = WallDefense.Evaluate(City(20000, 1000, 3));
            Assert.Greater(triple.wallLengthMeters, single.wallLengthMeters);
            Assert.Greater(triple.requiredGarrison, single.requiredGarrison);
        }

        [Test]
        public void ExplicitLength_IsRespected()
        {
            var city = City(10000, 500);
            city.GetBlock(SettlementBlockKind.Wall).length = 3000f;
            var snap = WallDefense.Evaluate(city);
            Assert.AreEqual(3000f, snap.wallLengthMeters, 0.01f);
            Assert.Greater(snap.requiredGarrison, 0);
        }

        [Test]
        public void Overcrowding_IsFlagged()
        {
            var city = City(1000, 5000);
            var snap = WallDefense.Evaluate(city);
            Assert.IsTrue(snap.overcrowded, "守军远超需求，补给/拥挤");
        }
    }
}
