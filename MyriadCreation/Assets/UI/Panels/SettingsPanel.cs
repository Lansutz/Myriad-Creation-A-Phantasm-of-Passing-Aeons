using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using MyriadCreation.UI.Common;
using MyriadCreation.App.Settings;
using MyriadCreation.Infrastructure.Save;

namespace MyriadCreation.UI.Panels
{
    /// <summary>
    /// 完整游戏设置面板（代码动态生成，沿用 TMPFontUtility 中文字体）：
    /// 分辨率 / 显示模式 / 垂直同步 / 画质 / 帧率上限 / UI 缩放 / 语言 /
    /// 主·音乐·音效音量 / 新游戏默认地图尺寸 / 存档路径只读提示 / 重置默认。
    /// 所有设置经 <see cref="SettingsModel"/> 持久化（PlayerPrefs，前缀 MyriadCreation.Settings.*），
    /// 应用时由 <see cref="SettingsService"/> 落到引擎；面板打开时重新读取当前值。
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        private const float PanelWidth = 900f;
        private const float PanelHeight = 1082f;

        private GameObject _root;
        private Action _onClose;
        private SettingsModel _model;

        private TMP_Dropdown _resolutionDropdown;
        private TMP_Dropdown _displayModeDropdown;
        private Toggle _vsyncToggle;
        private TMP_Dropdown _qualityDropdown;
        private TMP_Dropdown _fpsDropdown;
        private TMP_Dropdown _uiScaleDropdown;
        private TMP_Dropdown _languageDropdown;
        private TMP_Dropdown _fontChoiceDropdown;
        private TMP_Dropdown _mapSizeDropdown;
        private Slider _masterSlider;
        private Slider _musicSlider;
        private Slider _sfxSlider;
        private TextMeshProUGUI _masterValue;
        private TextMeshProUGUI _musicValue;
        private TextMeshProUGUI _sfxValue;
        private TextMeshProUGUI _hintText;

        private List<SettingsModel.ResolutionOption> _resolutionOptions = new List<SettingsModel.ResolutionOption>();
        private string[] _qualityNames = new string[0];
        private int _qualityLevelCount;

        /// <summary>构建设置面板（面板初始隐藏；调用 Show 打开）。</summary>
        public void Build(Transform parent, Action onClose)
        {
            _onClose = onClose;
            if (_root != null)
            {
                UnityEngine.Object.DestroyImmediate(_root);
                _root = null;
            }

            _model = SettingsModel.LoadOrCreate();
            _resolutionOptions = SettingsModel.BuildResolutionOptions(Screen.resolutions);
            _qualityNames = QualitySettings.names ?? new string[0];
            _qualityLevelCount = _qualityNames.Length;

            // 半透明遮罩
            var overlayObj = new GameObject("SettingsOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _root = overlayObj;
            var ort = overlayObj.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
            overlayObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);
            overlayObj.transform.SetParent(parent, false);

            // 面板
            var panelObj = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var prt = panelObj.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            panelObj.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 0.98f);
            panelObj.transform.SetParent(overlayObj.transform, false);
            var panel = panelObj.transform;

            float y = 470f;
            CreateText(panel, "Title", "设置", 32, new Color(0.88f, 0.75f, 0.45f, 1f), FontStyles.Bold,
                new Vector2(0, y), TextAlignmentOptions.Center, new Vector2(400, 40));
            y -= 40f;
            CreateSeparator(panel, y);
            y -= 34f;

            // ===== 显示 =====
            CreateSection(panel, "显示", ref y);

            CreateRowLabel(panel, "分辨率", y);
            _resolutionDropdown = CreateDropdown(panel, "分辨率",
                new Vector2(120, y - 6), new Vector2(380, 32), BuildOptionLabels(_resolutionOptions));
            y -= 42f;

            CreateRowLabel(panel, "显示模式", y);
            _displayModeDropdown = CreateDropdown(panel, "显示模式",
                new Vector2(120, y - 6), new Vector2(380, 32), new List<string>(SettingsModel.DisplayModeLabels));
            y -= 42f;

            _vsyncToggle = CreateToggle(panel, "垂直同步", new Vector2(20, y - 4), new Vector2(260, 28), true);
            y -= 38f;

            CreateRowLabel(panel, "画质等级", y);
            _qualityDropdown = CreateDropdown(panel, "画质等级",
                new Vector2(120, y - 6), new Vector2(380, 32),
                new List<string>(_qualityLevelCount > 0 ? _qualityNames : new[] { "默认（当前）" }));
            y -= 42f;

            CreateRowLabel(panel, "帧率上限", y);
            _fpsDropdown = CreateDropdown(panel, "帧率上限",
                new Vector2(120, y - 6), new Vector2(380, 32), BuildFrameRateLabels());
            y -= 42f;

            CreateRowLabel(panel, "UI 缩放", y);
            _uiScaleDropdown = CreateDropdown(panel, "UI 缩放",
                new Vector2(120, y - 6), new Vector2(380, 32), BuildUiScaleLabels());
            y -= 42f;

            // ===== 音频 =====
            CreateSection(panel, "音频", ref y);

            _masterSlider = CreateSlider(panel, "主音量", y, out _masterValue);
            y -= 42f;
            _musicSlider = CreateSlider(panel, "音乐音量", y, out _musicValue);
            y -= 42f;
            _sfxSlider = CreateSlider(panel, "音效音量", y, out _sfxValue);
            y -= 42f;

            // ===== 其它 =====
            CreateSection(panel, "其它", ref y);

            CreateRowLabel(panel, "界面字体", y);
            _fontChoiceDropdown = CreateDropdown(panel, "界面字体",
                new Vector2(120, y - 6), new Vector2(380, 32), new List<string>(SettingsModel.FontChoiceLabels));
            y -= 42f;

            CreateRowLabel(panel, "语言", y);
            _languageDropdown = CreateDropdown(panel, "语言",
                new Vector2(120, y - 6), new Vector2(380, 32), new List<string>(SettingsModel.LanguageLabels));
            y -= 42f;

            CreateRowLabel(panel, "新游戏默认地图尺寸", y);
            _mapSizeDropdown = CreateDropdown(panel, "新游戏默认地图尺寸",
                new Vector2(120, y - 6), new Vector2(380, 32), new List<string>(SettingsModel.MapSizeLabels));
            y -= 46f;

            _hintText = CreateText(panel, "Hint",
                "提示：vSync 开启时帧率上限不生效；语言切换后部分已打开面板文本需重开生效。",
                13, new Color(0.65f, 0.68f, 0.72f, 1f), FontStyles.Normal,
                new Vector2(0, y), TextAlignmentOptions.Center, new Vector2(PanelWidth - 80, 40));
            y -= 46f;

            // 存档路径（只读提示）
            CreateText(panel, "SavePathLabel", "存档路径（只读）：",
                14, new Color(0.6f, 0.62f, 0.66f, 1f), FontStyles.Normal,
                new Vector2(-300, y), TextAlignmentOptions.MidlineLeft, new Vector2(200, 22));
            y -= 24f;
            CreateText(panel, "SavePath", SaveSystem.GetSaveDirectory(),
                13, new Color(0.72f, 0.74f, 0.78f, 1f), FontStyles.Normal,
                new Vector2(0, y), TextAlignmentOptions.Center, new Vector2(PanelWidth - 80, 40));
            y -= 54f;

            // ===== 按钮 =====
            CreateButton(panel, "应用", new Vector2(-280, y), new Vector2(180, 42), true, OnApplyClicked);
            CreateButton(panel, "重置默认", new Vector2(0, y), new Vector2(180, 42), false, OnResetClicked);
            CreateButton(panel, "取消", new Vector2(280, y), new Vector2(180, 42), false, OnCancelClicked);

            RefreshControlsFromModel();
            _root.SetActive(false);
        }

        public void Show()
        {
            if (_root == null)
            {
                return;
            }
            UiSfx.PlayOpen();
            // 打开时重新读取当前值（含启动默认/其它入口修改）
            _model = SettingsModel.LoadOrCreate();
            RefreshControlsFromModel();
            _root.SetActive(true);
        }

        public void Hide()
        {
            if (_root != null && _root.activeSelf)
            {
                UiSfx.PlayClose();
                _root.SetActive(false);
            }
        }

        // ===== 应用/重置/取消 =====

        private void OnApplyClicked()
        {
            UiSfx.PlayClick();
            SyncControlsToModel();
            _model.Clamp(SettingsModel.GetQualityLevelCount());
            _model.Save();
            SettingsService.Apply(_model);
            // 界面字体：应用后立即刷新全场景 TMP（新字体缓存按 choice）。
            TMPFontUtility.ApplyFontChoice(_model.FontChoice);
            // 语言切换后本面板保留旧文本；提示后关闭，由用户重开面板/场景生效。
            if (_hintText != null)
            {
                _hintText.text = "设置已保存并应用。";
            }
            _onClose?.Invoke();
        }

        private void OnResetClicked()
        {
            UiSfx.PlayClick();
            _model = SettingsModel.ResetToDefaults();
            _model.Clamp(SettingsModel.GetQualityLevelCount());
            _model.Save();
            SettingsService.Apply(_model);
            TMPFontUtility.ApplyFontChoice(_model.FontChoice);
            RefreshControlsFromModel();
            if (_hintText != null)
            {
                _hintText.text = "已恢复默认设置。";
            }
        }

        private void OnCancelClicked()
        {
            _onClose?.Invoke();
        }

        // ===== 模型 ↔ 控件同步 =====

        private void RefreshControlsFromModel()
        {
            if (_model == null)
            {
                return;
            }
            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.value = SettingsModel.FindResolutionIndex(
                    _resolutionOptions, _model.ResolutionWidth, _model.ResolutionHeight, _model.ResolutionRefreshRate);
            }
            if (_displayModeDropdown != null)
            {
                _displayModeDropdown.value = SettingsModel.IndexOfDisplayMode(_model.DisplayMode);
            }
            if (_vsyncToggle != null)
            {
                _vsyncToggle.isOn = _model.VSync;
            }
            if (_qualityDropdown != null)
            {
                if (_qualityLevelCount > 0)
                {
                    int level = _model.QualityLevel >= 0
                        ? Mathf.Clamp(_model.QualityLevel, 0, _qualityLevelCount - 1)
                        : Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, _qualityLevelCount - 1);
                    _qualityDropdown.value = level;
                }
                else
                {
                    _qualityDropdown.value = 0;
                }
            }
            if (_fpsDropdown != null)
            {
                _fpsDropdown.value = SettingsModel.IndexOfFrameRate(_model.TargetFrameRate);
            }
            if (_uiScaleDropdown != null)
            {
                _uiScaleDropdown.value = SettingsModel.IndexOfUiScale(_model.UiScale);
            }
            if (_fontChoiceDropdown != null)
            {
                _fontChoiceDropdown.value = _model.FontChoice >= 0 && _model.FontChoice <= SettingsModel.MaxFontChoice
                    ? _model.FontChoice : 0;
            }
            if (_languageDropdown != null)
            {
                _languageDropdown.value = SettingsModel.IndexOfLanguage(_model.Language);
            }
            if (_mapSizeDropdown != null)
            {
                _mapSizeDropdown.value = SettingsModel.IndexOfMapSize(_model.DefaultMapSize);
            }
            if (_masterSlider != null)
            {
                _masterSlider.value = _model.MasterVolume;
            }
            if (_musicSlider != null)
            {
                _musicSlider.value = _model.MusicVolume;
            }
            if (_sfxSlider != null)
            {
                _sfxSlider.value = _model.SfxVolume;
            }
            UpdateVolumeLabels();
        }

        private void SyncControlsToModel()
        {
            if (_model == null)
            {
                _model = SettingsModel.CreateDefault();
            }
            if (_resolutionDropdown != null && _resolutionOptions.Count > 0)
            {
                int index = Mathf.Clamp(_resolutionDropdown.value, 0, _resolutionOptions.Count - 1);
                SettingsModel.ResolutionOption option = _resolutionOptions[index];
                _model.ResolutionWidth = option.Width;
                _model.ResolutionHeight = option.Height;
                _model.ResolutionRefreshRate = option.RefreshRate;
            }
            if (_displayModeDropdown != null)
            {
                int index = Mathf.Clamp(_displayModeDropdown.value, 0, SettingsModel.DisplayModes.Length - 1);
                _model.DisplayMode = SettingsModel.DisplayModes[index];
            }
            if (_vsyncToggle != null)
            {
                _model.VSync = _vsyncToggle.isOn;
            }
            if (_qualityDropdown != null)
            {
                _model.QualityLevel = _qualityLevelCount > 0 ? _qualityDropdown.value : SettingsModel.KeepCurrentQualityLevel;
            }
            if (_fpsDropdown != null)
            {
                int index = Mathf.Clamp(_fpsDropdown.value, 0, SettingsModel.AllowedFrameRates.Length - 1);
                _model.TargetFrameRate = SettingsModel.AllowedFrameRates[index];
            }
            if (_uiScaleDropdown != null)
            {
                int index = Mathf.Clamp(_uiScaleDropdown.value, 0, SettingsModel.AllowedUiScales.Length - 1);
                _model.UiScale = SettingsModel.AllowedUiScales[index];
            }
            if (_fontChoiceDropdown != null)
            {
                int choice = Mathf.Clamp(_fontChoiceDropdown.value, 0, SettingsModel.MaxFontChoice);
                _model.FontChoice = choice;
            }
            if (_languageDropdown != null)
            {
                int index = Mathf.Clamp(_languageDropdown.value, 0, SettingsModel.SupportedLanguages.Length - 1);
                _model.Language = SettingsModel.SupportedLanguages[index];
            }
            if (_mapSizeDropdown != null)
            {
                int index = Mathf.Clamp(_mapSizeDropdown.value, 0, SettingsModel.MapSizes.Length - 1);
                _model.DefaultMapSize = SettingsModel.MapSizes[index];
            }
            if (_masterSlider != null)
            {
                _model.MasterVolume = _masterSlider.value;
            }
            if (_musicSlider != null)
            {
                _model.MusicVolume = _musicSlider.value;
            }
            if (_sfxSlider != null)
            {
                _model.SfxVolume = _sfxSlider.value;
            }
        }

        private void UpdateVolumeLabels()
        {
            UpdateVolumeLabel(_masterValue, _masterSlider);
            UpdateVolumeLabel(_musicValue, _musicSlider);
            UpdateVolumeLabel(_sfxValue, _sfxSlider);
        }

        private static void UpdateVolumeLabel(TextMeshProUGUI label, Slider slider)
        {
            if (label != null && slider != null)
            {
                label.text = Mathf.RoundToInt(slider.value * 100f) + "%";
            }
        }

        // ===== 选项文本 =====

        private static List<string> BuildOptionLabels(List<SettingsModel.ResolutionOption> options)
        {
            var labels = new List<string>(options.Count);
            for (int i = 0; i < options.Count; i++)
            {
                labels.Add(options[i].Label);
            }
            return labels;
        }

        private static List<string> BuildFrameRateLabels()
        {
            var labels = new List<string>(SettingsModel.AllowedFrameRates.Length);
            for (int i = 0; i < SettingsModel.AllowedFrameRates.Length; i++)
            {
                int fps = SettingsModel.AllowedFrameRates[i];
                labels.Add(fps < 0 ? "无限制" : fps + " FPS");
            }
            return labels;
        }

        private static List<string> BuildUiScaleLabels()
        {
            var labels = new List<string>(SettingsModel.AllowedUiScales.Length);
            for (int i = 0; i < SettingsModel.AllowedUiScales.Length; i++)
            {
                labels.Add(SettingsModel.AllowedUiScales[i].ToString("0.0") + "×");
            }
            return labels;
        }

        // ===== 控件构造 =====

        private void CreateSection(Transform parent, string title, ref float y)
        {
            CreateText(parent, title + "_Section", title, 20, new Color(0.88f, 0.75f, 0.45f, 1f), FontStyles.Bold,
                new Vector2(-300, y), TextAlignmentOptions.MidlineLeft, new Vector2(300, 28));
            y -= 32f;
        }

        private void CreateRowLabel(Transform parent, string label, float y)
        {
            CreateText(parent, label + "_Label", label, 18, Color.white, FontStyles.Normal,
                new Vector2(-300, y), TextAlignmentOptions.MidlineLeft, new Vector2(380, 28));
        }

        private Slider CreateSlider(Transform parent, string label, float y, out TextMeshProUGUI valueText)
        {
            CreateRowLabel(parent, label, y);

            var sliderObj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
            var rt = sliderObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(80, y - 6);
            rt.sizeDelta = new Vector2(360, 20);
            var bg = sliderObj.GetComponent<Image>();
            bg.color = new Color(0.18f, 0.19f, 0.23f, 1f);
            bg.raycastTarget = true;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.SetParent(sliderObj.transform, false);
            fillAreaRt.anchorMin = new Vector2(0, 0.25f); fillAreaRt.anchorMax = new Vector2(1, 0.75f);
            fillAreaRt.offsetMin = new Vector2(0, 0); fillAreaRt.offsetMax = new Vector2(0, 0);

            var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.SetParent(fillArea.transform, false);
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(0, 1);
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            var fillImage = fillObj.GetComponent<Image>();
            fillImage.color = new Color(0.65f, 0.55f, 0.30f, 1f);
            fillImage.raycastTarget = false;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.SetParent(sliderObj.transform, false);
            handleAreaRt.anchorMin = Vector2.zero; handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(8, 0); handleAreaRt.offsetMax = new Vector2(-8, 0);

            var handleObj = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var handleRt = handleObj.GetComponent<RectTransform>();
            handleRt.SetParent(handleArea.transform, false);
            handleRt.sizeDelta = new Vector2(16, 24);
            var handleImage = handleObj.GetComponent<Image>();
            handleImage.color = new Color(0.88f, 0.75f, 0.45f, 1f);

            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 0f; slider.maxValue = 1f; slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImage;
            slider.onValueChanged.AddListener(_ => UpdateVolumeLabels());
            sliderObj.transform.SetParent(parent, false);

            valueText = CreateText(parent, label + "_Value", "100%", 15, new Color(0.75f, 0.78f, 0.82f, 1f),
                FontStyles.Normal, new Vector2(360, y), TextAlignmentOptions.MidlineLeft, new Vector2(70, 24));
            return slider;
        }

        private TMP_Dropdown CreateDropdown(Transform parent, string name, Vector2 anchoredPos, Vector2 size, List<string> options)
        {
            var ddObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_Dropdown));
            var rt = ddObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var bg = ddObj.GetComponent<Image>();
            bg.color = new Color(0.18f, 0.19f, 0.23f, 1f);

            var caption = CreateText(ddObj.transform, "Caption", options.Count > 0 ? options[0] : "",
                16, Color.white, FontStyles.Normal, new Vector2(0, -2), TextAlignmentOptions.MidlineLeft,
                new Vector2(size.x - 44, size.y));
            var captionRt = caption.GetComponent<RectTransform>();
            captionRt.anchorMin = Vector2.zero; captionRt.anchorMax = Vector2.one;
            captionRt.offsetMin = new Vector2(12, 0); captionRt.offsetMax = new Vector2(-32, 0);
            caption.raycastTarget = false;

            RectTransform template = CreateDropdownTemplate(ddObj.transform, options.Count);

            var dropdown = ddObj.GetComponent<TMP_Dropdown>();
            dropdown.targetGraphic = bg;
            dropdown.template = template;
            dropdown.captionText = caption;
            dropdown.itemText = template.Find("Viewport/Content/Item/Item Label")?.GetComponent<TextMeshProUGUI>();
            dropdown.ClearOptions();
            var optionData = new List<TMP_Dropdown.OptionData>(options.Count);
            for (int i = 0; i < options.Count; i++)
            {
                optionData.Add(new TMP_Dropdown.OptionData(options[i]));
            }
            dropdown.AddOptions(optionData);
            if (options.Count > 0)
            {
                dropdown.value = 0;
            }
            ddObj.transform.SetParent(parent, false);
            return dropdown;
        }

        /// <summary>程序化 Dropdown 模板（TMP_Dropdown 必须带 Item/Toggle 模板才能展开下拉列表）。</summary>
        private RectTransform CreateDropdownTemplate(Transform parent, int optionCount)
        {
            var templateObj = new GameObject("Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            var templateRt = templateObj.GetComponent<RectTransform>();
            templateRt.SetParent(parent, false);
            templateRt.anchorMin = new Vector2(0f, 0f); templateRt.anchorMax = new Vector2(1f, 0f);
            templateRt.pivot = new Vector2(0.5f, 1f);
            templateRt.anchoredPosition = new Vector2(0, -34);
            float height = Mathf.Clamp(optionCount * 28f + 8f, 60f, 240f);
            templateRt.sizeDelta = new Vector2(0, height);
            templateObj.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.17f, 0.98f);

            var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            var viewportRt = viewportObj.GetComponent<RectTransform>();
            viewportRt.SetParent(templateObj.transform, false);
            viewportRt.anchorMin = Vector2.zero; viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(0, 0); viewportRt.offsetMax = new Vector2(0, 0);
            viewportObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            var contentObj = new GameObject("Content", typeof(RectTransform));
            var contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.SetParent(viewportObj.transform, false);
            contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0, Mathf.Max(28f, optionCount * 28f));

            var itemObj = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            var itemRt = itemObj.GetComponent<RectTransform>();
            itemRt.SetParent(contentObj.transform, false);
            itemRt.anchorMin = new Vector2(0f, 1f); itemRt.anchorMax = new Vector2(1f, 1f);
            itemRt.pivot = new Vector2(0.5f, 1f);
            itemRt.anchoredPosition = Vector2.zero;
            itemRt.sizeDelta = new Vector2(0, 28);

            var itemBgObj = new GameObject("Item Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var itemBgRt = itemBgObj.GetComponent<RectTransform>();
            itemBgRt.SetParent(itemObj.transform, false);
            itemBgRt.anchorMin = Vector2.zero; itemBgRt.anchorMax = Vector2.one;
            itemBgRt.offsetMin = Vector2.zero; itemBgRt.offsetMax = Vector2.zero;
            var itemBg = itemBgObj.GetComponent<Image>();
            itemBg.color = new Color(0.16f, 0.17f, 0.21f, 0.9f);

            var checkObj = new GameObject("Item Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var checkRt = checkObj.GetComponent<RectTransform>();
            checkRt.SetParent(itemObj.transform, false);
            checkRt.anchorMin = new Vector2(0f, 0.5f); checkRt.anchorMax = new Vector2(0f, 0.5f);
            checkRt.pivot = new Vector2(0f, 0.5f); checkRt.anchoredPosition = new Vector2(8, 0);
            checkRt.sizeDelta = new Vector2(12, 12);
            var checkImage = checkObj.GetComponent<Image>();
            checkImage.color = new Color(0.88f, 0.75f, 0.45f, 1f);
            checkImage.raycastTarget = false;

            var label = CreateText(itemObj.transform, "Item Label", "", 16, Color.white, FontStyles.Normal,
                Vector2.zero, TextAlignmentOptions.MidlineLeft, new Vector2(0, 28));
            var labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(28, 0); labelRt.offsetMax = new Vector2(-8, 0);
            label.raycastTarget = false;

            var toggle = itemObj.GetComponent<Toggle>();
            toggle.targetGraphic = itemBg;
            toggle.graphic = checkImage;
            toggle.isOn = false;

            var scroll = templateObj.GetComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            templateObj.SetActive(false);
            return templateRt;
        }

        private Toggle CreateToggle(Transform parent, string label, Vector2 anchoredPos, Vector2 size, bool defaultValue)
        {
            var toggleObj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
            var rt = toggleObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var bg = toggleObj.GetComponent<Image>();
            bg.color = new Color(0.18f, 0.19f, 0.23f, 1f);
            bg.raycastTarget = true;

            var checkObj = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var checkRt = checkObj.GetComponent<RectTransform>();
            checkRt.SetParent(toggleObj.transform, false);
            checkRt.anchorMin = new Vector2(0f, 0.5f); checkRt.anchorMax = new Vector2(0f, 0.5f);
            checkRt.pivot = new Vector2(0f, 0.5f); checkRt.anchoredPosition = new Vector2(8, 0);
            checkRt.sizeDelta = new Vector2(16, 16);
            var checkImage = checkObj.GetComponent<Image>();
            checkImage.color = new Color(0.88f, 0.75f, 0.45f, 1f);
            checkImage.raycastTarget = false;

            var labelText = CreateText(toggleObj.transform, "Label", label, 17, Color.white, FontStyles.Normal,
                Vector2.zero, TextAlignmentOptions.MidlineLeft, new Vector2(size.x - 40, size.y));
            var labelRt = labelText.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(32, 0); labelRt.offsetMax = new Vector2(0, 0);
            labelText.raycastTarget = false;

            var toggle = toggleObj.GetComponent<Toggle>();
            toggle.targetGraphic = bg;
            toggle.graphic = checkImage;
            toggle.isOn = defaultValue;
            toggleObj.transform.SetParent(parent, false);
            return toggle;
        }

        private void CreateButton(Transform parent, string label, Vector2 pos, Vector2 size, bool primary, Action onClick)
        {
            var btnObj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            btnObj.GetComponent<Image>().color = primary
                ? new Color(0.30f, 0.25f, 0.12f, 1f)
                : new Color(0.18f, 0.19f, 0.23f, 1f);
            var btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            CreateText(btnObj.transform, "Label", label, 18,
                primary ? new Color(0.88f, 0.75f, 0.45f, 1f) : Color.white,
                primary ? FontStyles.Bold : FontStyles.Normal, Vector2.zero, TextAlignmentOptions.Center, size);
            btnObj.AddComponent<UIButtonHover>();
            btnObj.transform.SetParent(parent, false);
        }

        private void CreateSeparator(Transform parent, float y)
        {
            var line = new GameObject("Line", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 1f); lrt.anchorMax = new Vector2(0.5f, 1f);
            lrt.pivot = new Vector2(0.5f, 1f); lrt.anchoredPosition = new Vector2(0, y);
            lrt.sizeDelta = new Vector2(PanelWidth - 80, 1);
            line.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f, 1f);
            line.GetComponent<Image>().raycastTarget = false;
            line.transform.SetParent(parent, false);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, Color color,
            FontStyles style, Vector2 pos, TextAlignmentOptions align, Vector2? size = null)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos;
            rt.sizeDelta = size ?? new Vector2(400, 30);
            var tmp = obj.GetComponent<TextMeshProUGUI>();
            // 运行时动态创建的文本不会走 UIManager 启动时的一次性字体应用，必须显式赋字体，
            // 否则 TMP 默认字体缺失时整块设置面板文字不可见。
            TMP_FontAsset font = TMPFontUtility.GetChineseFont();
            if (font != null)
            {
                tmp.font = font;
            }
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            obj.transform.SetParent(parent, false);
            return tmp;
        }
    }
}
