using System;
using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.App.Settings;
using MyriadCreation.App.Contracts;



namespace MyriadCreation.UI
{
 /// 游戏启动入口
 /// 挂载到场景中的Bootstrap物体上，自动初始化GameManager和GameWorld
 /// <summary>地图尺寸预设（宽×高，总地块数）——去掉过小档，最大支持470万+地块</summary>

 /// <summary>地图环绕模式——决定边界是否连通</summary>

    public class Bootstrap : MonoBehaviour
    {
        [Header("地图配置")]
        [SerializeField] private MapSizePreset mapSizePreset = MapSizePreset.Large; // 默认 Large——生成 30-60s 可等
 // 运行时从预设解析的实际尺寸
        private int _mapWidth;
        private int _mapHeight;
        [SerializeField] private int randomSeed = 42;
        [SerializeField] private MapWrapMode wrapMode = MapWrapMode.Cylindrical;
        [SerializeField] private bool autoStartOnBoot = true;
 /// <summary>主菜单模式（true=启动显示主菜单——点开始游戏才建世界——
 /// 用于正式游戏流程；编辑器测试可关）</summary>
        [SerializeField] private bool startViaMenu = true;

        [Header("引用")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Light directionalLight;

        private void Awake()
        {
// B3-part2d：App 只依赖 App.Contracts；UiFactory 由 UI 程序集自注册（Bootstrap 不再引用 UI 具体类型）。
            AppServices.RegisterSettings(new AppSettingsService());
            AppServices.RegisterSaveSlots(new MyriadCreation.Infrastructure.Save.SaveSlotService());

// 启动应用玩家设置（分辨率/显示模式/vSync/画质/帧率/UI 缩放/音量/语言），
// 必须先于任何 UI/相机 构建，保证设置面板打开时看到的就是当前生效值。
            SettingsService.ApplySaved();

// 新游戏默认地图尺寸：优先使用设置面板保存值（未保存则沿用场景序列化默认），再解析实际尺寸
            mapSizePreset = SettingsService.GetDefaultMapSizePreset(mapSizePreset);
 // 从预设解析地图尺寸
            (_mapWidth, _mapHeight) = mapSizePreset switch
            {
                MapSizePreset.Large => (1024, 512),
                MapSizePreset.Huge => (2048, 1024),
                MapSizePreset.Reference => (1920, 1080),
                MapSizePreset.Enormous => (3072, 1536),
                _ => (512, 256)
            };

            Debug.Log($"[{GameConstants.GameNameShort}] {GameConstants.GameNameEn} | {GameConstants.GameNameZh} | v{GameConstants.Version}");

 // 加载内容注册表（Base/Mods 双目录数据驱动）
            ContentRegistry.Initialize();

 // 本地化初始化（键→文本；缺键回退键名；语言码走 BCP 47 国际标准）
// 语言优先使用设置面板保存值（ApplySaved 已按保存语言初始化过一次，这里保持一致）。
            Localization.Initialize(SettingsService.Current.Language);

 // 确保GameManager存在
            if (GameManager.Instance == null)
            {
                var gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }

            // B8：地图编辑服务不再走 AppServices 静态注册，由 GameManager 显式注入 Rendering 控制器。
            if (GameManager.Instance != null)
            {
                GameManager.Instance.InjectMapEditService();
                GameManager.Instance.InjectUiReadViews();
            }

// 把 Bootstrap 解析出的默认地图参数注入会话：主菜单开始游戏（SceneFlow → IGameSession）沿用该配置。
            AppServices.GameSession?.ConfigureNewGameDefaults(_mapWidth, _mapHeight, randomSeed, wrapMode);

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

 // 初始化场景链路控制器（主菜单→生成→游戏→返回主菜单）
            if (MyriadCreation.UI.SceneFlowController.Instance == null)
            {
                var flowObj = new GameObject("SceneFlowController");
                flowObj.AddComponent<MyriadCreation.UI.SceneFlowController>();
            }
        }

        private void Start()
        {
 // 主菜单模式：延迟到玩家点「开始游戏」（MainMenuUI.OnQuickStart → SceneFlowController.GenerateWorldWithLoading）
            if (autoStartOnBoot && !startViaMenu)
            {
                StartNewGame();
            }
            else if (autoStartOnBoot && startViaMenu)
            {
 // 通知 UIManager 显示主菜单（世界未建——等玩家操作）
                Debug.Log("[Bootstrap] 主菜单模式——等待玩家开始游戏");
            }
        }

 /// <summary>开始新游戏（世界生成面板——尺寸预设+种子参数化）</summary>
        public void StartNewGame()
        {
            GameManager.Instance.StartNewGame(_mapWidth, _mapHeight, randomSeed, wrapMode);
            Debug.Log($"[Bootstrap] 新游戏已启动：{_mapWidth}x{_mapHeight}（{mapSizePreset}），种子={randomSeed}，总地块={_mapWidth * _mapHeight}");
        }

 /// <summary>异步开始新游戏（loading 面板可用；省份后台生成）。</summary>
        /// <summary>带初始内容选项开始新游戏。</summary>
        public void StartNewGame(MyriadCreation.World.Generation.WorldSetupOptions setupOptions)
        {
            GameManager.Instance.StartNewGame(_mapWidth, _mapHeight, randomSeed, wrapMode, setupOptions);
        }

        public void StartNewGameAsync(Action onComplete = null)
        {
            GameManager.Instance.StartNewGameAsync(_mapWidth, _mapHeight, randomSeed, wrapMode, onComplete);
            Debug.Log($"[Bootstrap] 异步新游戏：{_mapWidth}x{_mapHeight}，种子={randomSeed}");
        }

 /// <summary>异步开始新游戏（带初始内容选项；loading/状态由 GameManager 管理）。</summary>
        public void StartNewGameAsyncWithOptions(MyriadCreation.World.Generation.WorldSetupOptions setupOptions, Action onComplete = null)
        {
            GameManager.Instance.StartNewGameAsync(_mapWidth, _mapHeight, randomSeed, wrapMode, onComplete, setupOptions);
            Debug.Log($"[Bootstrap] 异步新游戏（自定义初始内容）：{_mapWidth}x{_mapHeight}，种子={randomSeed}");
        }

 /// <summary>按生成面板参数配置并开始（尺寸预设索引——0 Large/1 Huge/2 Enormous）</summary>
        public void ConfigureAndStartNewGame(int presetIndex, int seed)
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
