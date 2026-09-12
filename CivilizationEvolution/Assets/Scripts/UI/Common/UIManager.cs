using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Infrastructure.Save;
using CivilizationEvolution.Rendering;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;
using CivilizationEvolution.UI;







using CivilizationEvolution.Simulation.Culture;
namespace CivilizationEvolution.UI.Common
{
 /// <summary>事件日志分类（决定富文本着色）</summary>

 /// UI管理器
 /// 管理游戏内所有UI面板：顶部信息栏、地块详情、政权面板、外交面板、事件日志、Toast提示
    public partial class UIManager : MonoBehaviour
    {
 /// <summary>UI管理器单例</summary>
        public static UIManager Instance { get; private set; }

        [Header("引用")]
        [SerializeField] private GameWorld world;
        [SerializeField] private MapRenderer mapRenderer;

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
        [SerializeField] private GameObject characterPanel;
        [SerializeField] private Button charOpenButton;
        [SerializeField] private Button charPrevButton;
        [SerializeField] private Button charNextButton;
        [SerializeField] private Button charCloseButton;

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

 /// <summary>统一设置游戏内UI可见性</summary>
        private void SetGameUIVisible(bool visible)
        {
 // 顶部信息栏（通过文本组件的gameObject控制）
            if (dateText != null) dateText.gameObject.SetActive(visible);
            if (realmNameText != null) realmNameText.gameObject.SetActive(visible);
 // 地块详情面板
            if (tileInfoPanel != null) tileInfoPanel.SetActive(visible);
 // 地图模式栏
            if (mapModeBar != null) mapModeBar.SetActive(visible);
            if (mapModePanel != null) mapModePanel.SetActive(visible);
 // 角色面板
            if (characterPanel != null) characterPanel.SetActive(false); // 面板默认关闭，由按钮控制
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
        }
        private readonly List<string> _eventLog = new List<string>();
        private const int MaxLogEntries = 100;

 // 角色面板状态
        private readonly List<int> _characterList = new List<int>();
        private int _charIndex = 0;

 // Toast 队列
        private readonly Queue<string> _toastQueue = new Queue<string>();
        private Coroutine _toastRoutine;
 // 地图编辑器UI面板
        #if UNITY_EDITOR
        private EditorUIPanel _editorPanel;
        #endif

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

 // 默认：宗教类别 + 教统子项（——默认地图=教统地图）
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
            if (familyTreeOpenButton != null) familyTreeOpenButton.onClick.AddListener(OpenFamilyTreePanel);
            if (familyTreeCloseButton != null) familyTreeCloseButton.onClick.AddListener(CloseFamilyTreePanel);
            if (familyTreePrevButton != null) familyTreePrevButton.onClick.AddListener(() => { _charIndex--; RefreshFamilyTreePanel(); });
            if (familyTreeNextButton != null) familyTreeNextButton.onClick.AddListener(() => { _charIndex++; RefreshFamilyTreePanel(); });

 #if UNITY_EDITOR
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
 #endif
            Debug.Log("[UIManager] UI初始化完成");
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

 /// <summary>查找区划（divisionId）</summary>
        private static AdminDivision FindDivision(
            System.Collections.Generic.List<AdminDivision> all, int divisionId)
        {
            if (all == null) return null;
            foreach (var d in all)
                if (d.divisionId == divisionId) return d;
            return null;
        }

 /// 组装官职显示（6 官职——文化定制称号[OfficeTitleCatalog]+持有者名——
 /// 政体语境键粗分：君主制 Kingdom/共和制 Republic——无持有者显示空缺）
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
                string officeName = ((OfficialOffice)o).ToString();
 // 文化定制称号（holder 的文化——无则默认）
                string title = OfficeTitleCatalog.GetDefaultTitleKey(officeName);
                var holder = cm?.GetCharacter(holderId);
                if (holder != null)
                {
                    var culture = world.cultures.TryGetValue(holder.cultureId, out var cd) ? cd : null;
                    title = OfficeTitleCatalog.GetTitle(culture, (OfficialOffice)o, polityKey);
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
        private static CivilizationEvolution.Infrastructure.Audio.MusicPlayerSystem MusicPlayer()
        {
            var mp = UnityEngine.Object.FindAnyObjectByType<CivilizationEvolution.Infrastructure.Audio.MusicPlayerSystem>(FindObjectsInactive.Include);
            if (mp == null)
            {
                var go = new GameObject("MusicPlayer");
                mp = go.AddComponent<CivilizationEvolution.Infrastructure.Audio.MusicPlayerSystem>();
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
 /// <summary>切换地图编辑器面板显示/隐藏</summary>
        #if UNITY_EDITOR
        public void ToggleEditorPanel()
        {
            _editorPanel?.TogglePanel();
        }
        #endif

 /// <summary>获取地图编辑器面板</summary>
        #if UNITY_EDITOR
        public EditorUIPanel GetEditorPanel() => _editorPanel;
        #endif

        public int GetSelectedTile() => _selectedTile;
        public void SetSelectedTile(int tile) => _selectedTile = tile;
    }
}
