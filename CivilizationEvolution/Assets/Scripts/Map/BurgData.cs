using System;
using System.Collections.Generic;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 子地块类型（对齐 CK3 男爵领 / FantasyMapSimulator Burg）
    /// 一个 Province 包含多个 Burg，Burg 是城镇/港口/首都的载体
    /// </summary>
    public enum BurgType
    {
        Village,   // 村庄（最低级，多数 Burg）
        Town,      // 集镇（有一定发展度）
        City,      // 城市（高发展度，省中心）
        Port,      // 港口（沿海/沿河，贸易节点）
        Capital,   // 首都（政权首都，特殊 Burg）
        Fortress   // 要塞（军事据点）
    }

    /// <summary>
    /// 子地块（Burg / 男爵领）
    /// 省份内的可编辑定居点，是人口、贸易、军事的具体载体
    /// 对齐 FantasyMapSimulator: BurgData / BurgsAndStateGenerator / capitalBurgID / IsBurgPortQualified
    /// </summary>
    [Serializable]
    public class BurgData
    {
        public int burgId;
        public string burgName;
        public BurgType type;
        public int provinceId;       // 所属省份
        public int tileIndex;        // 所在地块（单元格）
        public float x;              // 地块内精确坐标（0~1，用于像素级定位）
        public float y;

        // 经济与人口
        public float population;     // Burg 人口（独立于地块人口块）
        public float development;    // 发展度（0~100）
        public float wealth;         // 财富
        public float tradePower;     // 贸易力量（港口更高）

        // 军事
        public float fortification;  // 防御等级（0~10）
        public int garrison;         // 驻军人数

        // 状态
        public bool isCapital;       // 是否政权首都
        public bool isPort;          // 是否港口（沿海或沿河）
        public bool isCoastal;       // 是否沿海
        public bool hasMarket;       // 是否有市场（贸易节点）
        public bool hasTemple;       // 是否有宗教建筑
        public bool hasUniversity;   // 是否有大学（高学识）

        // 建设等级（0~3，对应村庄→集镇→城市→大都市）
        public int buildLevel;

        /// <summary>显示用名称（含类型前缀）</summary>
        public string DisplayName => type switch
        {
            BurgType.Capital => $"【首都】{burgName}",
            BurgType.City => $"【城】{burgName}",
            BurgType.Port => $"【港】{burgName}",
            BurgType.Fortress => $"【寨】{burgName}",
            BurgType.Town => $"【镇】{burgName}",
            _ => burgName
        };

        /// <summary>是否为主要定居点（城市/港口/首都/要塞）</summary>
        public bool IsMajorSettlement =>
            type == BurgType.City || type == BurgType.Port ||
            type == BurgType.Capital || type == BurgType.Fortress;
    }

    /// <summary>
    /// 子地块生成器
    /// 对齐 FantasyMapSimulator: BurgsAndStateGenerator
    /// 规则：每个省份至少 1 个 Burg（省中心），沿海省份有港口，高发展度省份有更多 Burg
    /// </summary>
    public class BurgGenerator
    {
        private readonly TileData[] _tiles;
        private readonly int _width;
        private readonly int _height;
        private readonly Dictionary<int, Province> _provinces;
        private readonly System.Random _rng;

        /// <summary>每省最少 Burg 数</summary>
        public const int MinBurgsPerProvince = 1;
        /// <summary>每省最多 Burg 数</summary>
        public const int MaxBurgsPerProvince = 6;
        /// <summary>港口判定：沿海且地块为海岸</summary>
        public const float PortSpawnChance = 0.6f;
        /// <summary>城市判定：省中心且发展度高</summary>
        public const float CitySpawnChance = 0.35f;
        /// <summary>要塞判定：边境省份</summary>
        public const float FortressSpawnChance = 0.25f;

        public BurgGenerator(TileData[] tiles, int width, int height,
            Dictionary<int, Province> provinces, int seed)
        {
            _tiles = tiles;
            _width = width;
            _height = height;
            _provinces = provinces;
            _rng = new System.Random(seed + 999);
        }

        /// <summary>
        /// 为所有省份生成 Burg
        /// </summary>
        public Dictionary<int, BurgData> Generate()
        {
            var burgs = new Dictionary<int, BurgData>();
            int nextBurgId = 0;

            foreach (var kv in _provinces)
            {
                int provinceId = kv.Key;
                Province province = kv.Value;
                if (province.memberTiles.Count == 0) continue;

                // 1. 省中心 Burg（必有）
                int centerTile = province.centerTileIndex;
                if (centerTile < 0 || centerTile >= _tiles.Length)
                    centerTile = province.memberTiles[0];

                var centerBurg = CreateBurg(ref nextBurgId, provinceId, centerTile,
                    IsProvinceCenter(province, centerTile) ? BurgType.City : BurgType.Town);
                centerBurg.hasMarket = true;
                centerBurg.development = 20f + (float)_rng.NextDouble() * 30f;
                centerBurg.population = 500f + (float)_rng.NextDouble() * 1500f;
                burgs[centerBurg.burgId] = centerBurg;

                // 2. 沿海省份：港口 Burg
                if (HasCoastalTile(province))
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

                // 3. 边境省份：要塞 Burg
                if (IsBorderProvince(province) && _rng.NextDouble() < FortressSpawnChance)
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

                // 4. 大省份：额外村庄 Burg
                int extraVillages = Math.Min(MaxBurgsPerProvince - 3,
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

        /// <summary>Burg 名称生成（地形特征词 + 通名；对齐省名生成风格）</summary>
        private string GenerateBurgName(TileData tile, BurgType type)
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
