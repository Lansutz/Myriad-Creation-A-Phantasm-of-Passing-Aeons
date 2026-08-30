using System;
using System.Collections.Generic;
using CivilizationEvolution.Map;
using UnityEngine;
using UnityEngine.UI;

namespace CivilizationEvolution.UI
{
    /// <summary>
    /// 地图生成参数面板（右侧浮动面板）
    /// 对齐 FantasyMapSimulator 编辑器内一体化工作流：
    ///   调整参数 → 生成地形 → 计算气候 → 重算水文 → 手动编辑
    ///
    /// 参数分组（可折叠）：
    ///   全局：地图尺寸、种子、生成模式
    ///   海陆：外海缓冲、海平面、陆地量、破碎度、海岸破碎度
    ///   气候：环流、热赤道、北缘纬度、南缘纬度、全球温度
    ///   水文：河网密度、侵蚀强度
    ///   省份划分：省份数量、大小差异、规整度
    ///
    /// 底部按钮：生成地形 / 计算气候 / 重算水文 / 全部生成
    /// </summary>
    public class MapGenerationPanel : MonoBehaviour
    {
        // ===== 外部引用 =====
        private MapGenerationConfig _config;
        private Action _onGenerateTerrain;
        private Action _onCalculateClimate;
        private Action _onRecalculateHydrology;

        // ===== UI 根节点 =====
        private GameObject _panelRoot;
        private RectTransform _panelRt;
        private Text _statusText;

        // ===== 全局参数控件 =====
        private Dropdown _mapSizeDropdown;
        private InputField _seedInput;
        private Button _randomSeedBtn;
        private Dropdown _genModeDropdown;

        // ===== 海陆参数控件 =====
        private Toggle _outerSeaBufferToggle;
        private Slider _seaLevelSlider; private Text _seaLevelText;
        private Slider _landAmountSlider; private Text _landAmountText;
        private Slider _fragmentationSlider; private Text _fragmentationText;
        private Slider _coastFragSlider; private Text _coastFragText;

        // ===== 气候参数控件 =====
        private Dropdown _circulationDropdown;
        private Slider _thermalEquatorSlider; private Text _thermalEquatorText;
        private Slider _northLatSlider; private Text _northLatText;
        private Slider _southLatSlider; private Text _southLatText;
        private Slider _globalTempSlider; private Text _globalTempText;

        // ===== 水文参数控件 =====
        private Slider _riverDensitySlider; private Text _riverDensityText;
        private Slider _erosionSlider; private Text _erosionText;

        // ===== 省份划分控件 =====
        private Slider _provinceCountSlider; private Text _provinceCountText;
        private Slider _provinceSizeVarSlider; private Text _provinceSizeVarText;
        private Slider _provinceRegularitySlider; private Text _provinceRegularityText;

        // ===== 折叠分组状态 =====
        private readonly Dictionary<string, bool> _foldState = new Dictionary<string, bool>
        {
            { "global", true },
            { "seaLand", true },
            { "climate", true },
            { "hydrology", false },
            { "province", false },
        };

        // ===== 颜色主题 =====
        private static readonly Color PanelBg = new Color(0.10f, 0.10f, 0.13f, 0.94f);
        private static readonly Color HeaderBg = new Color(0.18f, 0.20f, 0.25f, 1f);
        private static readonly Color ButtonNormal = new Color(0.22f, 0.24f, 0.30f, 1f);
        private static readonly Color ButtonActive = new Color(0.30f, 0.50f, 0.80f, 1f);
        private static readonly Color ButtonGenerate = new Color(0.25f, 0.55f, 0.35f, 1f);
        private static readonly Color TextColor = new Color(0.90f, 0.90f, 0.92f, 1f);
        private static readonly Color TextDim = new Color(0.55f, 0.55f, 0.60f, 1f);
        private static readonly Color SliderFill = new Color(0.35f, 0.55f, 0.85f, 1f);

        private const float PanelWidth = 260f;
        private const float ContentWidth = 236f;
        private const float LabelWidth = 110f;
        private const float ValueWidth = 60f;

        /// <summary>初始化生成参数面板</summary>
        public void Initialize(MapGenerationConfig config,
            Action onGenerateTerrain, Action onCalculateClimate, Action onRecalculateHydrology)
        {
            _config = config ?? new MapGenerationConfig();
            _onGenerateTerrain = onGenerateTerrain;
            _onCalculateClimate = onCalculateClimate;
            _onRecalculateHydrology = onRecalculateHydrology;

            CreatePanel();
            SyncUIFromConfig();
            Debug.Log("[MapGenerationPanel] 地图生成参数面板已创建");
        }

        public void Show() => _panelRoot?.SetActive(true);
        public void Hide() => _panelRoot?.SetActive(false);
        public void Toggle() { if (_panelRoot != null) _panelRoot.SetActive(!_panelRoot.activeSelf); }
        public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf;

        /// <summary>从配置同步到UI</summary>
        public void SyncUIFromConfig()
        {
            if (_config == null) return;

            _mapSizeDropdown.value = (int)_config.MapSize;
            _seedInput.text = _config.Seed < 0 ? "随机" : _config.Seed.ToString();
            _genModeDropdown.value = (int)_config.Basemap;

            _outerSeaBufferToggle.isOn = _config.OuterSeaBuffer;
            SetSlider(_seaLevelSlider, _seaLevelText, _config.SeaLevel, "F3");
            SetSlider(_landAmountSlider, _landAmountText, _config.LandAmount, "F2");
            SetSlider(_fragmentationSlider, _fragmentationText, _config.Fragmentation, "F3");
            SetSlider(_coastFragSlider, _coastFragText, _config.CoastFragmentation, "F2");

            _circulationDropdown.value = (int)_config.Circulation;
            SetSlider(_thermalEquatorSlider, _thermalEquatorText, _config.ThermalEquator, "F2");
            SetSlider(_northLatSlider, _northLatText, _config.NorthLatitude, "F0");
            SetSlider(_southLatSlider, _southLatText, _config.SouthLatitude, "F0");
            SetSlider(_globalTempSlider, _globalTempText, _config.GlobalTemperatureOffset, "+0.0;-0.0;0.0");

            SetSlider(_riverDensitySlider, _riverDensityText, _config.RiverDensity, "F2");
            SetSlider(_erosionSlider, _erosionText, _config.ErosionIntensity, "F2");

            SetSlider(_provinceCountSlider, _provinceCountText, _config.ProvinceCount, "F0");
            SetSlider(_provinceSizeVarSlider, _provinceSizeVarText, _config.ProvinceSizeVariance, "F2");
            SetSlider(_provinceRegularitySlider, _provinceRegularityText, _config.ProvinceRegularity, "F2");

            UpdateParamVisibility();
        }

        // ===== UI 创建 =====
        private void CreatePanel()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[MapGenerationPanel] 场景中没有Canvas");
                return;
            }

            _panelRoot = new GameObject("MapGenPanel", typeof(RectTransform), typeof(Image));
            _panelRoot.transform.SetParent(canvas.transform, false);
            _panelRt = (RectTransform)_panelRoot.transform;
            _panelRt.anchorMin = new Vector2(1f, 0.5f);
            _panelRt.anchorMax = new Vector2(1f, 0.5f);
            _panelRt.pivot = new Vector2(1f, 0.5f);
            _panelRt.anchoredPosition = new Vector2(-10f, 0f);
            _panelRt.sizeDelta = new Vector2(PanelWidth, 600f);
            var panelImg = _panelRoot.GetComponent<Image>();
            panelImg.color = PanelBg;
            panelImg.sprite = UITheme.RoundedPanelSprite;
            panelImg.type = Image.Type.Sliced;

            float y = -10f;

            // 标题
            var title = CreateText(_panelRoot.transform, "地图生成参数", 15, TextAnchor.MiddleCenter,
                new Vector2(12f, y), new Vector2(ContentWidth, 26f));
            title.color = TextColor;
            y -= 32f;

            // ===== 全局分组 =====
            y = CreateFoldableHeader("global", "全局设置", y);
            if (_foldState["global"])
            {
                y = CreateGlobalSection(y);
            }

            // ===== 海陆分组 =====
            y = CreateFoldableHeader("seaLand", "海陆", y);
            if (_foldState["seaLand"])
            {
                y = CreateSeaLandSection(y);
            }

            // ===== 气候分组 =====
            y = CreateFoldableHeader("climate", "气候", y);
            if (_foldState["climate"])
            {
                y = CreateClimateSection(y);
            }

            // ===== 水文分组 =====
            y = CreateFoldableHeader("hydrology", "水文与地貌", y);
            if (_foldState["hydrology"])
            {
                y = CreateHydrologySection(y);
            }

            // ===== 省份分组 =====
            y = CreateFoldableHeader("province", "省份划分", y);
            if (_foldState["province"])
            {
                y = CreateProvinceSection(y);
            }

            y -= 8f;
            CreateDivider(_panelRoot.transform, new Vector2(12f, y), ContentWidth);
            y -= 12f;

            // ===== 生成按钮区 =====
            y = CreateGenerateButtons(y);

            y -= 8f;
            // 状态文本
            _statusText = CreateText(_panelRoot.transform, "就绪", 10, TextAnchor.UpperLeft,
                new Vector2(12f, y), new Vector2(ContentWidth, 30f));
            _statusText.color = TextDim;
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            y -= 34f;

            _panelRt.sizeDelta = new Vector2(PanelWidth, Mathf.Abs(y) + 16f);
        }

        private float CreateGlobalSection(float y)
        {
            // 地图尺寸
            CreateLabel("地图尺寸", y);
            _mapSizeDropdown = CreateDropdown(_panelRoot.transform,
                new Vector2(12f, y - 20f), new Vector2(ContentWidth, 24f),
                new List<string> { "小 128×64", "中 256×128", "大 512×256", "超大 1024×512" });
            _mapSizeDropdown.onValueChanged.AddListener(v => { _config.MapSize = (MapGenerationConfig.MapSizePreset)v; UpdateStatus("地图尺寸已更新"); });
            y -= 32f;

            // 种子
            CreateLabel("随机种子", y);
            _seedInput = CreateInputField(_panelRoot.transform, "随机",
                new Vector2(12f, y - 20f), new Vector2(ContentWidth - 60f, 24f));
            _seedInput.onEndEdit.AddListener(v =>
            {
                if (int.TryParse(v, out int s)) _config.Seed = s;
                else _config.Seed = -1;
            });
            _randomSeedBtn = CreateButton(_panelRoot.transform, "随机",
                new Vector2(ContentWidth - 42f, y - 20f), new Vector2(54f, 24f));
            _randomSeedBtn.onClick.AddListener(() =>
            {
                _config.Seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                _seedInput.text = _config.Seed.ToString();
                UpdateStatus("已生成随机种子");
            });
            y -= 32f;

            // 生成模式
            CreateLabel("底图来源", y);
            _genModeDropdown = CreateDropdown(_panelRoot.transform,
                new Vector2(12f, y - 20f), new Vector2(ContentWidth, 24f),
                new List<string> { "程序生成", "内置底图", "导入高度图" });
            _genModeDropdown.onValueChanged.AddListener(v =>
            {
                _config.Basemap = (MapGenerationConfig.MapBasemap)v;
                UpdateParamVisibility();
                UpdateStatus($"底图来源: {_genModeDropdown.options[v].text}");
            });
            y -= 32f;

            return y;
        }

        private float CreateSeaLandSection(float y)
        {
            // 外海缓冲
            CreateLabel("外海缓冲", y);
            _outerSeaBufferToggle = CreateToggle(_panelRoot.transform,
                new Vector2(ContentWidth - 30f, y - 2f), new Vector2(24f, 20f));
            _outerSeaBufferToggle.onValueChanged.AddListener(v => { _config.OuterSeaBuffer = v; });
            y -= 28f;

            y = CreateSliderRow("海平面", 0f, 1f, out _seaLevelSlider, out _seaLevelText, y,
                v => { _config.SeaLevel = v; });
            y = CreateSliderRow("陆地量", 0f, 1f, out _landAmountSlider, out _landAmountText, y,
                v => { _config.LandAmount = v; });
            y = CreateSliderRow("破碎度", 0f, 1f, out _fragmentationSlider, out _fragmentationText, y,
                v => { _config.Fragmentation = v; });
            y = CreateSliderRow("海岸破碎度", 0f, 1f, out _coastFragSlider, out _coastFragText, y,
                v => { _config.CoastFragmentation = v; });

            return y;
        }

        private float CreateClimateSection(float y)
        {
            // 环流
            CreateLabel("环流模式", y);
            _circulationDropdown = CreateDropdown(_panelRoot.transform,
                new Vector2(12f, y - 20f), new Vector2(ContentWidth, 24f),
                new List<string> { "单环流", "双环流", "三环流", "增强季风" });
            _circulationDropdown.onValueChanged.AddListener(v => { _config.Circulation = (MapGenerationConfig.CirculationMode)v; });
            y -= 32f;

            y = CreateSliderRow("热赤道偏移", -1f, 1f, out _thermalEquatorSlider, out _thermalEquatorText, y,
                v => { _config.ThermalEquator = v; });
            y = CreateSliderRow("北缘纬度", -90f, 90f, out _northLatSlider, out _northLatText, y,
                v => { _config.NorthLatitude = v; });
            y = CreateSliderRow("南缘纬度", -90f, 90f, out _southLatSlider, out _southLatText, y,
                v => { _config.SouthLatitude = v; });
            y = CreateSliderRow("全球温度", -8f, 8f, out _globalTempSlider, out _globalTempText, y,
                v => { _config.GlobalTemperatureOffset = v; });

            return y;
        }

        private float CreateHydrologySection(float y)
        {
            y = CreateSliderRow("河网密度", 0f, 1f, out _riverDensitySlider, out _riverDensityText, y,
                v => { _config.RiverDensity = v; });
            y = CreateSliderRow("侵蚀强度", 0f, 1f, out _erosionSlider, out _erosionText, y,
                v => { _config.ErosionIntensity = v; });
            return y;
        }

        private float CreateProvinceSection(float y)
        {
            y = CreateSliderRow("省份数量", 20f, 500f, out _provinceCountSlider, out _provinceCountText, y,
                v => { _config.ProvinceCount = (int)v; });
            y = CreateSliderRow("大小差异", 0f, 1f, out _provinceSizeVarSlider, out _provinceSizeVarText, y,
                v => { _config.ProvinceSizeVariance = v; });
            y = CreateSliderRow("规整度", 0f, 1f, out _provinceRegularitySlider, out _provinceRegularityText, y,
                v => { _config.ProvinceRegularity = v; });
            return y;
        }

        private float CreateGenerateButtons(float y)
        {
            // 生成地形
            var genTerrainBtn = CreateButton(_panelRoot.transform, "生成地形",
                new Vector2(12f, y), new Vector2(ContentWidth, 30f));
            genTerrainBtn.GetComponent<Image>().color = ButtonGenerate;
            genTerrainBtn.onClick.AddListener(() =>
            {
                UpdateStatus("正在生成地形...");
                _onGenerateTerrain?.Invoke();
                UpdateStatus("地形生成完成");
            });
            y -= 36f;

            // 计算气候
            var calcClimateBtn = CreateButton(_panelRoot.transform, "计算气候",
                new Vector2(12f, y), new Vector2((ContentWidth - 8f) / 2f, 26f));
            calcClimateBtn.onClick.AddListener(() =>
            {
                UpdateStatus("正在计算气候...");
                _onCalculateClimate?.Invoke();
                UpdateStatus("气候计算完成");
            });

            // 重算水文
            var recalcHydroBtn = CreateButton(_panelRoot.transform, "重算水文",
                new Vector2(12f + (ContentWidth - 8f) / 2f + 8f, y), new Vector2((ContentWidth - 8f) / 2f, 26f));
            recalcHydroBtn.onClick.AddListener(() =>
            {
                UpdateStatus("正在重算水文...");
                _onRecalculateHydrology?.Invoke();
                UpdateStatus("水文重算完成");
            });
            y -= 32f;

            // 全部生成
            var genAllBtn = CreateButton(_panelRoot.transform, "全部生成 (地形+气候+水文)",
                new Vector2(12f, y), new Vector2(ContentWidth, 26f));
            genAllBtn.GetComponent<Image>().color = ButtonActive;
            genAllBtn.onClick.AddListener(() =>
            {
                UpdateStatus("正在全部生成...");
                _onGenerateTerrain?.Invoke();
                _onCalculateClimate?.Invoke();
                _onRecalculateHydrology?.Invoke();
                UpdateStatus("全部生成完成");
            });
            y -= 32f;

            return y;
        }

        // ===== 折叠分组 =====
        private float CreateFoldableHeader(string key, string title, float y)
        {
            var headerGo = new GameObject($"Header_{key}", typeof(RectTransform), typeof(Image), typeof(Button));
            headerGo.transform.SetParent(_panelRoot.transform, false);
            var rt = (RectTransform)headerGo.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(12f, y);
            rt.sizeDelta = new Vector2(ContentWidth, 24f);
            headerGo.GetComponent<Image>().color = HeaderBg;
            headerGo.GetComponent<Image>().sprite = UITheme.RoundedPanelSprite;
            headerGo.GetComponent<Image>().type = Image.Type.Sliced;

            var arrow = CreateText(headerGo.transform, _foldState[key] ? "▼" : "▶", 11, TextAnchor.MiddleLeft,
                new Vector2(6f, 0f), new Vector2(16f, 24f));
            arrow.color = TextDim;

            var txt = CreateText(headerGo.transform, title, 12, TextAnchor.MiddleLeft,
                new Vector2(22f, 0f), new Vector2(ContentWidth - 30f, 24f));
            txt.color = TextColor;

            string k = key;
            headerGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                _foldState[k] = !_foldState[k];
                arrow.text = _foldState[k] ? "▼" : "▶";
                // 简单实现：重建整个面板
                // 实际项目中可用CanvasGroup控制可见性
                RebuildPanel();
            });

            return y - 28f;
        }

        private void RebuildPanel()
        {
            // 保存当前配置
            var config = _config;
            var genT = _onGenerateTerrain;
            var calcC = _onCalculateClimate;
            var recalcH = _onRecalculateHydrology;

            // 销毁旧面板
            if (_panelRoot != null) Destroy(_panelRoot);

            // 重新创建
            Initialize(config, genT, calcC, recalcH);
        }

        // ===== 参数可见性 =====
        private void UpdateParamVisibility()
        {
            // 导入高度图模式下，禁用海陆骨架参数
            bool landSkeletonEnabled = _config.IsLandSkeletonParamsEnabled;

            if (_landAmountSlider != null) _landAmountSlider.interactable = landSkeletonEnabled;
            if (_fragmentationSlider != null) _fragmentationSlider.interactable = landSkeletonEnabled;
            if (_coastFragSlider != null) _coastFragSlider.interactable = landSkeletonEnabled;
        }

        // ===== UI 辅助方法 =====
        private void CreateLabel(string text, float y)
        {
            var t = CreateText(_panelRoot.transform, text, 11, TextAnchor.MiddleLeft,
                new Vector2(12f, y), new Vector2(LabelWidth, 18f));
            t.color = TextDim;
        }

        private float CreateSliderRow(string label, float min, float max,
            out Slider slider, out Text valueText, float y, Action<float> onChanged)
        {
            CreateLabel(label, y);
            valueText = CreateText(_panelRoot.transform, "", 11, TextAnchor.MiddleRight,
                new Vector2(ContentWidth - ValueWidth, y), new Vector2(ValueWidth, 18f));
            valueText.color = TextColor;
            y -= 20f;

            slider = CreateSlider(_panelRoot.transform,
                new Vector2(12f, y), new Vector2(ContentWidth, 18f), min, max, (min + max) / 2f);
            slider.onValueChanged.AddListener(v =>
            {
                onChanged?.Invoke(v);
                valueText.text = v.ToString("F2");
            });
            return y - 26f;
        }

        private void SetSlider(Slider slider, Text text, float value, string format)
        {
            if (slider != null) slider.value = value;
            if (text != null) text.text = value.ToString(format);
        }

        private void UpdateStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
        }

        // ===== 标准 UI 创建方法（复用 EditorUIPanel 的风格） =====
        private Text CreateText(Transform parent, string content, int fontSize, TextAnchor anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var txt = go.AddComponent<Text>();
            txt.text = content; txt.fontSize = fontSize; txt.alignment = anchor;
            txt.color = TextColor;
            txt.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "SimHei", "Arial" }, fontSize);
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            return txt;
        }

        private Button CreateButton(Transform parent, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = ButtonNormal; img.sprite = UITheme.RoundedPanelSprite; img.type = Image.Type.Sliced;

            var txtGo = new GameObject("Label", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = (RectTransform)txtGo.transform;
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.text = label; txt.fontSize = 11; txt.alignment = TextAnchor.MiddleCenter;
            txt.color = TextColor;
            txt.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "SimHei", "Arial" }, 11);
            return go.GetComponent<Button>();
        }

        private Slider CreateSlider(Transform parent, Vector2 pos, Vector2 size, float min, float max, float value)
        {
            var go = new GameObject("Slider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = new Vector2(0f, 0.5f); bgRt.anchorMax = new Vector2(1f, 0.5f);
            bgRt.sizeDelta = new Vector2(0f, 5f);
            bg.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillRt = (RectTransform)fillArea.transform;
            fillRt.anchorMin = new Vector2(0f, 0.5f); fillRt.anchorMax = new Vector2(1f, 0.5f);
            fillRt.sizeDelta = new Vector2(-18f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImgRt = (RectTransform)fill.transform;
            fillImgRt.anchorMin = Vector2.zero; fillImgRt.anchorMax = Vector2.one;
            fillImgRt.sizeDelta = Vector2.zero;
            fill.GetComponent<Image>().color = SliderFill;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var handleRt = (RectTransform)handleArea.transform;
            handleRt.anchorMin = new Vector2(0f, 0.5f); handleRt.anchorMax = new Vector2(1f, 0.5f);
            handleRt.sizeDelta = new Vector2(-18f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRt2 = (RectTransform)handle.transform;
            handleRt2.sizeDelta = new Vector2(14f, 14f);
            handle.GetComponent<Image>().color = new Color(0.75f, 0.75f, 0.80f, 1f);
            handle.GetComponent<Image>().sprite = UITheme.RoundedPanelSprite;
            handle.GetComponent<Image>().type = Image.Type.Sliced;

            var slider = go.AddComponent<Slider>();
            slider.fillRect = (RectTransform)fill.transform;
            slider.handleRect = handleRt2;
            slider.minValue = min; slider.maxValue = max; slider.value = value;
            return slider;
        }

        private InputField CreateInputField(Transform parent, string placeholder, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("InputField", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 1f);
            go.GetComponent<Image>().sprite = UITheme.RoundedPanelSprite;
            go.GetComponent<Image>().type = Image.Type.Sliced;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = new Vector2(0.05f, 0f); textRt.anchorMax = new Vector2(0.95f, 1f);
            textRt.sizeDelta = Vector2.zero;
            var txt = textGo.AddComponent<Text>();
            txt.text = placeholder; txt.fontSize = 11; txt.alignment = TextAnchor.MiddleLeft;
            txt.color = TextColor;
            txt.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "SimHei", "Arial" }, 11);

            var input = go.AddComponent<InputField>();
            input.textComponent = txt; input.targetGraphic = go.GetComponent<Image>();
            return input;
        }

        private Dropdown CreateDropdown(Transform parent, Vector2 pos, Vector2 size, List<string> options)
        {
            var go = new GameObject("Dropdown", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 1f);
            go.GetComponent<Image>().sprite = UITheme.RoundedPanelSprite;
            go.GetComponent<Image>().type = Image.Type.Sliced;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = new Vector2(0.05f, 0f); labelRt.anchorMax = new Vector2(0.85f, 1f);
            labelRt.sizeDelta = Vector2.zero;
            var label = labelGo.AddComponent<Text>();
            label.text = options.Count > 0 ? options[0] : "";
            label.fontSize = 11; label.alignment = TextAnchor.MiddleLeft;
            label.color = TextColor;
            label.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "SimHei", "Arial" }, 11);

            var arrowGo = new GameObject("Arrow", typeof(RectTransform));
            arrowGo.transform.SetParent(go.transform, false);
            var arrowRt = (RectTransform)arrowGo.transform;
            arrowRt.anchorMin = new Vector2(0.85f, 0f); arrowRt.anchorMax = new Vector2(0.95f, 1f);
            arrowRt.sizeDelta = Vector2.zero;
            var arrow = arrowGo.AddComponent<Text>();
            arrow.text = "▼"; arrow.fontSize = 9; arrow.alignment = TextAnchor.MiddleCenter;
            arrow.color = TextDim;
            arrow.font = Font.CreateDynamicFontFromOSFont(new[] { "Arial" }, 9);

            var dropdown = go.AddComponent<Dropdown>();
            dropdown.targetGraphic = go.GetComponent<Image>();
            dropdown.captionText = label;
            dropdown.AddOptions(options);
            return dropdown;
        }

        private Toggle CreateToggle(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 1f);
            go.GetComponent<Image>().sprite = UITheme.RoundedPanelSprite;
            go.GetComponent<Image>().type = Image.Type.Sliced;

            var checkGo = new GameObject("Checkmark", typeof(RectTransform));
            checkGo.transform.SetParent(go.transform, false);
            var checkRt = (RectTransform)checkGo.transform;
            checkRt.anchorMin = Vector2.zero; checkRt.anchorMax = Vector2.one;
            checkRt.offsetMin = new Vector2(3f, 3f); checkRt.offsetMax = new Vector2(-3f, -3f);
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = ButtonActive;
            checkImg.sprite = UITheme.RoundedPanelSprite;
            checkImg.type = Image.Type.Sliced;

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = go.GetComponent<Image>();
            toggle.graphic = checkImg;
            toggle.isOn = true;
            return toggle;
        }

        private void CreateDivider(Transform parent, Vector2 pos, float width)
        {
            var go = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(width, 1f);
            go.GetComponent<Image>().color = new Color(0.28f, 0.28f, 0.32f, 0.5f);
        }
    }
}
