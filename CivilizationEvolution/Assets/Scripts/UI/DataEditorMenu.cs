using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CivilizationEvolution.UI
{
 /// 数据编辑器菜单（代码动态生成）：选择编辑种族/文化/宗教/兵种/建筑/物资。 /// 挂在主菜单 Canvas 下，点"编辑器"显示。    public class DataEditorMenu : MonoBehaviour
    {
        private GameObject _root;

 /// <summary>构建数据编辑器菜单</summary>        public void Build(Transform parent, Action onClose)
        {
 // 半透明遮罩            var overlayObj = new GameObject("DataEditorOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _root = overlayObj;
            var ort = overlayObj.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
            overlayObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            overlayObj.transform.SetParent(parent, false);

 // 面板            var panelObj = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var prt = panelObj.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(520, 520);
            panelObj.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 0.98f);
            panelObj.transform.SetParent(overlayObj.transform, false);

            float y = 220;
 // 标题            CreateText(panelObj.transform, "Title", "编辑器", 32, new Color(0.88f, 0.75f, 0.45f, 1f), FontStyles.Bold, new Vector2(0, y));
            y -= 50;
 // 分隔线            var line = new GameObject("Line", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 1f); lrt.anchorMax = new Vector2(0.5f, 1f);
            lrt.pivot = new Vector2(0.5f, 1f); lrt.anchoredPosition = new Vector2(0, y);
            lrt.sizeDelta = new Vector2(440, 1);
            line.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f, 1f);
            line.transform.SetParent(panelObj.transform, false);
            y -= 30;

 // 编辑器选项            var editors = new (string name, string desc, Action action)[]
            {
                ("种族编辑", "编辑种族属性、天赋、初始科技", () => OpenEditor("种族")),
                ("文化编辑", "编辑文化传统、命名表、价值观", () => OpenEditor("文化")),
                ("宗教编辑", "编辑信仰、教义、信条、圣地", () => OpenEditor("宗教")),
                ("兵种编辑", "编辑兵种属性、装备、招募条件", () => OpenEditor("兵种")),
                ("建筑编辑", "编辑建筑效果、造价、解锁条件", () => OpenEditor("建筑")),
                ("物资编辑", "编辑物资分类、价值、加工链", () => OpenEditor("物资")),
            };

            float btnHeight = 52f, spacing = 8f;
            foreach (var (name, desc, action) in editors)
            {
                CreateEditorButton(panelObj.transform, name, desc, new Vector2(0, y), new Vector2(440, btnHeight), action);
                y -= btnHeight + spacing;
            }

 // 返回按钮            y -= 10;
            CreateButton(panelObj.transform, "返回", new Vector2(0, y), new Vector2(160, 36), false, () => onClose?.Invoke());

            _root.SetActive(false);
        }

        private void CreateEditorButton(Transform parent, string name, string desc, Vector2 pos, Vector2 size, Action onClick)
        {
            var btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            btnObj.GetComponent<Image>().color = new Color(0.14f, 0.15f, 0.19f, 0.95f);
            var btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

 // 名称（左对齐）            CreateText(btnObj.transform, "Name", name, 20, new Color(0.88f, 0.75f, 0.45f, 1f), FontStyles.Bold,
                new Vector2(-180, 0), TextAlignmentOptions.MidlineLeft, new Vector2(200, 28));
 // 描述（右对齐）            CreateText(btnObj.transform, "Desc", desc, 14, new Color(0.55f, 0.55f, 0.60f, 1f), FontStyles.Normal,
                new Vector2(100, 0), TextAlignmentOptions.MidlineRight, new Vector2(240, 20));

 // 底部装饰线            var line = new GameObject("Line", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = new Vector2(1, 0);
            lrt.pivot = new Vector2(0.5f, 0f); lrt.anchoredPosition = Vector2.zero;
            lrt.sizeDelta = new Vector2(0, 1);
            line.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.30f, 1f);
            line.transform.SetParent(btnObj.transform, false);

            btnObj.AddComponent<UIButtonHover>();
            btnObj.transform.SetParent(parent, false);
        }

        private void CreateButton(Transform parent, string label, Vector2 pos, Vector2 size, bool primary, Action onClick)
        {
            var btnObj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            btnObj.GetComponent<Image>().color = primary ? new Color(0.30f, 0.25f, 0.12f, 1f) : new Color(0.18f, 0.19f, 0.23f, 1f);
            var btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            CreateText(btnObj.transform, "Label", label, 18, primary ? new Color(0.88f, 0.75f, 0.45f, 1f) : Color.white,
                primary ? FontStyles.Bold : FontStyles.Normal, Vector2.zero, TextAlignmentOptions.Center, size);
            btnObj.AddComponent<UIButtonHover>();
            btnObj.transform.SetParent(parent, false);
        }

        private void CreateText(Transform parent, string name, string text, int fontSize, Color color,
            FontStyles style, Vector2 pos, TextAlignmentOptions align = TextAlignmentOptions.Center, Vector2? size = null)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos;
            rt.sizeDelta = size ?? new Vector2(400, 30);
            var tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
            tmp.fontStyle = style; tmp.alignment = align; tmp.raycastTarget = false;
            obj.transform.SetParent(parent, false);
        }

        private void OpenEditor(string type)
        {
            Debug.Log($"[DataEditorMenu] 打开{type}编辑器（待实现具体编辑面板）");
 // TODO: 打开对应的数据编辑器面板        }

        public void Show() => _root?.SetActive(true);
        public void Hide() => _root?.SetActive(false);
    }
}
