using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Core
{
 /// 游戏启动入口 /// 挂载到场景中的Bootstrap物体上，自动初始化GameManager和GameWorld /// <summary>地图尺寸预设（宽×高，总地块数）——去掉过小档，最大支持470万+地块</summary>    public enum MapSizePreset
    {
 // 小尺寸已删除（过小世界无意义——最小 Large—— // 尺寸精神照架空地图模拟器[FMS 8192×4096 级——技术上限 Enormous]）        Large,     // 1024×512 = 524,288 地块（最小）
        Huge,      // 2048×1024 = 2,097,152 地块（默认——平衡）
        Reference, // 1920×1080 = 2,073,600 地块（对齐参考项目）
        Enormous   // 3072×1536 = 4,718,592 地块（最大——需16GB+内存）
    }

 /// <summary>地图环绕模式——决定边界是否连通</summary>    public enum MapWrapMode
    {
        Flat,        // 平面：四边都不连通，标准矩形地图
        Cylindrical, // 柱面：左右连通（东西环绕），上下不连通——模拟地球
        Toroidal     // 环面：左右上下全连通
    }

    public class Bootstrap : MonoBehaviour
    {
        [Header("地图配置")]
        [SerializeField] private MapSizePreset mapSizePreset = MapSizePreset.Large; // 默认 Large——生成 30-60s 可等
 // 运行时从预设解析的实际尺寸        private int _mapWidth;
        private int _mapHeight;
        [SerializeField] private int randomSeed = 42;
        [SerializeField] private MapWrapMode wrapMode = MapWrapMode.Cylindrical;
        [SerializeField] private bool autoStartOnBoot = true;
 /// <summary>主菜单模式（true=启动显示主菜单——点开始游戏才建世界—— /// 用于正式游戏流程；编辑器测试可关）</summary>        [SerializeField] private bool startViaMenu = true;

        [Header("引用")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Light directionalLight;

        private void Awake()
        {
 // 从预设解析地图尺寸            (_mapWidth, _mapHeight) = mapSizePreset switch
            {
                MapSizePreset.Large => (1024, 512),
                MapSizePreset.Huge => (2048, 1024),
                MapSizePreset.Reference => (1920, 1080),
                MapSizePreset.Enormous => (3072, 1536),
                _ => (512, 256)
            };

            Debug.Log($"[{GameConstants.GameNameShort}] {GameConstants.GameNameEn} | {GameConstants.GameNameZh} | v{GameConstants.Version}");

 // 加载内容注册表（Base/Mods 双目录数据驱动）            ContentRegistry.Initialize();

 // 本地化初始化（键→文本；缺键回退键名；语言码走 BCP 47 国际标准）            Localization.Initialize("zh-Hans");

 // 确保GameManager存在            if (GameManager.Instance == null)
            {
                var gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }

 // 相机设置            if (mainCamera == null)
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

 // 灯光设置            if (directionalLight != null)
            {
                directionalLight.type = LightType.Directional;
                directionalLight.intensity = 1.2f;
                directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            Debug.Log("[Bootstrap] 游戏初始化完成");

 // 初始化场景链路控制器（主菜单→生成→游戏→返回主菜单）            if (CivilizationEvolution.UI.SceneFlowController.Instance == null)
            {
                var flowObj = new GameObject("SceneFlowController");
                flowObj.AddComponent<CivilizationEvolution.UI.SceneFlowController>();
            }
        }

        private void Start()
        {
 // 主菜单模式：延迟到玩家点"开始游戏"（UIManager.StartGameFromMenu 调 StartNewGame）            if (autoStartOnBoot && !startViaMenu)
            {
                StartNewGame();
            }
            else if (autoStartOnBoot && startViaMenu)
            {
 // 通知 UIManager 显示主菜单（世界未建——等玩家操作）                Debug.Log("[Bootstrap] 主菜单模式——等待玩家开始游戏");
            }
        }

 /// <summary>开始新游戏（世界生成面板——尺寸预设+种子参数化）</summary>        public void StartNewGame()
        {
            GameManager.Instance.StartNewGame(_mapWidth, _mapHeight, randomSeed, wrapMode);
            Debug.Log($"[Bootstrap] 新游戏已启动：{_mapWidth}x{_mapHeight}（{mapSizePreset}），种子={randomSeed}，总地块={_mapWidth * _mapHeight}");
        }

 /// <summary>按生成面板参数配置并开始（尺寸预设索引——0 Large/1 Huge/2 Enormous）</summary>        public void ConfigureAndStartNewGame(int presetIndex, int seed)
        {
            mapSizePreset = presetIndex switch
            {
                0 => MapSizePreset.Large,
                1 => MapSizePreset.Huge,
                _ => MapSizePreset.Enormous
            };
            randomSeed = seed;
            (_mapWidth, _mapHeight) = mapSizePreset switch
            {
                MapSizePreset.Large => (1024, 512),
                MapSizePreset.Huge => (2048, 1024),
                MapSizePreset.Reference => (1920, 1080),
                MapSizePreset.Enormous => (3072, 1536),
                _ => (1024, 512)
            };
            StartNewGame();
        }

 /// <summary>保存游戏</summary>        public void SaveGame(string saveName = "autosave")
        {
            GameManager.Instance.SaveGame(saveName);
        }

 /// <summary>加载游戏</summary>        public void LoadGame(string saveName)
        {
            GameManager.Instance.LoadGame(saveName);
        }

        private void Update()
        {
 // F5快速保存            if (Input.GetKeyDown(KeyCode.F5))
            {
                SaveGame("quicksave");
                Debug.Log("[Bootstrap] 快速保存完成");
            }
 // F9快速加载            if (Input.GetKeyDown(KeyCode.F9))
            {
                LoadGame("quicksave");
                Debug.Log("[Bootstrap] 快速加载完成");
            }
        }
    }
}
