using System;
using System.Threading.Tasks;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 球形行星地形生成器（专业版）
    /// 算法集成：
    ///   1. 3D球面Simplex噪声 + 域扭曲Domain Warping + 山脊噪声RidgedFbm
    ///   2. 分位数归一化（Quantile Normalization）——确保海陆比例精准
    ///   3. 球面Voronoi图（Spherical Voronoi）——板块构造边界
    ///   4. Priority-Flood水文模拟——洼地填充+排水方向+汇水面积+河流等级
    ///   5. Holdridge生命地带分类——生物温度+降水+潜在蒸散比→55群系
    ///   6. 多线程Parallel.For优化——百万级地块秒级生成
    ///
    /// 平面/球形可切换：数据层统一用球面坐标生成，渲染层可选平面等矩形或3D球体
    /// </summary>
    public class PlanetTerrainGenerator
    {
        private readonly SphericalNoise _noise;
        private readonly int _seed;

        // ===== 生成参数（可调节，借鉴Gleba量级）=====
        public float TargetLandFraction = 0.30f;   // 目标陆地比例（地球≈29%）
        public float TerrainFrequency = 1.8f;       // 基础地形频率
        public int TerrainOctaves = 6;               // 地形倍频数
        public float WarpStrength = 0.7f;            // 域扭曲强度
        public float WarpFrequency = 1.3f;           // 域扭曲频率
        public float MountainHeight = 0.35f;         // 山脉叠加高度
        public int PlateCount = 12;                   // 板块数量（球面Voronoi）
        public float PlateBoundaryMountainBoost = 0.25f; // 板块边界山脉加成
        public float AxialTilt = 0.409f;             // 行星轴倾角（弧度，地球23.44°）
        public float GlobalTempOffset = 0f;           // 全球温度偏移（±模拟冰期/间冰期）
        public float RiverThreshold = 80f;            // 河流阈值（汇水面积超过此值为河流）
        public bool UseMultithreading = true;         // 是否启用多线程

        public PlanetTerrainGenerator(int seed = 42)
        {
            _seed = seed;
            _noise = new SphericalNoise(seed);
        }

        /// <summary>
        /// 生成完整地形（填充TileData数组）
        /// 专业版流程：原始高程→分位数归一化→板块边界→海陆→物理属性→水文→Holdridge群系
        /// </summary>
        public void Generate(TileData[] tiles, int width, int height)
        {
            if (tiles == null || tiles.Length != width * height)
                throw new ArgumentException($"tiles长度({tiles?.Length}) != width*height({width * height})");

            int n = width * height;
            Debug.Log($"[PlanetTerrainGenerator] 专业版生成：{width}x{height}={n}地块，种子={_seed}，板块={PlateCount}");

            // ===== 第0步：预计算球面坐标（所有点的单位球面3D坐标）=====
            var sphereX = new float[n];
            var sphereY = new float[n];
            var sphereZ = new float[n];
            var latAbs = new float[n];
            Parallel.For(0, n, i =>
            {
                int x = i % width;
                int y = i / width;
                var (sx, sy, sz) = SphericalNoise.GridToSphere(x, y, width, height);
                sphereX[i] = sx; sphereY[i] = sy; sphereZ[i] = sz;
                latAbs[i] = Mathf.Abs(90f - (y / (float)height) * 180f);
            });

            // ===== 第1步：生成原始高程（球面噪声+域扭曲+山脊）=====
            var rawElevation = new float[n];
            ParallelLoop(0, n, i =>
            {
                float raw = _noise.DomainWarpFbm(sphereX[i], sphereY[i], sphereZ[i],
                    WarpStrength, WarpFrequency, TerrainOctaves);
                float elev = SphericalNoise.Normalize01(raw);
                // 山脊噪声叠加（制造山脉走向）
                float ridge = _noise.RidgedFbm(
                    sphereX[i] * 2.1f + 10f, sphereY[i] * 2.1f, sphereZ[i] * 2.1f + 5f, 4);
                elev = Mathf.Lerp(elev, elev + ridge * MountainHeight, 0.4f);
                rawElevation[i] = Mathf.Clamp01(elev);
            });

            // ===== 第2步：分位数归一化——确保海陆比例精准 =====
            float seaLevel = QuantileNormalize(rawElevation, TargetLandFraction);
            Debug.Log($"[PlanetTerrainGenerator] 分位数归一化完成，海平面阈值={seaLevel:F4}，目标陆地比例={TargetLandFraction}");

            // ===== 第3步：球面Voronoi板块构造 =====
            var voronoi = new SphericalVoronoi(_seed + 1000);
            voronoi.Generate(PlateCount, lloydIterations: 3);
            int[] plateIds = voronoi.AssignToGrid(width, height);
            bool[] plateBoundary = voronoi.DetectBoundaries(plateIds, width, height, wrapX: true);

            // 板块边界叠加山脉（汇聚边界造山，简化：所有边界都有一定抬升）
            ParallelLoop(0, n, i =>
            {
                if (plateBoundary[i] && rawElevation[i] > seaLevel)
                {
                    rawElevation[i] = Mathf.Clamp01(rawElevation[i] + PlateBoundaryMountainBoost);
                }
            });

            // ===== 第4步：海陆分离 + 基础属性 =====
            ParallelLoop(0, n, i =>
            {
                ref TileData tile = ref tiles[i];
                tile.tileIndex = i;
                tile.exists = true;
                tile.elevation01 = rawElevation[i];
                tile.isLand = rawElevation[i] > seaLevel;

                if (!tile.isLand)
                {
                    tile.oceanTier = (rawElevation[i] < seaLevel * 0.3f) ? GameEnums.OceanTier.DeepSea :
                                     (rawElevation[i] < seaLevel * 0.7f) ? GameEnums.OceanTier.NearSea :
                                     GameEnums.OceanTier.Coast;
                    tile.oceanDepth01 = (seaLevel - rawElevation[i]) / seaLevel;
                }
                else
                {
                    tile.oceanTier = GameEnums.OceanTier.Land;
                    tile.oceanDepth01 = 0f;
                }
            });

            // ===== 第5步：坡度、海岸、温度、降水 =====
            ParallelLoop(0, n, i =>
            {
                int x = i % width;
                int y = i / width;
                ref TileData tile = ref tiles[i];

                // 坡度（4邻域最大高程差）
                float maxDiff = 0f;
                if (x > 0) maxDiff = Mathf.Max(maxDiff, Mathf.Abs(tile.elevation01 - tiles[i - 1].elevation01));
                if (x < width - 1) maxDiff = Mathf.Max(maxDiff, Mathf.Abs(tile.elevation01 - tiles[i + 1].elevation01));
                if (y > 0) maxDiff = Mathf.Max(maxDiff, Mathf.Abs(tile.elevation01 - tiles[i - width].elevation01));
                if (y < height - 1) maxDiff = Mathf.Max(maxDiff, Mathf.Abs(tile.elevation01 - tiles[i + width].elevation01));
                tile.slopeDegree = maxDiff * 90f;

                // 海岸标记
                tile.isCoast = false;
                if (tile.isLand)
                {
                    if (x > 0 && !tiles[i - 1].isLand) tile.isCoast = true;
                    else if (x < width - 1 && !tiles[i + 1].isLand) tile.isCoast = true;
                    else if (y > 0 && !tiles[i - width].isLand) tile.isCoast = true;
                    else if (y < height - 1 && !tiles[i + width].isLand) tile.isCoast = true;
                }

                // 温度（纬度主导+高程递减+海陆调节）
                float lat = 90f - (y / (float)height) * 180f;
                float latFactor = Mathf.Cos(lat * Mathf.Deg2Rad);
                float baseTemp = -10f + latFactor * 38f;
                float elevationCooling = tile.elevation01 * 65f;
                float landEffect = tile.isLand ? (tile.isCoast ? 0f : -2f) : 3f;
                tile.annualTemp = baseTemp - elevationCooling + landEffect + GlobalTempOffset;

                // 降水（三圈环流+地形抬升+大陆性）
                float absLat = Mathf.Abs(lat);
                float circulationRain;
                if (absLat < 15f) circulationRain = 1800f;
                else if (absLat < 35f) circulationRain = 500f;
                else if (absLat < 60f) circulationRain = 1200f;
                else circulationRain = 250f;
                float orographicRain = tile.slopeDegree > 15f && tile.isCoast ? 400f : 0f;
                float continentality = tile.isLand && !tile.isCoast ? -200f : 0f;
                tile.annualPrecipMm = Mathf.Max(0f, circulationRain + orographicRain + continentality);
                tile.airHumidityPct = Mathf.Clamp01(tile.annualPrecipMm / 2000f);
            });

            // 准备isLand数组（后续多个算法使用）
            var isLandArray = new bool[n];
            for (int i = 0; i < n; i++) isLandArray[i] = tiles[i].isLand;

            // ===== 第5.5步：水力侵蚀（修改高程，形成河谷/峡谷/冲积扇）=====
            var erosion = new HydraulicErosion(width, height, _seed + 2000);
            erosion.ParticleCount = Mathf.Min(120000, n / 4); // 粒子数随地图大小调整
            erosion.Run(rawElevation, isLandArray);
            erosion.ApplyToTiles(tiles, rawElevation);
            Debug.Log($"[PlanetTerrainGenerator] 水力侵蚀完成");

            // 侵蚀后重新计算坡度和温度（高程已改变）
            ParallelLoop(0, n, i =>
            {
                int x = i % width;
                int y = i / height;
                ref TileData tile = ref tiles[i];
                if (!tile.isLand) return;

                // 重新计算坡度
                float maxDiff = 0f;
                if (x > 0) maxDiff = Mathf.Max(maxDiff, Mathf.Abs(tile.elevation01 - tiles[i - 1].elevation01));
                if (x < width - 1) maxDiff = Mathf.Max(maxDiff, Mathf.Abs(tile.elevation01 - tiles[i + 1].elevation01));
                if (y > 0) maxDiff = Mathf.Max(maxDiff, Mathf.Abs(tile.elevation01 - tiles[i - width].elevation01));
                if (y < height - 1) maxDiff = Mathf.Max(maxDiff, Mathf.Abs(tile.elevation01 - tiles[i + width].elevation01));
                tile.slopeDegree = maxDiff * 90f;

                // 重新计算温度（高程已改变）
                float lat = 90f - (y / (float)height) * 180f;
                float latFactor = Mathf.Cos(lat * Mathf.Deg2Rad);
                float baseTemp = -10f + latFactor * 38f;
                float elevationCooling = tile.elevation01 * 65f;
                float landEffect = tile.isCoast ? 0f : -2f;
                tile.annualTemp = baseTemp - elevationCooling + landEffect + GlobalTempOffset;
            });

            // ===== 第5.8步：大气环流GCM（气压带+三圈环流+科里奥利+季风+地形降水）=====
            var tempArray = new float[n];
            for (int i = 0; i < n; i++) tempArray[i] = tiles[i].annualTemp;
            var gcm = new AtmosphericCirculation(width, height);
            gcm.AxialTilt = AxialTilt;
            gcm.Run(rawElevation, isLandArray, tempArray);
            gcm.ApplyToTiles(tiles, tempArray);
            Debug.Log($"[PlanetTerrainGenerator] 大气环流GCM完成");

            // ===== 第5.9步：洋流模拟（风生环流+科里奥利+大陆阻挡+暖流寒流）=====
            var ocean = new OceanCurrentSimulator(width, height);
            ocean.Run(isLandArray, gcm.WindU, gcm.WindV, tempArray);
            ocean.ApplyCoastalEffects(tiles, isLandArray);
            Debug.Log($"[PlanetTerrainGenerator] 洋流模拟完成");

            // ===== 第6步：Priority-Flood水文模拟 =====
            var hydro = new HydrologySystem(width, height, wrapX: true);
            hydro.Run(rawElevation, isLandArray, RiverThreshold);
            hydro.ApplyToTiles(tiles);
            Debug.Log($"[PlanetTerrainGenerator] 水文模拟完成：河流{hydro.GetRiverTileCount()}地块，最大等级={hydro.GetMaxStreamOrder()}");

            // ===== 第6.5步：PCA地形特征提取（地形分类+大陆主轴）=====
            var pca = new TerrainPCA(width, height);
            pca.Run(rawElevation, isLandArray);
            Debug.Log($"[PlanetTerrainGenerator] PCA地形特征完成：大陆主轴=({pca.ContinentPrincipalAxis.x:F2}, {pca.ContinentPrincipalAxis.y:F2})");

            // ===== 第7步：Holdridge生命地带群系分类 + 肥力 =====
            ParallelLoop(0, n, i =>
            {
                ref TileData tile = ref tiles[i];
                tile.biome = HoldridgeBiomeClassifier.Classify(
                    tile.annualTemp, tile.annualPrecipMm, tile.elevation01,
                    tile.isLand, tile.isCoast, tile.isRiver, tile.slopeDegree, latAbs[i]);
                tile.climateZone = ClassifyClimateZone(tile.annualTemp, tile.annualPrecipMm);
                tile.fertility = HoldridgeBiomeClassifier.CalculateFertility(
                    tile.annualTemp, tile.annualPrecipMm, tile.elevation01,
                    tile.slopeDegree, tile.biome);
            });

            Debug.Log($"[PlanetTerrainGenerator] 生成完成，陆地{CountLand(tiles)}地块，海洋{n - CountLand(tiles)}地块");
        }

        // ===== 分位数归一化 =====
        /// <summary>
        /// 分位数归一化：将原始高程映射到0-1，使超过阈值的陆地比例等于targetLandFraction
        /// 原理：排序找到targetLandFraction分位数作为海平面，然后线性映射
        /// </summary>
        private float QuantileNormalize(float[] elevation, float targetLandFraction)
        {
            int n = elevation.Length;
            var sorted = new float[n];
            Array.Copy(elevation, sorted, n);
            Array.Sort(sorted);

            // 找到目标分位数对应的高程值作为海平面
            int seaLevelIndex = Mathf.Clamp((int)(n * (1f - targetLandFraction)), 0, n - 1);
            float seaLevel = sorted[seaLevelIndex];

            // 线性映射：海平面→0.5，最小值→0，最大值→1
            float min = sorted[0];
            float max = sorted[n - 1];
            float range = max - min;
            if (range < 0.001f) return 0.5f;

            for (int i = 0; i < n; i++)
            {
                // 以海平面为中心，向两侧拉伸
                float normalized;
                if (elevation[i] <= seaLevel)
                {
                    normalized = 0.5f * (elevation[i] - min) / (seaLevel - min);
                }
                else
                {
                    normalized = 0.5f + 0.5f * (elevation[i] - seaLevel) / (max - seaLevel);
                }
                elevation[i] = Mathf.Clamp01(normalized);
            }
            return 0.5f; // 归一化后的海平面就是0.5
        }

        // ===== 气候带分类 =====
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

        // ===== 工具 =====
        private void ParallelLoop(int from, int to, Action<int> body)
        {
            if (UseMultithreading && to - from > 1000)
                Parallel.For(from, to, body);
            else
                for (int i = from; i < to; i++) body(i);
        }

        private static int CountLand(TileData[] tiles)
        {
            int count = 0;
            foreach (var t in tiles) if (t.isLand) count++;
            return count;
        }
    }
}
