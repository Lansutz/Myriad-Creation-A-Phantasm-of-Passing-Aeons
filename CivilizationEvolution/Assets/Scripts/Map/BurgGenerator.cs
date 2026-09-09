using System;
using CivilizationEvolution.Core;
using System.Collections.Generic;

namespace CivilizationEvolution.Map
{
    public class BurgGenerator
    {
        private readonly TileData[] _tiles;
        private readonly int _width;
        private readonly int _height;
        private readonly Dictionary<int, Province> _provinces;
        private readonly System.Random _rng;

 /// <summary>每省最少 Burg 数</summary>        public const int MinBurgsPerProvince = 1;
 /// <summary>每省最多 Burg 数</summary>        public const int MaxBurgsPerProvince = 6;
 /// <summary>港口判定：沿海且地块为海岸</summary>        public const float PortSpawnChance = 0.6f;
 /// <summary>城市判定：省中心且发展度高</summary>        public const float CitySpawnChance = 0.35f;
 /// <summary>要塞判定：边境省份</summary>        public const float FortressSpawnChance = 0.25f;

        public BurgGenerator(TileData[] tiles, int width, int height,
            Dictionary<int, Province> provinces, int seed)
        {
            _tiles = tiles;
            _width = width;
            _height = height;
            _provinces = provinces;
            _rng = new System.Random(seed + 999);
        }

 /// 为所有省份生成 Burg        public Dictionary<int, BurgData> Generate()
        {
            var burgs = new Dictionary<int, BurgData>();
            int nextBurgId = 0;

            foreach (var kv in _provinces)
            {
                int provinceId = kv.Key;
                Province province = kv.Value;
                if (province.memberTiles.Count == 0) continue;

 // 1. 省中心 Burg（必有）                int centerTile = province.centerTileIndex;
                if (centerTile < 0 || centerTile >= _tiles.Length)
                    centerTile = province.memberTiles[0];

                var centerBurg = CreateBurg(ref nextBurgId, provinceId, centerTile,
                    IsProvinceCenter(province, centerTile) ? BurgType.City : BurgType.Town);
                centerBurg.hasMarket = true;
                centerBurg.development = 20f + (float)_rng.NextDouble() * 30f;
                centerBurg.population = 500f + (float)_rng.NextDouble() * 1500f;
                burgs[centerBurg.burgId] = centerBurg;

 // 2. 沿海省份：港口 Burg                if (HasCoastalTile(province))
                {
                    int coastalTile = FindCoastalTile(province);
                    if (coastalTile >= 0 && _rng.NextDouble() < PortSpawnChance)
                    {
                        var port = CreateBurg(ref nextBurgId, provinceId, coastalTile, BurgType.Port);
                        port.isPort = true;
                        port.isCoastal = true;
                        port.hasMarket = true;
                        port.tradePower = 30f + (float)_rng.NextDouble() * 50f;
                        port.development = 15f + (float)_rng.NextDouble() * 25f;
                        port.population = 300f + (float)_rng.NextDouble() * 1000f;
                        burgs[port.burgId] = port;
                    }
                }

 // 3. 边境省份：要塞 Burg                if (IsBorderProvince(province) && _rng.NextDouble() < FortressSpawnChance)
                {
                    int borderTile = FindBorderTile(province);
                    if (borderTile >= 0)
                    {
                        var fort = CreateBurg(ref nextBurgId, provinceId, borderTile, BurgType.Fortress);
                        fort.fortification = 3f + (float)_rng.NextDouble() * 5f;
                        fort.garrison = 100 + _rng.Next(200);
                        fort.development = 5f + (float)_rng.NextDouble() * 15f;
                        burgs[fort.burgId] = fort;
                    }
                }

 // 4. 大省份：额外村庄 Burg                int extraVillages = Math.Min(MaxBurgsPerProvince - 3,
                    province.memberTiles.Count / 40);
                for (int v = 0; v < extraVillages; v++)
                {
                    int tile = province.memberTiles[_rng.Next(province.memberTiles.Count)];
                    if (IsTileOccupiedByBurg(burgs, tile)) continue;
                    if (!_tiles[tile].isLand) continue;

                    var village = CreateBurg(ref nextBurgId, provinceId, tile, BurgType.Village);
                    village.development = 2f + (float)_rng.NextDouble() * 10f;
                    village.population = 50f + (float)_rng.NextDouble() * 300f;
                    burgs[village.burgId] = village;
                }
            }

            return burgs;
        }

        private BurgData CreateBurg(ref int nextId, int provinceId, int tileIndex, BurgType type)
        {
            ref TileData tile = ref _tiles[tileIndex];
            var burg = new BurgData
            {
                burgId = nextId++,
                burgName = GenerateBurgName(tile, type),
                type = type,
                provinceId = provinceId,
                tileIndex = tileIndex,
                x = 0.5f,
                y = 0.5f,
                isCoastal = tile.isCoast,
                buildLevel = type == BurgType.City ? 2 : type == BurgType.Town ? 1 : 0
            };

 // 初始化聚落类型学（形态/功能/等级/城形/堡型/升级路线）            SettlementTypologySystem.DeriveInitialType(burg, tile, _width, _height);

 // 覆盖：根据BurgType强制形态            burg.settlementType = SettlementEvolutionSystem.InferFromBurgType(type);
            burg.settlementLevel = type switch
            {
                BurgType.City or BurgType.Port or BurgType.Capital => SettlementLevel.LevelIII,
                BurgType.Town => SettlementLevel.LevelII,
                BurgType.Fortress => SettlementLevel.LevelII,
                _ => SettlementLevel.LevelI
            };

            return burg;
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

        private bool IsTileOccupiedByBurg(Dictionary<int, BurgData> burgs, int tile)
        {
            foreach (var b in burgs.Values)
                if (b.tileIndex == tile) return true;
            return false;
        }

 /// <summary>Burg 名称生成（地形特征词 + 通名；对齐省名生成风格）</summary>        private string GenerateBurgName(TileData tile, BurgType type)
        {
            string prefix = tile.elevation01 > 0.55f ? "山" : tile.isCoast ? "海" : "原";
            string mid = tile.annualPrecipMm > 900f ? "润" : tile.annualPrecipMm < 300f ? "干" : "丰";
            string suffix = type switch
            {
                BurgType.City => "城",
                BurgType.Port => "港",
                BurgType.Fortress => "寨",
                BurgType.Town => "镇",
                BurgType.Capital => "京",
                _ => "村"
            };
            return prefix + mid + suffix;
        }
    }
}
