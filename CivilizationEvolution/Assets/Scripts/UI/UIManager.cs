using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CivilizationEvolution.Core;
using CivilizationEvolution.Render;
using CivilizationEvolution.Race;
using CivilizationEvolution.Role;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.UI
{
    /// <summary>事件日志分类（决定富文本着色）</summary>
    public enum EventLogKind
    {
        System,   // 系统：蓝灰
        Info,     // 常规：白
        War,      // 战争：红
        Economy,  // 经济：绿
        Warning   // 警示：黄
    }

    /// <summary>
    /// UI管理器
    /// 管理游戏内所有UI面板：顶部信息栏、地块详情、政权面板、外交面板、事件日志、Toast提示
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        /// <summary>UI管理器单例</summary>
        public static UIManager Instance { get; private set; }

        [Header("引用")]
        [SerializeField] private GameWorld world;
        [SerializeField] private MapRenderer mapRenderer;

        [Header("顶部信息栏")]
        [SerializeField] private TMP_Text dateText;
        [SerializeField] private TMP_Text treasuryText;
        [SerializeField] private TMP_Text populationText;
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
        [SerializeField] private GameObject mapModeBar;

        [Header("角色面板")]
        [SerializeField] private GameObject characterPanel;
        [SerializeField] private Button charOpenButton;
        [SerializeField] private Button charPrevButton;
        [SerializeField] private Button charNextButton;
        [SerializeField] private Button charCloseButton;

        [Header("社会政治面板")]
        [SerializeField] private GameObject societyPanel;
        [SerializeField] private GameObject startMenuPanel;
        [SerializeField] private Button startGameButton;
        [SerializeField] private GameObject religionPanel;
        [SerializeField] private GameObject overviewPanel;
        [SerializeField] private TMP_Text overviewText;
        [SerializeField] private Button overviewCloseButton;
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

        [Header("家族树面板")]
        [SerializeField] private GameObject familyTreePanel;
        [SerializeField] private TMP_Text familyTreeText;
        [SerializeField] private Button familyTreeOpenButton;
        [SerializeField] private Button familyTreeCloseButton;
        [SerializeField] private Button familyTreePrevButton;
        [SerializeField] private Button familyTreeNextButton;
        [SerializeField] private TMP_Text charNameText;
        [SerializeField] private TMP_Text charStatusText;
        [SerializeField] private TMP_Text charStatsText;
        [SerializeField] private TMP_Text charPersonalityText;
        [SerializeField] private TMP_Text charDescText;
        [SerializeField] private TMP_Text charDnaText;

        // 选中的地块
        private int _selectedTile = -1;
        /// <summary>当前查看政权（视角——默认玩家政权——点选地块自动跟随其
        /// 所属政权——顶栏政权名/社会/宗教面板都以此为准——政权总览入口）</summary>
        private int _viewRealmId = -1;
        /// <summary>查看政权（公开——面板刷新用）</summary>
        public int ViewRealmId => _viewRealmId >= 0 ? _viewRealmId : (world != null ? world.PlayerRealmId : 0);
        private readonly List<string> _eventLog = new List<string>();
        private const int MaxLogEntries = 100;

        // 角色面板状态
        private readonly List<int> _characterList = new List<int>();
        private int _charIndex = 0;

        // Toast 队列
        private readonly Queue<string> _toastQueue = new Queue<string>();
        private Coroutine _toastRoutine;
        // 地图编辑器UI面板
        private EditorUIPanel _editorPanel;

        [Header("地图信息")]
        [SerializeField] private TMP_Text mapInfoText;

        // UI 数据更新节流（避免每帧全量遍历 8192 地块算总人口等重操作）
        private float _uiUpdateTimer;
        private const float UiUpdateInterval = 0.2f; // 5 次/秒，足够人眼流畅

        void Start()
        {
            Instance = this;
            ApplyChineseFont();
            InitializeUI();
            AddEventLog("游戏启动", EventLogKind.System);
        }

        /// <summary>
        /// 应用中文字体（simhei 黑体——内置 LegacyRuntime 字体不含中文，缺字体则 UI 显示方块）
        /// 遍历场景全部 Legacy Text 组件统一设置
        /// </summary>
        public void ApplyChineseFont()
        {
            // TMP SDF 中文字体（simhei → 4096 SDF 动态图集——替换 Legacy 字体）
            int count = TMPFontUtility.ApplyChineseFontToAll();
            Debug.Log($"[UIManager] TMP 中文字体已应用：{count} 处 UI 文本");
        }

                void Update()
        {
            // 鼠标点击需要实时响应，不节流
            HandleMouseClick();

            // UI 数据展示节流：0.2 秒更新一次（5fps），避免每帧全量遍历地块算总人口
            _uiUpdateTimer += Time.unscaledDeltaTime;
            if (_uiUpdateTimer >= UiUpdateInterval)
            {
                _uiUpdateTimer = 0f;
                UpdateTopBar();
                UpdateTileInfo();
                if (characterPanel != null && characterPanel.activeSelf)
                    UpdateCharacterPanel();
                if (religionPanel != null && religionPanel.activeSelf)
                    RefreshReligionPanel();
                if (societyPanel != null && societyPanel.activeSelf)
                    RefreshSocietyPanel();
                if (musicPanel != null && musicPanel.activeSelf)
                    RefreshMusicPanel();
                if (familyTreePanel != null && familyTreePanel.activeSelf)
                    RefreshFamilyTreePanel();
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

            // 默认：宗教类别 + 教统子项（用户定稿——默认地图=教统地图）
            if (displayModeDropdown != null) displayModeDropdown.value = 8; // 宗教
            OnMapCategoryChanged(displayModeDropdown != null ? displayModeDropdown.value : 8);
            if (displayModeSubDropdown != null) displayModeSubDropdown.value = 1; // 教统
            ApplyMapMode();

            // 角色面板按钮
            if (charOpenButton != null) charOpenButton.onClick.AddListener(OpenCharacterPanel);
            if (charPrevButton != null) charPrevButton.onClick.AddListener(() => { _charIndex--; UpdateCharacterPanel(); });
            if (charNextButton != null) charNextButton.onClick.AddListener(() => { _charIndex++; UpdateCharacterPanel(); });
            if (charCloseButton != null) charCloseButton.onClick.AddListener(CloseCharacterPanel);

            // 社会政治面板按钮
            if (societyOpenButton != null) societyOpenButton.onClick.AddListener(OpenSocietyPanel);
            if (religionOpenButton != null) religionOpenButton.onClick.AddListener(OpenReligionPanel);
            if (startGameButton != null) startGameButton.onClick.AddListener(StartGameFromMenu);
            if (viewRealmButton != null) viewRealmButton.onClick.AddListener(OpenViewRealmPanel);
            if (overviewCloseButton != null) overviewCloseButton.onClick.AddListener(CloseOverviewPanel);
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
            if (familyTreeOpenButton != null) familyTreeOpenButton.onClick.AddListener(OpenFamilyTreePanel);
            if (familyTreeCloseButton != null) familyTreeCloseButton.onClick.AddListener(CloseFamilyTreePanel);
            if (familyTreePrevButton != null) familyTreePrevButton.onClick.AddListener(() => { _charIndex--; RefreshFamilyTreePanel(); });
            if (familyTreeNextButton != null) familyTreeNextButton.onClick.AddListener(() => { _charIndex++; RefreshFamilyTreePanel(); });

            // 地图编辑器UI面板（代码动态生成，无需在Inspector手动搭建）
            if (mapRenderer != null)
            {
                var editorPanelObj = new GameObject("EditorUIPanel");
                editorPanelObj.transform.SetParent(transform, false);
                _editorPanel = editorPanelObj.AddComponent<EditorUIPanel>();
                var editor = mapRenderer.GetMapEditor();
                _editorPanel.Initialize(mapRenderer, editor);
                AddEventLog("编辑器UI面板已加载（Tab键显示/隐藏）", EventLogKind.System);
            }
            Debug.Log("[UIManager] UI初始化完成");
        }

        /// <summary>更新顶部信息栏</summary>
        private void UpdateTopBar()
        {
            if (world == null) return;

            if (dateText != null)
            {
                // 时间=已历时长（开局起算——非纪元年第——用户定稿）
                int ey = world.currentYear - world.startYear;
                int ed = world.currentDay - world.startDay;
                if (ed < 0) { ed += 365; ey -= 1; }
                dateText.text = ey <= 0 && ed <= 0 ? "开局"
                    : $"已历 {ey} 年 {ed} 天";
            }

            // 顶栏政权名（当前查看政权——点选跟随——死显示"未选择势力"修复）
            if (realmNameText != null)
            {
                int vr = ViewRealmId;
                if (world.realms.TryGetValue(vr, out var vrealm))
                {
                    bool isPlayer = vr == world.PlayerRealmId;
                    realmNameText.text = isPlayer ? $"{vrealm.realmName}（本家）" : vrealm.realmName;
                }
                else realmNameText.text = "无政权";
            }

            // 顶栏精简：国库/总人口移除（用户定稿——政权数据进政权界面——
            // 同时消除每 0.2s 全遍历地块算总人口的重操作）

            // 地图信息：尺寸 + 地块数 + 省份数
            if (mapInfoText != null)
            {
                int totalTiles = world.tiles.Length;
                int landTiles = world.GetLandTileCount();
                int seaTiles = world.GetSeaTileCount();
                int provinceCount = world.provinces != null ? world.provinces.Count : 0;
                mapInfoText.text = $"地图 {world.mapWidth}×{world.mapHeight}  地块 {totalTiles}(陆{landTiles}/海{seaTiles})  省份 {provinceCount}";
            }
        }

        /// <summary>更新地块详情</summary>
        private string GetCultureName(int cultureId)
        {
            if (world != null && world.cultures.TryGetValue(cultureId, out var c))
                return c.cultureName;
            return cultureId.ToString();
        }

        private string GetReligionName(int faithId)
        {
            var def = Culture.ReligionCatalog.Get(faithId);
            return def != null ? def.religionName : faithId.ToString();
        }

        private void UpdateTileInfo()
        {
            if (tileInfoPanel == null || _selectedTile < 0 || world == null) return;
            if (_selectedTile >= world.tiles.Length) return;

            ref TileData tile = ref world.tiles[_selectedTile];

            if (tileNameText != null)
            {
                // 标题=归属（CK3 式：点政权领=政权名——点无主=状态）
                int owner = tile.ownerRealmId;
                string realmTag;
                if (owner >= 0 && world.realms.TryGetValue(owner, out var or))
                    realmTag = or.realmName;
                else
                {
                    bool hasPop = tile.populationBlocks != null && tile.populationBlocks.Count > 0;
                    realmTag = hasPop ? "无主之地 · 聚落" : "无主之地 · 荒野";
                }
                tileNameText.text = $"{realmTag} · 地块 #{_selectedTile}";
            }
            // 查看政权按钮：仅领地块可用（无主无政权可看——隐藏）
            if (viewRealmButton != null)
                viewRealmButton.gameObject.SetActive(tile.ownerRealmId >= 0);
            if (tileTerrainText != null)
                tileTerrainText.text = $"高程: {tile.elevation01:F2}\n坡度: {tile.slopeDegree:F1}°\n海陆: {(tile.isLand ? "陆地" : "海洋")}\n海洋分级: {tile.oceanTier}";
            if (tileClimateText != null)
                tileClimateText.text = $"年均温: {tile.annualTemp:F1}℃\n年降水: {tile.annualPrecipMm:F0}mm\n湿度: {tile.airHumidityPct:F0}%\n温度带: {tile.climateZone}";
            if (tileBiomeText != null)
                tileBiomeText.text = $"群系: {tile.biome}\n肥力: {tile.fertility:F2}\n发展度: {tile.development:F2}";
            if (tilePopulationText != null)
            {
                float pop = 0f;
                if (tile.populationBlocks != null)
                    foreach (var pb in tile.populationBlocks)
                        pop += pb.count;
                long people = (long)(pop * 50f);

                // 地块归属分级（CK3 式——点任何地块都有准确反馈）：
                // 有主=政权领地；无主有人=部落/自由聚落；无主无人=荒野
                int owner = tile.ownerRealmId;
                if (owner >= 0 && world.realms.TryGetValue(owner, out var ownRealm))
                {
                    tilePopulationText.text = $"人口: {people:N0} 人\n所属: {ownRealm.realmName}";
                }
                else if (pop > 0f)
                {
                    // 无主聚落（部落/自由民——无人涂色但有人——待征服/演化）
                    int domCulture = Politics.PopulationStats.GetDominantCulture(tile);
                    string cName = GetCultureName(domCulture);
                    tilePopulationText.text = $"人口: {people:N0} 人（无主聚落·{cName}）";
                }
                else
                {
                    tilePopulationText.text = "荒芜之地（无政权·无人烟）";
                }
            }
            if (tileEconomyText != null)
                tileEconomyText.text = $"法理政权: {tile.ownerRealmId}\n占领政权: {tile.occupyingRealmId}\n道路: {tile.roadLevel}\n连通海域: {tile.seaConnectId}";
        }

        /// <summary>处理鼠标点击选中地块</summary>
        private void HandleMouseClick()
        {
            if (Input.GetMouseButtonDown(0) && mapRenderer != null)
            {
                int tile = mapRenderer.ScreenToTile(Input.mousePosition);
                if (tile >= 0)
                {
                    _selectedTile = tile;
                    // 视角跟随：点地块→查看其所属政权（政权总览数据源）
                    if (world != null && tile < world.tiles.Length && world.tiles[tile].ownerRealmId >= 0)
                        _viewRealmId = world.tiles[tile].ownerRealmId;
                    if (tileInfoPanel != null)
                        tileInfoPanel.SetActive(true);
                    AddEventLog($"选中地块 #{tile}");
                }
            }
        }

        /// <summary>设置游戏速度</summary>
        private void SetGameSpeed(float speed)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetGameSpeed(speed);
            AddEventLog($"游戏速度: {(speed == 0 ? "暂停" : speed + "x")}", EventLogKind.System);
        }

        /// <summary>显示模式切换</summary>
        // ===== 地图模式两级切换（类别 → 子项） =====

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
            [(5, 0)] = MapDisplayMode.Economy,
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

        /// <summary>类别切换：填充子项选项（无子项隐藏子 Dropdown）</summary>
        private void OnMapCategoryChanged(int index)
        {
            _mapCategory = index;
            if (displayModeSubDropdown != null)
            {
                if (MapModeSubs.TryGetValue(index, out var subs))
                {
                    displayModeSubDropdown.ClearOptions();
                    displayModeSubDropdown.AddOptions(subs);
                    displayModeSubDropdown.gameObject.SetActive(true);
                    displayModeSubDropdown.value = 0;
                    _mapSub = 0;
                }
                else
                {
                    displayModeSubDropdown.gameObject.SetActive(false);
                    _mapSub = 0;
                }
            }
            ApplyMapMode();
        }

        private void OnMapSubModeChanged(int index)
        {
            _mapSub = index;
            ApplyMapMode();
        }

        private void ApplyMapMode()
        {
            if (mapRenderer == null) return;
            if (ModeMap.TryGetValue((_mapCategory, _mapSub), out var mode))
                mapRenderer.SetDisplayMode(mode);
        }

        /// <summary>折叠/展开地图模式栏（顶部按钮——箭头下拉式）</summary>
        private void ToggleMapModeBar()
        {
            if (mapModeBar != null) mapModeBar.SetActive(!mapModeBar.activeSelf);
        }

        // ===== 角色面板 =====

        /// <summary>打开角色面板（刷新角色列表并显示第一个）</summary>
        public void OpenCharacterPanel()
        {
            var cm = world != null ? world.GetCharacterManager() : null;
            if (cm == null) return;

            _characterList.Clear();
            foreach (var c in cm.GetAllCharacters().Values)
                _characterList.Add(c.characterId);
            _characterList.Sort();
            _charIndex = 0;

            if (characterPanel != null) characterPanel.SetActive(true);
            UpdateCharacterPanel();
        }

        /// <summary>关闭角色面板</summary>
        public void CloseCharacterPanel()
        {
            if (characterPanel != null) characterPanel.SetActive(false);
        }

        /// <summary>打开社会政治面板</summary>
        /// <summary>刷新宗教面板（国教教统信息+支柱+圣人——静态文本生成）</summary>
        private void RefreshReligionPanel()
        {
            if (world == null || religionPanelText == null) return;
            int viewRealm = ViewRealmId; // 视角政权（点选跟随——非固定玩家）
            Culture.ReligionDef succession = null;
            Thought.FaithSystem faith = null;
            int patronSaint = -1;
            if (viewRealm >= 0 && viewRealm < world.realms.Count)
            {
                var realm = world.realms[viewRealm];
                succession = Culture.ReligionCatalog.Get(realm.stateReligionId);
                faith = world.GetFaithSystem(realm.stateReligionId);
                patronSaint = realm.statePatronSaintId;
            }
            religionPanelText.text = Culture.ReligionPanelText.Build(succession, faith, patronSaint);
        }

        /// <summary>主菜单开始游戏（世界初始化——玩家操作后启动——隐藏菜单进游戏）</summary>
        private void StartGameFromMenu()
        {
            var bootstrap = FindAnyObjectByType<Bootstrap>();
            if (bootstrap != null && (world == null || world.tiles.Length == 0))
                bootstrap.StartNewGame(); // 建世界（GameManager 初始化）
            if (startMenuPanel != null) startMenuPanel.SetActive(false);
        }

        /// <summary>查看政权总览（打开聚合面板——人口/国库/官职/宗教——
        /// 数据源=已有系统聚合——设计定稿）</summary>
        private void OpenViewRealmPanel()
        {
            if (overviewPanel != null)
            {
                RefreshOverviewPanel();
                overviewPanel.SetActive(true);
            }
        }

        private void CloseOverviewPanel()
        {
            if (overviewPanel != null) overviewPanel.SetActive(false);
        }

        /// <summary>刷新政权总览（_viewRealmId 视角政权——聚合各系统）</summary>
        private void RefreshOverviewPanel()
        {
            if (world == null || overviewText == null) return;
            int realmId = ViewRealmId;
            if (!world.realms.TryGetValue(realmId, out var realm))
            {
                overviewText.text = Culture.RealmOverviewText.Build(null, null);
                return;
            }
            var society = world.GetRealmSociety(realmId);
            var officeDisplay = BuildOfficeDisplay(world, realm);
            Culture.ReligionDef religion = realm.stateReligionId >= 0
                ? Culture.ReligionCatalog.Get(realm.stateReligionId) : null;
            string saint = "";
            if (realm.statePatronSaintId > 0 && religion != null)
            {
                foreach (var snt in Culture.CanonizationSystem.GetSaints(realm.stateReligionId))
                    if (snt.saintId == realm.statePatronSaintId) { saint = snt.saintName; break; }
            }
            overviewText.text = Culture.RealmOverviewText.Build(realm, society, officeDisplay,
                religion, saint, realmId == world.PlayerRealmId);
        }

        private void OpenReligionPanel()
        {
            if (religionPanel != null)
            {
                RefreshReligionPanel();
                religionPanel.SetActive(true);
            }
        }

        private void CloseReligionPanel()
        {
            if (religionPanel != null) religionPanel.SetActive(false);
        }

        private void OpenSocietyPanel()
        {
            if (societyPanel != null) societyPanel.SetActive(true);
            RefreshSocietyPanel();
        }

        /// <summary>关闭社会政治面板</summary>
        private void CloseSocietyPanel()
        {
            if (societyPanel != null) societyPanel.SetActive(false);
        }

        /// <summary>刷新社会政治面板（阶层画像/派系/政体变迁）</summary>
        private void RefreshSocietyPanel()
        {
            if (societyText == null || world == null) return;
            int realmId = ViewRealmId; // 当前查看政权（点选地块自动跟随——非固定玩家）
            if (!world.realms.TryGetValue(realmId, out var realm)) return;

            // 官职显示组装（officeHolders→文化定制称号+持有者名——OfficeTitle 消费）
            var officeDisplay = BuildOfficeDisplay(world, realm);
            societyText.text = SocietyPanelText.Build(realm,
                world.GetRealmSociety(realmId), world.Factions, world.RegimeDynamics, world.currentDay,
                officeDisplay);
        }

        /// <summary>
        /// 组装官职显示（6 官职——文化定制称号[OfficeTitleCatalog]+持有者名——
        /// 政体语境键粗分：君主制 Kingdom/共和制 Republic——无持有者显示空缺）
        /// </summary>
        private static Dictionary<int, string> BuildOfficeDisplay(GameWorld world, RealmData realm)
        {
            var result = new Dictionary<int, string>();
            if (world == null || realm == null || realm.officeHolders == null) return result;
            string polityKey = "Kingdom";
            if (realm.composition != null &&
                realm.composition.supremeSovereignty == GovernmentConstraints.SupremeSovereignty.Monarchy)
                polityKey = "Kingdom";
            else
                polityKey = "Republic";

            var cm = world.GetCharacterManager();
            for (int o = 0; o < 6; o++)
            {
                if (!realm.officeHolders.TryGetValue(o, out int holderId)) continue;
                string officeName = ((Politics.OfficialOffice)o).ToString();
                // 文化定制称号（holder 的文化——无则默认）
                string title = Politics.OfficeTitleCatalog.GetDefaultTitleKey(officeName);
                var holder = cm?.GetCharacter(holderId);
                if (holder != null)
                {
                    var culture = world.cultures.TryGetValue(holder.cultureId, out var cd) ? cd : null;
                    title = Politics.OfficeTitleCatalog.GetTitle(culture, (Politics.OfficialOffice)o, polityKey);
                    result[o] = $"{(int)o + 1}. {title}：{holder.firstName} {holder.lastName}";
                }
                else
                {
                    result[o] = $"{(int)o + 1}. {title}：空缺";
                }
            }
            return result;
        }

        /// <summary>获取音乐播放器（场景中查找或懒创建）</summary>
        private static CivilizationEvolution.Audio.MusicPlayerSystem MusicPlayer()
        {
            var mp = UnityEngine.Object.FindAnyObjectByType<CivilizationEvolution.Audio.MusicPlayerSystem>(FindObjectsInactive.Include);
            if (mp == null)
            {
                var go = new GameObject("MusicPlayer");
                mp = go.AddComponent<CivilizationEvolution.Audio.MusicPlayerSystem>();
                mp.LoadFromResources();
            }
            return mp;
        }

        /// <summary>打开音乐播放器面板</summary>
        private void OpenMusicPanel()
        {
            MusicPlayer();
            if (musicPanel != null) musicPanel.SetActive(true);
            RefreshMusicPanel();
        }

        /// <summary>关闭音乐播放器面板</summary>
        private void CloseMusicPanel()
        {
            if (musicPanel != null) musicPanel.SetActive(false);
        }

        /// <summary>刷新音乐面板（曲目列表+当前状态）</summary>
        private void RefreshMusicPanel()
        {
            if (musicText == null) return;
            var mp = MusicPlayer();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"── 音乐播放器 ── 音量 {mp.Volume * 100f:F0}%");
            sb.AppendLine(mp.IsPlaying ? "▶ 播放中" : "⏸ 暂停");
            for (int i = 0; i < mp.Playlist.Count; i++)
            {
                string mark = i == mp.CurrentIndex ? "▶ " : "  ";
                sb.AppendLine($"{mark}{i + 1}. {mp.Playlist[i].name}");
            }
            if (mp.Playlist.Count == 0)
                sb.AppendLine("（无曲目——请放入 Resources/Music/ 的 ogg/mp3）");
            musicText.text = sb.ToString();
        }

        /// <summary>打开家族树面板</summary>
        private void OpenFamilyTreePanel()
        {
            RefreshCharacterList();
            if (familyTreePanel != null) familyTreePanel.SetActive(true);
            RefreshFamilyTreePanel();
        }

        /// <summary>关闭家族树面板</summary>
        private void CloseFamilyTreePanel()
        {
            if (familyTreePanel != null) familyTreePanel.SetActive(false);
        }

        /// <summary>刷新家族树面板（当前角色分代树状）</summary>
        private void RefreshFamilyTreePanel()
        {
            if (familyTreeText == null || world == null) return;
            var cm = world.GetCharacterManager();
            if (cm == null) return;
            if (_characterList.Count == 0) RefreshCharacterList();
            if (_characterList.Count == 0)
            {
                familyTreeText.text = "（无角色）";
                return;
            }

            _charIndex = (_charIndex + _characterList.Count) % _characterList.Count;
            familyTreeText.text = cm.BuildFamilyTreeText(_characterList[_charIndex]);
        }

        /// <summary>刷新角色列表（角色面板数据源）</summary>
        private void RefreshCharacterList()
        {
            if (world == null) return;
            var cm = world.GetCharacterManager();
            if (cm == null) return;
            _characterList.Clear();
            foreach (var c in cm.GetCharactersByRealm(world.PlayerRealmId >= 0 ? world.PlayerRealmId : 0))
                _characterList.Add(c.characterId);
            if (_characterList.Count > 0)
                _charIndex = Mathf.Clamp(_charIndex, 0, _characterList.Count - 1);
        }

        /// <summary>刷新角色面板（每帧调用，角色数据动态变化）</summary>
        private void UpdateCharacterPanel()
        {
            var cm = world != null ? world.GetCharacterManager() : null;
            if (cm == null || _characterList.Count == 0)
            {
                if (charNameText != null) charNameText.text = "无角色";
                return;
            }

            if (_charIndex < 0) _charIndex = 0;
            if (_charIndex >= _characterList.Count) _charIndex = _characterList.Count - 1;

            var c = cm.GetCharacter(_characterList[_charIndex]);
            if (c == null) return;

            if (charNameText != null)
                charNameText.text = $"{c.fullName}  {c.age}岁 {(c.isMale ? "男" : "女")}  [{_charIndex + 1}/{_characterList.Count}]";

            if (charStatusText != null)
            {
                string rulerType = c.role == CharacterRole.Ruler ? $"｜{GetRulerTypeName(c.GetRulerType())}" : "";
                string disorder = MentalHealthSystem.GetDisorderName(c);
                string disorderStr = disorder.Length > 0
                    ? $"｜<color=#{ColorUtility.ToHtmlStringRGB(UITheme.LogWar)}>患{disorder}</color>" : "";
                charStatusText.text =
                    $"政权{c.realmId}｜{(c.isAlive ? "在世" : "已故")}｜威望 Lv{c.prestigeCapacityLevel}{rulerType}{disorderStr}";
            }

            if (charStatsText != null)
            {
                charStatsText.text =
                    $"武力 {c.martial:F0}    外交 {c.diplomacy:F0}    军事经略 {c.warfare:F0}\n" +
                    $"管理 {c.stewardship:F0}    谋略 {c.intrigue:F0}    学识 {c.learning:F0}\n" +
                    $"威望 {c.prestige:F0}/{c.GetPrestigeCapacity():F0}    恶名 {c.notoriety:F0}\n" +
                    $"健康 {c.health:F0}    压力 {c.stress:F0}    恐惧 {c.dread:F0}    肥胖 {c.obesity:F0}\n" +
                    $"魅力 {c.charm:F0}    预期寿命 {c.expectedLifespanYears:F0}岁";
            }

            if (charPersonalityText != null)
            {
                charPersonalityText.text =
                    $"大胆 {c.boldness:F0}    悲悯 {c.compassion:F0}    贪婪 {c.greed:F0}    荣誉 {c.honor:F0}\n" +
                    $"理性 {c.rationality:F0}    报复 {c.vengefulness:F0}    虔信 {c.piety:F0}";
            }

            if (charDescText != null)
                charDescText.text = c.GetPersonalityDescription();

            if (charDnaText != null)
            {
                string extra = "";
                var talentDef = DnaSystem.FindDef(c.dnaExpression.talentId);
                var defectDef = DnaSystem.FindDef(c.dnaExpression.defectId);
                if (talentDef != null) extra += $"｜天赋：{talentDef.name}";
                if (defectDef != null) extra += $"｜隐疾：{defectDef.name}";
                charDnaText.text = $"外貌：{c.dnaExpression.appearanceTag}{extra}";
            }
        }

        private static string GetRulerTypeName(RulerType type)
        {
            return type switch
            {
                RulerType.Benevolent => "明君",
                RulerType.Tyrant => "暴君",
                RulerType.TyrantFool => "昏暴之君",
                _ => "平庸之主"
            };
        }

        /// <summary>添加事件日志（按类型着色）</summary>
        public void AddEventLog(string message, EventLogKind kind = EventLogKind.Info)
        {
            string timestamp = world != null ? $"[{world.currentYear}年{world.currentDay}天] " : "";
            Color color = kind switch
            {
                EventLogKind.System => UITheme.LogSystem,
                EventLogKind.War => UITheme.LogWar,
                EventLogKind.Economy => UITheme.LogEconomy,
                EventLogKind.Warning => UITheme.LogWarning,
                _ => UITheme.LogInfo
            };
            string hex = ColorUtility.ToHtmlStringRGB(color);
            _eventLog.Add($"<color=#{ColorUtility.ToHtmlStringRGB(UITheme.TextDim)}>{timestamp}</color><color=#{hex}>{message}</color>");

            if (_eventLog.Count > MaxLogEntries)
                _eventLog.RemoveAt(0);

            if (eventLogText != null)
            {
                eventLogText.text = string.Join("\n", _eventLog);
                if (eventLogScroll != null)
                    eventLogScroll.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>显示顶部 Toast 提示（支持排队，自动渐隐）</summary>
        public void ShowToast(string message, float duration = 3f)
        {
            if (string.IsNullOrEmpty(message)) return;
            _toastQueue.Enqueue(message);
            if (_toastRoutine == null)
                _toastRoutine = StartCoroutine(ToastLoop());
        }

        private IEnumerator ToastLoop()
        {
            while (_toastQueue.Count > 0)
            {
                yield return ShowToastCoroutine(_toastQueue.Dequeue(), 3f);
            }
            _toastRoutine = null;
        }

        private IEnumerator ShowToastCoroutine(string message, float duration)
        {
            var go = new GameObject("Toast", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -60f);
            rt.sizeDelta = new Vector2(400f, 46f);

            var img = go.AddComponent<Image>();
            img.color = UITheme.ToastBg;
            img.sprite = UITheme.RoundedPanelSprite;
            img.type = Image.Type.Sliced;

            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var txt = txtGo.AddComponent<TMPro.TextMeshProUGUI>();
            txt.font = TMPFontUtility.GetChineseFont();
            txt.text = message;
            txt.fontSize = 18;
            txt.color = UITheme.TextMain;
            txt.alignment = TextAlignmentOptions.Center;
            txt.overflowMode = TextOverflowModes.Overflow;
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = Vector2.zero;
            txt.rectTransform.offsetMax = Vector2.zero;

            var cg = go.GetComponent<CanvasGroup>();

            float t = 0f;
            const float fadeIn = 0.25f;
            const float fadeOut = 0.4f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
            cg.alpha = 1f;
            yield return new WaitForSecondsRealtime(duration);

            t = fadeOut;
            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / fadeOut);
                yield return null;
            }
            Destroy(go);
        }

        /// <summary>显示确认对话框（简化：记录日志）</summary>
        public void ShowConfirmation(string title, string message, Action onConfirm, Action onCancel = null)
        {
            // 简化：直接确认
            AddEventLog($"{title}: {message}", EventLogKind.Warning);
            onConfirm?.Invoke();
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
        /// <summary>切换地图编辑器面板显示/隐藏</summary>
        public void ToggleEditorPanel()
        {
            _editorPanel?.TogglePanel();
        }

        /// <summary>获取地图编辑器面板</summary>
        public EditorUIPanel GetEditorPanel() => _editorPanel;

        public int GetSelectedTile() => _selectedTile;
        public void SetSelectedTile(int tile) => _selectedTile = tile;
    }
}
