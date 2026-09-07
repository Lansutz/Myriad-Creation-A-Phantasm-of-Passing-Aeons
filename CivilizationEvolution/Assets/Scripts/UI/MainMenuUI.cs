using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CivilizationEvolution.UI
{
    /// <summary>
    /// 代码动态生成的主菜单 UI（替代场景中圆角正方形旧主菜单）。
    /// 设计：深色背景 + 金色标题 + 矩形按钮（底部金线/灰线），无圆角。
    /// 按钮事件对接 UIManager 现有链路（开始游戏→生成面板、编辑器、设置、退出）。
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private UIManager uiManager;

        [Header("样式")]
        [SerializeField] private Color bgColor = new Color(0.06f, 0.07f, 0.10f, 0.96f);
        [SerializeField] private Color titleColor = new Color(0.88f, 0.75f, 0.45f, 1f);
        [SerializeField] private Color subtitleColor = new Color(0.55f, 0.55f, 0.60f, 1f);
        [SerializeField] private Color buttonBg = new Color(0.12f, 0.13f, 0.17f, 0.92f);
        [SerializeField] private Color buttonHover = new Color(0.20f, 0.22f, 0.30f, 0.96f);
        [SerializeField] private Color primaryLine = new Color(0.88f, 0.75f, 0.45f, 1f);
        [SerializeField] private Color secondaryLine = new Color(0.35f, 0.35f, 0.40f, 1f);
        [SerializeField] private Color exitTextColor = new Color(0.80f, 0.35f, 0.30f, 1f);

        private Canvas _canvas;
        private GameObject _root;
        private bool _initialized;

        void Awake()
        {
            if (uiManager == null)
                uiManager = FindAnyObjectByType<UIManager>();
        }

        /// <summary>构建主菜单 UI（代码动态生成，不依赖场景预制体）</summary>
        public void Build()
        {
            if (_initialized) return;

            // Canvas
            var canvasObj = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = canvasObj.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            DontDestroyOnLoad(canvasObj);

            // 根面板（全屏背景）
            _root = CreatePanel("Root", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, bgColor);
            _root.transform.SetParent(canvasObj.transform, false);

            // 标题区
            BuildTitle();

            // 按钮区
            BuildButtons();

            // 底部信息
            BuildFooter();

            _initialized = true;
            Debug.Log("[MainMenuUI] 主菜单构建完成");
        }

        private void BuildTitle()
        {
            // 游戏中文名
            var titleObj = CreateText("GameTitle", "纷繁的世界", 64, titleColor, FontStyles.Bold);
            var titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f);
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0, -120);
            titleRT.sizeDelta = new Vector2(800, 80);
            titleObj.transform.SetParent(_root.transform, false);

            // 英文副标题
            var subObj = CreateText("GameSubtitle", "Myriad Creation: A Phantasm of Passing Aeons", 20, subtitleColor, FontStyles.Normal);
            var subRT = subObj.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0.5f, 1f);
            subRT.anchorMax = new Vector2(0.5f, 1f);
            subRT.pivot = new Vector2(0.5f, 1f);
            subRT.anchoredPosition = new Vector2(0, -210);
            subRT.sizeDelta = new Vector2(1000, 30);
            subObj.transform.SetParent(_root.transform, false);

            // 分隔线
            var lineObj = CreatePanel("TitleLine", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0, -255), primaryLine);
            var lineRT = lineObj.GetComponent<RectTransform>();
            lineRT.sizeDelta = new Vector2(400, 2);
            lineObj.transform.SetParent(_root.transform, false);
        }

        private void BuildButtons()
        {
            // 按钮容器（垂直布局）
            var containerObj = new GameObject("ButtonContainer", typeof(RectTransform));
            var containerRT = containerObj.GetComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0.5f, 0.5f);
            containerRT.anchorMax = new Vector2(0.5f, 0.5f);
            containerRT.pivot = new Vector2(0.5f, 0.5f);
            containerRT.anchoredPosition = new Vector2(0, -40);
            containerRT.sizeDelta = new Vector2(320, 400);
            containerObj.transform.SetParent(_root.transform, false);

            // 按钮定义：(文本, 是否主按钮, 点击事件)
            var buttons = new (string label, bool primary, Action onClick)[]
            {
                ("开始新游戏", true, OnStartNewGame),
                ("加载游戏", false, OnLoadGame),
                ("地图编辑器", false, OnMapEditor),
                ("设置", false, OnSettings),
                ("退出游戏", false, OnExit),
            };

            float btnHeight = 52f;
            float spacing = 12f;
            float startY = (buttons.Length - 1) * (btnHeight + spacing) * 0.5f;

            for (int i = 0; i < buttons.Length; i++)
            {
                var (label, primary, onClick) = buttons[i];
                var btn = CreateButton(label, primary, onClick);
                var btnRT = btn.GetComponent<RectTransform>();
                btnRT.anchorMin = new Vector2(0.5f, 0.5f);
                btnRT.anchorMax = new Vector2(0.5f, 0.5f);
                btnRT.pivot = new Vector2(0.5f, 0.5f);
                btnRT.anchoredPosition = new Vector2(0, startY - i * (btnHeight + spacing));
                btnRT.sizeDelta = new Vector2(320, btnHeight);
                btn.transform.SetParent(containerObj.transform, false);
            }
        }

        private void BuildFooter()
        {
            var footerObj = CreateText("Footer", "v0.1.0  |  2026  |  按 Esc 返回主菜单", 14, subtitleColor, FontStyles.Normal);
            var footerRT = footerObj.GetComponent<RectTransform>();
            footerRT.anchorMin = new Vector2(0.5f, 0f);
            footerRT.anchorMax = new Vector2(0.5f, 0f);
            footerRT.pivot = new Vector2(0.5f, 0f);
            footerRT.anchoredPosition = new Vector2(0, 30);
            footerRT.sizeDelta = new Vector2(600, 20);
            footerObj.transform.SetParent(_root.transform, false);
        }

        // ===== 按钮创建 =====

        private GameObject CreateButton(string label, bool primary, Action onClick)
        {
            var btnObj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var img = btnObj.GetComponent<Image>();
            img.color = buttonBg;

            var btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            // 底部装饰线
            var lineObj = CreatePanel("BottomLine", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0.5f, 0f), Vector2.zero, primary ? primaryLine : secondaryLine);
            var lineRT = lineObj.GetComponent<RectTransform>();
            lineRT.sizeDelta = new Vector2(0, primary ? 3 : 1);
            lineObj.transform.SetParent(btnObj.transform, false);

            // 文字（左对齐）
            Color textColor = primary ? primaryLine : Color.white;
            if (label == "退出游戏") textColor = exitTextColor;
            var textObj = CreateText("Label", label, 22, textColor, primary ? FontStyles.Bold : FontStyles.Normal);
            var textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 0);
            textRT.anchorMax = new Vector2(1, 1);
            textRT.pivot = new Vector2(0, 0.5f);
            textRT.anchoredPosition = new Vector2(24, 0);
            textRT.sizeDelta = new Vector2(-48, 0);
            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            textObj.transform.SetParent(btnObj.transform, false);

            // 悬停效果（通过 Button 的 colors 实现简单变色）
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.4f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.85f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            return btnObj;
        }

        // ===== 工具方法 =====

        private GameObject CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPos, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            obj.GetComponent<Image>().color = color;
            return obj;
        }

        private GameObject CreateText(string name, string text, int fontSize, Color color, FontStyles style)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return obj;
        }

        // ===== 按钮事件（对接 UIManager 现有链路）=====

        private void OnStartNewGame()
        {
            Hide();
            if (uiManager != null)
            {
                // 调用 UIManager 的世界生成面板（通过反射或公开方法）
                uiManager.OpenNewGamePanelFromMainMenu();
            }
        }

        private void OnLoadGame()
        {
            // TODO: 加载游戏面板
            Debug.Log("[MainMenuUI] 加载游戏（待实现）");
        }

        private void OnMapEditor()
        {
            Hide();
            if (uiManager != null)
                uiManager.EnterEditorFromMainMenu();
        }

        private void OnSettings()
        {
            if (uiManager != null)
                uiManager.OpenSettingsFromMainMenu();
        }

        private void OnExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ===== 显示/隐藏 =====

        public void Show()
        {
            if (_root != null) _root.SetActive(true);
            if (_canvas != null) _canvas.enabled = true;
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
            if (_canvas != null) _canvas.enabled = false;
        }

        public bool IsVisible => _root != null && _root.activeSelf;

        void OnDestroy()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
        }
    }
}
