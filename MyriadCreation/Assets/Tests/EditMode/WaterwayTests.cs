using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Units;
using MyriadCreation.World.Hydrology;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P9/M4：河流与海洋同属水体；可通行判定单一来源；河网连通与合流次序可验证。
    /// </summary>
    public class WaterwayTests
    {
        private static TileData Land(float elevation = 0.5f, float precip = 800f)
        {
            return new TileData { exists = true, isLand = true, elevation01 = elevation, annualPrecipMm = precip };
        }

        private static TileData Sea(GameEnums.OceanTier tier)
        {
            return new TileData { exists = true, isLand = false, oceanTier = tier, elevation01 = 0f };
        }

        [Test]
        public void Classify_SeparatesOceanCoastRiverLand()
        {
            Assert.AreEqual(WaterBodyKind.Ocean, RiverNavigability.Classify(Sea(GameEnums.OceanTier.MidSea)));
            Assert.AreEqual(WaterBodyKind.Coast, RiverNavigability.Classify(Sea(GameEnums.OceanTier.Coast)));
            var river = Land();
            river.isRiver = true;
            river.riverStreamOrder = 3;
            Assert.AreEqual(WaterBodyKind.River, RiverNavigability.Classify(river));
            Assert.AreEqual(WaterBodyKind.None, RiverNavigability.Classify(Land()));
        }

        [Test]
        public void IsBoatPassable_OceanAndNavigableRiverOnly()
        {
            Assert.IsTrue(RiverNavigability.IsBoatPassable(Sea(GameEnums.OceanTier.Coast)));
            Assert.IsTrue(RiverNavigability.IsBoatPassable(Sea(GameEnums.OceanTier.DeepSea)));

            var deepRiver = Land();
            deepRiver.isRiver = true;
            deepRiver.riverStreamOrder = 2;
            Assert.IsTrue(RiverNavigability.IsBoatPassable(deepRiver));

            var shallowRiver = Land();
            shallowRiver.isRiver = true;
            shallowRiver.riverStreamOrder = 1;
            shallowRiver.riverWidthMeters = 5f;
            shallowRiver.riverDepthMeters = 0.3f;
            Assert.IsFalse(RiverNavigability.IsBoatPassable(shallowRiver));

            Assert.IsFalse(RiverNavigability.IsBoatPassable(Land()));
        }

        [Test]
        public void TerrainConditions_UsesSameNavigabilitySource()
        {
            var river = Land();
            river.isRiver = true;
            river.riverStreamOrder = 2;
            river.riverWidthMeters = 30f;
            river.riverDepthMeters = 1.2f;
            Assert.IsTrue(TerrainConditions.FromTile(river).IsNavigableRiver);

            var dry = Land();
            dry.isRiver = false;
            Assert.IsFalse(TerrainConditions.FromTile(dry).IsNavigableRiver);
        }

        [Test]
        public void RouteEfficiency_WaterIgnoresRoadLevel()
        {
            var tiles = new[] { Land(), Land() };
            tiles[0].roadLevel = 0;
            tiles[1].roadLevel = 0;

            var landRoute = new TradeRoute
            {
                nodeTileIndices = new System.Collections.Generic.List<int> { 0, 1 },
                transportMode = TransportMode.Land
            };
            var waterRoute = new TradeRoute
            {
                nodeTileIndices = new System.Collections.Generic.List<int> { 0, 1 },
                transportMode = TransportMode.River
            };
            landRoute.CalculateEfficiency(tiles);
            waterRoute.CalculateEfficiency(tiles);

            Assert.Greater(waterRoute.currentEfficiency, landRoute.currentEfficiency,
                "没有道路时，水路不应被道路等级拖累");
        }

        [Test]
        public void RouteMode_DerivesFromRealWaterConnectivity()
        {
            const int W = 5, H = 5;
            var tiles = new TileData[W * H];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = Land();
            for (int x = 0; x < W; x++) tiles[4 * W + x] = Sea(GameEnums.OceanTier.Coast);
            for (int y = 0; y < 4; y++)
            {
                int i = y * W + 2;
                tiles[i].isRiver = true;
                tiles[i].riverStreamOrder = 2;
                tiles[i].riverWidthMeters = 25f;
                tiles[i].riverDepthMeters = 1f;
            }

            // 两个沿海聚落经海路相连。
            Assert.AreEqual(TransportMode.Sea, RiverNavigability.ClassifyRoute(tiles, W, H, 3 * W + 0, 3 * W + 4));
            // 内陆河道两侧（2 列相邻）彼此连通，且不经过海。
            Assert.AreEqual(TransportMode.River, RiverNavigability.ClassifyRoute(tiles, W, H, 1 * W + 1, 1 * W + 3));
            // 两个内陆旱地聚落没有水路。
            Assert.AreEqual(TransportMode.Land, RiverNavigability.ClassifyRoute(tiles, W, H, 0 * W + 0, 1 * W + 4));
        }

        [Test]
        public void RiverNetwork_FlagsButSeparatesOrphanReach()
        {
            const int W = 5, H = 5;
            var tiles = new TileData[W * H];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = Land();

            // 底行是海；中间一列是入海河道。
            for (int x = 0; x < W; x++) tiles[4 * W + x] = Sea(GameEnums.OceanTier.Coast);
            for (int y = 0; y < 4; y++)
            {
                int index = y * W + 2;
                tiles[index].isRiver = true;
                tiles[index].riverStreamOrder = 2;
            }
            Assert.AreEqual(0, RiverNetworkAudit.CountRiverTilesWithoutOutlet(tiles, W, H));

            // 左上角一条完全封闭的河道没有出海口。
            tiles[0].isRiver = true;
            Assert.AreEqual(1, RiverNetworkAudit.CountRiverTilesWithoutOutlet(tiles, W, H));
        }

        [Test]
        public void ApplyToTiles_WritesHydraulicFields()
        {
            const int W = 7, H = 8;
            var elevation = new float[W * H];
            var isLand = new bool[W * H];
            for (int i = 0; i < W * H; i++) { elevation[i] = 10f; isLand[i] = true; }
            for (int x = 0; x < W; x++)
            {
                int i = (H - 1) * W + x;
                elevation[i] = 0f;
                isLand[i] = false;
            }
            (int x, int y, float e)[] channel =
            {
                (1, 2, 9f), (2, 3, 8f),
                (5, 2, 9f), (4, 3, 8f),
                (3, 4, 7f), (3, 5, 6f), (3, 6, 5f)
            };
            foreach (var (x, y, e) in channel) elevation[y * W + x] = e;

            var hydrology = new HydrologySystem(W, H, wrapX: false);
            hydrology.Run(elevation, isLand, riverThreshold: 2f);
            var tiles = new TileData[W * H];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = new TileData { exists = true, isLand = isLand[i] };
            hydrology.ApplyToTiles(tiles);

            int rivers = 0;
            int navigable = 0;
            foreach (var tile in tiles)
            {
                if (!tile.isRiver) continue;
                rivers++;
                Assert.Greater(tile.riverWidthMeters, 0f, "河宽必须有生产者");
                Assert.Greater(tile.riverDepthMeters, 0f, "河深必须有生产者");
                Assert.Greater(tile.riverStreamOrder, 0, "河序必须有生产者");
                if (RiverNavigability.IsBoatPassable(tile)) navigable++;
            }
            Assert.Greater(rivers, 0, "测试地形应生成河道");
            Assert.Greater(navigable, 0, "至少一段河道应可通航");
        }

        [Test]
        public void Confluence_HasOrderNotBelowTributaries()
        {
            const int W = 7, H = 8;
            var elevation = new float[W * H];
            var isLand = new bool[W * H];
            for (int i = 0; i < W * H; i++)
            {
                elevation[i] = 10f;
                isLand[i] = true;
            }
            // 底行为海；两条支流沿 y 字汇入第 3 列，再入海。
            for (int x = 0; x < W; x++)
            {
                int i = (H - 1) * W + x;
                elevation[i] = 0f;
                isLand[i] = false;
            }
            (int x, int y, float e)[] channel =
            {
                (1, 2, 9f), (2, 3, 8f),
                (5, 2, 9f), (4, 3, 8f),
                (3, 4, 7f), (3, 5, 6f), (3, 6, 5f)
            };
            foreach (var (x, y, e) in channel) elevation[y * W + x] = e;

            var hydrology = new HydrologySystem(W, H, wrapX: false);
            hydrology.Run(elevation, isLand, riverThreshold: 2f);

            int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
            int[] dy = { -1, -1, 0, 1, 1, 1, 0, -1 };
            int[] donors = new int[W * H];
            int[] downstreamOf = new int[W * H];
            for (int i = 0; i < W * H; i++)
            {
                downstreamOf[i] = -1;
                int dir = hydrology.FlowDirection[i];
                if (dir < 0 || dir > 7) continue;
                int x = i % W + dx[dir];
                int y = i / W + dy[dir];
                if (x < 0 || x >= W || y < 0 || y >= H) continue;
                int downstream = y * W + x;
                downstreamOf[i] = downstream;
                donors[downstream]++;
            }

            int confluences = 0;
            for (int i = 0; i < W * H; i++)
            {
                if (!hydrology.IsRiver[i] || donors[i] < 2) continue;
                confluences++;
                int maxUpstream = 0;
                for (int j = 0; j < W * H; j++)
                {
                    if (downstreamOf[j] == i && hydrology.IsRiver[j])
                    {
                        if (hydrology.StreamOrder[j] > maxUpstream) maxUpstream = hydrology.StreamOrder[j];
                    }
                }
                Assert.GreaterOrEqual(hydrology.StreamOrder[i], maxUpstream,
                    "汇流点的河序不应低于任一上游支流");
            }

            Assert.Greater(confluences, 0, "测试地形应形成至少一个汇流点");
        }
    }
}
