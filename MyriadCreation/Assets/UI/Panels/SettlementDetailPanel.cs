using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.UI.Common;
using MyriadCreation.UI.Presentation;
using MyriadCreation.App.Settings;
using MyriadCreation.App.Contracts;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.UI.Panels
{
public partial class SettlementDetailPanel : MonoBehaviour, ISettlementDetailUi
{
        private const float PanelWidth = 640f;
        private const float PanelHeight = 700f;
        private GameObject _canvasObj;
        private GameObject _root;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subtitleText;
        private TextMeshProUGUI _bodyText;
        private bool _built;
        private int _currentAnchorId = -1;

        // 4d：契约入口（不再保留 GameWorld fallback）
        private ISettlementQuery _settlementQuery;

        public void SetSettlementQuery(ISettlementQuery query)
        {
            _settlementQuery = query;
        }

        /// <summary>ISettlementDetailUi 新签名：绑定聚合只读视图。</summary>
        public void BindWorld(IWorldReadModel view)
        {
            _settlementQuery = view;
            if (IsOpen) Show(_currentAnchorId);
        }

        public ISettlementQuery SettlementQuery => _settlementQuery;

        /// <summary>面板是否打开（根遮罩激活）。</summary>
        public bool IsOpen => _root != null && _root.activeSelf;

        /// <summary>当前显示的聚落锚点 id（无效 id 时也会记录，用于排错；-1 表示尚未 Show）。</summary>
        public int CurrentAnchorId => _currentAnchorId;

        /// <summary>当前标题文本（测试/调试用）。</summary>
        public string TitleText => _titleText != null ? _titleText.text : string.Empty;

        /// <summary>当前正文文本（测试/调试用）。</summary>
        public string BodyText => _bodyText != null ? _bodyText.text : string.Empty;

        /// <summary>一次性构建 Canvas/根面板（默认隐藏）。parent 为空时挂在本组件所在对象下。</summary>
        public void Build(Transform parent)
        {
            if (_built)
            {
                return;
            }
            Transform host = parent != null ? parent : transform;

            // 自带 Canvas：不依赖宿主已有 Canvas，便于图标层/GameManager 任意入口创建。
            _canvasObj = new GameObject("SettlementDetailCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvasObj.transform.SetParent(host, false);
            var canvas = _canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // 层级约定：游戏 HUD 0-399；政府/法律面板 500；聚落详情 600（模态栈上应覆盖政府面板）；
            // 主菜单/设置等独立全屏 Canvas 1000+。新增模态面板请按此栈分配 sortingOrder。
            canvas.sortingOrder = 600;
            var scaler = _canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(SettingsService.BaseReferenceWidth, SettingsService.BaseReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
            // 动态 Canvas 在 Bootstrap 应用设置之后才创建：由绑定器补应用已保存的 UI 缩放。
            UiScaleBinder.Attach(_canvasObj);

            // 半透明遮罩（根）
            var overlayObj = new GameObject("SettlementDetailOverlay",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayObj.transform.SetParent(_canvasObj.transform, false);
            var ort = overlayObj.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero;
            ort.anchorMax = Vector2.one;
            ort.offsetMin = Vector2.zero;
            ort.offsetMax = Vector2.zero;
            overlayObj.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            _root = overlayObj;

            // 主面板
            var panelObj = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObj.transform.SetParent(overlayObj.transform, false);
            var prt = panelObj.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            panelObj.GetComponent<Image>().color = new Color(0.09f, 0.10f, 0.13f, 0.98f);
            Transform panel = panelObj.transform;

            // 标题（聚落名）
            _titleText = CreateText(panel, "Title", "聚落详情", 30,
                new Color(0.88f, 0.75f, 0.45f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
            var titleRt = _titleText.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0, -24);
            titleRt.sizeDelta = new Vector2(-48, 42);

            // 副标题（类型/标签）
            _subtitleText = CreateText(panel, "Subtitle", "", 18,
                new Color(0.70f, 0.74f, 0.80f, 1f), FontStyles.Normal, TextAlignmentOptions.Center);
            var subtitleRt = _subtitleText.GetComponent<RectTransform>();
            subtitleRt.anchorMin = new Vector2(0f, 1f);
            subtitleRt.anchorMax = new Vector2(1f, 1f);
            subtitleRt.pivot = new Vector2(0.5f, 1f);
            subtitleRt.anchoredPosition = new Vector2(0, -72);
            subtitleRt.sizeDelta = new Vector2(-48, 28);

            // 分隔线
            var lineObj = new GameObject("TitleLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lineObj.transform.SetParent(panel, false);
            var lrt = lineObj.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 1f);
            lrt.anchorMax = new Vector2(0.5f, 1f);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.anchoredPosition = new Vector2(0, -108);
            lrt.sizeDelta = new Vector2(PanelWidth - 64, 1);
            lineObj.GetComponent<Image>().color = new Color(0.30f, 0.31f, 0.36f, 1f);
            lineObj.GetComponent<Image>().raycastTarget = false;

            // 正文（多行）
            _bodyText = CreateText(panel, "Body", "", 17, Color.white, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            var bodyRt = _bodyText.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.pivot = new Vector2(0.5f, 1f);
            bodyRt.offsetMin = new Vector2(36f, 92f);
            bodyRt.offsetMax = new Vector2(-36f, -122f);

            // 城市俯视图入口（P4.2）：独立面板，由 UIManager 注册与开关。
            CreateButton(panel, "城市视图", new Vector2(-200f, 20f), new Vector2(180, 44), OpenCityView);

            // 关闭按钮
            Button closeButton = CreateButton(panel, "关闭", new Vector2(0f, 20f), new Vector2(170, 44), Hide);
            closeButton.transform.SetParent(panel, false);

            // ESC 提示
            TextMeshProUGUI escHint = CreateText(panel, "EscHint", "ESC 关闭", 13,
                new Color(0.55f, 0.57f, 0.62f, 1f), FontStyles.Normal, TextAlignmentOptions.MidlineRight);
            var hintRt = escHint.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(1f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.pivot = new Vector2(1f, 0f);
            hintRt.anchoredPosition = new Vector2(-24f, 28f);
            hintRt.sizeDelta = new Vector2(140f, 22f);

            _root.SetActive(false);
            // 4b5b：若 UIManager 已注入聚合视图，新面板自取契约入口；否则走 Legacy BindWorld/GameWorld fallback。
            if (_settlementQuery == null)
            {
                var ui = FindAnyObjectByType<MyriadCreation.UI.Common.UIManager>(FindObjectsInactive.Include);
                if (ui != null) _settlementQuery = ui.WorldReadModel;
            }
            _built = true;
        }

        /// <summary>打开并显示指定锚点聚落的详情；聚落不存在时显示"无数据"（不抛异常）。</summary>
        public void Show(int settlementAnchorId)
        {
            if (!_built)
            {
                Build(null);
            }
            _currentAnchorId = settlementAnchorId;
            if (_settlementQuery != null)
            {
                RefreshContentFromQuery(settlementAnchorId);
            }
            else
            {
                SetTexts("聚落详情", "无数据", "聚落查询未注入");
            }
            if (_root != null)
            {
                _root.SetActive(true);
            }
        }

        /// <summary>4d：ISettlementQuery 契约渲染（DTO 最小字段）。</summary>
        private void RefreshContentFromQuery(int anchorId)
        {
            if (_titleText == null || _subtitleText == null || _bodyText == null)
            {
                return;
            }
            SettlementSnapshotDto settlement = _settlementQuery.GetSettlement(anchorId);
            if (settlement == null)
            {
                SetTexts("聚落详情", "无数据", $"未找到聚落（anchorId = {anchorId}）");
                return;
            }
            string name = string.IsNullOrEmpty(settlement.settlementName) ? $"聚落 #{anchorId}" : settlement.settlementName;
            string subtitle = $"等级 {settlement.constructionTier} · 人口 {settlement.population:N0} · 财富 {settlement.wealth:F0}";
            var sb = new StringBuilder();
            sb.Append("所属政权：").Append(settlement.realmId >= 0 ? settlement.realmId.ToString() : "无").Append('\n');
            sb.Append("守军：").Append(settlement.garrison.ToString()).Append("（素质 ").Append(settlement.garrisonQuality.ToString("F2")).Append("）\n");
            sb.Append("首都：").Append(settlement.isCapital ? "是" : "否").Append(" | 港口：").Append(settlement.isPort ? "是" : "否").Append('\n');
            sb.Append("形态：").Append(settlement.settlementForm).Append(" | 等级：").Append(settlement.settlementLevel);
            SetTexts(name, subtitle, sb.ToString());
        }

        public void Hide()
        {
            if (_root != null && _root.activeSelf)
            {
                UiSfx.PlayClose();
                _root.SetActive(false);
            }
        }

        private void Update()
        {
            // ESC 关闭（仅面板打开时；不拦截其它系统已处理的 ESC）
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }

        private void SetTexts(string title, string subtitle, string body)
        {
            if (_titleText != null)
            {
                _titleText.text = title;
            }
            if (_subtitleText != null)
            {
                _subtitleText.text = subtitle;
            }
            if (_bodyText != null)
            {
                _bodyText.text = body;
            }
        }

        private static string YesNo(bool value)
        {
            return value ? "是" : "否";
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, Color color,
            FontStyles style, TextAlignmentOptions align)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var tmp = obj.GetComponent<TextMeshProUGUI>();
            // 运行时动态文本必须显式赋中文字体（TMP 默认字体资产缺失时否则不可见）。
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

        private void OpenCityView()
        {
            var ui = FindAnyObjectByType<MyriadCreation.UI.Common.UIManager>(FindObjectsInactive.Include);
            ui?.OpenCityView(_currentAnchorId);
        }

        private static Button CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size, System.Action onClick)
        {
            var btnObj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            btnObj.GetComponent<Image>().color = new Color(0.30f, 0.25f, 0.12f, 1f);
            var button = btnObj.GetComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            TextMeshProUGUI labelText = CreateText(btnObj.transform, "Label", label, 18,
                new Color(0.88f, 0.75f, 0.45f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
            var labelRt = labelText.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            btnObj.transform.SetParent(parent, false);
            return button;
        }
}
}
