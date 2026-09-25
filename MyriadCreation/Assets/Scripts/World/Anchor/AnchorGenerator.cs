using System;
using System.Collections.Generic;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.World.Anchor
{
    /// <summary>
    /// 锚点生成器：在地图上放置锚点（空间），并初始化对应的聚居点（游戏内容）。
    /// 返回两个字典：锚点字典（空间）和聚居点字典（内容），通过 anchorId 一一对应。
    /// </summary>
    public class AnchorGenerator
    {
        private readonly TileData[] _tiles;
        private readonly int _width;
        private readonly int _height;
        private readonly Dictionary<int, Province> _provinces;
        private readonly System.Random _rng;

        public const int MinBurgsPerProvince = 1;
        public const int MaxBurgsPerProvince = 6;
        public const float PortSpawnChance = 0.6f;
        public const float CitySpawnChance = 0.35f;
        public const float FortressSpawnChance = 0.25f;

        public AnchorGenerator(TileData[] tiles, int width, int height,
            Dictionary<int, Province> provinces, int seed)
        {
            _tiles = tiles;
            _width = width;
            _height = height;
            _provinces = provinces;
            _rng = new System.Random(seed + 999);
        }

        /// <summary>生成锚点+聚居点对。返回 (anchors, settlements)。</summary>
        public (Dictionary<int, AnchorData> anchors, Dictionary<int, SettlementData> settlements) Generate()
        {
            var anchors = new Dictionary<int, AnchorData>();
            var settlements = new Dictionary<int, SettlementData>();
            int nextId = 0;

            foreach (var kv in _provinces)
            {
                int provinceId = kv.Key;
                Province province = kv.Value;
                if (province.memberTiles.Count == 0) continue;

                int centerTile = province.centerTileIndex;
                if (centerTile < 0 || centerTile >= _tiles.Length)
                    centerTile = province.memberTiles[0];

                var (anchor, settlement) = CreatePair(ref nextId, provinceId, centerTile,
                    IsProvinceCenter(province, centerTile) ? SettlementType.City : SettlementType.Village);
                settlement.hasMarket = true;
                settlement.development = 20f + (float)_rng.NextDouble() * 30f;
                settlement.population = 500f + (float)_rng.NextDouble() * 1500f;
                anchors[anchor.anchorId] = anchor;
                settlements[settlement.anchorId] = settlement;

                if (HasCoastalTile(province))
                {
                    int coastalTile = FindCoastalTile(province);
                    if (coastalTile >= 0 && _rng.NextDouble() < PortSpawnChance)
                    {
                        var (a, s) = CreatePair(ref nextId, provinceId, coastalTile, SettlementType.City);
                        s.isPort = true;
                        s.isCoastal = true;
                        s.hasMarket = true;
                        s.tradePower = 30f + (float)_rng.NextDouble() * 50f;
                        s.development = 15f + (float)_rng.NextDouble() * 25f;
                        s.population = 300f + (float)_rng.NextDouble() * 1000f;
                        anchors[a.anchorId] = a;
                        settlements[s.anchorId] = s;
                    }
                }

                if (IsBorderProvince(province) && _rng.NextDouble() < FortressSpawnChance)
                {
                    int borderTile = FindBorderTile(province);
                    if (borderTile >= 0)
                    {
                        var (a, s) = CreatePair(ref nextId, provinceId, borderTile, SettlementType.Fort);
                        s.fortification = 3f + (float)_rng.NextDouble() * 5f;
                        s.garrison = 100 + _rng.Next(200);
                        s.development = 5f + (float)_rng.NextDouble() * 15f;
                        anchors[a.anchorId] = a;
                        settlements[s.anchorId] = s;
                    }
                }

                int extraVillages = Math.Min(MaxBurgsPerProvince - 3,
                    province.memberTiles.Count / 40);
                for (int v = 0; v < extraVillages; v++)
                {
                    int tile = province.memberTiles[_rng.Next(province.memberTiles.Count)];
                    if (IsTileOccupied(anchors, tile)) continue;
                    if (!_tiles[tile].isLand) continue;

                    var (a, s) = CreatePair(ref nextId, provinceId, tile, SettlementType.Village);
                    s.development = 2f + (float)_rng.NextDouble() * 10f;
                    s.population = 50f + (float)_rng.NextDouble() * 300f;
                    anchors[a.anchorId] = a;
                    settlements[s.anchorId] = s;
                }
            }

            return (anchors, settlements);
        }

        private (AnchorData, SettlementData) CreatePair(ref int nextId, int provinceId, int tileIndex, SettlementType type)
        {
            ref TileData tile = ref _tiles[tileIndex];
            int id = nextId++;

            var anchor = new AnchorData
            {
                anchorId = id,
                provinceId = provinceId,
                tileIndex = tileIndex,
                x = 0.5f,
                y = 0.5f,
            };

            var settlement = new SettlementData
            {
                anchorId = id,
                settlementName = GenerateName(tile, type),
                isCoastal = tile.isCoast,
                settlementCategory = type == SettlementType.Fort ? SettlementCategory.Outpost : SettlementCategory.Burg,
                constructionProgress = 0f,
                constructionTier = 1
            };

            SettlementTypologySystem.DeriveInitialType(settlement, tile, _width, _height);
            EconomicCompositionSystem.InitializeComposition(settlement, tile);

            settlement.settlementType = type;
            settlement.settlementLevel = type switch
            {
                SettlementType.City => SettlementLevel.LevelIII,
                SettlementType.Village => SettlementLevel.LevelII,
                SettlementType.Fort => SettlementLevel.LevelII,
                _ => SettlementLevel.LevelI
            };

            return (anchor, settlement);
        }

        private bool IsProvinceCenter(Province p, int tile) => p.centerTileIndex == tile;

        private bool HasCoastalTile(Province p)
        {
            foreach (int t in p.memberTiles)
                if (_tiles[t].isCoast) return true;
            return false;
        }

        private int FindCoastalTile(Province p)
        {
            foreach (int t in p.memberTiles)
                if (_tiles[t].isCoast) return t;
            return -1;
        }

        private bool IsBorderProvince(Province p)
        {
            foreach (int t in p.memberTiles)
                if (Province.IsBorder(_tiles, _width, _height, t)) return true;
            return false;
        }

        private int FindBorderTile(Province p)
        {
            foreach (int t in p.memberTiles)
                if (Province.IsBorder(_tiles, _width, _height, t)) return t;
            return -1;
        }

        private bool IsTileOccupied(Dictionary<int, AnchorData> anchors, int tile)
        {
            foreach (var a in anchors.Values)
                if (a.tileIndex == tile) return true;
            return false;
        }

        private string GenerateName(TileData tile, SettlementType type)
        {
            string prefix = tile.elevation01 > 0.55f ? "山" : tile.isCoast ? "海" : "原";
            string mid = tile.annualPrecipMm > 900f ? "润" : tile.annualPrecipMm < 300f ? "干" : "丰";
            string suffix = type switch
            {
                SettlementType.Fort => "寨",
                SettlementType.City => "城",
                SettlementType.Village => "村",
                SettlementType.Village => "镇",
                _ => "村"
            };
            return prefix + mid + suffix;
        }
    }
}
