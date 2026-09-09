using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CivilizationEvolution.UI
{
 /// 代码动态生成的主菜单 UI（替代场景中圆角正方形旧主菜单）。 /// 设计：深色背景 + 金色标题 + 矩形按钮（底部金线/灰线），无圆角。 /// 按钮：开始游戏（→地图编辑器）、编辑器（→数据编辑器）、设置、退出。    public class MainMenuUI : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private UIManager uiManager;

        [Header("样式")]
        [SerializeField] private Color bgColor = new Color(0.06f, 0.07f, 0.10f, 0.96f);
        [SerializeField] private Color titleColor = new Color(0.88f, 0.75f, 0.45f, 1f);
        [SerializeField] private Color subtitleColor = new Color(0.55f, 0.55f, 0.60f, 1f);
        [SerializeField] private Color buttonBg = new Color(0.12f, 0.13f, 0.17f, 0.92f);
        [SerializeField] private Color primaryLine = new Color(0.88f, 0.75f, 0.45f, 1f);
        [SerializeField] private Color secondaryLine = new Color(0.35f, 0.35f, 0.40f, 1f);
        [SerializeField] private Color exitTextColor = new Color(0.80f, 0.35f, 0.30f, 1f);

        private Canvas _canvas;
        private GameObject _root;
        private SettingsPanel _settingsPanel;
        private DataEditorMenu _dataEditorMenu;
        private SaveLoadPanel _saveLoadPanel;
        private bool _initialized;

        void Awake()
        {
            if (uiManager == null)
                uiManager = FindAnyObjectByType<UIManager>();
        }

 /// <summary>构建主菜单 UI（代码动态生成，不依赖场景预制体）</summary>        public void Build()
        {
            if (_initialized) return;

            var canvasObj = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = canvasObj.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            DontDestroyOnLoad(canvasObj);

            _root = CreatePanel("Root", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, bgColor);
            _root.transform.SetParent(canvasObj.transform, false);

            BuildTitle();
            BuildButtons();
            BuildFooter();

            _initialized = true;
            Debug.Log("[MainMenuUI] 主菜单构建完成");
        }

        private void BuildTitle()
        {
            var titleObj = CreateText("GameTitle", "纷繁的世界", 64, titleColor, FontStyles.Bold);
            var rt = titleObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(0, -120);
            rt.sizeDelta = new Vector2(800, 80);
            titleObj.transform.SetParent(_root.transform, false);

            var subObj = CreateText("GameSubtitle", "Myriad Creation: A Phantasm of Passing Aeons", 20, subtitleColor, FontStyles.Normal);
            var srt = subObj.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 1f); srt.anchorMax = new Vector2(0.5f, 1f);
            srt.pivot = new Vector2(0.5f, 1f); srt.anchoredPosition = new Vector2(0, -210);
            srt.sizeDelta = new Vector2(1000, 30);
            subObj.transform.SetParent(_root.transform, false);

            var lineObj = CreatePanel("TitleLine", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0, -255), primaryLine);
            lineObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 2);
            lineObj.transform.SetParent(_root.transform, false);
        }

        private void BuildButtons()
        {
            var containerObj = new GameObject("ButtonContainer", typeof(RectTransform));
            var crt = containerObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f); crt.anchoredPosition = new Vector2(0, -40);
            crt.sizeDelta = new Vector2(320, 320);
            containerObj.transform.SetParent(_root.transform, false);

 // 按钮：开始游戏（主）、编辑器、设置、退出            var buttons = new (string label, bool primary, Action onClick)[]
            {
                ("创建世界", true, OnStartGame),
                ("加载世界", false, OnLoadWorld),
                ("编辑器", false, OnDataEditor),
                ("设置", false, OnSettings),
                ("退出游戏", false, OnExit),
            };

            float btnHeight = 56f, spacing = 14f;
            float startY = (buttons.Length - 1) * (btnHeight + spacing) * 0.5f;
            for (int i = 0; i < buttons.Length; i++)
            {
                var (label, primary, onClick) = buttons[i];
                var btn = CreateButton(label, primary, onClick);
                var brt = btn.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.5f, 0.5f); brt.anchorMax = new Vector2(0.5f, 0.5f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.anchoredPosition = new Vector2(0, startY - i * (btnHeight + spacing));
                brt.sizeDelta = new Vector2(320, btnHeight);
                btn.transform.SetParent(containerObj.transform, false);
            }
        }

        private void BuildFooter()
        {
            var footerObj = CreateText("Footer", "v0.1.0  |  2026  |  按 Esc 返回主菜单", 14, subtitleColor, FontStyles.Normal);
            var rt = footerObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = new Vector2(0, 30);
            rt.sizeDelta = new Vector2(600, 20);
            footerObj.transform.SetParent(_root.transform, false);
        }

        private GameObject CreateButton(string label, bool primary, Action onClick)
        {
            var btnObj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.GetComponent<Image>().color = buttonBg;
            var btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var lineObj = CreatePanel("BottomLine", Vector2.zero, new Vector2(1, 0),
                new Vector2(0.5f, 0f), Vector2.zero, primary ? primaryLine : secondaryLine);
            lineObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, primary ? 3 : 1);
            lineObj.transform.SetParent(btnObj.transform, false);

            Color textColor = primary ? primaryLine : Color.white;
            if (label == "退出游戏") textColor = exitTextColor;
            var textObj = CreateText("Label", label, 22, textColor, primary ? FontStyles.Bold : FontStyles.Normal);
            var trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.pivot = new Vector2(0, 0.5f); trt.anchoredPosition = new Vector2(24, 0);
            trt.sizeDelta = new Vector2(-48, 0);
            textObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
            textObj.transform.SetParent(btnObj.transform, false);

            var colors = btn.colors;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.4f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.85f, 1f);
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            btnObj.AddComponent<UIButtonHover>();
            return btnObj;
        }

        private GameObject CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPos, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.pivot = pivot; rt.anchoredPosition = anchoredPos;
            obj.GetComponent<Image>().color = color;
            return obj;
        }

        private GameObject CreateText(string name, string text, int fontSize, Color color, FontStyles style)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
            tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
            return obj;
        }

 // ===== 按钮事件 =====
 /// <summary>创建世界 → 进入地图编辑器（全海空白地图 + 右侧编辑器UI + 右下速度控制）</summary>        private void OnStartGame()
        {
            Hide();
            var flow = SceneFlowController.Instance;
            if (flow != null) flow.EnterMapEditor();
            else Debug.LogWarning("[MainMenuUI] SceneFlowController 不存在，无法进入地图编辑器");
        }

 /// <summary>加载世界 → 打开存档选择面板，选择后进入地图编辑器并加载存档</summary>        private void OnLoadWorld()
        {
            if (_saveLoadPanel == null)
            {
                var obj = new GameObject("SaveLoadPanel", typeof(RectTransform));
                obj.transform.SetParent(_root.transform, false);
                _saveLoadPanel = obj.AddComponent<SaveLoadPanel>();
                _saveLoadPanel.Build(_root.transform, (fileName) =>
                {
 // 选择存档后，进入地图编辑器并加载                    var flow = SceneFlowController.Instance;
                    if (flow != null) flow.EnterMapEditorWithSave(fileName);
                    else Debug.LogWarning("[MainMenuUI] SceneFlowController 不存在，无法加载存档");
                }, () => { _saveLoadPanel.Hide(); });
            }
            _saveLoadPanel.RefreshList();
            _saveLoadPanel.Show();
        }

 /// <summary>编辑器 → 数据编辑器（种族/文化/宗教等内容编辑选择）</summary>        private void OnDataEditor()
        {
            if (_dataEditorMenu == null)
            {
                var obj = new GameObject("DataEditorMenu", typeof(RectTransform));
                obj.transform.SetParent(_root.transform, false);
                _dataEditorMenu = obj.AddComponent<DataEditorMenu>();
                _dataEditorMenu.Build(_root.transform, () => { _dataEditorMenu.Hide(); });
            }
            _dataEditorMenu.Show();
        }

 /// <summary>设置 → 游戏设置面板（音量/分辨率/画质/语言）</summary>        private void OnSettings()
        {
            if (_settingsPanel == null)
            {
                var obj = new GameObject("SettingsPanel", typeof(RectTransform));
                obj.transform.SetParent(_root.transform, false);
                _settingsPanel = obj.AddComponent<SettingsPanel>();
                _settingsPanel.Build(_root.transform, () => { _settingsPanel.Hide(); });
            }
            _settingsPanel.Show();
        }

        private void OnExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

 // ===== 显示/隐藏 =====        public void Show() { if (_root != null) _root.SetActive(true); if (_canvas != null) _canvas.enabled = true; }
        public void Hide() { if (_root != null) _root.SetActive(false); if (_canvas != null) _canvas.enabled = false; }
        public bool IsVisible => _root != null && _root.activeSelf;

        void OnDestroy() { if (_canvas != null) Destroy(_canvas.gameObject); }
    }
}
