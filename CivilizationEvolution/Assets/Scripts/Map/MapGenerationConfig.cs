using System;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 地图生成参数配置（玩家可见 + 高级隐藏）
    ///
    /// 设计参考：
    ///   - FantasyMapSimulator：编辑器内一体化，参数在编辑过程中调整（9参数）
    ///   - 地图上发生的事：开始菜单→生成面板→生成→进入编辑器（独立生成流程）
    ///
    /// 核心设计：
    ///   1. 三种生成模式：程序化生成 / 导入高度图 / 空白地图
    ///   2. 基础参数（所有模式可见）：地图尺寸、纬度范围、省份数、种子、海平面
    ///   3. 程序化参数（仅程序化模式可见）：陆地比例、起伏度、山脉强度、破碎度、板块数
    ///   4. 气候参数（所有模式可见）：轴倾角、全球温度、环流模式、季风强度、热赤道偏移
    ///   5. 水文参数（所有模式可见）：河网密度、侵蚀强度
    ///   6. 导入高度图模式：隐藏程序化参数，以图片亮度为高程，仍可调整气候/水文/海平面
    ///
    /// 工作流（参考"地图上发生的事"）：
    ///   开始菜单 → 点击"开始游戏" → 弹出地图生成设置面板
    ///   → 选择生成模式 → 调整参数 → 点击"生成"
    ///   → 生成地形+气候+水文 → 进入地图编辑器
    ///   → 编辑器内可手动编辑地形 → 点击"按当前地形重算气候"
    /// </summary>
    [Serializable]
    public class MapGenerationConfig
    {
        // ============================================================
        // 生成模式
        // ============================================================

        [Header("【生成模式】")]
        [Tooltip("地形生成方式。程序化=噪声自动生成；导入高度图=以图片亮度为高程；空白地图=全海洋手动绘制。")]
        public GenerationMode Mode = GenerationMode.Procedural;

        /// <summary>生成模式</summary>
        public enum GenerationMode
        {
            [Tooltip("程序化生成：使用球面噪声+板块构造自动生成地形。显示全部地形参数。")]
            Procedural,
            [Tooltip("导入高度图：以灰度图片亮度作为高程。隐藏程序化地形参数，仅保留海平面/气候/水文。")]
            ImportHeightmap,
            [Tooltip("空白地图：全海洋，玩家手动绘制地形。隐藏所有地形生成参数。")]
            Blank
        }

        // ============================================================
        // 【基础参数】（所有模式可见，6个）
        // ============================================================

        [Header("【基础参数】")]
        [Tooltip("地图内部像素尺寸（宽度×高度）。越大越精细但生成/运行越慢。预设：128×64 / 256×128 / 512×256 / 1024×512。")]
        public MapSizePreset MapSize = MapSizePreset.Medium;

        [Tooltip("地图上边缘对应的纬度（度）。正值=北纬，负值=南纬。控制地图覆盖的纬度范围。")]
        [Range(-90f, 90f)] public float NorthLatitude = 70f;

        [Tooltip("地图下边缘对应的纬度（度）。正值=北纬，负值=南纬。控制地图覆盖的纬度范围。")]
        [Range(-90f, 90f)] public float SouthLatitude = -70f;

        [Tooltip("陆地划分出的省份总数。越多省份越细碎，越少省份越大块。")]
        [Range(20, 500)] public int ProvinceCount = 100;

        [Tooltip("随机种子。相同种子+相同设置=相同地图。留空或-1=随机生成。")]
        public int Seed = -1;

        [Tooltip("海平面高度（0~1）。海平面以上为陆地，以下为海洋。导入高度图模式下可调整海陆分界。")]
        [Range(0f, 1f)] public float SeaLevel = 0.5f;

        /// <summary>地图尺寸预设</summary>
        public enum MapSizePreset
        {
            [Tooltip("小地图：128×64=8192地块。快速生成，适合测试。")]
            Small,      // 128×64
            [Tooltip("中地图：256×128=32768地块。平衡性能与细节，默认推荐。")]
            Medium,     // 256×128
            [Tooltip("大地图：512×256=131072地块。细节丰富，生成较慢。")]
            Large,      // 512×256
            [Tooltip("超大地图：1024×512=524288地块。极致细节，生成很慢，需高性能电脑。")]
            ExtraLarge  // 1024×512
        }

        /// <summary>获取地图尺寸（宽, 高）</summary>
        public (int width, int height) GetMapDimensions()
        {
            return MapSize switch
            {
                MapSizePreset.Small => (128, 64),
                MapSizePreset.Medium => (256, 128),
                MapSizePreset.Large => (512, 256),
                MapSizePreset.ExtraLarge => (1024, 512),
                _ => (256, 128)
            };
        }

        /// <summary>获取实际种子（-1时随机）</summary>
        public int GetActualSeed()
        {
            return Seed < 0 ? new System.Random().Next() : Seed;
        }

        // ============================================================
        // 【程序化地形参数】（仅程序化模式可见，5个）
        // ============================================================

        [Header("【程序化地形】（仅程序化生成模式）")]
        [Tooltip("陆地占全球面积的比例。地球≈29%。分位数归一化确保精准。")]
        [Range(0.15f, 0.60f)] public float LandFraction = 0.30f;

        [Tooltip("地形起伏程度。合并控制噪声频率和域扭曲强度。低=平坦大陆，高=崎岖破碎。")]
        [Range(0.5f, 2.0f)] public float TerrainRoughness = 1.0f;

        [Tooltip("山脉叠加高度。控制山脊噪声和板块边界造山强度。")]
        [Range(0f, 0.80f)] public float MountainIntensity = 0.35f;

        [Tooltip("陆地破碎度。控制半岛、岛屿、海湾的数量。低=完整大陆，高=群岛密布。")]
        [Range(0f, 1.0f)] public float LandFragmentation = 0.5f;

        [Tooltip("构造板块数量。板块越多，山脉带越密集。球面Voronoi生成。")]
        [Range(6, 20)] public int PlateCount = 12;

        // ============================================================
        // 【气候参数】（所有模式可见，5个）
        // ============================================================

        [Header("【气候】")]
        [Tooltip("行星自转轴倾角（度）。0=无季节，23.44=地球标准，45=极端季节。控制纬度带宽度和季节强度。")]
        [Range(0f, 45f)] public float AxialTiltDegrees = 23.44f;

        [Tooltip("全球平均温度偏移（度C）。负值=冰期（冰川扩张），正值=间冰期（温暖湿润）。")]
        [Range(-8f, 8f)] public float GlobalTemperatureOffset = 0f;

        [Tooltip("大气环流模式。单圈=极端气候（赤道极热极地极冷），三圈=地球标准（Hadley/Ferrel/Polar），增强季风=海陆热力差异放大。")]
        public CirculationMode Circulation = CirculationMode.ThreeCell;

        [Tooltip("季风强度。控制季节性风向反转。0=无季风，1=强季风（东亚/南亚式）。")]
        [Range(0f, 1.0f)] public float MonsoonStrength = 0.6f;

        [Tooltip("热赤道偏移（纬度）。全球降水带南北偏移。正值=北移，负值=南移。影响沙漠带和雨林带位置。0=热赤道位于地图中央。")]
        [Range(-15f, 15f)] public float ThermalEquatorOffset = 0f;

        /// <summary>大气环流模式</summary>
        public enum CirculationMode
        {
            [Tooltip("单圈环流：赤道-极地直接环流。极端气候，赤道极热极地极冷，气候对比弱。")]
            SingleCell,
            [Tooltip("双圈环流：简化的两圈环流。气候对比中等。")]
            DoubleCell,
            [Tooltip("三圈环流：地球标准（Hadley 0-30度 / Ferrel 30-60度 / Polar 60-90度）。气候带分明，对比最强。")]
            ThreeCell,
            [Tooltip("增强季风：三圈基础上海陆热力差异放大，季节性风向反转强烈。东亚/南亚式气候。")]
            EnhancedMonsoon
        }

        // ============================================================
        // 【水文与地貌参数】（所有模式可见，2个）
        // ============================================================

        [Header("【水文与地貌】")]
        [Tooltip("河网密度。控制形成河流所需的汇水面积阈值。稀疏=大河少，密集=河网密布。")]
        [Range(0f, 1.0f)] public float RiverDensity = 0.5f;

        [Tooltip("水力侵蚀强度。0=无侵蚀（原始地形），1=强侵蚀（深谷峡谷）。粒子基侵蚀模拟。")]
        [Range(0f, 1.0f)] public float ErosionIntensity = 0.3f;

        // ============================================================
        // 【高级参数】（折叠面板，所有模式下程序化参数仅程序化模式可见）
        // ============================================================

        [Header("【高级 · 地形细节】（仅程序化模式）")]
        [Range(0.5f, 4.0f)] public float TerrainFrequency = 1.8f;
        [Range(3, 10)] public int TerrainOctaves = 6;
        [Range(0f, 1.5f)] public float WarpStrength = 0.7f;
        [Range(0.5f, 3.0f)] public float WarpFrequency = 1.3f;
        [Range(0f, 0.5f)] public float PlateBoundaryBoost = 0.25f;

        [Header("【高级 · 气候细节】")]
        [Range(200f, 1500f)] public float OrographicPrecipFactor = 800f;
        [Range(0.1f, 0.8f)] public float RainShadowFactor = 0.3f;

        [Header("【高级 · 侵蚀细节】")]
        [Range(0.1f, 0.8f)] public float ErosionRate = 0.3f;
        [Range(0.1f, 0.8f)] public float DepositionRate = 0.3f;
        [Range(0.005f, 0.05f)] public float EvaporationRate = 0.02f;
        [Range(0f, 0.5f)] public float ErosionInertia = 0.05f;

        // ============================================================
        // 固定参数（不暴露给玩家）
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

        /// <summary>程序化地形参数是否可见（仅程序化生成模式）</summary>
        public bool IsProceduralParamsVisible => Mode == GenerationMode.Procedural;

        /// <summary>高级地形细节参数是否可见</summary>
        public bool IsAdvancedTerrainVisible => Mode == GenerationMode.Procedural;

        // ============================================================
        // 方法：将配置应用到各个生成器
        // ============================================================

        /// <summary>将配置应用到PlanetTerrainGenerator</summary>
        public void ApplyToGenerator(PlanetTerrainGenerator gen)
        {
            if (Mode == GenerationMode.Procedural)
            {
                gen.TargetLandFraction = LandFraction;
                gen.MountainHeight = MountainIntensity;
                gen.PlateCount = PlateCount;
                gen.PlateBoundaryMountainBoost = PlateBoundaryBoost;
                gen.TerrainFrequency = TerrainFrequency * TerrainRoughness;
                gen.WarpStrength = WarpStrength * TerrainRoughness;
                gen.WarpFrequency = WarpFrequency * (0.5f + LandFragmentation);
                gen.TerrainOctaves = TerrainOctaves;
            }
            // 导入高度图/空白模式：不设置程序化参数，由外部直接设置高程
            gen.AxialTilt = AxialTiltDegrees * Mathf.Deg2Rad;
            gen.GlobalTempOffset = GlobalTemperatureOffset;
            gen.RiverThreshold = Mathf.Lerp(200f, 20f, RiverDensity);
        }

        /// <summary>将配置应用到AtmosphericCirculation</summary>
        public void ApplyToGCM(AtmosphericCirculation gcm)
        {
            gcm.AxialTilt = AxialTiltDegrees * Mathf.Deg2Rad;
            gcm.MonsoonStrength = MonsoonStrength * (Circulation == CirculationMode.EnhancedMonsoon ? 1.5f : 1f);
            gcm.OrographicPrecipFactor = OrographicPrecipFactor;
            gcm.RainShadowFactor = RainShadowFactor;
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

        // ============================================================
        // 预设模板
        // ============================================================

        public static MapGenerationConfig Default() => new MapGenerationConfig();

        public static MapGenerationConfig EarthLike()
        {
            return new MapGenerationConfig
            {
                Mode = GenerationMode.Procedural,
                MapSize = MapSizePreset.Medium,
                NorthLatitude = 70f,
                SouthLatitude = -70f,
                ProvinceCount = 100,
                SeaLevel = 0.5f,
                LandFraction = 0.29f,
                TerrainRoughness = 1.0f,
                MountainIntensity = 0.35f,
                LandFragmentation = 0.5f,
                PlateCount = 12,
                AxialTiltDegrees = 23.44f,
                GlobalTemperatureOffset = 0f,
                Circulation = CirculationMode.ThreeCell,
                MonsoonStrength = 0.6f,
                ThermalEquatorOffset = 0f,
                RiverDensity = 0.5f,
                ErosionIntensity = 0.3f
            };
        }

        public static MapGenerationConfig IceAge()
        {
            var cfg = EarthLike();
            cfg.GlobalTemperatureOffset = -6f;
            cfg.LandFraction = 0.35f;
            cfg.RiverDensity = 0.3f;
            cfg.SeaLevel = 0.45f;
            return cfg;
        }

        public static MapGenerationConfig Hothouse()
        {
            var cfg = EarthLike();
            cfg.GlobalTemperatureOffset = 6f;
            cfg.LandFraction = 0.22f;
            cfg.RiverDensity = 0.7f;
            cfg.MonsoonStrength = 0.8f;
            cfg.SeaLevel = 0.55f;
            return cfg;
        }

        public static MapGenerationConfig Arid()
        {
            var cfg = EarthLike();
            cfg.GlobalTemperatureOffset = 2f;
            cfg.MonsoonStrength = 0.2f;
            cfg.RiverDensity = 0.2f;
            cfg.ThermalEquatorOffset = 5f;
            return cfg;
        }
    }
}
