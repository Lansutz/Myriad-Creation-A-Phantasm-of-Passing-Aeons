using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.1 文化分裂：有故土、在异政权内抗拒同化且占比足够时派生子文化。</summary>
    public class CultureSplitTests
    {
        private static CultureData Culture(int id, bool allowsBranching = true)
        {
            return new CultureData
            {
                cultureId = id,
                cultureName = "C" + id,
                allowsBranching = allowsBranching,
                maturity = 50f
            };
        }

        private static PopulationBlock Pop(int cultureId, float count, float penetration)
        {
            return new PopulationBlock
            {
                cultureId = cultureId,
                count = count,
                culturePenetration = penetration,
                raceId = 1,
                faithId = 1,
                socialClass = GameEnums.SocialClass.Peasant
            };
        }

        private static TileData Tile(int realmId, params PopulationBlock[] blocks)
        {
            return new TileData
            {
                exists = true,
                isLand = true,
                ownerRealmId = realmId,
                populationBlocks = new List<PopulationBlock>(blocks)
            };
        }

        private static Dictionary<int, RealmData> TwoRealms()
        {
            return new Dictionary<int, RealmData>
            {
                [0] = new RealmData { realmId = 0, realmName = "西境", primaryCultureId = 1 },
                [1] = new RealmData { realmId = 1, realmName = "故土", primaryCultureId = 2 }
            };
        }

        [Test]
        public void ResistingMinorityWithHomeland_SplitsIntoChildCulture()
        {
            var cultures = new Dictionary<int, CultureData> { [1] = Culture(1), [2] = Culture(2) };
            var realms = TwoRealms();
            var tiles = new[] { Tile(0, Pop(1, 100f, 0f), Pop(2, 40f, 10f)) };

            int childId = CultureSplitSystem.MonthlyTick(realms, cultures, tiles, 1000);

            Assert.AreEqual(3, childId);
            Assert.IsTrue(cultures.ContainsKey(3));
            Assert.AreEqual(2, cultures[3].parentCultureId);
            Assert.IsFalse(cultures[3].allowsBranching, "子文化不再继续分裂");
            CollectionAssert.Contains(cultures[2].childCultureIds, 3);
            Assert.AreEqual(1000, cultures[2].lastSplitDay);
            Assert.AreEqual(3, tiles[0].populationBlocks[1].cultureId, "宿主政权内的人口改挂子文化");
        }

        [Test]
        public void Cooldown_PreventsImmediateSecondSplit()
        {
            var cultures = new Dictionary<int, CultureData> { [1] = Culture(1), [2] = Culture(2) };
            var realms = TwoRealms();
            var tiles = new[] { Tile(0, Pop(1, 100f, 0f), Pop(2, 40f, 10f)) };

            Assert.AreEqual(3, CultureSplitSystem.MonthlyTick(realms, cultures, tiles, 1000));
            Assert.AreEqual(-1, CultureSplitSystem.MonthlyTick(realms, cultures, tiles, 1100));
        }

        [Test]
        public void NoHomeland_DoesNotSplit()
        {
            var cultures = new Dictionary<int, CultureData> { [1] = Culture(1), [2] = Culture(2) };
            var realms = new Dictionary<int, RealmData>
            {
                [0] = new RealmData { realmId = 0, primaryCultureId = 1 }
            };
            var tiles = new[] { Tile(0, Pop(1, 100f, 0f), Pop(2, 40f, 10f)) };

            Assert.AreEqual(-1, CultureSplitSystem.MonthlyTick(realms, cultures, tiles, 1000));
        }

        [Test]
        public void IntegratedMinority_DoesNotSplit()
        {
            var cultures = new Dictionary<int, CultureData> { [1] = Culture(1), [2] = Culture(2) };
            var realms = TwoRealms();
            var tiles = new[] { Tile(0, Pop(1, 100f, 0f), Pop(2, 40f, 80f)) };

            Assert.AreEqual(-1, CultureSplitSystem.MonthlyTick(realms, cultures, tiles, 1000));
        }

        [Test]
        public void TinyMinority_DoesNotSplit()
        {
            var cultures = new Dictionary<int, CultureData> { [1] = Culture(1), [2] = Culture(2) };
            var realms = TwoRealms();
            var tiles = new[] { Tile(0, Pop(1, 100f, 0f), Pop(2, 10f, 10f)) };

            Assert.AreEqual(-1, CultureSplitSystem.MonthlyTick(realms, cultures, tiles, 1000));
        }
    }
}
