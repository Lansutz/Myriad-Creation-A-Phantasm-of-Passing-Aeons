using System.Collections.Generic;
using CivilizationEvolution.Map;
using CivilizationEvolution.Render;
using MapEditor = CivilizationEvolution.Render.MapEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CivilizationEvolution.UI
{
    /// <summary>
    /// 地图编辑器 UI 面板
    /// 代码动态生成浮动工具栏：模式切换、8种工具、画笔大小、省份选择、Burg类型、撤销重做
    /// 对齐 FantasyMapSimulator: CustomMapImageLayer / MapBrush / cursorPixelPosition
    /// </summary>
    public class EditorUIPanel : MonoBehaviour
    {
        private MapEditor _editor;
        private MapSaveSystem _saveSystem;
        private MapRenderer _renderer;

        // UI 引用
        private GameObject _panelRoot;
        private Text _titleText;
        private Text _statusText;
        private Button _toggleEditBtn;
        private Slider _brushSizeSlider;
        private Text _brushSizeText;
        private InputField _provinceInput;
        private Dropdown _burgTypeDropdown;
        private InputField _saveNameInput;
        private InputField _renameProvinceInput;
        private Button _undoBtn;
        private Button _redoBtn;
        private readonly List<Button> _toolButtons = new List<Button>();
        private readonly List<EditorTool> _toolList = new List<EditorTool>
        {
            EditorTool.TerrainLand,
            EditorTool.TerrainSea,
            EditorTool.TerrainMountain,
            EditorTool.TerrainPlain,
            EditorTool.ProvincePaint,
            EditorTool.ProvinceErase,
            EditorTool.BurgPlace,
            EditorTool.BurgRemove
        };

        private static readonly string[] ToolNames =
        {
            "造陆", "填海", "造山", "平整",
            "绘省", "擦省", "放镇", "删镇"
        };

        private static readonly Color PanelBgColor = new Color(0.12f, 0.12f, 0.15f, 0.92f);
        private static readonly Color ButtonNormalColor = new Color(0.25f, 0.25f, 0.3f, 1f);
        private static readonly Color ButtonActiveColor = new Color(0.35f, 0.55f, 0.85f, 1f);
        private static readonly Color TextColor = new Color(0.9f, 0.9f, 0.92f, 1f);
        private static readonly Color TextDimColor = new Color(0.6f, 0.6f, 0.65f, 1f);

        /// <summary>初始化编辑器 UI 面板</summary>
        public void Initialize(MapRenderer renderer, MapEditor editor)
        {
            _renderer = renderer;
            _editor = editor;
            // 双保险（查漏补缺：场景重建/时序——editor 引用可能为空——
            // 从 renderer 懒取——仍空则跳过面板创建[编辑器模式才需]）
            if (_editor == null && renderer != null)
                _editor = renderer.GetMapEditor();
            if (_editor == null)
            {
                Debug.LogWarning("[EditorUIPanel] 编辑器未就绪——跳过面板创建（非编辑器场景安全）");
                return;
            }
            CreatePanel();
            RegisterEvents();
            _saveSystem = new MapSaveSystem(_renderer != null ? _renderer.World : null, _renderer);
            UpdateUIState();
            _panelRoot.SetActive(false); // 默认隐藏，按快捷键或按钮显示
            Debug.Log("[EditorUIPanel] 编辑器UI面板已创建");
        }

        /// <summary>切换面板显示/隐藏</summary>
        public void TogglePanel()
        {
            if (_panelRoot == null) return;
            _panelRoot.SetActive(!_panelRoot.activeSelf);
        }

        public void ShowPanel() => _panelRoot?.SetActive(true);
        public void HidePanel() => _panelRoot?.SetActive(false);
        public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf;

        private void Update()
        {
            // 快捷键：E 切换编辑模式，Tab 切换面板，Ctrl+Z 撤销，Ctrl+Y 重做
            if (Input.GetKeyDown(KeyCode.Tab))
                TogglePanel();

            if (_editor == null || !IsVisible) return;

            if (Input.GetKeyDown(KeyCode.E))
                _editor.ToggleEditMode();

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.Z)) _editor.Undo();
                if (Input.GetKeyDown(KeyCode.Y)) _editor.Redo();
            }

            // 数字键 1-8 快速切换工具
            if (Input.inputString.Length > 0 && char.IsDigit(Input.inputString[0]))
            {
                int idx = Input.inputString[0] - '1';
                if (idx >= 0 && idx < _toolList.Count)
                    _editor.SetTool(_toolList[idx]);
            }

            UpdateUIState();
        }

        // ===== UI 创建 =====
        private void CreatePanel()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[EditorUIPanel] 场景中没有Canvas，无法创建编辑器UI");
                return;
            }

            // 面板根节点
            _panelRoot = new GameObject("EditorPanel", typeof(RectTransform), typeof(Image));
            _panelRoot.transform.SetParent(canvas.transform, false);
            var panelRt = (RectTransform)_panelRoot.transform;
            panelRt.anchorMin = new Vector2(0f, 0.5f);
            panelRt.anchorMax = new Vector2(0f, 0.5f);
            panelRt.pivot = new Vector2(0f, 0.5f);
            panelRt.anchoredPosition = new Vector2(10f, 0f);
            panelRt.sizeDelta = new Vector2(210f, 520f);
            var panelImg = _panelRoot.GetComponent<Image>();
            panelImg.color = PanelBgColor;
            panelImg.sprite = UITheme.RoundedPanelSprite;
            panelImg.type = Image.Type.Sliced;

            float y = -12f;
            float contentWidth = 190f;

            // 标题
            _titleText = CreateText(_panelRoot.transform, "地图编辑器", 16, TextAnchor.MiddleCenter,
                new Vector2(10f, y), new Vector2(contentWidth, 28f));
            _titleText.color = TextColor;
            y -= 34f;

            // 编辑模式切换按钮
            _toggleEditBtn = CreateButton(_panelRoot.transform, "编辑模式: 关",
                new Vector2(10f, y), new Vector2(contentWidth, 32f));
            _toggleEditBtn.onClick.AddListener(() => _editor.ToggleEditMode());
            y -= 40f;

            // 分隔线
            CreateDivider(_panelRoot.transform, new Vector2(10f, y), contentWidth);
            y -= 12f;

            // 工具标题
            CreateText(_panelRoot.transform, "工具 (1-8)", 12, TextAnchor.MiddleLeft,
                new Vector2(10f, y), new Vector2(contentWidth, 18f)).color = TextDimColor;
            y -= 22f;

            // 工具按钮网格（4列2行）
            float btnW = 44f, btnH = 36f, gap = 4f;
            for (int i = 0; i < _toolList.Count; i++)
            {
                int col = i % 4;
                int row = i / 4;
                float bx = 10f + col * (btnW + gap);
                float by = y - row * (btnH + gap);
                var btn = CreateButton(_panelRoot.transform, ToolNames[i],
                    new Vector2(bx, by), new Vector2(btnW, btnH));
                int toolIdx = i;
                btn.onClick.AddListener(() => _editor.SetTool(_toolList[toolIdx]));
                _toolButtons.Add(btn);
            }
            y -= 2 * (btnH + gap) + 8f;

            // 分隔线
            CreateDivider(_panelRoot.transform, new Vector2(10f, y), contentWidth);
            y -= 12f;

            // 画笔大小
            CreateText(_panelRoot.transform, "画笔大小", 12, TextAnchor.MiddleLeft,
                new Vector2(10f, y), new Vector2(100f, 18f)).color = TextDimColor;
            _brushSizeText = CreateText(_panelRoot.transform, "1", 12, TextAnchor.MiddleRight,
                new Vector2(110f, y), new Vector2(90f, 18f));
            _brushSizeText.color = TextColor;
            y -= 22f;

            _brushSizeSlider = CreateSlider(_panelRoot.transform,
                new Vector2(10f, y), new Vector2(contentWidth, 20f), 1f, 8f, 1f);
            _brushSizeSlider.onValueChanged.AddListener(v =>
            {
                _editor.BrushSize = (int)v;
                _brushSizeText.text = ((int)v).ToString();
            });
            y -= 30f;

            // 省份 ID 输入（省份画笔用）
            CreateText(_panelRoot.transform, "省份ID (绘省用)", 12, TextAnchor.MiddleLeft,
                new Vector2(10f, y), new Vector2(contentWidth, 18f)).color = TextDimColor;
            y -= 22f;
            _provinceInput = CreateInputField(_panelRoot.transform, "0",
                new Vector2(10f, y), new Vector2(contentWidth, 26f));
            _provinceInput.onValueChanged.AddListener(v =>
            {
                if (int.TryParse(v, out int id))
                    _editor.SelectedProvinceId = id;
            });
            y -= 34f;

            // Burg 类型下拉（Burg放置用）
            CreateText(_panelRoot.transform, "子地块类型 (放镇用)", 12, TextAnchor.MiddleLeft,
                new Vector2(10f, y), new Vector2(contentWidth, 18f)).color = TextDimColor;
            y -= 22f;
            _burgTypeDropdown = CreateDropdown(_panelRoot.transform,
                new Vector2(10f, y), new Vector2(contentWidth, 26f),
                new List<string> { "村庄", "集镇", "城市", "港口", "首都", "要塞" });
            _burgTypeDropdown.onValueChanged.AddListener(v =>
            {
                _editor.SelectedBurgType = (BurgType)v;
            });
            _burgTypeDropdown.value = 2; // 默认城市
            y -= 34f;

            // 分隔线
            CreateDivider(_panelRoot.transform, new Vector2(10f, y), contentWidth);
            y -= 12f;

            // 撤销/重做按钮
            _undoBtn = CreateButton(_panelRoot.transform, "撤销 Ctrl+Z",
                new Vector2(10f, y), new Vector2(92f, 28f));
            _undoBtn.onClick.AddListener(() => _editor.Undo());
            _redoBtn = CreateButton(_panelRoot.transform, "重做 Ctrl+Y",
                new Vector2(108f, y), new Vector2(92f, 28f));
            _redoBtn.onClick.AddListener(() => _editor.Redo());
            y -= 36f;

            // 状态文本
            _statusText = CreateText(_panelRoot.transform, "", 11, TextAnchor.UpperLeft,
                new Vector2(10f, y), new Vector2(contentWidth, 50f));
            _statusText.color = TextDimColor;
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;

            // 分隔线
            CreateDivider(_panelRoot.transform, new Vector2(10f, y), contentWidth);
            y -= 12f;

            // 存档与导出标题
            CreateText(_panelRoot.transform, "存档 / 导出", 12, TextAnchor.MiddleLeft,
                new Vector2(10f, y), new Vector2(contentWidth, 18f)).color = TextDimColor;
            y -= 22f;

            // 存档文件名输入
            _saveNameInput = CreateInputField(_panelRoot.transform, "my_map",
                new Vector2(10f, y), new Vector2(contentWidth, 24f));
            y -= 30f;

            // 保存/加载按钮
            var saveBtn = CreateButton(_panelRoot.transform, "保存地图",
                new Vector2(10f, y), new Vector2(92f, 26f));
            saveBtn.onClick.AddListener(() => {
                if (_saveSystem != null && !string.IsNullOrEmpty(_saveNameInput.text))
                    _saveSystem.SaveMap(_saveNameInput.text);
            });
            var loadBtn = CreateButton(_panelRoot.transform, "加载地图",
                new Vector2(108f, y), new Vector2(92f, 26f));
            loadBtn.onClick.AddListener(() => {
                if (_saveSystem != null && !string.IsNullOrEmpty(_saveNameInput.text))
                    _saveSystem.LoadMap(_saveNameInput.text);
            });
            y -= 34f;

            // 重算海洋按钮（绘制完陆地后重新计算海洋等级）
            var recalcOceanBtn = CreateButton(_panelRoot.transform, "重算海洋",
                new Vector2(10f, y), new Vector2(contentWidth, 26f));
            recalcOceanBtn.onClick.AddListener(() => {
                if (_editor != null)
                {
                    _editor.RecalculateOceanZones();
                    if (_renderer != null) _renderer.ForceRefresh();
                }
            });
            y -= 34f;

            // 省份重命名
            CreateText(_panelRoot.transform, "省份重命名 (ID,名称)", 11, TextAnchor.MiddleLeft,
                new Vector2(10f, y), new Vector2(contentWidth, 16f)).color = TextDimColor;
            y -= 18f;
            _renameProvinceInput = CreateInputField(_panelRoot.transform, "0,新省名",
                new Vector2(10f, y), new Vector2(120f, 22f));
            var renameBtn = CreateButton(_panelRoot.transform, "重命名",
                new Vector2(136f, y), new Vector2(64f, 22f));
            renameBtn.onClick.AddListener(() => {
                if (_saveSystem == null || string.IsNullOrEmpty(_renameProvinceInput.text)) return;
                char comma = (char)44;
                char fullComma = (char)65292;
                var parts = _renameProvinceInput.text.Split(new char[] { comma, fullComma }, 2);
                if (parts.Length == 2 && int.TryParse(parts[0], out int pid))
                    _saveSystem.RenameProvince(pid, parts[1]);
            });
            y -= 30f;

            // 导出PNG按钮
            var exportBtn = CreateButton(_panelRoot.transform, "导出当前视图PNG",
                new Vector2(10f, y), new Vector2(contentWidth, 26f));
            exportBtn.onClick.AddListener(() => {
                if (_saveSystem != null)
                    _saveSystem.ExportMapPNG(_saveNameInput.text + "_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            });
            y -= 32f;

            // 打开存档目录按钮
            var openDirBtn = CreateButton(_panelRoot.transform, "打开存档目录",
                new Vector2(10f, y), new Vector2(contentWidth, 22f));
            openDirBtn.onClick.AddListener(() => MapSaveSystem.OpenSaveDirectory());
            y -= 28f;
            // 调整面板高度
            panelRt.sizeDelta = new Vector2(210f, Mathf.Abs(y) + 20f);
        }

        private void RegisterEvents()
        {
            if (_editor == null) return;
            _editor.OnEditModeChanged += UpdateUIState;
            _editor.OnToolChanged += UpdateUIState;
            _editor.OnMapEdited += UpdateUIState;
        }

        private void UpdateUIState()
        {
            if (_editor == null) return;

            // 编辑模式按钮
            if (_toggleEditBtn != null)
            {
                var btnText = _toggleEditBtn.GetComponentInChildren<Text>();
                if (btnText != null)
                    btnText.text = _editor.IsEditMode ? "编辑模式: 开" : "编辑模式: 关";
                var img = _toggleEditBtn.GetComponent<Image>();
                if (img != null)
                    img.color = _editor.IsEditMode ? ButtonActiveColor : ButtonNormalColor;
            }

            // 工具按钮高亮
            for (int i = 0; i < _toolButtons.Count && i < _toolList.Count; i++)
            {
                var img = _toolButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = _editor.CurrentTool == _toolList[i] ? ButtonActiveColor : ButtonNormalColor;
            }

            // 画笔大小
            if (_brushSizeSlider != null && Mathf.Abs(_brushSizeSlider.value - _editor.BrushSize) > 0.01f)
                _brushSizeSlider.value = _editor.BrushSize;
            if (_brushSizeText != null)
                _brushSizeText.text = _editor.BrushSize.ToString();

            // 撤销/重做按钮状态
            if (_undoBtn != null) _undoBtn.interactable = _editor.UndoCount > 0;
            if (_redoBtn != null) _redoBtn.interactable = _editor.RedoCount > 0;

            // 状态文本
            if (_statusText != null)
            {
                string toolName = _editor.CurrentTool.ToString();
                _statusText.text =
                    $"工具: {toolName}\n" +
                    $"画笔: {_editor.BrushSize}\n" +
                    $"省份ID: {_editor.SelectedProvinceId}\n" +
                    $"撤销: {_editor.UndoCount}  重做: {_editor.RedoCount}\n" +
                    $"快捷键: Tab面板 E编辑 1-8工具 Ctrl+Z/Y";
            }
        }

        // ===== UI 辅助创建方法 =====
        private Text CreateText(Transform parent, string content, int fontSize, TextAnchor anchor,
            Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var txt = go.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = anchor;
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
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = ButtonNormalColor;
            img.sprite = UITheme.RoundedPanelSprite;
            img.type = Image.Type.Sliced;

            var txtGo = new GameObject("Label", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = (RectTransform)txtGo.transform;
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 12;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = TextColor;
            txt.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "SimHei", "Arial" }, 12);
            return go.GetComponent<Button>();
        }

        private Slider CreateSlider(Transform parent, Vector2 pos, Vector2 size,
            float min, float max, float value)
        {
            var go = new GameObject("Slider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = new Vector2(0f, 0.5f);
            bgRt.anchorMax = new Vector2(1f, 0.5f);
            bgRt.sizeDelta = new Vector2(0f, 6f);
            bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillRt = (RectTransform)fillArea.transform;
            fillRt.anchorMin = new Vector2(0f, 0.5f);
            fillRt.anchorMax = new Vector2(1f, 0.5f);
            fillRt.sizeDelta = new Vector2(-20f, 0f);
            fillRt.anchoredPosition = new Vector2(0f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImgRt = (RectTransform)fill.transform;
            fillImgRt.anchorMin = Vector2.zero;
            fillImgRt.anchorMax = Vector2.one;
            fillImgRt.sizeDelta = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.35f, 0.55f, 0.85f, 1f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var handleRt = (RectTransform)handleArea.transform;
            handleRt.anchorMin = new Vector2(0f, 0.5f);
            handleRt.anchorMax = new Vector2(1f, 0.5f);
            handleRt.sizeDelta = new Vector2(-20f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRt2 = (RectTransform)handle.transform;
            handleRt2.sizeDelta = new Vector2(16f, 16f);
            handle.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.75f, 1f);
            handle.GetComponent<Image>().sprite = UITheme.RoundedPanelSprite;
            handle.GetComponent<Image>().type = Image.Type.Sliced;

            var slider = go.AddComponent<Slider>();
            slider.fillRect = (RectTransform)fill.transform;
            slider.handleRect = handleRt2;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            return slider;
        }

        private InputField CreateInputField(Transform parent, string placeholder, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("InputField", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f, 1f);
            go.GetComponent<Image>().sprite = UITheme.RoundedPanelSprite;
            go.GetComponent<Image>().type = Image.Type.Sliced;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = new Vector2(0.05f, 0f);
            textRt.anchorMax = new Vector2(0.95f, 1f);
            textRt.sizeDelta = Vector2.zero;
            var txt = textGo.AddComponent<Text>();
            txt.text = placeholder;
            txt.fontSize = 13;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = TextColor;
            txt.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "SimHei", "Arial" }, 13);

            var input = go.AddComponent<InputField>();
            input.textComponent = txt;
            input.targetGraphic = go.GetComponent<Image>();
            return input;
        }

        private Dropdown CreateDropdown(Transform parent, Vector2 pos, Vector2 size, List<string> options)
        {
            var go = new GameObject("Dropdown", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f, 1f);
            go.GetComponent<Image>().sprite = UITheme.RoundedPanelSprite;
            go.GetComponent<Image>().type = Image.Type.Sliced;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = new Vector2(0.05f, 0f);
            labelRt.anchorMax = new Vector2(0.85f, 1f);
            labelRt.sizeDelta = Vector2.zero;
            var label = labelGo.AddComponent<Text>();
            label.text = options.Count > 0 ? options[0] : "";
            label.fontSize = 13;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = TextColor;
            label.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "SimHei", "Arial" }, 13);

            var arrowGo = new GameObject("Arrow", typeof(RectTransform));
            arrowGo.transform.SetParent(go.transform, false);
            var arrowRt = (RectTransform)arrowGo.transform;
            arrowRt.anchorMin = new Vector2(0.85f, 0f);
            arrowRt.anchorMax = new Vector2(0.95f, 1f);
            arrowRt.sizeDelta = Vector2.zero;
            var arrow = arrowGo.AddComponent<Text>();
            arrow.text = "▼";
            arrow.fontSize = 10;
            arrow.alignment = TextAnchor.MiddleCenter;
            arrow.color = TextDimColor;
            arrow.font = Font.CreateDynamicFontFromOSFont(new[] { "Arial" }, 10);

            var dropdown = go.AddComponent<Dropdown>();
            dropdown.targetGraphic = go.GetComponent<Image>();
            dropdown.captionText = label;
            dropdown.AddOptions(options);
            return dropdown;
        }

        private void CreateDivider(Transform parent, Vector2 pos, float width)
        {
            var go = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(width, 1f);
            go.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f, 0.6f);
        }
    }
}
