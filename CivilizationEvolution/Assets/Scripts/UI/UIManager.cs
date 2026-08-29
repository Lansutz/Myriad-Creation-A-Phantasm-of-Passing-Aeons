using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CivilizationEvolution.Core;
using CivilizationEvolution.Render;

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

        // 选中的地块
        private int _selectedTile = -1;
        private readonly List<string> _eventLog = new List<string>();
        private const int MaxLogEntries = 100;

        // Toast 队列
        private readonly Queue<string> _toastQueue = new Queue<string>();
        private Coroutine _toastRoutine;

        void Start()
        {
            InitializeUI();
            AddEventLog("游戏启动", EventLogKind.System);
        }

        void Update()
        {
            UpdateTopBar();
            UpdateTileInfo();
            HandleMouseClick();
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
        }

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

        public int GetSelectedTile() => _selectedTile;
        public void SetSelectedTile(int tile) => _selectedTile = tile;
    }
}
