using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>聚落影响力：地形/距离/巡逻预算产生不同档位。</summary>
    public class SettlementInfluenceTests
    {
        private static TileData[] FlatMap(int w, int h)
        {
            var tiles = new TileData[w * h];
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = new TileData { exists = true, isLand = true, movementCost = 1f };
            return tiles;
        }

        private static SettlementData Settlement(int id, int tile, SettlementLevel level,
            SettlementForm form, float population, int garrison)
        {
            return new SettlementData
            {
                anchorId = id,
                settlementName = "聚落" + id,
                settlementLevel = level,
                settlementForm = form,
                siteMode = SiteMode.SedentarySettlement,
                population = population,
                garrison = garrison,
                anchor = new AnchorData { anchorId = id, tileIndex = tile },
            };
        }

        private static (SettlementData controller, SettlementData target, TileData[] tiles) Setup(
            bool mountainBetween)
        {
            int w = 11, h = 11;
            var tiles = FlatMap(w, h);
            var controller = Settlement(1, 5 * w + 5, SettlementLevel.LevelIII,
                SettlementForm.City, 2000f, 200);
            var target = Settlement(2, 5 * w + 7, SettlementLevel.LevelI,
                SettlementForm.Village, 100f, 0);
            controller.anchor.controlledAnchorIds.Add(target.anchorId);
            if (mountainBetween)
            {
                int mid = 5 * w + 6;
                tiles[mid].movementCost = 6f;
                tiles[mid].slopeDegree = 70f;
            }
            return (controller, target, tiles);
        }

        [Test]
        public void Influence_TierFallsWithTerrainCost()
        {
            var settings = new InfluenceSettings
            {
                baseBudget = 2f, levelBudget = 1f,
                populationBudgetFactor = 0f, garrisonBudgetFactor = 0f,
                maxBudget = 20f,
            };
            var flat = Setup(false);
            var flatField = SettlementInfluenceSystem.Calculate(
                new System.Collections.Generic.Dictionary<int, SettlementData>
                {
                    [flat.controller.anchorId] = flat.controller,
                    [flat.target.anchorId] = flat.target,
                }, flat.tiles, 11, 11, settings);

            var hill = Setup(true);
            var hillField = SettlementInfluenceSystem.Calculate(
                new System.Collections.Generic.Dictionary<int, SettlementData>
                {
                    [hill.controller.anchorId] = hill.controller,
                    [hill.target.anchorId] = hill.target,
                }, hill.tiles, 11, 11, settings);

            int targetTile = flat.target.tileIndex;
            Assert.GreaterOrEqual(flatField.tierByTile[targetTile], (byte)InfluenceTier.Contested);
            Assert.Greater(flatField.tierByTile[targetTile], hillField.tierByTile[targetTile],
                "山地阻断后档位必须下降");
            Assert.AreEqual((int)InfluenceTier.None, 0);
        }

        [Test]
        public void Influence_WritesControlTierToSubordinateAnchor()
        {
            var (controller, target, tiles) = Setup(false);
            var settlements = new System.Collections.Generic.Dictionary<int, SettlementData>
            {
                [controller.anchorId] = controller,
                [target.anchorId] = target,
            };
            SettlementInfluenceSystem.UpdateControlTiers(settlements, tiles, 11, 11);
            Assert.Greater(target.anchor.influenceTier, (int)InfluenceTier.None);
        }

        [Test]
        public void TerrainCost_RoadAndRiverChangeCost()
        {
            var flat = new TileData { exists = true, isLand = true, movementCost = 1f };
            var road = flat; road.roadLevel = GameEnums.RoadLevel.DirtRoad;
            var river = flat; river.isRiver = true; river.riverDepthMeters = 1f;
            float flatCost = SettlementInfluenceSystem.TerrainCost(flat, new InfluenceSettings());
            Assert.Less(SettlementInfluenceSystem.TerrainCost(road, new InfluenceSettings()), flatCost);
            Assert.Greater(SettlementInfluenceSystem.TerrainCost(river, new InfluenceSettings()), flatCost);
        }
    }
}
