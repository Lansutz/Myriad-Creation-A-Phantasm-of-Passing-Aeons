using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CivilizationEvolution.Core;
using CivilizationEvolution.Render;
using CivilizationEvolution.Race;
using CivilizationEvolution.Role;

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
        [SerializeField] private Text dateText;
        [SerializeField] private Text treasuryText;
        [SerializeField] private Text populationText;
        [SerializeField] private Text realmNameText;

        [Header("地块详情面板")]
        [SerializeField] private GameObject tileInfoPanel;
        [SerializeField] private Text tileNameText;
        [SerializeField] private Text tileTerrainText;
        [SerializeField] private Text tileClimateText;
        [SerializeField] private Text tileBiomeText;
        [SerializeField] private Text tilePopulationText;
        [SerializeField] private Text tileEconomyText;

        [Header("事件日志")]
        [SerializeField] private GameObject eventLogPanel;
        [SerializeField] private Text eventLogText;
        [SerializeField] private ScrollRect eventLogScroll;

        [Header("速度控制")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button speed1Button;
        [SerializeField] private Button speed2Button;
        [SerializeField] private Button speed3Button;

        [Header("显示模式切换")]
        [SerializeField] private Dropdown displayModeDropdown;

        [Header("角色面板")]
        [SerializeField] private GameObject characterPanel;
        [SerializeField] private Button charOpenButton;
        [SerializeField] private Button charPrevButton;
        [SerializeField] private Button charNextButton;
        [SerializeField] private Button charCloseButton;

        [Header("社会政治面板")]
        [SerializeField] private GameObject societyPanel;
        [SerializeField] private Text societyText;
        [SerializeField] private Button societyOpenButton;
        [SerializeField] private Button societyCloseButton;

        [Header("音乐播放器")]
        [SerializeField] private GameObject musicPanel;
        [SerializeField] private Text musicText;
        [SerializeField] private Button musicOpenButton;
        [SerializeField] private Button musicCloseButton;
        [SerializeField] private Button musicPlayButton;
        [SerializeField] private Button musicPauseButton;
        [SerializeField] private Button musicNextButton;
        [SerializeField] private Button musicPrevButton;
        [SerializeField] private UnityEngine.UI.Slider musicVolumeSlider;

        [Header("家族树面板")]
        [SerializeField] private GameObject familyTreePanel;
        [SerializeField] private Text familyTreeText;
        [SerializeField] private Button familyTreeOpenButton;
        [SerializeField] private Button familyTreeCloseButton;
        [SerializeField] private Button familyTreePrevButton;
        [SerializeField] private Button familyTreeNextButton;
        [SerializeField] private Text charNameText;
        [SerializeField] private Text charStatusText;
        [SerializeField] private Text charStatsText;
        [SerializeField] private Text charPersonalityText;
        [SerializeField] private Text charDescText;
        [SerializeField] private Text charDnaText;

        // 选中的地块
        private int _selectedTile = -1;
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
        [SerializeField] private Text mapInfoText;

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
            var font = Resources.Load<Font>("Fonts/simhei");
            if (font == null)
            {
                Debug.LogWarning("[UIManager] 中文字体缺失（Resources/Fonts/simhei.ttf）——UI 中文可能显示异常");
                return;
            }
            int count = 0;
            foreach (var text in FindObjectsOfType<UnityEngine.UI.Text>(true))
            {
                text.font = font;
                count++;
            }
            Debug.Log($"[UIManager] 中文字体已应用：{count} 处 UI 文本");
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

            // 显示模式下拉框
            if (displayModeDropdown != null)
            {
                displayModeDropdown.ClearOptions();
                displayModeDropdown.AddOptions(new List<string>
                {
                    "地形", "气候", "群系", "政治", "人口", "经济"
                });
                displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);
            }

            // 角色面板按钮
            if (charOpenButton != null) charOpenButton.onClick.AddListener(OpenCharacterPanel);
            if (charPrevButton != null) charPrevButton.onClick.AddListener(() => { _charIndex--; UpdateCharacterPanel(); });
            if (charNextButton != null) charNextButton.onClick.AddListener(() => { _charIndex++; UpdateCharacterPanel(); });
            if (charCloseButton != null) charCloseButton.onClick.AddListener(CloseCharacterPanel);

            // 社会政治面板按钮
            if (societyOpenButton != null) societyOpenButton.onClick.AddListener(OpenSocietyPanel);
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
                dateText.text = $"第 {world.currentYear} 年 第 {world.currentDay} 天";

            // 计算总人口
            float totalPop = 0f;
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (world.tiles[i].populationBlocks != null)
                    foreach (var pb in world.tiles[i].populationBlocks)
                        totalPop += pb.count;
            }

            if (populationText != null)
                populationText.text = $"人口: {Mathf.RoundToInt(totalPop * 50)}";

            // 国库（简化：取第一个政权）
            if (treasuryText != null && world.realms.Count > 0)
            {
                var enumerator = world.realms.GetEnumerator();
                enumerator.MoveNext();
                treasuryText.text = $"国库: {Mathf.RoundToInt(enumerator.Current.Value.treasury)}";
            }
        
            // 地图信息：尺寸 + 地块数 + 省份数
            if (mapInfoText != null)
            {
                int totalTiles = world.tiles.Length;
                int landTiles = world.GetLandTileCount();
                int seaTiles = world.GetSeaTileCount();
                int provinceCount = world.provinces != null ? world.provinces.Count : 0;
                mapInfoText.text = $"地图 {world.mapWidth}×{world.mapHeight}  地块 {totalTiles}(陆{landTiles}/海{seaTiles})  省份 {provinceCount}";
            }}

        /// <summary>更新地块详情</summary>
        private void UpdateTileInfo()
        {
            if (tileInfoPanel == null || _selectedTile < 0 || world == null) return;
            if (_selectedTile >= world.tiles.Length) return;

            ref TileData tile = ref world.tiles[_selectedTile];

            if (tileNameText != null)
                tileNameText.text = $"地块 #{_selectedTile}";
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
                tilePopulationText.text = $"人口: {Mathf.RoundToInt(pop * 50)}\n人口块: {(tile.populationBlocks?.Count ?? 0)}\n稳定值: {tile.stability:F0}\n秩序: {tile.order:F0}";
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
        private void OnDisplayModeChanged(int index)
        {
            if (mapRenderer == null) return;
            mapRenderer.SetDisplayMode((MapDisplayMode)index);
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
            int realmId = world.PlayerRealmId >= 0 ? world.PlayerRealmId : 0;
            if (!world.realms.TryGetValue(realmId, out var realm)) return;

            societyText.text = SocietyPanelText.Build(realm,
                world.GetRealmSociety(realmId), world.Factions, world.RegimeDynamics, world.currentDay);
        }

        /// <summary>获取音乐播放器（场景中查找或懒创建）</summary>
        private static CivilizationEvolution.Audio.MusicPlayerSystem MusicPlayer()
        {
            var mp = UnityEngine.Object.FindFirstObjectByType<CivilizationEvolution.Audio.MusicPlayerSystem>(FindObjectsInactive.Include);
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
            var txt = txtGo.AddComponent<Text>();
            txt.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "Segoe UI" }, 18);
            txt.text = message;
            txt.fontSize = 18;
            txt.color = UITheme.TextMain;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
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
