using System;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 地图生成参数配置（玩家可见 + 高级隐藏）
    ///
    /// 设计原则：
    ///   1. 先编辑地形，再计算派生数据（气候/水文/群系）
    ///   2. 玩家可见参数精简（12个），比FantasyMapSimulator(9个)略多
    ///   3. 分类清晰：地形生成 / 气候 / 水文与地貌
    ///   4. 高级参数折叠，默认不显示
    ///   5. 所有参数有默认值，好上手
    ///
    /// 工作流：
    ///   阶段1：地形创建（程序化生成 / 空白手绘 / 导入高度图）
    ///   阶段2：地形编辑（画笔：升高/降低/平滑/噪声 + 海平面调整）
    ///   阶段3：气候与水文计算（点击"计算气候"按钮，自动计算温度→降水→洋流→侵蚀→河网→群系）
    /// </summary>
    [Serializable]
    public class MapGenerationConfig
    {
        // ============================================================
        // 【玩家可见 · 地形生成】（5个参数，程序化生成时显示）
        // ============================================================

        [Header("【地形生成】")]
        [Tooltip("陆地占全球面积的比例。地球≈29%。越高陆地越多。")]
        [Range(0.15f, 0.60f)] public float LandFraction = 0.30f;

        [Tooltip("地形起伏程度。控制整体地形频率和域扭曲强度。低=平坦大陆，高=崎岖破碎。")]
        [Range(0.5f, 2.0f)] public float TerrainRoughness = 1.0f;

        [Tooltip("山脉叠加高度。控制山脊噪声和板块边界造山强度。")]
        [Range(0f, 0.80f)] public float MountainIntensity = 0.35f;

        [Tooltip("陆地破碎度。控制半岛、岛屿、海湾的数量。低=完整大陆，高=群岛密布。")]
        [Range(0f, 1.0f)] public float LandFragmentation = 0.5f;

        [Tooltip("构造板块数量。板块越多，山脉带越密集。6=少板块大大陆，20=多板块破碎。")]
        [Range(6, 20)] public int PlateCount = 12;

        // ============================================================
        // 【玩家可见 · 气候】（5个参数）
        // ============================================================

        [Header("【气候】")]
        [Tooltip("行星自转轴倾角（度）。0=无季节，23.44=地球标准，45=极端季节。控制纬度带宽度和季节强度。")]
        [Range(0f, 45f)] public float AxialTiltDegrees = 23.44f;

        [Tooltip("全球平均温度偏移（°C）。负值=冰期（冰川扩张、沙漠扩大），正值=间冰期（温暖湿润）。")]
        [Range(-8f, 8f)] public float GlobalTemperatureOffset = 0f;

        [Tooltip("大气环流模式。单圈=简单赤道-极地环流（极端气候），三圈=地球标准（Hadley/Ferrel/Polar），增强季风=海陆热力差异放大。")]
        public CirculationMode Circulation = CirculationMode.ThreeCell;

        [Tooltip("季风强度。控制海陆热力差异导致的季节性风向反转。0=无季风，1=强季风（东亚/南亚式）。")]
        [Range(0f, 1.0f)] public float MonsoonStrength = 0.6f;

        [Tooltip("热赤道偏移（纬度）。控制全球降水带的南北偏移。正值=北移，负值=南移。影响沙漠带和雨林带位置。")]
        [Range(-15f, 15f)] public float ThermalEquatorOffset = 0f;

        // ============================================================
        // 【玩家可见 · 水文与地貌】（2个参数）
        // ============================================================

        [Header("【水文与地貌】")]
        [Tooltip("河网密度。控制形成河流所需的汇水面积阈值。稀疏=大河少，密集=河网密布。")]
        [Range(0f, 1.0f)] public float RiverDensity = 0.5f;

        [Tooltip("水力侵蚀强度。控制雨滴粒子对地形的雕塑程度。0=无侵蚀（原始地形），1=强侵蚀（深谷峡谷）。")]
        [Range(0f, 1.0f)] public float ErosionIntensity = 0.3f;

        // ============================================================
        // 【高级设置 · 地形】（默认折叠，玩家可选展开）
        // ============================================================

        [Header("【高级 · 地形细节】")]
        [Tooltip("基础噪声频率。高=细节多但规模小，低=大尺度地形。")]
        [Range(0.5f, 4.0f)] public float TerrainFrequency = 1.8f;

        [Tooltip("噪声倍频数。高=更多细节层次，性能开销增大。")]
        [Range(3, 10)] public int TerrainOctaves = 6;

        [Tooltip("域扭曲强度。控制地形的自然弯曲程度。高=更扭曲的海岸线和山脉。")]
        [Range(0f, 1.5f)] public float WarpStrength = 0.7f;

        [Tooltip("域扭曲频率。控制扭曲的尺度。")]
        [Range(0.5f, 3.0f)] public float WarpFrequency = 1.3f;

        [Tooltip("板块边界山脉加成。板块汇聚边界的额外抬升量。")]
        [Range(0f, 0.5f)] public float PlateBoundaryBoost = 0.25f;

        // ============================================================
        // 【高级设置 · 气候细节】
        // ============================================================

        [Header("【高级 · 气候细节】")]
        [Tooltip("地形降水系数。迎风坡抬升降水的强度。")]
        [Range(200f, 1500f)] public float OrographicPrecipFactor = 800f;

        [Tooltip("雨影效应系数。背风坡降水保留比例。低=强雨影（沙漠），高=弱雨影。")]
        [Range(0.1f, 0.8f)] public float RainShadowFactor = 0.3f;

        // ============================================================
        // 【高级设置 · 侵蚀细节】
        // ============================================================

        [Header("【高级 · 侵蚀细节】")]
        [Tooltip("侵蚀率。粒子携带沉积物的能力。")]
        [Range(0.1f, 0.8f)] public float ErosionRate = 0.3f;

        [Tooltip("沉积率。粒子沉积沉积物的速度。")]
        [Range(0.1f, 0.8f)] public float DepositionRate = 0.3f;

        [Tooltip("蒸发率。每步水量减少比例。高=粒子寿命短（侵蚀范围小）。")]
        [Range(0.005f, 0.05f)] public float EvaporationRate = 0.02f;

        [Tooltip("惯性。粒子保持方向的能力。0=完全沿坡度，1=完全保持原方向。")]
        [Range(0f, 0.5f)] public float ErosionInertia = 0.05f;

        // ============================================================
        // 【系统 · 固定参数（不暴露给玩家）】
        // ============================================================

        /// <summary>行星自转角速度（rad/s，地球值）。物理常数，不暴露。</summary>
        public const float PlanetOmega = 7.292e-5f;
        /// <summary>行星半径（m，地球值）。物理常数，不暴露。</summary>
        public const float PlanetRadius = 6371000f;
        /// <summary>太阳常数（W/m²，地球值）。物理常数，不暴露。</summary>
        public const float SolarConstant = 1361f;
        /// <summary>是否启用多线程。默认开启，不暴露。</summary>
        public const bool UseMultithreading = true;
        /// <summary>洋流迭代次数。固定20次，效果足够。</summary>
        public const int OceanIterations = 20;
        /// <summary>侵蚀粒子最大寿命。固定64步。</summary>
        public const int ErosionMaxLifetime = 64;

        // ============================================================
        // 枚举
        // ============================================================

        /// <summary>大气环流模式</summary>
        public enum CirculationMode
        {
            [Tooltip("单圈环流：赤道-极地直接环流。极端气候，赤道极热极地极冷。")]
            SingleCell,
            [Tooltip("三圈环流：地球标准（Hadley 0-30° / Ferrel 30-60° / Polar 60-90°）。气候带分明。")]
            ThreeCell,
            [Tooltip("增强季风：海陆热力差异放大，季节性风向反转强烈。东亚/南亚式气候。")]
            EnhancedMonsoon
        }

        // ============================================================
        // 方法：将配置应用到各个生成器
        // ============================================================

        /// <summary>将配置应用到PlanetTerrainGenerator</summary>
        public void ApplyToGenerator(PlanetTerrainGenerator gen)
        {
            // 玩家可见参数 → 生成器内部参数
            gen.TargetLandFraction = LandFraction;
            gen.MountainHeight = MountainIntensity;
            gen.PlateCount = PlateCount;
            gen.PlateBoundaryMountainBoost = PlateBoundaryBoost;
            gen.AxialTilt = AxialTiltDegrees * Mathf.Deg2Rad;
            gen.GlobalTempOffset = GlobalTemperatureOffset;
            gen.RiverThreshold = Mathf.Lerp(200f, 20f, RiverDensity); // 密度高→阈值低→河流多

            // 地形起伏度 → 合并频率和扭曲
            gen.TerrainFrequency = TerrainFrequency * TerrainRoughness;
            gen.WarpStrength = WarpStrength * TerrainRoughness;
            gen.WarpFrequency = WarpFrequency * (0.5f + LandFragmentation);
            gen.TerrainOctaves = TerrainOctaves;
        }

        /// <summary>将配置应用到AtmosphericCirculation</summary>
        public void ApplyToGCM(AtmosphericCirculation gcm)
        {
            gcm.AxialTilt = AxialTiltDegrees * Mathf.Deg2Rad;
            gcm.MonsoonStrength = MonsoonStrength * (Circulation == CirculationMode.EnhancedMonsoon ? 1.5f : 1f);
            gcm.OrographicPrecipFactor = OrographicPrecipFactor;
            gcm.RainShadowFactor = RainShadowFactor;

            // 环流模式影响（简化：SingleCell修改纬度降水带）
            // 具体在GCM内部根据模式调整
        }

        /// <summary>将配置应用到HydraulicErosion</summary>
        public void ApplyToErosion(HydraulicErosion erosion, int totalTiles)
        {
            // 侵蚀强度 → 粒子数量和侵蚀率
            erosion.ParticleCount = (int)(ErosionIntensity * Mathf.Min(150000, totalTiles / 3));
            erosion.ErosionRate = ErosionRate * (0.5f + ErosionIntensity);
            erosion.DepositionRate = DepositionRate;
            erosion.EvaporationRate = EvaporationRate;
            erosion.Inertia = ErosionInertia;
        }

        /// <summary>获取默认配置</summary>
        public static MapGenerationConfig Default()
        {
            return new MapGenerationConfig();
        }

        /// <summary>预设：地球类行星</summary>
        public static MapGenerationConfig EarthLike()
        {
            return new MapGenerationConfig
            {
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

        /// <summary>预设：冰期星球</summary>
        public static MapGenerationConfig IceAge()
        {
            var cfg = EarthLike();
            cfg.GlobalTemperatureOffset = -6f;
            cfg.LandFraction = 0.35f; // 海平面下降，陆地增多
            cfg.RiverDensity = 0.3f; // 冰川侵蚀，河流少
            return cfg;
        }

        /// <summary>预设：温室星球</summary>
        public static MapGenerationConfig Hothouse()
        {
            var cfg = EarthLike();
            cfg.GlobalTemperatureOffset = 6f;
            cfg.LandFraction = 0.22f; // 海平面上升，陆地减少
            cfg.RiverDensity = 0.7f; // 降水多，河网密
            cfg.MonsoonStrength = 0.8f;
            return cfg;
        }

        /// <summary>预设：干旱星球</summary>
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
