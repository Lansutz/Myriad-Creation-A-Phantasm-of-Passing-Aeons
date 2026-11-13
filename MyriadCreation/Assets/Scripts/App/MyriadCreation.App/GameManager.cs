using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.App.Contracts;
using MyriadCreation.App.Editing;
using MyriadCreation.App;
using MyriadCreation.Rendering.Editing;



namespace MyriadCreation.UI
{
 /// 游戏全局管理器
 /// 管理游戏状态、场景切换、全局配置
    public class GameManager : MonoBehaviour, IGameSession
    {
        public static GameManager Instance { get; private set; }


        [Header("游戏速度")]
        [UnityEngine.Range(0f, 5f)]
        [SerializeField] private float gameSpeed = 1f;

        [Header("世界引用")]
        [SerializeField] private GameWorld world;

 /// <summary>当前游戏状态（只读）</summary>
        public GameState CurrentState => currentState;
 /// <summary>游戏速度</summary>
        public float GameSpeed => gameSpeed;
 /// <summary>是否暂停（只读）</summary>
        public bool IsPaused => isPaused;
 /// <summary>当前世界（只读外部访问）</summary>
        public GameWorld World => world;

        /// <summary>IGameSession（Core 只读）：是否已有世界（替代旧 World 属性供 SceneFlow liveness）。</summary>
        public bool HasWorld => world != null;

 /// <summary>是否正在游戏（IGameSession 契约）。</summary>
        public bool IsPlaying => currentState == GameState.Playing;

 // 主菜单「开始游戏」使用的默认地图参数：Bootstrap 在创建会话后注入（ConfigureNewGameDefaults）。
        private int _defaultMapWidth = 128;
        private int _defaultMapHeight = 64;
        private int _defaultSeed = 42;
        private MapWrapMode _defaultWrapMode = MapWrapMode.Cylindrical;

 // 运行时状态
        private GameState currentState = GameState.MainMenu;
        private bool isPaused = false;

 // 全局事件
        public event Action<GameState> OnGameStateChanged;
        public event Action<float> OnGameSpeedChanged;
        public event Action OnNewGameStarted;
        public event Action OnGameLoaded;
        public event Action OnGameSaved;

 // 聚落图标 → 详情面板接线（Map 1 / UI 3）：图标层只发事件，UI 层订阅后打开面板
        private MyriadCreation.Rendering.SettlementIconLayer _boundIconLayer;
        private ISettlementDetailUi _settlementDetailPanel;
        private WorldEditService _worldEditService;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            AppServices.RegisterGameSession(this);

            // B8：地图编辑执行落到 App（WorldEditService）；服务由 GameManager 持有并显式注入 Rendering 控制器。
            _worldEditService = new WorldEditService(() => world);
            InjectMapEditService();
            InjectUiReadViews();
        }

        private void Start()
        {
            ChangeState(GameState.MainMenu);
        }

 /// <summary>切换游戏状态</summary>
        private void TryShowGameUi()
        {
            var ui = AppServices.UiHost;
            if (ui != null) ui.ShowGameUI();
        }

        public void ChangeState(GameState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            OnGameStateChanged?.Invoke(newState);

            switch (newState)
            {
                case GameState.Playing:
                    isPaused = false;
                    Time.timeScale = gameSpeed;
                    break;
                case GameState.Paused:
                    isPaused = true;
                    Time.timeScale = 0f;
                    break;
                case GameState.MapEditor:
                    isPaused = true;
                    Time.timeScale = 0f;
                    break;
            }
        }

 /// <summary>开始新游戏</summary>
        public void StartNewGame(int mapWidth = 128, int mapHeight = 64, int seed = 42,
            MapWrapMode wrapMode = MapWrapMode.Cylindrical,
            MyriadCreation.World.Generation.WorldSetupOptions setupOptions = null)
        {
            ChangeState(GameState.Loading);

            if (world == null)
            {
 // 优先复用场景中已存在的 GameWorld（MapRenderer/UIManager/MapEditor 都接线到它）， // 避免再新建第二个 GameWorld 去生成地形、而渲染器仍对着空世界导致黑屏。
                world = FindAnyObjectByType<GameWorld>();
            }
            // B3-Step3：创建/复用 + 尺寸/SetupOptions + InitializeWorld 收敛到 WorldFactory。
            var worldHost = world != null ? world.gameObject : new GameObject("GameWorld");
            world = WorldFactory.Create(worldHost,
                setupOptions ?? MyriadCreation.World.Generation.WorldSetupOptions.CreateDefault(),
                mapWidth, mapHeight);

            new WorldGenerationService(world).GenerateTerrain(seed);
            // B7：世界生成后注入只读地图视图（Rendering 不再需要 Simulation 依赖）。
            InjectMapWorldView();
            // 世界生成 → 聚落图标层重建（代码创建在 MapPlane 上；场景零改动）
            RebindSettlementIcons();

            ChangeState(GameState.Playing);
            TryShowGameUi();
            OnNewGameStarted?.Invoke();
        }

 /// <summary>开始新游戏（异步：地形同步、省份后台生成，完成回调）。</summary>
        public void StartNewGameAsync(int mapWidth = 128, int mapHeight = 64, int seed = 42,
            MapWrapMode wrapMode = MapWrapMode.Cylindrical, Action onComplete = null,
            MyriadCreation.World.Generation.WorldSetupOptions setupOptions = null)
        {
            ChangeState(GameState.Loading);
            if (world == null)
            {
                world = FindAnyObjectByType<GameWorld>();
            }
            // B3-Step3：创建/复用 + 尺寸/SetupOptions + InitializeWorld 收敛到 WorldFactory。
            var worldHost = world != null ? world.gameObject : new GameObject("GameWorld");
            world = WorldFactory.Create(worldHost,
                setupOptions ?? MyriadCreation.World.Generation.WorldSetupOptions.CreateDefault(),
                mapWidth, mapHeight);

            var ui = AppServices.UiHost;
            // Unity 对象用 ==/!= 判销毁（?. 不会走 Unity 的重载，destroyed 对象仍会抛 MissingReferenceException）
            if (ui != null) ui.ShowLoading("世界生成中…");
            StartCoroutine(GenerateWorldAsync(seed, onComplete, ui));
        }

        private IEnumerator GenerateWorldAsync(int seed, Action onComplete,
            IGameUiHost ui)
        {
            var sourceWorld = world;
            if (sourceWorld == null)
            {
                if (ui != null) ui.HideLoading();
                yield break;
            }

            IEnumerator terrain = new WorldGenerationService(sourceWorld).GenerateTerrainAsync(seed, null,
                (progress, message) => { if (ui != null) ui.SetLoadingProgress(progress, message); });

            // 手动驱动嵌套协程：本对象是 DontDestroyOnLoad，场景切换/重新开局时旧世界会被销毁，
            // 若继续 MoveNext 会在已销毁对象上抛 MissingReferenceException 并污染下一个场景/测试。
            while (true)
            {
                if (sourceWorld == null || world != sourceWorld) yield break;
                bool moved;
                try
                {
                    moved = terrain.MoveNext();
                }
                catch (MissingReferenceException)
                {
                    yield break;
                }
                if (!moved) break;
                yield return terrain.Current;
            }

            if (sourceWorld == null || world != sourceWorld) yield break;
            if (ui != null) ui.HideLoading();
            // B7：异步世界生成完成后注入只读地图视图。
            InjectMapWorldView();
            // 异步生成完成 → 聚落图标层重建
            RebindSettlementIcons();
            ChangeState(GameState.Playing);
            TryShowGameUi();
            OnNewGameStarted?.Invoke();
            onComplete?.Invoke();
        }

        // ===== IGameSession：默认参数 + 无参数入口（主菜单经 SceneFlow 调用） =====
        public void ConfigureNewGameDefaults(int mapWidth, int mapHeight, int seed, MapWrapMode wrapMode)
        {
            _defaultMapWidth = Mathf.Max(1, mapWidth);
            _defaultMapHeight = Mathf.Max(1, mapHeight);
            _defaultSeed = seed;
            _defaultWrapMode = wrapMode;
        }

        public void StartNewGameAsync(Action onComplete)
        {
            StartNewGameAsync(_defaultMapWidth, _defaultMapHeight, _defaultSeed, _defaultWrapMode, onComplete, null);
        }

        public void StartNewGameAsync(MyriadCreation.World.Generation.WorldSetupOptions setupOptions, Action onComplete)
        {
            StartNewGameAsync(_defaultMapWidth, _defaultMapHeight, _defaultSeed, _defaultWrapMode, onComplete, setupOptions);
        }

        // ===== IGameSession（4d-UI）：Core DTO 入口；WorldSetupOptions 只保留在具体类内部 =====
        public void StartNewGame(NewGameRequestDto request)
        {
            if (request == null)
            {
                StartNewGame(_defaultMapWidth, _defaultMapHeight, _defaultSeed, _defaultWrapMode, null);
                return;
            }

            int width = request.mapWidth > 0 ? request.mapWidth : _defaultMapWidth;
            int height = request.mapHeight > 0 ? request.mapHeight : _defaultMapHeight;
            int seed = request.seed == NewGameRequestDto.DefaultSeedSentinel ? _defaultSeed : request.seed;
            MapWrapMode wrap = request.wrapMode >= 0 ? (MapWrapMode)request.wrapMode : _defaultWrapMode;
            StartNewGame(width, height, seed, wrap, NewGameRequestMapper.ToWorldSetup(request));
        }

        public void StartNewGameAsync(NewGameRequestDto request, Action onComplete)
        {
            if (request == null)
            {
                StartNewGameAsync(onComplete);
                return;
            }

            int width = request.mapWidth > 0 ? request.mapWidth : _defaultMapWidth;
            int height = request.mapHeight > 0 ? request.mapHeight : _defaultMapHeight;
            int seed = request.seed == NewGameRequestDto.DefaultSeedSentinel ? _defaultSeed : request.seed;
            MapWrapMode wrap = request.wrapMode >= 0 ? (MapWrapMode)request.wrapMode : _defaultWrapMode;
            StartNewGameAsync(width, height, seed, wrap, onComplete, NewGameRequestMapper.ToWorldSetup(request));
        }

 /// <summary>保存游戏</summary>
        public void SaveGame(string saveName)
        {
            if (world == null) return;
            SaveSystem.SaveGame(world, saveName);
            OnGameSaved?.Invoke();
        }

 /// <summary>加载游戏</summary>
        public void LoadGame(string saveName)
        {
            ChangeState(GameState.Loading);
            var loadedWorld = SaveSystem.LoadGame(saveName);
            if (loadedWorld != null)
            {
                if (world != null) Destroy(world.gameObject);
                world = loadedWorld;
                // 读档会新建 GameWorld 并销毁旧世界，场景里持有旧世界引用的渲染器/UI 必须重新绑定，
                // 否则地图不渲染、HUD 空白（表现为"读档后黑屏/没反应"）。
                var mapRenderer = FindAnyObjectByType<MyriadCreation.Rendering.MapRenderer>();
                if (mapRenderer != null) mapRenderer.SetWorldView(new MapWorldViewAdapter(world));
                var ui = AppServices.UiHost;
                if (ui != null) ui.BindWorld(world != null ? (IWorldReadModel)world : null);
                // 运行时地图编辑控制器同样持有旧世界引用（可能挂在 inactive 物体上），读档后必须重绑，
                // 否则框选/移动/抬升/撤销全部作用在已销毁的旧世界上。
                var mapEditing = FindAnyObjectByType<MyriadCreation.Rendering.Editing.RuntimeMapEditingController>(
                    FindObjectsInactive.Include);
                if (mapEditing != null)
                {
                    mapEditing.SetWorldView(mapRenderer != null ? mapRenderer.WorldView : null);
                    mapEditing.BindWorld(world);
                    mapEditing.SetEditService(_worldEditService);
                }
                InjectUiReadViews();
                // 读档/换世界后聚落图标层必须随新世界重建
                RebindSettlementIcons();
                ChangeState(GameState.Playing);
                TryShowGameUi();
                OnGameLoaded?.Invoke();
                }
            else
            {
                ChangeState(GameState.MainMenu);
                Debug.LogError($"[GameManager] 加载存档失败: {saveName}");
            }
        }

 /// <summary>设置游戏速度</summary>
        public void SetGameSpeed(float speed)
        {
            gameSpeed = Mathf.Clamp(speed, 0f, 5f);
            if (!isPaused && currentState == GameState.Playing)
                Time.timeScale = gameSpeed;
            OnGameSpeedChanged?.Invoke(gameSpeed);
        }

 /// <summary>暂停/继续</summary>
        public void TogglePause()
        {
            if (currentState == GameState.Playing)
                ChangeState(GameState.Paused);
            else if (currentState == GameState.Paused)
                ChangeState(GameState.Playing);
        }

 /// <summary>返回主菜单</summary>
        public void ReturnToMainMenu()
        {
            // 换世界前解绑图标事件、关闭详情面板，避免面板显示上一个世界的聚落
            UnbindSettlementIconEvents();
            if (_settlementDetailPanel != null && !(_settlementDetailPanel is UnityEngine.Object stale && stale == null))
                _settlementDetailPanel.Hide();
            if (world != null)
            {
                Destroy(world.gameObject);
                world = null;
            }
            ChangeState(GameState.MainMenu);
        }

        private void OnDestroy()
        {
            UnbindSettlementIconEvents();
            AppServices.UnregisterGameSession(this);
            _worldEditService = null;
        }

        /// <summary>B8：把 App 的 WorldEditService 显式注入场景中的运行时地图编辑控制器（不再走 AppServices 静态注册）。</summary>
        public void InjectMapEditService()
        {
            if (_worldEditService == null)
            {
                _worldEditService = new WorldEditService(() => world);
            }

            var mapEditing = FindAnyObjectByType<RuntimeMapEditingController>(FindObjectsInactive.Include);
            if (mapEditing != null)
            {
                mapEditing.SetEditService(_worldEditService);
            }
        }

        /// <summary>B8-4b2：把聚合只读视图与法律查询/命令注入 UIManager（不改 World 签名）。</summary>
        public void InjectUiReadViews()
        {
            var ui = AppServices.UiHost;
            if (ui == null) return;

            var readModel = world != null ? new WorldReadModel(world) : null;
            ui.SetWorldReadModel(readModel);
            ui.SetMapStatsView(readModel); // 兼容 4a 路径（同一对象同时实现 IMapStatsView）
            if (world != null)
            {
                ui.SetLawServices(world.LawQuery, world.LawCommand);
                ui.SetDecreeCommand(world.DecreeCommand);
            }
        }

        /// <summary>B7：把当前 GameWorld 注入 MapRenderer 的 IMapWorldView（Rendering 只依赖契约）。</summary>
        private void InjectMapWorldView()
        {
            if (world == null) return;
            var mapRenderer = FindAnyObjectByType<MyriadCreation.Rendering.MapRenderer>();
            if (mapRenderer != null) mapRenderer.SetWorldView(new MapWorldViewAdapter(world));
            var mapEditing = FindAnyObjectByType<RuntimeMapEditingController>(FindObjectsInactive.Include);
            if (mapEditing != null)
            {
                mapEditing.SetWorldView(mapRenderer != null ? mapRenderer.WorldView : null);
                mapEditing.BindWorld(world);
                mapEditing.SetEditService(_worldEditService);
            }
            InjectUiReadViews();
        }

        /// <summary>
        /// 世界创建/切换/读档后重建聚落锚点图标层（Map 1），并把图标点击接到聚落详情面板（UI 3）。
        /// 图标层/面板都由代码创建（Ensure / AddComponent），不在场景里摆对象；找不到渲染器时静默跳过。
        /// </summary>
        private void RebindSettlementIcons()
        {
            var mapRenderer = FindAnyObjectByType<MyriadCreation.Rendering.MapRenderer>();
            if (mapRenderer == null) return;
            var icons = MyriadCreation.Rendering.SettlementIconLayer.Ensure(mapRenderer);
            if (icons == null) return;

            // 场景切换/世界替换后图标层实例可能变：重新订阅，避免重复订阅导致面板被 Show 多次
            if (!ReferenceEquals(_boundIconLayer, icons))
            {
                UnbindSettlementIconEvents();
                _boundIconLayer = icons;
                icons.SettlementClicked += OnSettlementIconClicked;
            }

            EnsureSettlementDetailPanel();
            if (_settlementDetailPanel != null)
            {
                _settlementDetailPanel.BindWorld(world != null ? (IWorldReadModel)world : null); // 换世界/读档后面板数据源同步
                _settlementDetailPanel.Hide();           // 新世界/读档后先隐藏旧详情
            }
            icons.SetSettlementView(mapRenderer != null ? mapRenderer.SettlementView : null);
        }

        /// <summary>图标点击（SelectAtScreen 成功选中）→ 打开聚落详情面板。只开面板，不改选中态（图标层已高亮）。</summary>
        private void OnSettlementIconClicked(int anchorId)
        {
            EnsureSettlementDetailPanel();
            if (_settlementDetailPanel != null) _settlementDetailPanel.Show(anchorId);
        }

        private void UnbindSettlementIconEvents()
        {
            if (_boundIconLayer != null)
            {
                _boundIconLayer.SettlementClicked -= OnSettlementIconClicked;
                _boundIconLayer = null;
            }
        }

        /// <summary>查找/创建聚落详情面板（代码动态 UI；面板自带独立 Canvas，场景零改动）。</summary>
        private ISettlementDetailUi EnsureSettlementDetailPanel()
        {
            if (_settlementDetailPanel != null && !(_settlementDetailPanel is UnityEngine.Object stale && stale == null))
                return _settlementDetailPanel;

            // B3-part2b：动态 UI 创建收口到 UiFactory（App 不再直接 new/AddComponent UI 类型）。
            _settlementDetailPanel = AppServices.UiFactory?.EnsureSettlementDetailUi();
            return _settlementDetailPanel;
        }

        private void Update()
        {
 // 空格键暂停
            if (Input.GetKeyDown(KeyCode.Space) &&
                (currentState == GameState.Playing || currentState == GameState.Paused))
            {
                TogglePause();
            }
        }
    }
}
