using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.UI
{
    public class GovernmentPanel : MonoBehaviour
    {
        [SerializeField] private GameWorld world;
        [SerializeField] private int targetRealmId = 0;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private bool autoGenerateUI = true;

        private GovernmentComposition _current;
        private Dictionary<GovernmentConstraints.GovernmentDimension, Dropdown> _dd = new Dictionary<GovernmentConstraints.GovernmentDimension, Dropdown>();
        private Dictionary<GovernmentConstraints.GovernmentDimension, GameObject> _sub = new Dictionary<GovernmentConstraints.GovernmentDimension, GameObject>();
        private Text _catText, _sumText, _secCountText;
        private GameObject _secContainer;
        private Dropdown _tplDd;

        private static readonly GovernmentConstraints.GovernmentDimension[] Order = {
            GovernmentConstraints.GovernmentDimension.SupremeSovereignty,
            GovernmentConstraints.GovernmentDimension.SupremeSuccession,
            GovernmentConstraints.GovernmentDimension.SupremeScope,
            GovernmentConstraints.GovernmentDimension.CentralExistence,
            GovernmentConstraints.GovernmentDimension.CentralSuccession,
            GovernmentConstraints.GovernmentDimension.CentralInstitution,
            GovernmentConstraints.GovernmentDimension.LocalSuccession,
            GovernmentConstraints.GovernmentDimension.LocalScope,
            GovernmentConstraints.GovernmentDimension.SpatialStructure
        };

        private void Awake() { if (autoGenerateUI && contentContainer) GenUI(); }
        private void Start() { Init(); }

        public void Init()
        {
            if (!world) world = FindAnyObjectByType<GameWorld>();
            _current = (world?.realms?.Count > targetRealmId) ? (world.realms[targetRealmId].composition ?? new GovernmentComposition()) : new GovernmentComposition();
            Refresh();
        }

        private void GenUI()
        {
            foreach (Transform c in contentContainer) Destroy(c.gameObject);
            _dd.Clear(); _sub.Clear();
            MakeHeader("政体系统", "9维政体组合编辑器");
            MakeCategory();
            MakeTemplate();
            foreach (var d in Order) MakeDim(d);
            MakeSecondary();
            MakeSummary();
            MakeButtons();
        }

        private void MakeHeader(string t, string s)
        {
            var o = new GameObject("H"); o.transform.SetParent(contentContainer, false);
            o.AddComponent<VerticalLayoutGroup>().spacing = 2;
            var t1 = MkText(t, 18, FontStyle.Bold); t1.transform.SetParent(o.transform, false);
            var t2 = MkText(s, 12, FontStyle.Normal); t2.color = new Color(.7f,.7f,.7f); t2.transform.SetParent(o.transform, false);
        }

        private void MakeCategory()
        {
            var o = new GameObject("Cat"); o.transform.SetParent(contentContainer, false);
            o.AddComponent<HorizontalLayoutGroup>().spacing = 10;
            MkText("政体大类:", 14, FontStyle.Bold).transform.SetParent(o.transform, false);
            _catText = MkText("君主制", 14, FontStyle.Normal); _catText.color = new Color(1f,.8f,.4f); _catText.transform.SetParent(o.transform, false);
        }

        private void MakeTemplate()
        {
            var o = new GameObject("Tpl"); o.transform.SetParent(contentContainer, false);
            o.AddComponent<HorizontalLayoutGroup>().spacing = 10;
            MkText("快速模板:", 12, FontStyle.Normal).transform.SetParent(o.transform, false);
            _tplDd = MkDd(); _tplDd.transform.SetParent(o.transform, false);
            var opts = new List<string> { "选择模板..." };
            foreach (var t in GovernmentConstraints.GetTemplates()) opts.Add(t.name);
            _tplDd.AddOptions(opts);
            var b = MkBtn("应用", 60); b.transform.SetParent(o.transform, false);
            b.onClick.AddListener(ApplyTpl);
        }

        private void MakeDim(GovernmentConstraints.GovernmentDimension d)
        {
            var o = new GameObject($"D_{d}"); o.transform.SetParent(contentContainer, false);
            o.AddComponent<VerticalLayoutGroup>().spacing = 4;
            var m = new GameObject("M"); m.transform.SetParent(o.transform, false);
            m.AddComponent<HorizontalLayoutGroup>().spacing = 10;
            MkText(GovernmentConstraints.GetDimensionName(d)+":", 12, FontStyle.Bold).transform.SetParent(m.transform, false);
            var dd = MkDd(); dd.transform.SetParent(m.transform, false); _dd[d] = dd;
            var cap = d;
            dd.onValueChanged.AddListener(i => OnDim(cap, i));
            var s = new GameObject("S"); s.transform.SetParent(o.transform, false);
            var vl = s.AddComponent<VerticalLayoutGroup>(); vl.spacing = 2; vl.padding = new RectOffset(20,0,0,0);
            _sub[d] = s;
        }

        private void MakeSecondary()
        {
            _secContainer = new GameObject("Sec"); _secContainer.transform.SetParent(contentContainer, false);
            _secContainer.AddComponent<VerticalLayoutGroup>().spacing = 4;
            var h = new GameObject("H"); h.transform.SetParent(_secContainer.transform, false);
            h.AddComponent<HorizontalLayoutGroup>().spacing = 10;
            MkText("次要成分:", 12, FontStyle.Bold).transform.SetParent(h.transform, false);
            _secCountText = MkText("0/2", 12, FontStyle.Normal); _secCountText.color = new Color(.7f,.7f,.7f); _secCountText.transform.SetParent(h.transform, false);
            var add = MkBtn("+ 添加", 80); add.transform.SetParent(h.transform, false);
            add.onClick.AddListener(AddSec);
            var l = new GameObject("List"); l.transform.SetParent(_secContainer.transform, false);
            var vl = l.AddComponent<VerticalLayoutGroup>(); vl.spacing = 2; vl.padding = new RectOffset(20,0,0,0);
        }

        private void MakeSummary()
        {
            var o = new GameObject("Sum"); o.transform.SetParent(contentContainer, false);
            o.AddComponent<VerticalLayoutGroup>().spacing = 4;
            MkText("摘要:", 12, FontStyle.Bold).transform.SetParent(o.transform, false);
            _sumText = MkText("", 11, FontStyle.Normal); _sumText.color = new Color(.8f,.8f,.8f); _sumText.transform.SetParent(o.transform, false);
        }

        private void MakeButtons()
        {
            var o = new GameObject("Btns"); o.transform.SetParent(contentContainer, false);
            o.AddComponent<HorizontalLayoutGroup>().spacing = 10;
            var a = MkBtn("应用政体", 120); a.transform.SetParent(o.transform, false); a.onClick.AddListener(Apply);
            var r = MkBtn("重置", 80); r.transform.SetParent(o.transform, false); r.onClick.AddListener(Reset);
        }

        private void OnDim(GovernmentConstraints.GovernmentDimension d, int idx)
        {
            if (_current == null) return;
            var opts = GovernmentConstraints.GetAvailableOptions(d, _current);
            if (idx <= 0 || idx > opts.Count) return;
            int v = opts[idx - 1];
            switch (d)
            {
                case GovernmentConstraints.GovernmentDimension.SupremeSovereignty: _current.supremeSovereignty = (GovernmentConstraints.SupremeSovereignty)v; break;
                case GovernmentConstraints.GovernmentDimension.SupremeSuccession: _current.supremeSuccession.primary = v; break;
                case GovernmentConstraints.GovernmentDimension.SupremeScope: _current.supremeScope.primary = v; break;
                case GovernmentConstraints.GovernmentDimension.CentralExistence: _current.centralExistence = (GovernmentConstraints.CentralExistence)v; break;
                case GovernmentConstraints.GovernmentDimension.CentralSuccession: _current.centralSuccession.primary = v; break;
                case GovernmentConstraints.GovernmentDimension.CentralInstitution: _current.centralInstitution.primary = v; break;
                case GovernmentConstraints.GovernmentDimension.LocalSuccession: _current.localSuccession.primary = v; break;
                case GovernmentConstraints.GovernmentDimension.LocalScope: _current.localScope.primary = v; break;
                case GovernmentConstraints.GovernmentDimension.SpatialStructure: _current.spatialStructure.primary = v; break;
            }
            if (d == GovernmentConstraints.GovernmentDimension.SupremeSovereignty)
            {
                var no = GovernmentConstraints.GetAvailableOptions(GovernmentConstraints.GovernmentDimension.SupremeSuccession, _current);
                if (no.Count > 0 && !no.Contains(_current.supremeSuccession.primary)) _current.supremeSuccession.primary = no[0];
                _current.supremeSuccession.secondary.Clear();
            }
            Refresh();
        }

        private void AddSec()
        {
            if (_current == null) return;
            int max = GovernmentConstraints.GetMaxSecondaryCount(.5f);
            if (_current.supremeSuccession.secondary.Count >= max) return;
            var opts = GovernmentConstraints.GetAvailableOptions(GovernmentConstraints.GovernmentDimension.SupremeSuccession, _current, false);
            var av = new List<int>();
            foreach (var o in opts) if (o != _current.supremeSuccession.primary && !_current.supremeSuccession.secondary.Contains(o)) av.Add(o);
            if (av.Count == 0) return;
            _current.supremeSuccession.secondary.Add(av[0]);
            Refresh();
        }

        private void ApplyTpl()
        {
            if (_tplDd == null || _tplDd.value <= 0) return;
            var tpls = GovernmentConstraints.GetTemplates();
            int i = _tplDd.value - 1;
            if (i < 0 || i >= tpls.Count) return;
            var t = tpls[i];
            foreach (var kv in t.recommendedPrimary)
            {
                switch (kv.Key)
                {
                    case GovernmentConstraints.GovernmentDimension.SupremeSovereignty: _current.supremeSovereignty = (GovernmentConstraints.SupremeSovereignty)kv.Value; break;
                    case GovernmentConstraints.GovernmentDimension.SupremeSuccession: _current.supremeSuccession.primary = kv.Value; break;
                    case GovernmentConstraints.GovernmentDimension.SupremeScope: _current.supremeScope.primary = kv.Value; break;
                    case GovernmentConstraints.GovernmentDimension.CentralExistence: _current.centralExistence = (GovernmentConstraints.CentralExistence)kv.Value; break;
                    case GovernmentConstraints.GovernmentDimension.CentralSuccession: _current.centralSuccession.primary = kv.Value; break;
                    case GovernmentConstraints.GovernmentDimension.CentralInstitution: _current.centralInstitution.primary = kv.Value; break;
                    case GovernmentConstraints.GovernmentDimension.LocalSuccession: _current.localSuccession.primary = kv.Value; break;
                    case GovernmentConstraints.GovernmentDimension.LocalScope: _current.localScope.primary = kv.Value; break;
                    case GovernmentConstraints.GovernmentDimension.SpatialStructure: _current.spatialStructure.primary = kv.Value; break;
                }
            }
            _current.titleDistribution = t.recommendedTitleDist;
            _current.domainDistribution = t.recommendedDomainDist;
            _current.supremeSuccession.secondary.Clear();
            Refresh();
        }

        private void Apply() { if (world?.realms?.Count > targetRealmId) world.realms[targetRealmId].composition = _current; }
        private void Reset() { _current = new GovernmentComposition(); Refresh(); }

        private void Refresh()
        {
            foreach (var d in Order) { RefreshDd(d); RefreshSub(d); }
            RefreshSec();
            if (_catText) _catText.text = _current.supremeSovereignty == GovernmentConstraints.SupremeSovereignty.Monarchy ? "君主制" : "共和制";
            if (_sumText)
            {
                var p = new List<string> { $"归属:{_current.supremeSovereignty}", $"交接:{GovernmentConstraints.GetComponentName(GovernmentConstraints.GovernmentDimension.SupremeSuccession,_current.supremeSuccession.primary)}" };
                if (_current.supremeSuccession.secondary.Count > 0) { var n = new List<string>(); foreach (var s in _current.supremeSuccession.secondary) n.Add(GovernmentConstraints.GetComponentName(GovernmentConstraints.GovernmentDimension.SupremeSuccession,s)); p.Add($"次要:{string.Join("+",n)}"); }
                p.Add($"头衔:{_current.titleDistribution}"); p.Add($"领地:{_current.domainDistribution}");
                p.Add($"中央:{(_current.centralExistence==GovernmentConstraints.CentralExistence.Established?"有常设":"无常设")}");
                p.Add($"央地:{GovernmentConstraints.GetComponentName(GovernmentConstraints.GovernmentDimension.SpatialStructure,_current.spatialStructure.primary)}");
                _sumText.text = string.Join(" | ", p);
            }
        }

        private void RefreshDd(GovernmentConstraints.GovernmentDimension d)
        {
            if (!_dd.ContainsKey(d)) return;
            var dd = _dd[d]; dd.ClearOptions();
            var opts = GovernmentConstraints.GetAvailableOptions(d, _current);
            var ss = new List<string> { "选择..." };
            foreach (var o in opts) ss.Add(GovernmentConstraints.GetComponentName(d, o));
            dd.AddOptions(ss);
            int cur = GetCur(d); int ci = opts.IndexOf(cur); dd.value = ci >= 0 ? ci + 1 : 0;
        }

        private int GetCur(GovernmentConstraints.GovernmentDimension d)
        {
            switch (d)
            {
                case GovernmentConstraints.GovernmentDimension.SupremeSovereignty: return (int)_current.supremeSovereignty;
                case GovernmentConstraints.GovernmentDimension.SupremeSuccession: return _current.supremeSuccession.primary;
                case GovernmentConstraints.GovernmentDimension.SupremeScope: return _current.supremeScope.primary;
                case GovernmentConstraints.GovernmentDimension.CentralExistence: return (int)_current.centralExistence;
                case GovernmentConstraints.GovernmentDimension.CentralSuccession: return _current.centralSuccession.primary;
                case GovernmentConstraints.GovernmentDimension.CentralInstitution: return _current.centralInstitution.primary;
                case GovernmentConstraints.GovernmentDimension.LocalSuccession: return _current.localSuccession.primary;
                case GovernmentConstraints.GovernmentDimension.LocalScope: return _current.localScope.primary;
                case GovernmentConstraints.GovernmentDimension.SpatialStructure: return _current.spatialStructure.primary;
                default: return 0;
            }
        }

        private void RefreshSub(GovernmentConstraints.GovernmentDimension d)
        {
            if (!_sub.ContainsKey(d)) return;
            var c = _sub[d];
            foreach (Transform ch in c.transform) Destroy(ch.gameObject);
            var ag = GovernmentConstraints.GetActiveSubOptionGroups(d, _current);
            foreach (var g in ag)
            {
                var l = MkText($"  {g.groupName}:", 11, FontStyle.Bold); l.color = new Color(.9f,.9f,.7f); l.transform.SetParent(c.transform, false);
                var dd = MkDd(); dd.transform.SetParent(c.transform, false);
                var os = new List<string> { "选择..." };
                foreach (var o in g.options) os.Add(o.name);
                dd.AddOptions(os);
            }
            if (d == GovernmentConstraints.GovernmentDimension.SupremeScope)
            {
                var tl = MkText("  头衔分配:", 11, FontStyle.Bold); tl.color = new Color(.9f,.9f,.7f); tl.transform.SetParent(c.transform, false);
                var td = MkDd(); td.transform.SetParent(c.transform, false);
                var to = GovernmentConstraints.GetAvailableTitleDistributions(_current);
                var ts = new List<string> { "选择..." };
                foreach (var o in to) ts.Add(o.ToString());
                td.AddOptions(ts); td.value = (int)_current.titleDistribution + 1;
                td.onValueChanged.AddListener(i => { if (i > 0) _current.titleDistribution = (GovernmentConstraints.TitleDistribution)(i-1); Refresh(); });
                var dl = MkText("  领地分配:", 11, FontStyle.Bold); dl.color = new Color(.9f,.9f,.7f); dl.transform.SetParent(c.transform, false);
                var dd2 = MkDd(); dd2.transform.SetParent(c.transform, false);
                var ds = new List<string> { "选择..." };
                foreach (GovernmentConstraints.DomainDistribution o in Enum.GetValues(typeof(GovernmentConstraints.DomainDistribution))) ds.Add(o.ToString());
                dd2.AddOptions(ds); dd2.value = (int)_current.domainDistribution + 1;
                dd2.onValueChanged.AddListener(i => { if (i > 0) _current.domainDistribution = (GovernmentConstraints.DomainDistribution)(i-1); Refresh(); });
            }
        }

        private void RefreshSec()
        {
            if (!_secContainer) return;
            Transform list = null;
            foreach (Transform ch in _secContainer.transform) if (ch.name == "List") list = ch;
            if (list == null) return;
            foreach (Transform ch in list.transform) Destroy(ch.gameObject);
            int max = GovernmentConstraints.GetMaxSecondaryCount(.5f);
            if (_secCountText) _secCountText.text = $"{_current.supremeSuccession.secondary.Count}/{max}";
            for (int i = 0; i < _current.supremeSuccession.secondary.Count; i++)
            {
                int ci = i;
                var row = new GameObject($"S{i}"); row.transform.SetParent(list, false);
                row.AddComponent<HorizontalLayoutGroup>().spacing = 10;
                MkText($"  第{i+1}次要:", 11, FontStyle.Normal).transform.SetParent(row.transform, false);
                var dd = MkDd(); dd.transform.SetParent(row.transform, false);
                var opts = GovernmentConstraints.GetAvailableOptions(GovernmentConstraints.GovernmentDimension.SupremeSuccession, _current, false);
                var av = new List<int>(); var ss = new List<string> { "选择..." };
                foreach (var o in opts) if (o != _current.supremeSuccession.primary && !_current.supremeSuccession.secondary.Contains(o)) { av.Add(o); ss.Add(GovernmentConstraints.GetComponentName(GovernmentConstraints.GovernmentDimension.SupremeSuccession,o)); }
                int cur = _current.supremeSuccession.secondary[i];
                if (!av.Contains(cur)) { av.Insert(0,cur); ss.Insert(1,GovernmentConstraints.GetComponentName(GovernmentConstraints.GovernmentDimension.SupremeSuccession,cur)); }
                dd.AddOptions(ss);
                int cidx = av.IndexOf(cur); dd.value = cidx >= 0 ? cidx + 1 : 0;
                dd.onValueChanged.AddListener(idx => { if (idx <= 0) return; _current.supremeSuccession.secondary[ci] = av[idx-1]; Refresh(); });
                var rm = MkBtn("×", 30); rm.transform.SetParent(row.transform, false);
                rm.onClick.AddListener(() => { _current.supremeSuccession.secondary.RemoveAt(ci); Refresh(); });
            }
        }

        private Text MkText(string t, int s, FontStyle st)
        {
            var o = new GameObject("Text"); var txt = o.AddComponent<Text>();
            txt.text = t; txt.fontSize = s; txt.fontStyle = st; txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft; txt.horizontalOverflow = HorizontalWrapMode.Overflow; txt.verticalOverflow = VerticalWrapMode.Overflow;
            if (!txt.font) txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            o.AddComponent<LayoutElement>().minWidth = 100;
            return txt;
        }

        private Dropdown MkDd()
        {
            var o = new GameObject("DD"); var dd = o.AddComponent<Dropdown>();
            var tpl = new GameObject("Tpl"); tpl.transform.SetParent(o.transform, false);
            var tr = tpl.AddComponent<RectTransform>(); tr.anchorMin = new Vector2(0,0); tr.anchorMax = new Vector2(1,0); tr.pivot = new Vector2(.5f,1); tr.sizeDelta = new Vector2(0,150); tpl.SetActive(false);
            var vp = new GameObject("VP"); vp.transform.SetParent(tpl.transform, false);
            var vr = vp.AddComponent<RectTransform>(); vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one; vr.offsetMin = Vector2.zero; vr.offsetMax = new Vector2(-18,0);
            var mk = vp.AddComponent<Mask>(); mk.showMaskGraphic = false; vp.AddComponent<Image>().color = new Color(.2f,.2f,.2f);
            var ct = new GameObject("CT"); ct.transform.SetParent(vp.transform, false);
            var cr = ct.AddComponent<RectTransform>(); cr.anchorMin = new Vector2(0,1); cr.anchorMax = new Vector2(1,1); cr.pivot = new Vector2(.5f,1); cr.sizeDelta = new Vector2(0,28);
            ct.AddComponent<VerticalLayoutGroup>().spacing = 2;
            var it = new GameObject("Item"); it.transform.SetParent(ct.transform, false);
            var ir = it.AddComponent<RectTransform>(); ir.anchorMin = new Vector2(0,.5f); ir.anchorMax = new Vector2(1,.5f); ir.sizeDelta = new Vector2(0,28);
            var tg = it.AddComponent<Toggle>(); tg.isOn = true; it.AddComponent<Image>().color = new Color(.3f,.3f,.3f);
            var il = new GameObject("Label"); il.transform.SetParent(it.transform, false);
            var ilr = il.AddComponent<RectTransform>(); ilr.anchorMin = Vector2.zero; ilr.anchorMax = Vector2.one; ilr.offsetMin = new Vector2(10,0); ilr.offsetMax = new Vector2(-10,0);
            var itxt = il.AddComponent<Text>(); itxt.fontSize = 12; itxt.color = Color.white; itxt.alignment = TextAnchor.MiddleLeft;
            if (!itxt.font) itxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tg.targetGraphic = it.GetComponent<Image>();
            var cols = tg.colors; cols.normalColor = new Color(.3f,.3f,.3f); cols.highlightedColor = new Color(.4f,.4f,.4f); cols.pressedColor = new Color(.5f,.5f,.5f); cols.selectedColor = new Color(.4f,.5f,.7f); tg.colors = cols;
            dd.template = tr; dd.captionText = MkCap(o); dd.itemText = itxt;
            dd.options = new List<Dropdown.OptionData>(); dd.AddOptions(new List<string> { "选择..." });
            o.AddComponent<LayoutElement>().minWidth = 150;
            return dd;
        }

        private Text MkCap(GameObject p)
        {
            var o = new GameObject("Label"); o.transform.SetParent(p.transform, false);
            var r = o.AddComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(10,0); r.offsetMax = new Vector2(-30,0);
            var t = o.AddComponent<Text>(); t.fontSize = 12; t.color = Color.white; t.alignment = TextAnchor.MiddleLeft;
            if (!t.font) t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return t;
        }

        private Button MkBtn(string t, float w)
        {
            var o = new GameObject("Btn"); var b = o.AddComponent<Button>(); o.AddComponent<Image>().color = new Color(.3f,.4f,.6f);
            var cols = b.colors; cols.normalColor = new Color(.3f,.4f,.6f); cols.highlightedColor = new Color(.4f,.5f,.7f); cols.pressedColor = new Color(.2f,.3f,.5f); b.colors = cols;
            var to = new GameObject("Text"); to.transform.SetParent(o.transform, false);
            var tr = to.AddComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            var txt = to.AddComponent<Text>(); txt.text = t; txt.fontSize = 12; txt.color = Color.white; txt.alignment = TextAnchor.MiddleCenter;
            if (!txt.font) txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var le = o.AddComponent<LayoutElement>(); le.minWidth = w; le.minHeight = 28;
            return b;
        }

        public void Show() { if (panelRoot) panelRoot.SetActive(true); Init(); }
        public void Hide() { if (panelRoot) panelRoot.SetActive(false); }
        public void Toggle() { if (panelRoot) { if (panelRoot.activeSelf) Hide(); else Show(); } }
    }
}
