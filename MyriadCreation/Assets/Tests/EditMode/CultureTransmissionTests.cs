using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.1 文化传播：阻力取既有公式，进度满 100 才换文化并合并同属性人口块。</summary>
    public class CultureTransmissionTests
    {
        private static CultureData Culture(int id, int livelihood = 0, int mobility = 0)
        {
            return new CultureData
            {
                cultureId = id,
                cultureName = "C" + id,
                livelihoodType = livelihood,
                mobilityType = mobility,
                burialType = 0,
                materialStyle = 0,
                symbolicFocus = 0,
                environmentAdapt = 0,
                maturity = 50f
            };
        }

        private static TileData Tile(int ownerRealmId, params PopulationBlock[] blocks)
        {
            return new TileData
            {
                tileIndex = 0,
                exists = true,
                isLand = true,
                ownerRealmId = ownerRealmId,
                populationBlocks = new List<PopulationBlock>(blocks)
            };
        }

        private static PopulationBlock Pop(int cultureId, float count)
        {
            return new PopulationBlock
            {
                cultureId = cultureId,
                count = count,
                raceId = 1,
                faithId = 1,
                socialClass = GameEnums.SocialClass.Peasant
            };
        }

        [Test]
        public void SimilarCulture_Assimilates_AndMergesIntoExistingBlock()
        {
            var cultures = new Dictionary<int, CultureData>
            {
                [1] = Culture(1),
                [2] = Culture(2)
            };
            var realms = new Dictionary<int, RealmData>
            {
                [0] = new RealmData { realmId = 0, primaryCultureId = 1 }
            };
            var tiles = new[]
            {
                Tile(0, Pop(1, 100f), Pop(2, 50f))
            };

            int converted = 0;
            for (int month = 0; month < 40 && converted == 0; month++)
            {
                converted = CultureTransmissionSystem.MonthlyTick(realms, cultures, tiles);
            }

            Assert.AreEqual(1, converted, "相似文化应在进度满后完成转换");
            Assert.AreEqual(1, tiles[0].populationBlocks.Count, "转换后应并入同属性人口块");
            Assert.AreEqual(1, tiles[0].populationBlocks[0].cultureId);
            Assert.AreEqual(150f, tiles[0].populationBlocks[0].count, 0.001f, "人口守恒");
        }

        [Test]
        public void DissimilarCulture_DoesNotAssimilate()
        {
            CultureData target = Culture(1, livelihood: 0, mobility: 0);
            target.worshipVector = new[] { 1f, 0f };
            CultureData source = Culture(2, livelihood: 4, mobility: 3);
            source.worshipVector = new[] { 0f, 1f };
            source.burialType = 3;
            source.materialStyle = 3;
            source.symbolicFocus = 3;
            source.environmentAdapt = 3;
            var cultures = new Dictionary<int, CultureData>
            {
                [1] = target,
                [2] = source
            };
            var realms = new Dictionary<int, RealmData>
            {
                [0] = new RealmData { realmId = 0, primaryCultureId = 1 }
            };
            var tiles = new[]
            {
                Tile(0, Pop(2, 50f))
            };

            int converted = 0;
            for (int month = 0; month < 60; month++)
            {
                converted += CultureTransmissionSystem.MonthlyTick(realms, cultures, tiles);
            }

            Assert.AreEqual(0, converted, "异质文化不应自动同化");
            Assert.AreEqual(2, tiles[0].populationBlocks[0].cultureId);
        }

        [Test]
        public void NoOwnerRealm_IsSkipped()
        {
            var cultures = new Dictionary<int, CultureData> { [1] = Culture(1), [2] = Culture(2) };
            var realms = new Dictionary<int, RealmData>();
            var tiles = new[] { Tile(-1, Pop(2, 50f)) };

            Assert.AreEqual(0, CultureTransmissionSystem.MonthlyTick(realms, cultures, tiles));
        }
    }
}
