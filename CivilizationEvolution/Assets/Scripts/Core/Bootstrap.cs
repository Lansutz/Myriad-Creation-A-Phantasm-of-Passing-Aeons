using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Core
{
    /// <summary>
    /// 游戏启动入口
    /// 挂载到场景中的Bootstrap物体上，自动初始化GameManager和GameWorld
    /// </summary>
    /// <summary>地图尺寸预设（宽×高，总地块数）</summary>
    public enum MapSizePreset
    {
        Tiny,    // 64×32 = 2,048 地块
        Small,   // 128×64 = 8,192 地块
        Medium,  // 256×128 = 32,768 地块
        Large,   // 512×256 = 131,072 地块
        Huge,    // 1024×512 = 524,288 地块
        Reference // 1920×1080 = 2,073,600 地块（对齐参考项目）
    }

    public class Bootstrap : MonoBehaviour
    {
        [Header("地图配置")]
        [SerializeField] private MapSizePreset mapSizePreset = MapSizePreset.Large;
        // 运行时从预设解析的实际尺寸
        private int _mapWidth;
        private int _mapHeight;
        [SerializeField] private int randomSeed = 42;
        [SerializeField] private bool autoStartOnBoot = true;

        [Header("引用")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Light directionalLight;

        private void Awake()
        {
            // 从预设解析地图尺寸
            (_mapWidth, _mapHeight) = mapSizePreset switch
            {
                MapSizePreset.Tiny => (64, 32),
                MapSizePreset.Small => (128, 64),
                MapSizePreset.Medium => (256, 128),
                MapSizePreset.Large => (512, 256),
                MapSizePreset.Huge => (1024, 512),
                MapSizePreset.Reference => (1920, 1080),
                _ => (256, 128)
            };

            Debug.Log($"[{GameConstants.GameNameShort}] {GameConstants.GameNameEn} | {GameConstants.GameNameZh} | v{GameConstants.Version}");

            // 加载内容注册表（Base/Mods 双目录数据驱动）
            ContentRegistry.Initialize();

            // 本地化初始化（键→文本；缺键回退键名；语言码走 BCP 47 国际标准）
            Localization.Initialize("zh-Hans");

            // 确保GameManager存在
            if (GameManager.Instance == null)
            {
                var gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }

            // 相机设置
            if (mainCamera == null)
                mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = _mapHeight * 0.6f;
                mainCamera.transform.position = new Vector3(_mapWidth * 0.5f, 100f, _mapHeight * 0.5f);
                mainCamera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color(0.1f, 0.15f, 0.25f);
            }

            // 灯光设置
            if (directionalLight != null)
            {
                directionalLight.type = LightType.Directional;
                directionalLight.intensity = 1.2f;
                directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            Debug.Log("[Bootstrap] 游戏初始化完成");
        }

        private void Start()
        {
            if (autoStartOnBoot)
            {
                StartNewGame();
            }
        }

        /// <summary>开始新游戏</summary>
        public void StartNewGame()
        {
            GameManager.Instance.StartNewGame(_mapWidth, _mapHeight, randomSeed);
            Debug.Log($"[Bootstrap] 新游戏已启动：{_mapWidth}x{_mapHeight}（{mapSizePreset}），种子={randomSeed}，总地块={_mapWidth * _mapHeight}");
        }

        /// <summary>保存游戏</summary>
        public void SaveGame(string saveName = "autosave")
        {
            GameManager.Instance.SaveGame(saveName);
        }

        /// <summary>加载游戏</summary>
        public void LoadGame(string saveName)
        {
            GameManager.Instance.LoadGame(saveName);
        }

        private void Update()
        {
            // F5快速保存
            if (Input.GetKeyDown(KeyCode.F5))
            {
                SaveGame("quicksave");
                Debug.Log("[Bootstrap] 快速保存完成");
            }
            // F9快速加载
            if (Input.GetKeyDown(KeyCode.F9))
            {
                LoadGame("quicksave");
                Debug.Log("[Bootstrap] 快速加载完成");
            }
        }
    }
}
