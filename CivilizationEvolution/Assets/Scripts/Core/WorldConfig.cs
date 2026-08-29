using UnityEngine;

namespace CivilizationEvolution.Core
{
    /// <summary>
    /// 世界配置（ScriptableObject 资产）
    /// 在 Project 窗口右键 Create/Civilization Evolution/World Config 创建配置预设，
    /// 可保存多套海陆/气候参数（如盘古大陆、群岛世界），挂到 GameWorld.config 上使用。
    /// 运行时 GameWorld 会 Instantiate 一份副本，修改不会污染资产。
    /// 注意：ScriptableObject 必须定义在与类名同名的文件中，否则 .asset 无法绑定脚本。
    /// </summary>
    [CreateAssetMenu(fileName = "WorldConfig", menuName = "Civilization Evolution/World Config", order = 0)]
    public class WorldConfig : ScriptableObject
    {
        [Header("地图形状")]
        [Tooltip("是否左右连通（环绕世界）")]
        public bool wrapX = true;
        [Tooltip("是否上下连通（完整环面）")]
        public bool wrapY = false;

        [Header("海陆参数")]
        [Range(0f, 1f)] public float seaLevel = 0.5f;
        [Range(0.1f, 0.8f)] public float landAmount = 0.29f;
        [Range(0f, 1f)] public float landFragment = 0.4f;
        [Range(0f, 1f)] public float coastFragment = 0.3f;
        [Range(0f, 1f)] public float oceanBuffer = 0.35f;
        [Range(70f, 90f)] public float planetMaxLat = 90f;
        [Range(280f, 400f)] public float planetTotalLon = 360f;

        [Header("气候参数-基础")]
        public GameEnums.CirculationMode circulationMode = GameEnums.CirculationMode.TripleCell;
        [Range(-30f, 30f)] public float thermalEquatorLat = 7f;
        [Range(0f, 50f)] public float climTropicNorth = 23f;
        [Range(-50f, 0f)] public float climTropicSouth = -23f;

        [Header("气候参数-高级")]
        [Range(800f, 1600f)] public float stellarIrradiance = 1361f;
        [Range(0f, 1f)] public float seasonIntensity = 0.4f;
        [Range(0.2f, 1.8f)] public float heatTransport = 1.0f;
        [Range(0f, 1.2f)] public float monsoonStrength = 0.8f;

        [Header("内部物理常量")]
        [Tooltip("行星反照率")] public float albedo = 0.3f;
        [Tooltip("温室效应增温（摄氏度）")] public float greenhouseFactor = 33f;
        [Tooltip("气温垂直递减率（摄氏度/千米）")] public float lapseRate = 6.5f;

        /// <summary>创建一份带默认值的运行时实例（不依赖资产文件）</summary>
        public static WorldConfig CreateRuntimeInstance()
        {
            var cfg = CreateInstance<WorldConfig>();
            cfg.name = "RuntimeWorldConfig";
            return cfg;
        }
    }
}
