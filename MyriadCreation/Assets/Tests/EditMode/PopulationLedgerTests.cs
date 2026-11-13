using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.5 C：人口块增删/转移/改身份走单一入口，人口守恒。</summary>
    public class PopulationLedgerTests
    {
        private static PopulationBlock Pop(int culture, int faith, float count, GameEnums.SocialClass cls = GameEnums.SocialClass.Peasant)
        {
            return new PopulationBlock
            {
                cultureId = culture,
                faithId = faith,
                count = count,
                raceId = 1,
                socialClass = cls
            };
        }

        [Test]
        public void AddCount_MergesIntoFirstBlock_AndConserves()
        {
            var tile = new TileData { populationBlocks = new List<PopulationBlock> { Pop(1, 1, 100f) } };
            PopulationLedger.AddCount(ref tile, 25f);
            Assert.AreEqual(125f, PopulationLedger.Total(tile), 0.001f);
            Assert.AreEqual(1, tile.populationBlocks.Count);

            PopulationLedger.AddCount(ref tile, -50f);
            Assert.AreEqual(75f, PopulationLedger.Total(tile), 0.001f);
        }

        [Test]
        public void Add_MergesSameIdentity_ElseAppends()
        {
            var blocks = new List<PopulationBlock> { Pop(1, 1, 10f) };
            PopulationLedger.Add(blocks, Pop(1, 1, 5f));
            Assert.AreEqual(1, blocks.Count);
            Assert.AreEqual(15f, blocks[0].count, 0.001f);

            PopulationLedger.Add(blocks, Pop(2, 1, 7f));
            Assert.AreEqual(2, blocks.Count);
        }

        [Test]
        public void SplitToNewBlock_ConservesTotal()
        {
            var blocks = new List<PopulationBlock> { Pop(1, 1, 100f) };
            var template = Pop(1, 2, 0f);
            PopulationLedger.SplitToNewBlock(blocks, 0, 30f, template);

            Assert.AreEqual(100f, blocks[0].count + blocks[1].count, 0.001f);
            Assert.AreEqual(2, blocks[1].faithId);
        }

        [Test]
        public void ChangeCulture_MergesIntoTarget()
        {
            var blocks = new List<PopulationBlock> { Pop(1, 1, 100f), Pop(2, 1, 40f) };
            PopulationLedger.ChangeCulture(blocks, 1, 1, 100f);

            Assert.AreEqual(1, blocks.Count);
            Assert.AreEqual(140f, blocks[0].count, 0.001f);
            Assert.AreEqual(100f, blocks[0].culturePenetration, 0.001f);
        }

        [Test]
        public void RemoveFromIndex_AndPrune()
        {
            var blocks = new List<PopulationBlock> { Pop(1, 1, 10f), Pop(2, 1, 0.01f) };
            PopulationLedger.RemoveFromIndex(blocks, 0, 4f);
            Assert.AreEqual(6f, blocks[0].count, 0.001f);

            PopulationLedger.Prune(blocks);
            Assert.AreEqual(1, blocks.Count);
        }
    }
}
