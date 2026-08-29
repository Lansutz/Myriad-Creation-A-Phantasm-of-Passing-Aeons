using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CivilizationEvolution.Core;
using CivilizationEvolution.Render;

namespace CivilizationEvolution.UI
{
    /// <summary>
    /// UI管理器
    /// 管理游戏内所有UI面板：顶部信息栏、地块详情、政权面板、外交面板、事件日志
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
        private List<string> _eventLog = new List<string>();
        private const int MaxLogEntries = 100;

        void Start()
        {
            InitializeUI();
            AddEventLog("游戏启动");
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
                speed3Button.onClick.AddListener(() => SetGameSpeed(5f));

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
            AddEventLog($"游戏速度: {(speed == 0 ? "暂停" : speed + "x")}");
        }

        /// <summary>显示模式切换</summary>
        private void OnDisplayModeChanged(int index)
        {
            if (mapRenderer == null) return;
            mapRenderer.SetDisplayMode((MapDisplayMode)index);
        }

        /// <summary>添加事件日志</summary>
        public void AddEventLog(string message)
        {
            string timestamp = world != null ? $"[{world.currentYear}年{world.currentDay}天] " : "";
            _eventLog.Add(timestamp + message);

            if (_eventLog.Count > MaxLogEntries)
                _eventLog.RemoveAt(0);

            if (eventLogText != null)
            {
                eventLogText.text = string.Join("\n", _eventLog);
                if (eventLogScroll != null)
                    eventLogScroll.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>显示提示消息</summary>
        public void ShowToast(string message, float duration = 3f)
        {
            AddEventLog(message);
            // 简化：直接加到日志
        }

        /// <summary>显示确认对话框</summary>
        public void ShowConfirmation(string title, string message, Action onConfirm, Action onCancel = null)
        {
            // 简化：直接确认
            AddEventLog($"{title}: {message}");
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
