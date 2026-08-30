using System;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 地图生成参数配置（对齐FantasyMapSimulator编辑器内一体化模式）
    ///
    /// 工作流（参考FantasyMapSimulator，Unity引擎，编辑器内一体化）：
    ///   主菜单 → 地图编辑器 → 直接进入编辑器界面
    ///   → 右侧参数面板调整海陆/气候/水文参数
    ///   → 点击"生成地形"（程序化）或用画笔手动编辑
    ///   → 点击"计算气候"（温度/降水/洋流/群系）
    ///   → 点击"重算水文"（河网/侵蚀）
    ///
    /// 不学"地图上发生的事"的独立生成面板流程（Go引擎，开始菜单→生成面板→生成→进入编辑器），
    /// 因为其工作流不适合Unity的编辑器内一体化优化。
    ///
    /// 参数面板结构：
    ///   顶部全局：种子、地图尺寸、省份数量、地块数量、形态
    ///   【海陆】：外海缓冲、海平面、陆地量、破碎度、海岸破碎度（对齐FantasyMapSimulator）
    ///   【气候】：环流、热赤道、北缘纬度、南缘纬度、全球温度（本项目特色）
    ///   【水文与地貌】：河网密度、侵蚀强度（本项目特色）
    ///   【省份划分】：省份大小差异、省份规整度
    ///   【高度图】：导入灰度图/内置地形/清除（编辑器功能，非参数）
    ///
    /// 导入灰度图后：形态、陆地量、破碎度不再影响海陆骨架（禁用对应参数）
    /// </summary>
    [Serializable]
    public class MapGenerationConfig
    {
        // ============================================================
        // 顶部全局参数
        // ============================================================

        [Header("【全局】")]
        [Tooltip("地图底图来源。程序生成=噪声自动生成；内置底图=预设模板；导入高度图=以灰度图亮度为高程。")]
        public MapBasemap Basemap = MapBasemap.Procedural;

        [Tooltip("随机种子。相同种子+相同设置=相同地图。-1=随机生成。")]
        public int Seed = -1;

        [Tooltip("地图内部像素尺寸。更大更细致但生成和运行更耗时。")]
        public MapSizePreset MapSize = MapSizePreset.Medium;

        [Tooltip("陆地划分出的省份总数。数量越高，省界越密。")]
        [Range(20, 2000)] public int ProvinceCount = 200;

        [Tooltip("平均每个省份的地块数量。地块是省界划分的最小调整单位，不参与模拟。总地块数=省份数×每省地块数。")]
        [Range(8, 200)] public int TilesPerProvince = 24;

        [Tooltip("大陆布局形态。单陆=一块主大陆；双陆=两块大陆；环形陆地=大陆环绕中央海；成片群岛=无大块陆地。")]
        public ContinentShape Shape = ContinentShape.SingleLandmass;

        /// <summary>地图底图来源</summary>
        public enum MapBasemap
        {
            [Tooltip("程序生成：使用球面噪声+板块构造自动生成地形。显示全部海陆参数。")]
            Procedural,
            [Tooltip("内置底图：使用预设地形模板（如地球、盘古大陆等）。")]
            Builtin,
            [Tooltip("导入高度图：以灰度图片亮度作为高程。导入后形态、陆地量、破碎度不再影响海陆骨架。")]
            ImportHeightmap
        }

        /// <summary>地图尺寸预设</summary>
        public enum MapSizePreset
        {
            [Tooltip("微型：256×128=32768地块。快速测试用。")]
            Tiny,
            [Tooltip("小型：512×256=131072地块。平衡性能与细节。")]
            Small,
            [Tooltip("中型：1024×512=524288地块。默认推荐，细节丰富。")]
            Medium,
            [Tooltip("大型：2048×1024=2097152地块。大战略地图，生成较慢。")]
            Large,
            [Tooltip("巨型：4096×2048=8388608地块。极致细节，需高性能电脑。")]
            Huge
        }

        /// <summary>大陆布局形态</summary>
        public enum ContinentShape
        {
            [Tooltip("单陆：一块主大陆，周围海洋。")]
            SingleLandmass,
            [Tooltip("双陆：两块大陆隔海相望。")]
            DualLandmass,
            [Tooltip("环形陆地：大陆环绕中央海洋（地中海式）。")]
            RingLandmass,
            [Tooltip("成片群岛：无大块陆地，大量岛屿散布。")]
            Archipelago
        }

        /// <summary>获取地图尺寸（宽, 高）</summary>
        public (int width, int height) GetMapDimensions()
        {
            return MapSize switch
            {
                MapSizePreset.Tiny => (256, 128),
                MapSizePreset.Small => (512, 256),
                MapSizePreset.Medium => (1024, 512),
                MapSizePreset.Large => (2048, 1024),
                MapSizePreset.Huge => (4096, 2048),
                _ => (1024, 512)
            };
        }

        /// <summary>获取总地块数</summary>
        public int GetTotalTiles()
        {
            var (w, h) = GetMapDimensions();
            return w * h;
        }

        /// <summary>获取实际种子（-1时随机）</summary>
        public int GetActualSeed()
        {
            return Seed < 0 ? new System.Random().Next() : Seed;
        }

        // ============================================================
        // 【海陆】分组
        // ============================================================

        [Header("【海陆】")]
        [Tooltip("外海缓冲。默认开启；在地图四周渐进压低地势，避免大陆贴住画面边缘。")]
        public bool OuterSeaBuffer = true;

        [Tooltip("海平面。设定海陆判定高度；提高会淹没低地、扩大海洋并切断陆桥。")]
        [Range(0f, 1f)] public float SeaLevel = 0.05f;

        [Tooltip("陆地量。塑造大陆骨架的整体强度；提高会让更多地形露出海面。")]
        [Range(0f, 1f)] public float LandAmount = 0.85f;

        [Tooltip("破碎度。控制大陆与岛群的大尺度破碎程度；提高会形成更多、更小的陆块。")]
        [Range(0f, 1f)] public float Fragmentation = 0.06f;

        [Tooltip("海岸破碎度。只增加近海的海湾、半岛和小岛细节；不改变大陆的大致尺度。")]
        [Range(0f, 1f)] public float CoastFragmentation = 0.80f;

        // ============================================================
        // 【气候】分组
        // ============================================================

        [Header("【气候】")]
        [Tooltip("环流模式。决定纬向气候带和海岸冷暖湿干差异；三环流的气候对比最强。")]
        public CirculationMode Circulation = CirculationMode.ThreeCell;

        [Tooltip("热赤道。上下平移热带、干湿带与寒带；0表示热赤道位于地图中央。")]
        [Range(-1f, 1f)] public float ThermalEquator = 0f;

        [Tooltip("北缘纬度。设置地图上边缘对应的纬度，用于截取全球气候带。")]
        [Range(-90f, 90f)] public float NorthLatitude = 65f;

        [Tooltip("南缘纬度。设置地图下边缘对应的纬度，用于截取全球气候带。")]
        [Range(-90f, 90f)] public float SouthLatitude = -10f;

        [Tooltip("全球温度偏移（度C）。负值=冰期（冰川扩张），正值=间冰期（温暖湿润）。本项目特色参数。")]
        [Range(-8f, 8f)] public float GlobalTemperatureOffset = 0f;

        /// <summary>大气环流模式</summary>
        public enum CirculationMode
        {
            [Tooltip("单环流：赤道-极地直接环流。极端气候，赤道极热极地极冷。")]
            SingleCell,
            [Tooltip("双环流：简化的两圈环流。气候对比中等。")]
            DoubleCell,
            [Tooltip("三环流：地球标准（Hadley 0-30度 / Ferrel 30-60度 / Polar 60-90度）。气候带分明，对比最强。")]
            ThreeCell,
            [Tooltip("增强季风：三圈基础上海陆热力差异放大，季节性风向反转强烈。")]
            EnhancedMonsoon
        }

        // ============================================================
        // 【水文与地貌】分组（本项目特色）
        // ============================================================

        [Header("【水文与地貌】")]
        [Tooltip("河网密度。控制形成河流所需的汇水面积阈值。稀疏=大河少，密集=河网密布。")]
        [Range(0f, 1f)] public float RiverDensity = 0.5f;

        [Tooltip("水力侵蚀强度。0=无侵蚀（原始地形），1=强侵蚀（深谷峡谷）。粒子基侵蚀模拟。")]
        [Range(0f, 1f)] public float ErosionIntensity = 0.3f;

        // ============================================================
        // 【省份划分】分组
        // ============================================================

        [Header("【省份划分】")]
        [Tooltip("省份大小差异。控制省份面积的差距；越高越容易同时出现大省和小省。")]
        [Range(0f, 1f)] public float ProvinceSizeVariance = 0.90f;

        [Tooltip("省份规整度。整理省份种子的位置；越高越均匀规整，越低越自然不规则。")]
        [Range(0f, 1f)] public float ProvinceRegularity = 1.0f;

        // ============================================================
        // 【高度图】分组
        // ============================================================

        [Header("【高度图】")]
        [Tooltip("导入的灰度图高度文件路径。为空则按上方旋钮程序生成地形。")]
        public string HeightmapPath = "";

        [Tooltip("是否已导入高度图。导入后形态、陆地量与破碎度不再影响海陆骨架。")]
        public bool HasImportedHeightmap = false;

        // ============================================================
        // 【开局势力】分组（后续实现）
        // ============================================================

        [Header("【开局势力】")]
        [Tooltip("初始文明数量。0=无初始文明，玩家从部落开始。")]
        [Range(0, 50)] public int InitialCivilizations = 10;

        [Tooltip("初始文明发展程度。低=部落起步，高=已有王国。")]
        [Range(0f, 1f)] public float InitialCivilizationLevel = 0.3f;

        // ============================================================
        // 高级参数（折叠面板）
        // ============================================================

        [Header("【高级 · 地形细节】")]
        [Range(0.5f, 4.0f)] public float TerrainFrequency = 1.8f;
        [Range(3, 10)] public int TerrainOctaves = 6;
        [Range(0f, 1.5f)] public float WarpStrength = 0.7f;
        [Range(0.5f, 3.0f)] public float WarpFrequency = 1.3f;
        [Range(0f, 0.5f)] public float PlateBoundaryBoost = 0.25f;
        [Range(6, 20)] public int PlateCount = 12;

        [Header("【高级 · 气候细节】")]
        [Range(0f, 1f)] public float MonsoonStrength = 0.6f;
        [Range(200f, 1500f)] public float OrographicPrecipFactor = 800f;
        [Range(0.1f, 0.8f)] public float RainShadowFactor = 0.3f;

        [Header("【高级 · 侵蚀细节】")]
        [Range(0.1f, 0.8f)] public float ErosionRate = 0.3f;
        [Range(0.1f, 0.8f)] public float DepositionRate = 0.3f;
        [Range(0.005f, 0.05f)] public float EvaporationRate = 0.02f;
        [Range(0f, 0.5f)] public float ErosionInertia = 0.05f;

        // ============================================================
        // 固定参数（不暴露）
        // ============================================================
        public const float PlanetOmega = 7.292e-5f;
        public const float PlanetRadius = 6371000f;
        public const float SolarConstant = 1361f;
        public const bool UseMultithreading = true;
        public const int OceanIterations = 20;
        public const int ErosionMaxLifetime = 64;

        // ============================================================
        // 参数可见性判断
        // ============================================================

        /// <summary>海陆骨架参数是否可用（形态、陆地量、破碎度）。导入高度图后不可用。</summary>
        public bool IsLandSkeletonParamsEnabled => !HasImportedHeightmap && Basemap != MapBasemap.ImportHeightmap;

        /// <summary>程序化地形参数是否可见（仅程序生成模式）</summary>
        public bool IsProceduralParamsVisible => Basemap == MapBasemap.Procedural && !HasImportedHeightmap;

        /// <summary>高级地形细节是否可见</summary>
        public bool IsAdvancedTerrainVisible => Basemap == MapBasemap.Procedural && !HasImportedHeightmap;

        // ============================================================
        // 方法：将配置应用到各个生成器
        // ============================================================

        /// <summary>将配置应用到PlanetTerrainGenerator</summary>
        public void ApplyToGenerator(PlanetTerrainGenerator gen)
        {
            if (IsProceduralParamsVisible)
            {
                // 陆地量→目标陆地比例
                gen.TargetLandFraction = Mathf.Lerp(0.15f, 0.60f, LandAmount);
                // 破碎度→域扭曲频率
                gen.WarpFrequency = WarpFrequency * (0.5f + Fragmentation * 2f);
                // 海岸破碎度→高频噪声
                gen.TerrainFrequency = TerrainFrequency * (0.8f + CoastFragmentation * 0.4f);
                // 板块数量
                gen.PlateCount = PlateCount;
                gen.PlateBoundaryMountainBoost = PlateBoundaryBoost;
                gen.WarpStrength = WarpStrength;
                gen.TerrainOctaves = TerrainOctaves;
            }

            // 海平面：通过分位数归一化自动计算，SeaLevel参数用于导入高度图模式的海陆分界

            // 气候参数
            gen.AxialTilt = 23.44f * Mathf.Deg2Rad; // 固定轴倾角，纬度范围由北缘/南缘控制
            gen.GlobalTempOffset = GlobalTemperatureOffset;
            gen.RiverThreshold = Mathf.Lerp(200f, 20f, RiverDensity);
        }

        /// <summary>将配置应用到AtmosphericCirculation</summary>
        public void ApplyToGCM(AtmosphericCirculation gcm)
        {
            gcm.MonsoonStrength = MonsoonStrength * (Circulation == CirculationMode.EnhancedMonsoon ? 1.5f : 1f);
            gcm.OrographicPrecipFactor = OrographicPrecipFactor;
            gcm.RainShadowFactor = RainShadowFactor;
            // 热赤道偏移→纬度偏移
            gcm.Season = ThermalEquator * 0.25f;
        }

        /// <summary>将配置应用到HydraulicErosion</summary>
        public void ApplyToErosion(HydraulicErosion erosion, int totalTiles)
        {
            erosion.ParticleCount = (int)(ErosionIntensity * Mathf.Min(150000, totalTiles / 3));
            erosion.ErosionRate = ErosionRate * (0.5f + ErosionIntensity);
            erosion.DepositionRate = DepositionRate;
            erosion.EvaporationRate = EvaporationRate;
            erosion.Inertia = ErosionInertia;
        }

        /// <summary>获取纬度范围（北缘, 南缘）</summary>
        public (float north, float south) GetLatitudeRange()
        {
            return (NorthLatitude, SouthLatitude);
        }

        // ============================================================
        // 预设模板
        // ============================================================

        public static MapGenerationConfig Default() => new MapGenerationConfig();

        public static MapGenerationConfig EarthLike()
        {
            return new MapGenerationConfig
            {
                Basemap = MapBasemap.Procedural,
                MapSize = MapSizePreset.Medium,
                ProvinceCount = 200,
                TilesPerProvince = 12,
                Shape = ContinentShape.DualLandmass,
                OuterSeaBuffer = true,
                SeaLevel = 0.05f,
                LandAmount = 0.85f,
                Fragmentation = 0.06f,
                CoastFragmentation = 0.80f,
                Circulation = CirculationMode.ThreeCell,
                ThermalEquator = 0f,
                NorthLatitude = 65f,
                SouthLatitude = -10f,
                GlobalTemperatureOffset = 0f,
                RiverDensity = 0.5f,
                ErosionIntensity = 0.3f,
                ProvinceSizeVariance = 0.90f,
                ProvinceRegularity = 1.0f,
                InitialCivilizations = 10
            };
        }

        public static MapGenerationConfig IceAge()
        {
            var cfg = EarthLike();
            cfg.GlobalTemperatureOffset = -6f;
            cfg.SeaLevel = 0.02f; // 海平面下降
            cfg.LandAmount = 0.90f;
            cfg.RiverDensity = 0.3f;
            cfg.Shape = ContinentShape.SingleLandmass;
            return cfg;
        }

        public static MapGenerationConfig Hothouse()
        {
            var cfg = EarthLike();
            cfg.GlobalTemperatureOffset = 6f;
            cfg.SeaLevel = 0.10f; // 海平面上升
            cfg.LandAmount = 0.75f;
            cfg.RiverDensity = 0.7f;
            cfg.MonsoonStrength = 0.8f;
            return cfg;
        }

        public static MapGenerationConfig ArchipelagoWorld()
        {
            var cfg = EarthLike();
            cfg.Shape = ContinentShape.Archipelago;
            cfg.Fragmentation = 0.80f;
            cfg.CoastFragmentation = 0.95f;
            cfg.LandAmount = 0.40f;
            cfg.ProvinceCount = 500;
            return cfg;
        }
    }
}
