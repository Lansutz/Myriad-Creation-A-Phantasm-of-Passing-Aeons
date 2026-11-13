using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.UI.Common;
using MyriadCreation.App.Settings;
using MyriadCreation.App.Contracts;
namespace MyriadCreation.UI.Panels
{
 /// 代码动态生成的主菜单 UI（替代场景中圆角正方形旧主菜单）。
 /// 设计：深色背景 + 金色标题 + 矩形按钮（底部金线/灰线），无圆角。
 /// 按钮：开始游戏（→地图编辑器）、编辑器（→数据编辑器）、设置、退出。
    public class MainMenuUI : MonoBehaviour, IMainMenuUi
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
        private SaveLoadPanel _saveLoadPanel;
        private GameObject _createWorldPanel;
        private TMPro.TMP_InputField _polityInput, _settlementInput, _unitInput;
        private bool _naturalSpawning = true;
        private TMPro.TextMeshProUGUI _naturalLabel;
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

            // Canvas 必须挂在 MainMenuUI 自己的层级下：否则按组件查找子节点找不到 UI（PlayMode 冒烟用
            // GetComponentInChildren<Canvas>/GetComponentsInChildren<Button> 直接失败），OnDestroy 时
            // 也无法随宿主一起销毁（旧实现是独立根对象 + DontDestroyOnLoad，会留下孤儿 Canvas）。
            var canvasObj = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);
            _canvas = canvasObj.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // 高于场景残留的旧 UICanvas（sortingOrder 0），保证主菜单渲染在最上层且优先被射线命中，
            // 不会被 HEAD 恢复版旧 UI 遮挡点击。
            _canvas.sortingOrder = 1000;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            // 应用已保存的 UI 缩放（Bootstrap 启动时主菜单 Canvas 尚未创建，ApplySaved 只作用于已存在 Canvas）。
            SettingsService.ApplyUiScale(SettingsService.Current.UiScale);
            // 应用已保存的界面字体选择（默认 simhei；1/2 为思源黑/宋）。
            TMPFontUtility.ApplyFontChoice(SettingsService.Current.FontChoice);
            // 预热：主菜单首次显示前确保当前 choice 的 TMP SDF 已生成；之后命中缓存，不重复创建。
            TMPFontUtility.GetChineseFont();
            // 不单独 DontDestroyOnLoad：MainMenuUI 挂在 DDOL 的 SceneFlowController 下，Canvas 作为
            // 其子级一起跨场景保留；对非根对象调用 DontDestroyOnLoad 只会产生警告。

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
            crt.sizeDelta = new Vector2(320, 380);
            containerObj.transform.SetParent(_root.transform, false);

 // 按钮：开始游戏（主，一键默认世界）、创建世界（自定义参数）、加载世界、设置、退出
            var buttons = new (string label, bool primary, Action onClick)[]
            {
                ("开始游戏", true, OnQuickStart),
                ("创建世界", false, OnStartGame),
                ("加载世界", false, OnLoadWorld),
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
            btn.onClick.AddListener(() => { UiSfx.PlayClick(); onClick?.Invoke(); });

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
            // 本工程 TMP 默认字体资产缺失（日志会报 The LiberationSans SDF Font Asset was not found），
            // 动态文本若不显式赋字体就是不可见的空文字（按钮只剩底框，像 UI 坏了）。用统一直连中文字体。
            TMP_FontAsset font = TMPFontUtility.GetChineseFont();
            if (font != null) tmp.font = font;
            tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
            tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
            return obj;
        }

 // ===== 按钮事件 =====
 /// <summary>
 /// 开始游戏：一键异步生成默认世界（loading 面板 → 完成后进入游戏/编辑器）。
 /// 先生成校验 Bootstrap 再隐藏菜单：失败时菜单保留，避免"点了没反应且菜单消失"。
 /// </summary>
        private void OnQuickStart()
        {
            var flow = AppServices.AppFlow;
            if (flow == null)
            {
                Debug.LogError("[MainMenuUI] AppFlow 未注册，无法开始游戏");
                return;
            }
            Hide();
            // 走应用流程契约：内部负责切状态与 loading 异步生成，UI 不再引用 App 具体类型。
            flow.StartNewGame(new NewGameRequestDto());
        }

 /// <summary>创建世界 → 打开自定义世界参数面板（政权/聚落/单位数量）。</summary>
        private void OnStartGame()
        {
            ShowCreateWorldPanel();
        }

        /// <summary>创建世界：政权/聚落/单位数量全部可填，默认 0；生成后进入世界。</summary>
        private void ShowCreateWorldPanel()
        {
            if (_createWorldPanel != null) { _createWorldPanel.SetActive(true); return; }

            _createWorldPanel = CreatePanel("CreateWorldPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Color(0.06f, 0.07f, 0.10f, 0.98f));
            var rt = _createWorldPanel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(560, 420);
            _createWorldPanel.transform.SetParent(_root.transform, false);

            var title = CreateText("Title", "创建世界", 30, titleColor, FontStyles.Bold);
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 165);
            title.transform.SetParent(_createWorldPanel.transform, false);

            _polityInput = CreateCountInput("政权数量", 90, "0");
            _settlementInput = CreateCountInput("聚落数量（-1=按省自动）", 35, "0");
            _unitInput = CreateCountInput("单位数量", -20, "0");

            var naturalBtn = CreateButton("自然生成：开", false, ToggleNatural);
            var nrt = naturalBtn.GetComponent<RectTransform>();
            nrt.anchoredPosition = new Vector2(0, -85);
            nrt.sizeDelta = new Vector2(320, 42);
            naturalBtn.transform.SetParent(_createWorldPanel.transform, false);
            _naturalLabel = naturalBtn.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            var generate = CreateButton("生成世界", true, GenerateWorldFromPanel);
            var grt = generate.GetComponent<RectTransform>();
            grt.anchoredPosition = new Vector2(-90, -160);
            grt.sizeDelta = new Vector2(240, 52);
            generate.transform.SetParent(_createWorldPanel.transform, false);

            var back = CreateButton("返回", false, () => _createWorldPanel.SetActive(false));
            var brt = back.GetComponent<RectTransform>();
            brt.anchoredPosition = new Vector2(100, -160);
            brt.sizeDelta = new Vector2(160, 52);
            back.transform.SetParent(_createWorldPanel.transform, false);
        }

        private TMPro.TMP_InputField CreateCountInput(string label, float y, string value)
        {
            var row = new GameObject("Row", typeof(RectTransform));
            var rrt = row.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.5f, 0.5f); rrt.anchorMax = new Vector2(0.5f, 0.5f);
            rrt.anchoredPosition = new Vector2(0, y); rrt.sizeDelta = new Vector2(460, 40);
            row.transform.SetParent(_createWorldPanel.transform, false);

            var text = CreateText("Label", label, 18, Color.white, FontStyles.Normal);
            var trt = text.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0); trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = new Vector2(0, 0); trt.offsetMax = new Vector2(0, 0);
            text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
            text.transform.SetParent(row.transform, false);

            var fieldGo = new GameObject("Input", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMPro.TMP_InputField));
            var frt = fieldGo.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(1, 0.5f); frt.anchorMax = new Vector2(1, 0.5f);
            frt.pivot = new Vector2(1, 0.5f); frt.anchoredPosition = Vector2.zero; frt.sizeDelta = new Vector2(100, 36);
            fieldGo.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.17f, 1f);
            var input = fieldGo.GetComponent<TMPro.TMP_InputField>();
            var textGo = CreateText("Text", value, 18, Color.white, FontStyles.Normal);
            textGo.transform.SetParent(fieldGo.transform, false);
            var txt = textGo.GetComponent<TextMeshProUGUI>();
            txt.alignment = TextAlignmentOptions.Center;
            input.textComponent = txt;
            input.text = value;
            input.contentType = TMPro.TMP_InputField.ContentType.IntegerNumber;
            fieldGo.transform.SetParent(row.transform, false);
            return input;
        }

        private void ToggleNatural()
        {
            _naturalSpawning = !_naturalSpawning;
            if (_naturalLabel != null) _naturalLabel.text = "自然生成：" + (_naturalSpawning ? "开" : "关");
        }

        private void GenerateWorldFromPanel()
        {
            int polity = ParseCount(_polityInput, 0, 12);
            int settlement = ParseCount(_settlementInput, -1, 1000);
            int unit = ParseCount(_unitInput, 0, 1000);
            var request = new NewGameRequestDto
            {
                contentMode = NewGameRequestDto.ContentModeCustom,
                generatePolities = polity > 0,
                generateSettlements = settlement != 0,
                generateInitialUnits = unit > 0,
                initialPolityCount = polity,
                initialSettlementCount = settlement,
                initialUnitCount = unit,
                enableNaturalSpawning = _naturalSpawning,
            };
            var flow = AppServices.AppFlow;
            if (flow == null)
            {
                Debug.LogError("[MainMenuUI] AppFlow 未注册，无法生成世界");
                return;
            }
            Hide();
            // 异步 + loading 由 AppFlow 实现负责，避免 UI 依赖 App 具体类型。
            flow.StartNewGame(request);
        }

        private static int ParseCount(TMPro.TMP_InputField input, int min, int max)
        {
            if (input == null || !int.TryParse(input.text, out int value)) value = 0;
            return Mathf.Clamp(value, min, max);
        }

 /// <summary>加载世界 → 打开存档选择面板，选择后进入地图编辑器并加载存档</summary>
        private void OnLoadWorld()
        {
            if (_saveLoadPanel == null)
            {
                var obj = new GameObject("SaveLoadPanel", typeof(RectTransform));
                obj.transform.SetParent(_root.transform, false);
                _saveLoadPanel = obj.AddComponent<SaveLoadPanel>();
                _saveLoadPanel.Build(_root.transform, (fileName) => StartCoroutine(LoadSelectedSave(fileName)),
                    () => { _saveLoadPanel.Hide(); });
            }
            _saveLoadPanel.RefreshList();
            _saveLoadPanel.Show();
        }

        /// <summary>
        /// 读档：先显示 loading 并等一帧（读档是同步重操作），再真正调用 SaveSystem 读档。
        /// 旧实现只调用 SceneFlowController.EnterMapEditorWithSave（内部只切状态/打日志、从不读档），
        /// 玩家点存档后世界没有变化 = "点了没反应"。
        /// </summary>
        private IEnumerator LoadSelectedSave(string saveName)
        {
            var ui = FindAnyObjectByType<UIManager>();
            if (ui != null) ui.ShowLoading("读取存档…");
            yield return null; // 让 loading 面板先渲染出来，避免同步读档期间黑屏假死

            var session = AppServices.GameSession;
            if (session == null)
            {
                Debug.LogError("[MainMenuUI] GameSession 未注册，无法加载存档");
                if (ui != null) ui.HideLoading();
                yield break;
            }

            session.LoadGame(saveName);

            bool loaded = session.IsPlaying && session.CurrentState != GameState.MainMenu;
            if (loaded)
            {
                AppServices.AppFlow?.EnterMapEditorWithSave(saveName);
                Hide();
            }
            else
            {
                Debug.LogError($"[MainMenuUI] 读档失败: {saveName}");
            }
            if (ui != null) ui.HideLoading();
        }

 /// <summary>设置 → 游戏设置面板（音量/分辨率/画质/语言）</summary>
        private void OnSettings()
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

 // ===== 显示/隐藏 =====
        public void Show() { if (_root != null) _root.SetActive(true); if (_canvas != null) _canvas.enabled = true; }
        public void Hide() { if (_root != null) _root.SetActive(false); if (_canvas != null) _canvas.enabled = false; }
        public bool IsVisible => _root != null && _root.activeSelf;

        void OnDestroy() { if (_canvas != null) Destroy(_canvas.gameObject); }
    }
}
