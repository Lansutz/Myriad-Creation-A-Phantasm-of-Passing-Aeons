using System;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 球形行星地形生成器
    /// 使用3D球面Simplex噪声+域扭曲生成地形，避免等矩形投影两极拉伸
    /// 包含：海陆生成、温度模拟、降水模拟、55群系映射
    /// 平面/球形可切换：数据层统一用球面坐标，渲染层可选平面或球体
    /// </summary>
    public class PlanetTerrainGenerator
    {
        private readonly SphericalNoise _noise;
        private readonly int _seed;

        // ===== 生成参数（可调节，借鉴Gleba量级）=====
        public float SeaLevel = 0.48f;          // 海平面阈值（0~1，越大陆地越少）
        public float TerrainFrequency = 1.8f;    // 基础地形频率
        public int TerrainOctaves = 6;            // 地形倍频数
        public float WarpStrength = 0.7f;         // 域扭曲强度
        public float WarpFrequency = 1.3f;        // 域扭曲频率
        public float MountainHeight = 0.35f;      // 山脉叠加高度
        public float AxialTilt = 0.409f;          // 行星轴倾角（弧度，地球23.44°）
        public float GlobalTempOffset = 0f;        // 全球温度偏移（±可模拟冰期/间冰期）

        public PlanetTerrainGenerator(int seed = 42)
        {
            _seed = seed;
            _noise = new SphericalNoise(seed);
        }

        /// <summary>
        /// 生成完整地形（填充TileData数组）
        /// </summary>
        /// <param name="tiles">地块数组（需已分配）</param>
        /// <param name="width">地图宽度（建议=height*2，等矩形投影）</param>
        /// <param name="height">地图高度</param>
        public void Generate(TileData[] tiles, int width, int height)
        {
            if (tiles == null || tiles.Length != width * height)
                throw new ArgumentException($"tiles长度({tiles?.Length}) != width*height({width * height})");

            Debug.Log($"[PlanetTerrainGenerator] 开始生成：{width}x{height}={width * height}地块，种子={_seed}");

            // 第一遍：生成基础高程+海陆
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    ref TileData tile = ref tiles[idx];
                    tile.tileIndex = idx;
                    tile.exists = true;

                    // 网格坐标→单位球面3D坐标
                    var (sx, sy, sz) = SphericalNoise.GridToSphere(x, y, width, height);

                    // 域扭曲fBm生成基础高程（0~1）
                    float raw = _noise.DomainWarpFbm(sx, sy, sz, WarpStrength, WarpFrequency, TerrainOctaves);
                    float elevation = SphericalNoise.Normalize01(raw);

                    // 山脊噪声叠加（制造山脉走向）
                    float ridge = _noise.RidgedFbm(sx * 2.1f + 10f, sy * 2.1f, sz * 2.1f + 5f, 4);
                    elevation = Mathf.Lerp(elevation, elevation + ridge * MountainHeight, 0.4f);
                    elevation = Mathf.Clamp01(elevation);

                    tile.elevation01 = elevation;
                    tile.isLand = elevation > SeaLevel;

                    // 海陆属性
                    if (!tile.isLand)
                    {
                        tile.oceanTier = (elevation < SeaLevel * 0.3f) ? GameEnums.OceanTier.DeepSea :
                                         (elevation < SeaLevel * 0.7f) ? GameEnums.OceanTier.NearSea :
                                         GameEnums.OceanTier.Coast;
                        tile.oceanDepth01 = (SeaLevel - elevation) / SeaLevel;
                    }
                    else
                    {
                        tile.oceanTier = GameEnums.OceanTier.Land;
                        tile.oceanDepth01 = 0f;
                    }

                    // 坡度（近似：与邻域高程差）
                    tile.slopeDegree = 0f; // 第二遍计算
                }
            }

            // 第二遍：计算坡度、海岸标记、温度、降水
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    ref TileData tile = ref tiles[idx];

                    // 坡度：与4邻域最大高程差
                    float maxDiff = 0f;
                    if (x > 0) maxDiff = Mathf.Max(maxDiff, Mathf.Abs(tile.elevation01 - tiles[idx - 1].elevation01));
                    if (x < width - 1) maxDiff = Mathf.Max(maxDiff, Mathf.Abs(tile.elevation01 - tiles[idx + 1].elevation01));
                    if (y > 0) maxDiff = Mathf.Max(maxDiff, Mathf.Abs(tile.elevation01 - tiles[idx - width].elevation01));
                    if (y < height - 1) maxDiff = Mathf.Max(maxDiff, Mathf.Abs(tile.elevation01 - tiles[idx + width].elevation01));
                    tile.slopeDegree = maxDiff * 90f; // 近似角度

                    // 海岸标记：陆地且邻接海洋
                    tile.isCoast = false;
                    if (tile.isLand)
                    {
                        if (x > 0 && !tiles[idx - 1].isLand) tile.isCoast = true;
                        else if (x < width - 1 && !tiles[idx + 1].isLand) tile.isCoast = true;
                        else if (y > 0 && !tiles[idx - width].isLand) tile.isCoast = true;
                        else if (y < height - 1 && !tiles[idx + width].isLand) tile.isCoast = true;
                    }

                    // 温度计算（纬度主导+高程递减+海陆调节）
                    float lat = 90f - (y / (float)height) * 180f; // -90~90
                    float latFactor = Mathf.Cos(lat * Mathf.Deg2Rad); // 赤道1，两极0
                    float baseTemp = -10f + latFactor * 38f; // -10°C(极)~28°C(赤道)
                    float elevationCooling = tile.elevation01 * 65f; // 每0.1高程降6.5°C
                    float landEffect = tile.isLand ? (tile.isCoast ? 0f : -2f) : 3f; // 海洋调节温度
                    tile.annualTemp = baseTemp - elevationCooling + landEffect + GlobalTempOffset;

                    // 降水计算（纬度风带+地形抬升+距海距离近似）
                    // 三圈环流：赤道低压带(多雨)、副热带高压(少雨)、温带低压(多雨)、极地高压(少雨)
                    float absLat = Mathf.Abs(lat);
                    float circulationRain;
                    if (absLat < 15f) circulationRain = 1800f; // 赤道多雨
                    else if (absLat < 35f) circulationRain = 500f; // 副热带少雨（沙漠带）
                    else if (absLat < 60f) circulationRain = 1200f; // 温带多雨
                    else circulationRain = 250f; // 极地少雨

                    // 地形抬升：迎风坡降水增加（简化：坡度大+面向海洋）
                    float orographicRain = tile.slopeDegree > 15f && tile.isCoast ? 400f : 0f;
                    // 大陆性：内陆降水减少（简化：距海岸越远越少）
                    float continentality = tile.isLand && !tile.isCoast ? -200f : 0f;

                    tile.annualPrecipMm = Mathf.Max(0f, circulationRain + orographicRain + continentality);
                    tile.airHumidityPct = Mathf.Clamp01(tile.annualPrecipMm / 2000f);
                }
            }

            // 第三遍：群系映射（温度+降水+高程+海陆→55群系）
            for (int i = 0; i < tiles.Length; i++)
            {
                ref TileData tile = ref tiles[i];
                tile.biome = ClassifyBiome(in tile);
                tile.climateZone = ClassifyClimateZone(tile.annualTemp, tile.annualPrecipMm);
                tile.fertility = CalculateFertility(in tile);
            }

            Debug.Log("[PlanetTerrainGenerator] 地形生成完成");
        }

        /// <summary>
        /// 群系分类：温度+降水+高程+海陆→55个BiomeType
        /// 分类逻辑：先分海陆，陆地按温度带→湿度→高程→特殊地形
        /// </summary>
        private static GameEnums.BiomeType ClassifyBiome(in TileData tile)
        {
            // 海洋群系（简化：按深度）
            if (!tile.isLand)
            {
                if (tile.oceanTier == GameEnums.OceanTier.Coast) return GameEnums.BiomeType.Mangrove; // 沿海红树林
                return GameEnums.BiomeType.EndorheicLake; // 占位：海洋用统一渲染
            }

            float temp = tile.annualTemp;
            float precip = tile.annualPrecipMm;
            float elev = tile.elevation01;
            float slope = tile.slopeDegree;

            // ===== 极寒/高海拔 =====
            if (temp < -5f || elev > 0.85f)
            {
                if (elev > 0.92f) return GameEnums.BiomeType.MountainGlacier; // 山岳冰川
                if (temp < -10f) return GameEnums.BiomeType.IceSheet; // 冰盖
                return GameEnums.BiomeType.Tundra; // 冻原
            }

            // ===== 亚寒带（寒温带）=====
            if (temp < 5f)
            {
                if (precip > 600f) return GameEnums.BiomeType.BorealForest; // 寒带针叶林
                if (precip > 300f) return GameEnums.BiomeType.AlpineMeadow; // 高山草甸
                return GameEnums.BiomeType.ColdDesert; // 寒冷沙漠
            }

            // ===== 温带 =====
            if (temp < 18f)
            {
                // 高海拔山地
                if (elev > 0.7f)
                {
                    if (slope > 25f) return GameEnums.BiomeType.FoldMountains; // 褶皱山地
                    return GameEnums.BiomeType.HighMountains; // 高亢山地
                }
                if (elev > 0.55f && slope > 20f) return GameEnums.BiomeType.LowHills; // 低山丘陵

                // 干旱
                if (precip < 250f) return GameEnums.BiomeType.InlandDesert; // 内陆沙漠
                if (precip < 450f)
                {
                    if (tile.isCoast) return GameEnums.BiomeType.CoastalDesert; // 滨海沙漠
                    return GameEnums.BiomeType.SemiAridShrubland; // 半干旱灌丛
                }

                // 半干旱草原
                if (precip < 700f) return GameEnums.BiomeType.TemperateGrassland; // 温带草原

                // 湿润森林
                if (precip > 1200f && tile.isCoast) return GameEnums.BiomeType.CoastalLowland; // 滨海低地
                return GameEnums.BiomeType.DeciduousForest; // 落叶阔叶林
            }

            // ===== 亚热带/热带 =====
            // 干旱
            if (precip < 200f)
            {
                if (tile.isCoast) return GameEnums.BiomeType.CoastalDesert; // 滨海沙漠
                return GameEnums.BiomeType.HotDesert; // 炎热沙漠
            }
            if (precip < 400f) return GameEnums.BiomeType.Savanna; // 稀树草原

            // 半湿润
            if (precip < 800f)
            {
                if (temp > 22f) return GameEnums.BiomeType.TropicalMonsoon; // 季雨林
                return GameEnums.BiomeType.MonsoonForest; // 季风干湿林
            }

            // 湿润
            if (tile.isCoast && precip > 1500f) return GameEnums.BiomeType.Mangrove; // 红树林
            if (temp > 24f) return GameEnums.BiomeType.TropicalRainforest; // 雨林
            return GameEnums.BiomeType.EvergreenForest; // 常绿阔叶林
        }

        /// <summary>气候带分类（9带，对齐GameEnums.ClimateZone）</summary>
        private static GameEnums.ClimateZone ClassifyClimateZone(float temp, float precip)
        {
            if (temp < -5f) return GameEnums.ClimateZone.PolarFrigid;
            if (temp < 5f) return GameEnums.ClimateZone.Subarctic;
            if (temp < 10f) return GameEnums.ClimateZone.TemperateCold;
            if (temp < 15f) return precip < 400f ? GameEnums.ClimateZone.InlandAridTemperate : GameEnums.ClimateZone.TemperateMild;
            if (temp < 20f) return precip < 400f ? GameEnums.ClimateZone.InlandAridTemperate : GameEnums.ClimateZone.TemperateWarm;
            if (temp < 24f) return GameEnums.ClimateZone.Subtropical;
            return GameEnums.ClimateZone.Tropical;
        }

        /// <summary>
        /// 肥力计算：群系+温度+降水+坡度+高程
        /// 冲积平原/火山灰平原/三角洲最肥，沙漠/冰盖/山地最贫瘠
        /// </summary>
        private static float CalculateFertility(in TileData tile)
        {
            if (!tile.isLand) return 0f;

            float baseFert = 0.3f;

            // 群系加成
            switch (tile.biome)
            {
                case GameEnums.BiomeType.AlluvialPlain:
                case GameEnums.BiomeType.VolcanicAshPlain:
                case GameEnums.BiomeType.Delta:
                case GameEnums.BiomeType.GreatRiverPlain:
                    baseFert = 0.9f; break;
                case GameEnums.BiomeType.Interfluvial:
                case GameEnums.BiomeType.EnclosedBasin:
                case GameEnums.BiomeType.PiedmontBasin:
                case GameEnums.BiomeType.PluvialFan:
                    baseFert = 0.75f; break;
                case GameEnums.BiomeType.DeciduousForest:
                case GameEnums.BiomeType.EvergreenForest:
                case GameEnums.BiomeType.TropicalRainforest:
                case GameEnums.BiomeType.LowHills:
                    baseFert = 0.6f; break;
                case GameEnums.BiomeType.TemperateGrassland:
                case GameEnums.BiomeType.Savanna:
                case GameEnums.BiomeType.MonsoonForest:
                case GameEnums.BiomeType.TropicalMonsoon:
                    baseFert = 0.5f; break;
                case GameEnums.BiomeType.BorealForest:
                case GameEnums.BiomeType.AlpineMeadow:
                case GameEnums.BiomeType.CoastalLowland:
                    baseFert = 0.35f; break;
                case GameEnums.BiomeType.HotDesert:
                case GameEnums.BiomeType.ColdDesert:
                case GameEnums.BiomeType.InlandDesert:
                case GameEnums.BiomeType.IceSheet:
                case GameEnums.BiomeType.Tundra:
                case GameEnums.BiomeType.MountainGlacier:
                case GameEnums.BiomeType.HighMountains:
                case GameEnums.BiomeType.FoldMountains:
                    baseFert = 0.1f; break;
                case GameEnums.BiomeType.DesertOasis:
                    baseFert = 0.7f; break;
                default:
                    baseFert = 0.3f; break;
            }

            // 坡度惩罚
            baseFert *= Mathf.Clamp01(1f - tile.slopeDegree / 60f);
            // 高程惩罚
            baseFert *= Mathf.Clamp01(1f - Mathf.Max(0, tile.elevation01 - 0.6f) * 1.5f);
            // 降水修正（过湿或过干都略降）
            float precipFactor = tile.annualPrecipMm < 300f ? 0.7f :
                                  tile.annualPrecipMm > 2500f ? 0.85f : 1f;
            baseFert *= precipFactor;

            return Mathf.Clamp01(baseFert);
        }
    }
}
