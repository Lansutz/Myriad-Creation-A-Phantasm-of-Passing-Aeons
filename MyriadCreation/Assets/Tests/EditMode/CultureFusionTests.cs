using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.1 文化融合：少数且非任何政权主文化、整合度足够时被吸收。</summary>
    public class CultureFusionTests
    {
        private static CultureData Culture(int id)
        {
            return new CultureData
            {
                cultureId = id,
                cultureName = "C" + id,
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

        [Test]
        public void SmallIntegratedMinority_IsFused_AndPopulationConverts()
        {
            var cultures = new Dictionary<int, CultureData> { [1] = Culture(1), [2] = Culture(2) };
            var realms = new Dictionary<int, RealmData>
            {
                [0] = new RealmData { realmId = 0, primaryCultureId = 1 }
            };
            var tiles = new[] { Tile(0, Pop(1, 100f, 0f), Pop(2, 10f, 80f)) };
            int fusedMinority = -1;
            int fusedPrimary = -1;

            int fused = CultureFusionSystem.MonthlyTick(realms, cultures, tiles, (m, p) => { fusedMinority = m; fusedPrimary = p; });

            Assert.AreEqual(1, fused);
            Assert.AreEqual(2, fusedMinority);
            Assert.AreEqual(1, fusedPrimary);
            Assert.AreEqual(1, cultures[2].mergedIntoCultureId);
            Assert.AreEqual(1, tiles[0].populationBlocks.Count, "人口应并入主文化块");
            Assert.AreEqual(1, tiles[0].populationBlocks[0].cultureId);
            Assert.AreEqual(110f, tiles[0].populationBlocks[0].count, 0.001f, "人口守恒");
        }

        [Test]
        public void LargeMinority_IsNotFused()
        {
            var cultures = new Dictionary<int, CultureData> { [1] = Culture(1), [2] = Culture(2) };
            var realms = new Dictionary<int, RealmData>
            {
                [0] = new RealmData { realmId = 0, primaryCultureId = 1 }
            };
            var tiles = new[] { Tile(0, Pop(1, 100f, 0f), Pop(2, 50f, 80f)) };

            Assert.AreEqual(0, CultureFusionSystem.MonthlyTick(realms, cultures, tiles));
            Assert.AreEqual(-1, cultures[2].mergedIntoCultureId);
        }

        [Test]
        public void MinorityThatIsPrimaryElsewhere_IsNotFused()
        {
            var cultures = new Dictionary<int, CultureData> { [1] = Culture(1), [2] = Culture(2) };
            var realms = new Dictionary<int, RealmData>
            {
                [0] = new RealmData { realmId = 0, primaryCultureId = 1 },
                [1] = new RealmData { realmId = 1, primaryCultureId = 2 }
            };
            var tiles = new[] { Tile(0, Pop(1, 100f, 0f), Pop(2, 10f, 80f)) };

            Assert.AreEqual(0, CultureFusionSystem.MonthlyTick(realms, cultures, tiles));
            Assert.AreEqual(-1, cultures[2].mergedIntoCultureId);
        }

        [Test]
        public void LowPenetration_IsNotFused()
        {
            var cultures = new Dictionary<int, CultureData> { [1] = Culture(1), [2] = Culture(2) };
            var realms = new Dictionary<int, RealmData>
            {
                [0] = new RealmData { realmId = 0, primaryCultureId = 1 }
            };
            var tiles = new[] { Tile(0, Pop(1, 100f, 0f), Pop(2, 10f, 20f)) };

            Assert.AreEqual(0, CultureFusionSystem.MonthlyTick(realms, cultures, tiles));
        }
    }
}
