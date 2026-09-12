using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using CivilizationEvolution.UI.Common;
namespace CivilizationEvolution.UI.Panels
{
 /// 存档选择面板——列出 MapSaves 目录下的所有 .json 存档，
 /// 选择后通过回调通知调用方加载指定存档。
 /// 代码动态生成，不依赖场景预制体。
    public class SaveLoadPanel : MonoBehaviour
    {
        [SerializeField] private Color panelBg = new Color(0.08f, 0.09f, 0.12f, 0.98f);
        [SerializeField] private Color titleColor = new Color(0.88f, 0.75f, 0.45f, 1f);
        [SerializeField] private Color itemBg = new Color(0.14f, 0.15f, 0.19f, 0.9f);
        [SerializeField] private Color itemHover = new Color(0.20f, 0.22f, 0.28f, 1f);
        [SerializeField] private Color itemText = new Color(0.85f, 0.85f, 0.88f, 1f);
        [SerializeField] private Color subText = new Color(0.55f, 0.55f, 0.60f, 1f);
        [SerializeField] private Color lineColor = new Color(0.35f, 0.35f, 0.40f, 1f);

        private GameObject _root;
        private Transform _listContent;
        private Action<string> _onSelectSave;
        private Action _onClose;

 /// <summary>构建存档选择面板</summary>
        public void Build(Transform parent, Action<string> onSelectSave, Action onClose)
        {
            _onSelectSave = onSelectSave;
            _onClose = onClose;

 // 半透明遮罩
            var overlayObj = new GameObject("SaveLoadOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayObj.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            var ort = overlayObj.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
            overlayObj.transform.SetParent(parent, false);
            _root = overlayObj;

 // 主面板
            var panelObj = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObj.GetComponent<Image>().color = panelBg;
            var prt = panelObj.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(560, 520);
            panelObj.transform.SetParent(overlayObj.transform, false);

 // 标题
            var titleObj = CreateText("Title", "加载世界", 28, titleColor, FontStyles.Bold);
            var trt = titleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f); trt.anchoredPosition = new Vector2(0, -20);
            trt.sizeDelta = new Vector2(0, 40);
            titleObj.transform.SetParent(panelObj.transform, false);

 // 标题下划线
            var lineObj = new GameObject("TitleLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lineObj.GetComponent<Image>().color = lineColor;
            var lrt = lineObj.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 1f); lrt.anchorMax = new Vector2(0.5f, 1f);
            lrt.pivot = new Vector2(0.5f, 1f); lrt.anchoredPosition = new Vector2(0, -65);
            lrt.sizeDelta = new Vector2(480, 1);
            lineObj.transform.SetParent(panelObj.transform, false);

 // 滚动列表视口
            var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewportObj.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 0.6f);
            var vrt = viewportObj.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0f, 0f); vrt.anchorMax = new Vector2(1f, 1f);
            vrt.pivot = new Vector2(0.5f, 0.5f);
            vrt.offsetMin = new Vector2(30, 80); vrt.offsetMax = new Vector2(-30, -80);
            viewportObj.transform.SetParent(panelObj.transform, false);

 // 列表内容
            var contentObj = new GameObject("Content", typeof(RectTransform));
            _listContent = contentObj.transform;
            var crt = contentObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(0, 0);
            contentObj.transform.SetParent(viewportObj.transform, false);

 // 关闭按钮
            var closeBtn = CreateButton("关闭", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 20), new Vector2(160, 44), () => { _onClose?.Invoke(); Hide(); });
            closeBtn.transform.SetParent(panelObj.transform, false);

            RefreshList();
        }

 /// <summary>刷新存档列表</summary>
        public void RefreshList()
        {
 // 清空旧列表
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                Destroy(_listContent.GetChild(i).gameObject);

            string saveDir = Path.Combine(Application.persistentDataPath, "MapSaves");
            if (!Directory.Exists(saveDir))
            {
                ShowEmptyHint("暂无存档");
                return;
            }

            string[] files = Directory.GetFiles(saveDir, "*.json");
            if (files == null || files.Length == 0)
            {
                ShowEmptyHint("暂无存档");
                return;
            }

 // 按修改时间倒序
            Array.Sort(files, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));

            float itemHeight = 72f;
            float spacing = 8f;
            float totalHeight = files.Length * (itemHeight + spacing);
            _listContent.GetComponent<RectTransform>().sizeDelta = new Vector2(0, totalHeight);

            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(files[i]);
                DateTime modTime = File.GetLastWriteTime(files[i]);
                long size = new FileInfo(files[i]).Length;

                var item = CreateSaveItem(fileName, modTime, size, i, itemHeight, spacing);
                item.transform.SetParent(_listContent, false);
            }
        }

        private void ShowEmptyHint(string text)
        {
            var hintObj = CreateText("EmptyHint", text, 18, subText, FontStyles.Normal);
            var hrt = hintObj.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.5f, 0.5f); hrt.anchorMax = new Vector2(0.5f, 0.5f);
            hrt.pivot = new Vector2(0.5f, 0.5f); hrt.anchoredPosition = Vector2.zero;
            hrt.sizeDelta = new Vector2(300, 30);
            hintObj.transform.SetParent(_listContent, false);
            _listContent.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 100);
        }

        private GameObject CreateSaveItem(string fileName, DateTime modTime, long sizeBytes, int index, float height, float spacing)
        {
            var itemObj = new GameObject($"SaveItem_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            itemObj.GetComponent<Image>().color = itemBg;
            var irt = itemObj.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 1f); irt.anchorMax = new Vector2(1f, 1f);
            irt.pivot = new Vector2(0.5f, 1f);
            irt.anchoredPosition = new Vector2(0, -index * (height + spacing));
            irt.sizeDelta = new Vector2(0, height);

            var btn = itemObj.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = itemHover;
            colors.pressedColor = new Color(0.25f, 0.27f, 0.33f, 1f);
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            btn.onClick.AddListener(() =>
            {
                _onSelectSave?.Invoke(fileName);
                Hide();
            });

 // 存档名
            var nameObj = CreateText("Name", fileName, 20, itemText, FontStyles.Bold);
            var nrt = nameObj.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 1f); nrt.anchorMax = new Vector2(1f, 1f);
            nrt.pivot = new Vector2(0f, 1f); nrt.anchoredPosition = new Vector2(16, -10);
            nrt.sizeDelta = new Vector2(-32, 28);
            nameObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
            nameObj.transform.SetParent(itemObj.transform, false);

 // 时间和大小
            string sizeStr = sizeBytes < 1024 ? $"{sizeBytes} B" :
                              sizeBytes < 1048576 ? $"{sizeBytes / 1024} KB" :
                              $"{sizeBytes / 1048576}.{(sizeBytes % 1048576) / 104858} MB";
            string info = $"{modTime:yyyy-MM-dd HH:mm}  |  {sizeStr}";
            var infoObj = CreateText("Info", info, 14, subText, FontStyles.Normal);
            var ifrt = infoObj.GetComponent<RectTransform>();
            ifrt.anchorMin = new Vector2(0f, 0f); ifrt.anchorMax = new Vector2(1f, 0f);
            ifrt.pivot = new Vector2(0f, 0f); ifrt.anchoredPosition = new Vector2(16, 10);
            ifrt.sizeDelta = new Vector2(-32, 20);
            infoObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomLeft;
            infoObj.transform.SetParent(itemObj.transform, false);

 // 底部细线
            var lineObj = new GameObject("Line", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lineObj.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.30f, 0.5f);
            var lrt = lineObj.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot = new Vector2(0.5f, 0f); lrt.anchoredPosition = Vector2.zero;
            lrt.sizeDelta = new Vector2(0, 1);
            lineObj.transform.SetParent(itemObj.transform, false);

            return itemObj;
        }

        private GameObject CreateText(string name, string text, int fontSize, Color color, FontStyles style)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
            tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
            return obj;
        }

        private GameObject CreateButton(string label, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 size, Action onClick)
        {
            var btnObj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.GetComponent<Image>().color = new Color(0.15f, 0.16f, 0.20f, 0.95f);
            var brt = btnObj.GetComponent<RectTransform>();
            brt.anchorMin = anchorMin; brt.anchorMax = anchorMax;
            brt.pivot = new Vector2(0.5f, 0.5f); brt.anchoredPosition = anchoredPos;
            brt.sizeDelta = size;
            btnObj.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());

            var textObj = CreateText("Label", label, 18, new Color(0.85f, 0.85f, 0.88f, 1f), FontStyles.Normal);
            var trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            textObj.transform.SetParent(btnObj.transform, false);

            return btnObj;
        }

        public void Show() { if (_root != null) _root.SetActive(true); }
        public void Hide() { if (_root != null) _root.SetActive(false); }
        public bool IsVisible => _root != null && _root.activeSelf;
    }
}
