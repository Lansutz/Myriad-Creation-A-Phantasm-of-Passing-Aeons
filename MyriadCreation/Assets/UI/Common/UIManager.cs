using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Rendering;
using MyriadCreation.UI;
using MyriadCreation.Content.Definitions;
using MyriadCreation.App.Contracts;
using MyriadCreation.Core.Contracts;
using MyriadCreation.App.Settings;
using MyriadCreation.UI.Panels;

namespace MyriadCreation.UI.Common
{
public partial class UIManager : MonoBehaviour, MyriadCreation.App.Contracts.IGameUiHost
{
        public static UIManager Instance { get; private set; }

        [SerializeField] private MapRenderer mapRenderer;

        private IWorldReadModel _worldReadModel;
        private IMapStatsView _mapStatsView;
        private ILawQuery _lawQuery;
        private ILawCommand _lawCommand;
        private IDecreeCommand _decreeCommand;

        public IWorldReadModel WorldReadModel => _worldReadModel;

        /// <summary>4d：当前查看政权（无 GameWorld 时用注入 view 的默认 0 兜底）。</summary>
        public int ViewRealmId => _viewRealmId >= 0 ? _viewRealmId : 0;

        /// <summary>4d：换世界/读档后重绑聚合视图（IGameUiHost 新签名）。</summary>
        public void BindWorld(IWorldReadModel view)
        {
            SetWorldReadModel(view);
            EnsureGovernmentButton();
        }

        public void NotifyWorldChanged(IWorldReadModel view)
        {
            BindWorld(view);
        }

        /// <summary>4d：无 view 时顶栏显示 "-"/空，不抛。</summary>
        private void UpdateTopBar()
        {
            var stats = _mapStatsView;
            if (dateText != null)
            {
                if (stats != null)
                {
                    int ey = stats.CurrentYear - stats.StartYear;
                    int ed = stats.CurrentDay - stats.StartDay;
                    if (ed < 0) { ed += 365; ey -= 1; }
                    dateText.text = ey <= 0 && ed <= 0 ? "开局" : $"已历 {ey} 年 {ed} 天";
                }
                else dateText.text = "-";
            }

            if (realmNameText != null)
            {
                var dto = _worldReadModel?.GetRealm(ViewRealmId);
                realmNameText.text = dto != null && !string.IsNullOrEmpty(dto.realmName) ? dto.realmName : "-";
            }

            if (mapInfoText != null)
            {
                mapInfoText.text = stats != null
                    ? $"地图 {stats.MapWidth}×{stats.MapHeight}  地块 {stats.TotalTiles}(陆{stats.LandTiles}/海{stats.SeaTiles})  省份 {stats.ProvinceCount}"
                    : "-";
            }
        }

        // 4d：地图点击选择依赖旧 GameWorld 地块；UI 无地块契约，改为安全空实现。
        private void HandleMouseClick()
        {
        }

        /// <summary>4d：注入聚合视图并刷新相关面板（null 时显示空/不抛）。</summary>
        public void SetWorldReadModel(IWorldReadModel view)
        {
            _worldReadModel = view;
            _mapStatsView = view;
            UpdateTopBar();
            var settlementPanel = FindAnyObjectByType<MyriadCreation.UI.Panels.SettlementDetailPanel>(
                FindObjectsInactive.Include);
            if (settlementPanel != null) settlementPanel.SetSettlementQuery(view);
            if (_governmentPanel != null) _governmentPanel.SetWorldReadModel(view);
            if (_characterPanelView != null) _characterPanelView.SetWorldReadModel(view);
            if (_familyTreeView != null) _familyTreeView.SetQueries(view, view);
            if (_innovationView != null) _innovationView.SetInnovationQuery(view);
        }

        /// <summary>B8-4a 兼容：注入地图统计视图（null 表示未注入，顶栏显示 "-"）。</summary>
        public void SetMapStatsView(IMapStatsView view)
        {
            _mapStatsView = view;
            UpdateTopBar();
        }

        /// <summary>B8-4a：注入法律查询/命令（GovernmentPanel 优先使用；未注入时面板回退 world）。</summary>
        public void SetLawServices(ILawQuery query, ILawCommand command)
        {
            _lawQuery = query;
            _lawCommand = command;
            ApplyLawServicesToGovernmentPanel();
        }

        private void ApplyLawServicesToGovernmentPanel()
        {
            if (_governmentPanel != null)
            {
                _governmentPanel.SetLawServices(_lawQuery, _lawCommand);
            }
        }

        // 4b5b：法令“下发命令”契约注入（GameWorld.IssueDecreeOrders 只保留 Legacy fallback）
        public void SetDecreeCommand(IDecreeCommand command)
        {
            _decreeCommand = command;
            ApplyDecreeCommandToGovernmentPanel();
        }

        private void ApplyDecreeCommandToGovernmentPanel()
        {
            if (_governmentPanel != null)
            {
                _governmentPanel.SetDecreeCommand(_decreeCommand);
            }
        }

        [Header("顶部信息栏")]
        [SerializeField] private TMP_Text dateText;
        [SerializeField] private TMP_Text realmNameText;

        [Header("地块详情面板")]
        [SerializeField] private GameObject tileInfoPanel;
        [SerializeField] private TMP_Text tileNameText;
        [SerializeField] private TMP_Text tileTerrainText;
        [SerializeField] private TMP_Text tileClimateText;
        [SerializeField] private TMP_Text tileBiomeText;
        [SerializeField] private TMP_Text tilePopulationText;
        [SerializeField] private Button viewRealmButton;
        [SerializeField] private TMP_Text tileEconomyText;

        [Header("事件日志")]
        [SerializeField] private GameObject eventLogPanel;
        [SerializeField] private TMP_Text eventLogText;
        [SerializeField] private ScrollRect eventLogScroll;

        [Header("速度控制")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button speed1Button;
        [SerializeField] private Button speed2Button;
        [SerializeField] private Button speed3Button;

        [Header("显示模式切换")]
        [SerializeField] private TMPro.TMP_Dropdown displayModeDropdown;
        [SerializeField] private TMPro.TMP_Dropdown displayModeSubDropdown;
        [SerializeField] private Button mapModeToggleButton;
        [SerializeField] private GameObject mapModeBar;          // 旧 Dropdown 栏（保留兼容）
        [SerializeField] private GameObject mapModePanel;        // 模式面板（CK3 式按钮列表）
        [SerializeField] private RectTransform mapModeListRoot;  // 动态按钮容器
        private int _modeCat = 0;    // 当前类别（0 政治）
        private int _modeSub = 0;    // 当前子项
        private bool _modeSubOpen = false; // 子模式展开态

        [Header("角色面板")]
        // P1.6：角色面板由 CharacterPanelView 单文件负责，管理器只注册与转调。
        private CharacterPanelView _characterPanelView;
        private MyriadCreation.UI.Panels.CityViewPanel _cityViewPanel;
        private FamilyTreePanelView _familyTreeView;
        private InnovationPanelView _innovationView;
        public UiPanelRegistry Panels { get; } = new UiPanelRegistry();
        [SerializeField] private Button charOpenButton;

        [Header("社会政治面板")]
        [SerializeField] private GameObject societyPanel;
        [SerializeField] private GameObject religionPanel;
        [SerializeField] private GameObject overviewPanel;
        [SerializeField] private TMP_Text overviewText;
        [SerializeField] private Button overviewCloseButton;
        [SerializeField] private Button overviewBackButton;
        [SerializeField] private UnityEngine.UI.HorizontalLayoutGroup divisionButtonsRoot;
 /// <summary>下钻导航状态：-1=政权总览；≥0=查看区划（divisionId）</summary>
        private int _viewingDivisionId = -1;
 /// <summary>区划导航栈（返回用——父链）</summary>
        private readonly List<int> _divisionNavStack = new List<int>();
        [SerializeField] private TMPro.TMP_Text religionPanelText;
        [SerializeField] private Button religionOpenButton;
        [SerializeField] private Button religionCloseButton;
        [SerializeField] private TMP_Text societyText;
        [SerializeField] private Button societyOpenButton;
        [SerializeField] private Button societyCloseButton;

        [Header("音乐播放器")]
        [SerializeField] private GameObject musicPanel;
        [SerializeField] private TMP_Text musicText;
        [SerializeField] private Button musicOpenButton;
        [SerializeField] private Button musicCloseButton;
        [SerializeField] private Button musicPlayButton;
        [SerializeField] private Button musicPauseButton;
        [SerializeField] private Button musicNextButton;
        [SerializeField] private Button musicPrevButton;
        [SerializeField] private UnityEngine.UI.Slider musicVolumeSlider;

        [Header("世界生成 Loading")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private TMP_Text loadingText;

        public void ShowLoading(string message = "世界生成中…")
        {
            if (loadingPanel != null) loadingPanel.SetActive(true);
            if (loadingText != null) loadingText.text = message;
        }

        public void SetLoadingProgress(float progress, string message = null)
        {
            if (loadingText == null) return;
            int pct = UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Clamp01(progress) * 100f);
            loadingText.text = string.IsNullOrEmpty(message)
                ? $"世界生成中… {pct}%"
                : $"{message} {pct}%";
        }

        public void HideLoading()
        {
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }

        [Header("家族树面板")]

 // 选中的地块
        private int _selectedTile = -1;
 /// <summary>当前查看政权（视角——默认玩家政权——点选地块自动跟随其
 /// 所属政权——顶栏政权名/社会/宗教面板都以此为准——政权总览入口）</summary>
        private int _viewRealmId = -1;
 /// <summary>显示所有游戏内UI（进入游戏/地图编辑器时调用）</summary>
        public void ShowGameUI()
        {
            SetGameUIVisible(true);
        }

 /// <summary>隐藏所有游戏内UI（主菜单时调用）</summary>
        public void HideGameUI()
        {
            SetGameUIVisible(false);
        }

        /// <summary>聚落详情面板是否打开（IGameUiHost；SceneFlow ESC 栈用，避免 App 直接找 UI 类型）。</summary>
        public bool IsSettlementDetailOpen
        {
            get
            {
                var panel = UnityEngine.Object.FindAnyObjectByType<MyriadCreation.UI.Panels.SettlementDetailPanel>(
                    UnityEngine.FindObjectsInactive.Include);
                return panel != null && panel.IsOpen;
            }
        }

        /// <summary>打开聚落详情（通过 UiFactory 创建/复用，供 IGameUiHost 调用）。</summary>
        public void OpenSettlementDetail(int anchorId)
        {
            var factory = AppServices.UiFactory;
            var panel = factory != null ? factory.EnsureSettlementDetailUi() : null;
            panel?.Show(anchorId);
        }

 /// <summary>统一设置游戏内UI可见性（IGameUiHost 契约实现）。</summary>
        public void SetGameUIVisible(bool visible)
        {
 // 顶部信息栏（通过文本组件的gameObject控制）
            if (dateText != null) dateText.gameObject.SetActive(visible);
            if (realmNameText != null) realmNameText.gameObject.SetActive(visible);
 // 顶部整条（标题/日期/角色按钮）——只有各子控件引用，按角色按钮的父级整条开关，
 // 避免主菜单时残留旧 HUD 文本/按钮盖在动态主菜单下面。
            if (charOpenButton != null && charOpenButton.transform.parent != null)
                charOpenButton.transform.parent.gameObject.SetActive(visible);
 // 地块详情面板
            if (tileInfoPanel != null) tileInfoPanel.SetActive(visible);
 // 地图模式栏
            if (mapModeBar != null) mapModeBar.SetActive(visible);
            if (mapModePanel != null) mapModePanel.SetActive(visible);
 // 角色面板
 // 社会/宗教面板
            if (societyPanel != null) societyPanel.SetActive(false);
            if (religionPanel != null) religionPanel.SetActive(false);
 // 事件日志
            if (eventLogPanel != null) eventLogPanel.SetActive(visible);
 // 速度控制按钮
            if (pauseButton != null) pauseButton.gameObject.SetActive(visible);
            if (speed1Button != null) speed1Button.gameObject.SetActive(visible);
            if (speed2Button != null) speed2Button.gameObject.SetActive(visible);
            if (speed3Button != null) speed3Button.gameObject.SetActive(visible);
// 政府/法律面板入口（TopBar 动态按钮）与面板本体
            if (_governmentOpenButton != null) _governmentOpenButton.gameObject.SetActive(visible);
            if (_governmentPanel != null && !visible) _governmentPanel.Hide();
        }
        private readonly List<string> _eventLog = new List<string>();
        private const int MaxLogEntries = 100;

 // 角色面板状态

 // Toast 队列
        private readonly Queue<string> _toastQueue = new Queue<string>();
        private Coroutine _toastRoutine;

        [Header("地图信息")]
        [SerializeField] private TMP_Text mapInfoText;

 // UI 数据更新节流（避免每帧全量遍历 8192 地块算总人口等重操作）
        private float _uiUpdateTimer;
        private const float UiUpdateInterval = 0.2f; // 5 次/秒，足够人眼流畅

        void Awake()
        {
            // B3-part2b：UI 在 Awake 注册反向契约，保证 App/SceneFlow 的 Start 阶段就能拿到（Start 顺序不保证）。
            AppServices.RegisterUiHost(this);
        }

        void Start()
        {
            Instance = this;
            AppServices.RegisterUiHost(this);
            ApplyChineseFont();
            TMPFontUtility.ApplyFontChoice(SettingsService.Current.FontChoice);
            InitializeUI();
            RegisterRuntimePanels();
            AddEventLog("游戏启动", EventLogKind.System);
        }

        /// <summary>P1.6：每个面板一个文件，构建后注册到本管理器；管理器不再为面板持有大组字段。</summary>
        private void RegisterRuntimePanels()
        {
            if (_characterPanelView != null)
            {
                return;
            }
            var go = new GameObject("CharacterPanelView");
            go.transform.SetParent(transform, false);
            _characterPanelView = go.AddComponent<CharacterPanelView>();
            _characterPanelView.Build(go.transform);
            _characterPanelView.SetWorldReadModel(_worldReadModel);
            _characterPanelView.SetViewRealm(ViewRealmId);
            Panels.Register(_characterPanelView);

            var familyGo = new GameObject("FamilyTreePanelView");
            familyGo.transform.SetParent(transform, false);
            _familyTreeView = familyGo.AddComponent<FamilyTreePanelView>();
            _familyTreeView.Build(familyGo.transform);
            _familyTreeView.SetQueries(_worldReadModel, _worldReadModel);
            _familyTreeView.SetViewRealm(ViewRealmId);
            Panels.Register(_familyTreeView);

            var innovationGo = new GameObject("InnovationPanelView");
            innovationGo.transform.SetParent(transform, false);
            _innovationView = innovationGo.AddComponent<InnovationPanelView>();
            _innovationView.Build(innovationGo.transform);
            _innovationView.SetInnovationQuery(_worldReadModel);
            _innovationView.SetViewRealm(ViewRealmId);
            Panels.Register(_innovationView);

            var cityGo = new GameObject("CityViewPanel");
            cityGo.transform.SetParent(transform, false);
            _cityViewPanel = cityGo.AddComponent<MyriadCreation.UI.Panels.CityViewPanel>();
            _cityViewPanel.Build(cityGo.transform);
            _cityViewPanel.SetQuery(_worldReadModel);
            Panels.Register(_cityViewPanel);

            TMPFontUtility.ApplyFontChoice(SettingsService.Current.FontChoice);
        }

        /// <summary>革新面板入口；面板未注册时静默忽略。</summary>
        /// <summary>城市俯视图入口（P4.2）；面板未注册时静默忽略。</summary>
        public void OpenCityView(int anchorId)
        {
            if (_cityViewPanel == null || anchorId < 0) return;
            _cityViewPanel.ShowFor(anchorId);
        }

        public void OpenInnovationPanel()
        {
            if (_innovationView == null)
            {
                return;
            }
            _innovationView.SetViewRealm(ViewRealmId);
            _innovationView.Show();
        }

 /// 应用中文字体（simhei 黑体——内置 LegacyRuntime 字体不含中文，缺字体则 UI 显示方块）
 /// 遍历场景全部 Legacy Text 组件统一设置
        public void ApplyChineseFont()
        {
 // TMP SDF 中文字体（simhei → 4096 SDF 动态图集——替换 Legacy 字体）
            int count = TMPFontUtility.ApplyChineseFontToAll();
            Debug.Log($"[UIManager] TMP 中文字体已应用：{count} 处 UI 文本");
        }

                void Update()
        {
 // G：政府/法律面板开关（输入框中打字时不触发；主菜单不响应）
            if (Input.GetKeyDown(KeyCode.G) && !IsTypingInInputField()
                && (AppServices.GameSession == null || AppServices.GameSession.CurrentState != GameState.MainMenu))
            {
                ToggleGovernmentPanel();
            }

 // P1.6：面板各有独立文件与注册表；这里只给一个键盘入口（不依赖场景缺失的按钮）。
            if (!IsTypingInInputField()
                && (AppServices.GameSession == null || AppServices.GameSession.CurrentState != GameState.MainMenu))
            {
                if (Input.GetKeyDown(KeyCode.C)) OpenCharacterPanel();
                else if (Input.GetKeyDown(KeyCode.F)) OpenFamilyTreePanel();
                else if (Input.GetKeyDown(KeyCode.I)) OpenInnovationPanel();
            }

 // 鼠标点击需要实时响应，不节流
            HandleMouseClick();

 // UI 数据展示节流：0.2 秒更新一次（5fps），避免每帧全量遍历地块算总人口
            _uiUpdateTimer += Time.unscaledDeltaTime;
            if (_uiUpdateTimer >= UiUpdateInterval)
            {
                _uiUpdateTimer = 0f;
                UpdateTopBar();
                // 分辨率变化：重算 TopBar「政府」按钮宽度/字号/内边距（窄屏 <1280 缩到 64）。
                if (Screen.width != _lastGovernmentLayoutWidth)
                {
                    RefreshGovernmentButtonLayout(Screen.width);
                }
                UpdateTileInfo();
                if (religionPanel != null && religionPanel.activeSelf)
                    RefreshReligionPanel();
                if (societyPanel != null && societyPanel.activeSelf)
                    RefreshSocietyPanel();
                if (musicPanel != null && musicPanel.activeSelf)
                    RefreshMusicPanel();
            }
        }

 /// <summary>初始化UI</summary>
        private void InitializeUI()
        {
 // 速度按钮
            if (pauseButton != null)
                pauseButton.onClick.AddListener(() => SetGameSpeed(0f));
            if (speed1Button != null)
                speed1Button.onClick.AddListener(() => SetGameSpeed(1f));
            if (speed2Button != null)
                speed2Button.onClick.AddListener(() => SetGameSpeed(2f));
            if (speed3Button != null)
                speed3Button.onClick.AddListener(() => SetGameSpeed(3f)); // 修复：原为 5f（与按钮标注 3x 不符）

 // 地图模式（两级：类别 Dropdown + 子项 Dropdown——政治第 1；宗教默认教统）
            if (displayModeDropdown != null)
            {
                displayModeDropdown.ClearOptions();
                displayModeDropdown.AddOptions(MapModeCategories); // 9 类（政治第 1）
                displayModeDropdown.onValueChanged.AddListener(OnMapCategoryChanged);
            }
            if (displayModeSubDropdown != null)
                displayModeSubDropdown.onValueChanged.AddListener(OnMapSubModeChanged);
            if (mapModeToggleButton != null)
                mapModeToggleButton.onClick.AddListener(ToggleMapModeBar);

 // 默认：宗教类别 + 教统子项（——默认地图=教统地图）
            if (displayModeDropdown != null) displayModeDropdown.value = 8; // 宗教
            OnMapCategoryChanged(displayModeDropdown != null ? displayModeDropdown.value : 8);
            if (displayModeSubDropdown != null) displayModeSubDropdown.value = 1; // 教统
            ApplyMapMode();

 // 角色面板按钮
            if (charOpenButton != null) charOpenButton.onClick.AddListener(OpenCharacterPanel);

 // 社会政治面板按钮
            if (societyOpenButton != null) societyOpenButton.onClick.AddListener(OpenSocietyPanel);
            if (religionOpenButton != null) religionOpenButton.onClick.AddListener(OpenReligionPanel);
            if (viewRealmButton != null) viewRealmButton.onClick.AddListener(OpenViewRealmPanel);
            if (overviewCloseButton != null) overviewCloseButton.onClick.AddListener(CloseOverviewPanel);
            if (overviewBackButton != null) overviewBackButton.onClick.AddListener(OnOverviewBack);
            if (religionCloseButton != null) religionCloseButton.onClick.AddListener(CloseReligionPanel);
            if (societyCloseButton != null) societyCloseButton.onClick.AddListener(CloseSocietyPanel);

 // 音乐播放器按钮
            if (musicOpenButton != null) musicOpenButton.onClick.AddListener(OpenMusicPanel);
            if (musicCloseButton != null) musicCloseButton.onClick.AddListener(CloseMusicPanel);
            if (musicPlayButton != null) musicPlayButton.onClick.AddListener(() => MusicPlayer()?.Play());
            if (musicPauseButton != null) musicPauseButton.onClick.AddListener(() => MusicPlayer()?.Pause());
            if (musicNextButton != null) musicNextButton.onClick.AddListener(() => MusicPlayer()?.Next());
            if (musicPrevButton != null) musicPrevButton.onClick.AddListener(() => MusicPlayer()?.Prev());
            if (musicVolumeSlider != null)
            {
                if (MusicPlayer() != null) musicVolumeSlider.value = MusicPlayer().Volume;
                musicVolumeSlider.onValueChanged.AddListener(v => { if (MusicPlayer() != null) MusicPlayer().Volume = v; });
            }

 // 家族树面板按钮

// 政府/法律面板入口（TopBar 动态创建「政府」按钮——场景零改动）
            EnsureGovernmentButton();

            Debug.Log("[UIManager] UI初始化完成");
        }

 /// <summary>设置游戏速度</summary>
        private void SetGameSpeed(float speed)
        {
            AppServices.GameSession?.SetGameSpeed(speed);
            AddEventLog($"游戏速度: {(speed == 0 ? "暂停" : speed + "x")}", EventLogKind.System);
        }

 /// <summary>显示模式切换</summary> // ===== 地图模式两级切换（类别 → 子项） =====
 /// <summary>地图类别（政治第 1——第一政权常态地图）</summary>
        private static readonly List<string> MapModeCategories = new List<string>
        {
            "政治", "地形", "气候", "群系", "人口", "经济", "外交", "文化", "宗教"
        };

 /// <summary>类别 → 子项选项（null=无子项——隐藏子 Dropdown）</summary>
        private static readonly Dictionary<int, List<string>> MapModeSubs = new Dictionary<int, List<string>>
        {
            [6] = new List<string> { "一般外交", "联盟阵营" },   // 外交
            [7] = new List<string> { "主文化", "分支文化" },    // 文化
            [8] = new List<string> { "宗教", "教统", "传统" }   // 宗教（默认教统）
        };

 /// <summary>类别+子项 → MapDisplayMode（无子项类别直接用类别映射）</summary>
        private static readonly Dictionary<(int cat, int sub), MapDisplayMode> ModeMap = new Dictionary<(int, int), MapDisplayMode>
        {
            [(0, 0)] = MapDisplayMode.Political,
            [(1, 0)] = MapDisplayMode.Terrain,
            [(2, 0)] = MapDisplayMode.Climate,
            [(3, 0)] = MapDisplayMode.Biome,
            [(4, 0)] = MapDisplayMode.Population,
            [(6, 0)] = MapDisplayMode.Diplomacy,
            [(6, 1)] = MapDisplayMode.Alliance,
            [(7, 0)] = MapDisplayMode.Culture,
            [(7, 1)] = MapDisplayMode.CultureBranch,
            [(8, 0)] = MapDisplayMode.Religion,
            [(8, 1)] = MapDisplayMode.ReligionSuccession,
            [(8, 2)] = MapDisplayMode.ReligionTradition
        };

        private int _mapCategory = 8;   // 当前类别（默认宗教）
        private int _mapSub = 1;         // 当前子项（默认教统）

 /// <summary>分辨率去重列表（从高到低——宽≥1280）</summary>
        private static List<Resolution> BuildResolutionList()
        {
            var seen = new HashSet<string>();
            var list = new List<Resolution>();
            var res = Screen.resolutions;
            for (int i = res.Length - 1; i >= 0; i--)
            {
                if (res[i].width < 1280) continue;
                if (seen.Add($"{res[i].width}×{res[i].height}"))
                    list.Add(res[i]);
            }
            if (list.Count == 0) list.Add(new Resolution { width = 1920, height = 1080 });
            return list;
        }

 /// <summary>获取音乐播放器（场景中查找或懒创建）</summary>
        private static MyriadCreation.Infrastructure.Audio.MusicPlayerSystem MusicPlayer()
        {
            var mp = UnityEngine.Object.FindAnyObjectByType<MyriadCreation.Infrastructure.Audio.MusicPlayerSystem>(FindObjectsInactive.Include);
            if (mp == null)
            {
                var go = new GameObject("MusicPlayer");
                mp = go.AddComponent<MyriadCreation.Infrastructure.Audio.MusicPlayerSystem>();
                mp.LoadFromResources();
            }
            return mp;
        }


 // ===== 面板控制 =====
        public void ToggleTileInfoPanel()
        {
            if (tileInfoPanel != null)
                tileInfoPanel.SetActive(!tileInfoPanel.activeSelf);
        }

        public void ToggleEventLogPanel()
        {
            if (eventLogPanel != null)
                eventLogPanel.SetActive(!eventLogPanel.activeSelf);
        }
        public int GetSelectedTile() => _selectedTile;
        public void SetSelectedTile(int tile) => _selectedTile = tile;
}
}
