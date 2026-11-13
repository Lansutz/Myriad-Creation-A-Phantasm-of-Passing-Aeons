using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P3.4 城市俯视图布局：按区块面积比例切分、确定性、可重算（不存档）。
    /// </summary>
    public class CityLayoutTests
    {
        private static SettlementBlock Block(string id, float area, SettlementBlockKind kind = SettlementBlockKind.District,
            CityDistrictType district = CityDistrictType.None, int wallLayers = 0)
        {
            return new SettlementBlock
            {
                blockId = id,
                areaRatio = area,
                kind = kind,
                districtType = district,
                wallLayerCount = wallLayers,
                enabled = true
            };
        }

        [Test]
        public void Build_AllocatesEveryEnabledBlockInsideMap()
        {
            var blocks = new List<SettlementBlock>
            {
                Block("a", 2f),
                Block("b", 1f),
                Block("c", 1f)
            };

            CityLayoutPlan plan = CityLayoutGenerator.Build(blocks, 100, 60);

            Assert.AreEqual(3, plan.lots.Count, "每个启用区块都应有一块分区");
            foreach (CityLot lot in plan.lots)
            {
                Assert.GreaterOrEqual(lot.rect.x, 0);
                Assert.LessOrEqual(lot.rect.x + lot.rect.width, 100, "分区不得越界");
                Assert.AreEqual(60, lot.rect.height);
            }
        }

        [Test]
        public void Build_WeightsLotsByAreaRatio()
        {
            var blocks = new List<SettlementBlock>
            {
                Block("big", 3f),
                Block("small", 1f)
            };

            CityLayoutPlan plan = CityLayoutGenerator.Build(blocks, 100, 100);

            CityLot big = plan.lots.Find(l => l.blockId == "big");
            CityLot small = plan.lots.Find(l => l.blockId == "small");
            Assert.IsNotNull(big);
            Assert.IsNotNull(small);
            Assert.Greater(big.rect.width, small.rect.width, "面积大的区块应分到更宽的分区");
        }

        [Test]
        public void Build_IsDeterministic()
        {
            var blocks = new List<SettlementBlock> { Block("b", 1f), Block("a", 2f) };

            CityLayoutPlan first = CityLayoutGenerator.Build(blocks, 80, 80);
            CityLayoutPlan second = CityLayoutGenerator.Build(blocks, 80, 80);

            Assert.AreEqual(first.lots.Count, second.lots.Count);
            for (int i = 0; i < first.lots.Count; i++)
            {
                Assert.AreEqual(first.lots[i].blockId, second.lots[i].blockId, "顺序必须按 blockId 稳定");
                Assert.AreEqual(first.lots[i].rect.x, second.lots[i].rect.x);
                Assert.AreEqual(first.lots[i].rect.width, second.lots[i].rect.width);
            }
        }

        [Test]
        public void Build_EmitsWallRingOnlyWhenWallBlockExists()
        {
            var withoutWall = new List<SettlementBlock> { Block("a", 1f) };
            var withWall = new List<SettlementBlock>
            {
                Block("a", 1f),
                Block("wall", 0.2f, SettlementBlockKind.Wall, CityDistrictType.None, 2)
            };

            Assert.IsFalse(CityLayoutGenerator.Build(withoutWall).hasWall, "没有城墙区块就没有外环");
            CityLayoutPlan plan = CityLayoutGenerator.Build(withWall);
            Assert.IsTrue(plan.hasWall, "有城墙区块应生成外环");
            Assert.AreEqual(100, plan.wall.width);
            Assert.AreEqual(100, plan.wall.height);
        }

        [Test]
        public void Build_EmptySettlementIsValid()
        {
            CityLayoutPlan plan = CityLayoutGenerator.Build(new List<SettlementBlock>());

            Assert.AreEqual(0, plan.lots.Count, "空聚落是合法状态，不应抛出");
        }
    }
}
