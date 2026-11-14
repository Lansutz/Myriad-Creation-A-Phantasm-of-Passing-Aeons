using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MyriadCreation.Core.Contracts;
using MyriadCreation.UI.Common;

namespace MyriadCreation.UI.Panels
{
public partial class GovernmentPanel : MonoBehaviour, IUserInterfacePanel
{
        [SerializeField] private int targetRealmId = 0;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private bool autoGenerateUI = true;

        private ILawQuery _lawQuery;
        private ILawCommand _lawCommand;
        private IDecreeCommand _decreeCommand;
        private IWorldReadModel _worldReadModel;

        private Text _status;
        private static Font _uiFont;

        public ILawQuery LawQuery => _lawQuery;
        public ILawCommand LawCommand => _lawCommand;
        public IDecreeCommand DecreeCommand => _decreeCommand;

        /// <summary>注入法律查询/命令（UIManager/GameManager）；无注入时回退 world.LawQuery/LawCommand。</summary>
        public void SetLawServices(ILawQuery query, ILawCommand command)
        {
            _lawQuery = query;
            _lawCommand = command;
        }

        /// <summary>4b5b：注入聚合只读视图（realm 名等轻量数据；旧路径由 Legacy fallback）。</summary>
        public void SetWorldReadModel(IWorldReadModel view)
        {
            _worldReadModel = view;
        }

        public IWorldReadModel WorldReadModel => _worldReadModel;

        /// <summary>4b5b：注入法令下发命令（无注入时 Legacy 回退 world.DecreeCommand）。</summary>
        public void SetDecreeCommand(IDecreeCommand command)
        {
            _decreeCommand = command;
        }

        /// <summary>面板是否打开（动态 Build 的根遮罩激活；兼容场景内旧引用）。</summary>
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public string PanelId => "government";

        private static Font UiFont
        {
            get
            {
                if (_uiFont == null)
                {
                    // 动态面板用中文字体（LegacyRuntime.ttf 无中文字形，会显示方块）。
                    _uiFont = Resources.Load<Font>("simhei");
                    if (_uiFont == null)
                    {
                        _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                }
                return _uiFont;
            }
        }

        private void Awake() { if (autoGenerateUI && contentContainer) GenUI(); }
        private void Start() { Init(); }

        private void Update()
        {
            // 打开状态下 ESC 关闭（面板根激活时才拦截，避免影响其它系统）。
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }

        /// <summary>
        /// 代码动态创建面板（场景零改动）：自建遮罩根/面板/标题/关闭按钮/滚动内容区，
        /// 设置 panelRoot/contentContainer 引用并生成 UI 骨架；默认隐藏，由 Show() 打开。
        /// </summary>
        public void Build(Transform parent)
        {
            if (panelRoot != null)
            {
                return;
            }
            Transform host = parent != null ? parent : transform;

            // 半透明根遮罩（拦截点击，模态）
            var rootObj = new GameObject("GovernmentRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rootObj.transform.SetParent(host, false);
            var rootRt = rootObj.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            rootObj.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            panelRoot = rootObj;

            // 面板
            var panelObj = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObj.transform.SetParent(rootObj.transform, false);
            var panelRt = panelObj.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(680f, 720f);
            panelObj.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.98f);

            // 标题
            var title = MkText("政府", 22, FontStyle.Bold);
            title.color = new Color(0.88f, 0.75f, 0.45f, 1f);
            title.alignment = TextAnchor.MiddleCenter;
            var titleRt = (RectTransform)title.transform;
            titleRt.SetParent(panelObj.transform, false);
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -14f);
            titleRt.sizeDelta = new Vector2(-120f, 32f);

            // 关闭按钮
            var closeButton = MkBtn("关闭", 80f);
            closeButton.onClick.AddListener(Hide);
            var closeRt = (RectTransform)closeButton.transform;
            closeRt.SetParent(panelObj.transform, false);
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-14f, -14f);
            closeRt.sizeDelta = new Vector2(80f, 32f);

            // 滚动内容区
            var scrollObj = new GameObject("Scroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            scrollObj.transform.SetParent(panelObj.transform, false);
            var scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(18f, 16f);
            scrollRt.offsetMax = new Vector2(-18f, -56f);
            scrollObj.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);

            var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var viewportRt = viewportObj.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportObj.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewportObj.GetComponent<Mask>().showMaskGraphic = false;

            var contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var layout = contentObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 3f;
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentContainer = contentObj.transform;

            var scroll = scrollObj.GetComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            GenUI();
            panelRoot.SetActive(false);
        }

        private void GenUI()
        {
            foreach (Transform c in contentContainer) Destroy(c.gameObject);
            MakeHeader("政府", "已固化的制度 / 正在生效的法令 / 正在推行的法令");
            _status = MkText("", 12, FontStyle.Normal);
            _status.color = new Color(1f, .85f, .55f);
            _status.transform.SetParent(contentContainer, false);
        }

        private void MakeHeader(string title, string subtitle)
        {
            var o = new GameObject("H"); o.transform.SetParent(contentContainer, false);
            o.AddComponent<VerticalLayoutGroup>().spacing = 2;
            var t1 = MkText(title, 18, FontStyle.Bold); t1.transform.SetParent(o.transform, false);
            var t2 = MkText(subtitle, 12, FontStyle.Normal);
            t2.color = new Color(.7f, .7f, .7f);
            t2.transform.SetParent(o.transform, false);
            t2.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void AddSection(string title)
        {
            var header = MkText(title, 15, FontStyle.Bold);
            header.color = new Color(.85f, .9f, 1f);
            header.transform.SetParent(contentContainer, false);
        }

        private void AddLine(string text, Color color)
        {
            var line = MkText(text, 12, FontStyle.Normal);
            line.color = color;
            line.horizontalOverflow = HorizontalWrapMode.Wrap;
            line.transform.SetParent(contentContainer, false);
        }

        private void AddAction(string label, string actionText, Action action)
        {
            var row = new GameObject("R");
            row.transform.SetParent(contentContainer, false);
            row.AddComponent<HorizontalLayoutGroup>().spacing = 8;
            var btn = MkBtn(actionText, 180);
            btn.transform.SetParent(row.transform, false);
            btn.onClick.AddListener(() => { UiSfx.PlayClick(); action(); });
            if (!string.IsNullOrEmpty(label))
            {
                var text = MkText(label, 12, FontStyle.Normal);
                text.color = new Color(.9f, .9f, .9f);
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.transform.SetParent(row.transform, false);
            }
        }

        public void Init()
        {
            Refresh();
        }

        private int ResolveRealmId()
        {
            if (_worldReadModel == null) return -1;
            if (_worldReadModel.GetRealm(targetRealmId) != null) return targetRealmId;
            var all = _worldReadModel.GetAllRealms();
            if (all != null && all.Count > 0 && all[0] != null)
            {
                targetRealmId = all[0].realmId;
                return targetRealmId;
            }
            return -1;
        }

        private int ResolveRulerId(int realmId)
        {
            var ruler = _worldReadModel?.GetRuler(realmId);
            return ruler != null ? ruler.characterId : -1;
        }

        private void Refresh()
        {
            if (contentContainer == null) return;
            foreach (Transform c in contentContainer)
            {
                if (_status != null && c.gameObject == _status.gameObject) continue;
                Destroy(c.gameObject);
            }
            if (_worldReadModel == null || LawQuery == null || LawCommand == null)
            {
                AddSection("世界未加载");
                AddLine("世界未加载（无 UI 只读视图/法律契约）", new Color(.7f, .7f, .7f));
                return;
            }
            int realmId = ResolveRealmId();
            if (realmId < 0)
            {
                AddSection("世界未加载");
                AddLine("当前政权不存在", new Color(.7f, .7f, .7f));
                return;
            }
            int now = _worldReadModel.CurrentDay;
            LawCommand.EnsureKnownLaws(realmId);
            LawCommand.TickPendingLaws(realmId, now);
            LawCommand.TickDecreeExpiry(realmId, now);
            var view = LawQuery.GetGovernmentView(realmId, now);
            if (view == null)
            {
                AddSection("世界未加载");
                AddLine("无法读取政府视图", new Color(.7f, .7f, .7f));
                return;
            }

            AddSection("已固化的制度");
            if (view.enactedLaws.Count == 0) AddLine("尚未固化任何制度", new Color(.7f, .7f, .7f));
            foreach (var item in view.enactedLaws) AddLine(item.label, new Color(.75f, .95f, .75f));

            AddSection("正在生效的法令");
            if (view.activeDecrees.Count == 0) AddLine("没有生效中的法令", new Color(.7f, .7f, .7f));
            foreach (var item in view.activeDecrees)
                AddAction(item.label, "撤销", () => TryRevokeDecree(item.decreeId));

            AddSection("正在推行的法令");
            if (view.pendingLaws.Count == 0 && view.pendingDecrees.Count == 0)
                AddLine("没有正在推行的法令", new Color(.7f, .7f, .7f));
            foreach (var item in view.pendingLaws)
                AddAction(item.label, "取消", () => TryCancelPendingLaw(item.groupId));
            foreach (var item in view.pendingDecrees)
                AddLine(item.label, new Color(.85f, .85f, .7f));

            AddSection("可推行的制度");
            if (view.proposableLaws.Count == 0)
                AddLine("没有可推行的制度（需已知、有提案权、满足前置）", new Color(.7f, .7f, .7f));
            foreach (var item in view.proposableLaws)
            {
                var capturedLawId = item.lawId;
                var capturedLabel = item.label;
                AddAction("", "推行：" + capturedLabel, () => TryBeginEnactLaw(capturedLawId, capturedLabel));
            }

            AddSection("可颁布的法令");
            if (view.enactableDecrees.Count == 0) AddLine("没有可颁布的法令", new Color(.7f, .7f, .7f));
            foreach (var decree in view.enactableDecrees)
            {
                var capturedDecreeId = decree.decreeId;
                var capturedLabel = decree.label;
                AddAction("", "颁布：" + capturedLabel, () => TryEnactDecree(capturedDecreeId, capturedLabel));
            }
        }

        private void TryBeginEnactLaw(string lawId, string displayName)
        {
            int realmId = ResolveRealmId();
            if (realmId < 0 || string.IsNullOrEmpty(lawId) || LawCommand == null) return;
            int now = _worldReadModel.CurrentDay;
            string reason;
            bool ok = LawCommand.BeginEnactLaw(realmId, lawId, ResolveRulerId(realmId), now, out reason);
            if (ok)
            {
                int days = -1;
                foreach (var pending in LawQuery.GetPendingLaws(realmId))
                {
                    if (pending.lawId == lawId) { days = pending.remainingDays; break; }
                }
                if (_status != null) _status.text = days > 0 ? $"开始推行：{displayName}（{days} 天）" : $"开始推行：{displayName}";
            }
            else if (_status != null) _status.text = "无法推行：" + reason;
            Refresh();
        }

        private void TryCancelPendingLaw(string groupId)
        {
            int realmId = ResolveRealmId();
            if (realmId < 0 || LawCommand == null) return;
            string reason;
            bool ok = LawCommand.CancelPendingLaw(realmId, groupId, ResolveRulerId(realmId), out reason);
            if (_status != null) _status.text = ok ? "已取消推行" : "无法取消：" + reason;
            Refresh();
        }

        private void TryRevokeDecree(string decreeId)
        {
            int realmId = ResolveRealmId();
            if (realmId < 0 || LawCommand == null) return;
            string reason;
            bool ok = LawCommand.RevokeDecree(realmId, decreeId, out reason);
            if (_status != null) _status.text = ok ? $"已撤销法令：{decreeId}" : "没有生效中的该法令";
            Refresh();
        }

        private void TryEnactDecree(string decreeId, string displayName)
        {
            int realmId = ResolveRealmId();
            if (realmId < 0 || string.IsNullOrEmpty(decreeId) || LawCommand == null) return;
            string reason;
            int now = _worldReadModel.CurrentDay;
            bool ok = LawCommand.EnactDecree(realmId, decreeId, now, out reason);
            if (_status != null) _status.text = ok ? $"法令生效：{displayName}" : "无法颁布：" + reason;
            if (ok && DecreeCommand != null)
            {
                int orders = DecreeCommand.IssueDecreeOrders(decreeId, realmId);
                if (_status != null) _status.text += orders > 0 ? $"；已下发 {orders} 道命令" : "；无下属官员可传递";
            }
            Refresh();
        }

        private Text MkText(string text, int size, FontStyle style)
        {
            var go = new GameObject("Text");
            var txt = go.AddComponent<Text>();
            txt.text = text; txt.fontSize = size; txt.fontStyle = style; txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            if (!txt.font) txt.font = UiFont;
            go.AddComponent<LayoutElement>().minWidth = 100;
            return txt;
        }

        private Button MkBtn(string text, float width)
        {
            var go = new GameObject("Btn");
            var btn = go.AddComponent<Button>();
            go.AddComponent<Image>().color = new Color(.3f, .4f, .6f);
            var colors = btn.colors;
            colors.normalColor = new Color(.3f, .4f, .6f);
            colors.highlightedColor = new Color(.4f, .5f, .7f);
            colors.pressedColor = new Color(.2f, .3f, .5f);
            btn.colors = colors;
            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(go.transform, false);
            var rect = labelGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<Text>();
            label.text = text; label.fontSize = 12; label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            if (!label.font) label.font = UiFont;
            var element = go.AddComponent<LayoutElement>();
            element.minWidth = width; element.minHeight = 28;
            return btn;
        }

        public void Show()
        {
            if (panelRoot == null) return;
            if (!panelRoot.activeSelf) UiSfx.PlayOpen();
            panelRoot.SetActive(true);
            Init();
        }

        public void Hide()
        {
            if (panelRoot == null || !panelRoot.activeSelf) return;
            UiSfx.PlayClose();
            panelRoot.SetActive(false);
        }

        public void Toggle() { if (panelRoot) { if (panelRoot.activeSelf) Hide(); else Show(); } }
}
}
