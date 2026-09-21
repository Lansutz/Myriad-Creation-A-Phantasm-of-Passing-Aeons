using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using CivilizationEvolution.UI.Common;
namespace CivilizationEvolution.UI.Panels
{
 /// 游戏设置面板（代码动态生成）：音量/分辨率/画质/语言/全屏。
 /// 挂在主菜单 Canvas 下，点"设置"显示。
    public class SettingsPanel : MonoBehaviour
    {
        private GameObject _root;
        private Slider _masterVol, _musicVol, _sfxVol;
        private TMP_Dropdown _resolution, _quality, _language;
        private Toggle _fullscreen;

 /// <summary>构建设置面板</summary>
        public void Build(Transform parent, Action onClose)
        {
 // 半透明遮罩
            var overlayObj = new GameObject("SettingsOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _root = overlayObj;
            var ort = overlayObj.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
            overlayObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            overlayObj.transform.SetParent(parent, false);

 // 面板
            var panelObj = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var prt = panelObj.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(560, 560);
            panelObj.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 0.98f);
            panelObj.transform.SetParent(overlayObj.transform, false);

            float y = 240;
 // 标题
            CreateText(panelObj.transform, "Title", "设置", 32, new Color(0.88f, 0.75f, 0.45f, 1f), FontStyles.Bold, new Vector2(0, y));
            y -= 50;

 // 分隔线
            var line = new GameObject("Line", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 1f); lrt.anchorMax = new Vector2(0.5f, 1f);
            lrt.pivot = new Vector2(0.5f, 1f); lrt.anchoredPosition = new Vector2(0, y);
            lrt.sizeDelta = new Vector2(480, 1);
            line.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f, 1f);
            line.transform.SetParent(panelObj.transform, false);
            y -= 30;

 // 音量
            _masterVol = CreateSlider(panelObj.transform, "主音量", ref y, AudioListener.volume);
            _musicVol = CreateSlider(panelObj.transform, "音乐音量", ref y, 0.8f);
            _sfxVol = CreateSlider(panelObj.transform, "音效音量", ref y, 0.9f);
            y -= 10;

 // 分辨率
            _resolution = CreateDropdown(panelObj.transform, "分辨率", ref y,
                new string[] { "1920×1080", "2560×1440", "3840×2160", "1280×720" });
 // 画质
            _quality = CreateDropdown(panelObj.transform, "画质", ref y,
                new string[] { "低", "中", "高", "超高" });
            _quality.value = QualitySettings.GetQualityLevel();
 // 语言
            _language = CreateDropdown(panelObj.transform, "语言", ref y,
                new string[] { "简体中文", "English" });
            y -= 10;

 // 全屏
            _fullscreen = CreateToggle(panelObj.transform, "全屏模式", ref y, Screen.fullScreen);

 // 按钮
            y -= 20;
            CreateButton(panelObj.transform, "应用", new Vector2(-90, y), new Vector2(140, 40), true, () =>
            {
                AudioListener.volume = _masterVol.value;
                QualitySettings.SetQualityLevel(_quality.value);
                Screen.fullScreen = _fullscreen.isOn;
                var res = _resolution.options[_resolution.value].text.Split('×');
                if (res.Length == 2 && int.TryParse(res[0], out int rw) && int.TryParse(res[1], out int rh))
                    Screen.SetResolution(rw, rh, _fullscreen.isOn);
                onClose?.Invoke();
            });
            CreateButton(panelObj.transform, "取消", new Vector2(90, y), new Vector2(140, 40), false, () => onClose?.Invoke());

            _root.SetActive(false);
        }

        private Slider CreateSlider(Transform parent, string label, ref float y, float defaultValue)
        {
            CreateText(parent, label + "_Label", label, 18, Color.white, FontStyles.Normal, new Vector2(-180, y), TextAlignmentOptions.MidlineLeft, new Vector2(160, 24));
            var sliderObj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
            var rt = sliderObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(60, y - 2);
            rt.sizeDelta = new Vector2(240, 20);
            sliderObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);
            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = defaultValue;
            slider.transform.SetParent(parent, false);
            y -= 36;
            return slider;
        }

        private TMP_Dropdown CreateDropdown(Transform parent, string label, ref float y, string[] options)
        {
            CreateText(parent, label + "_Label", label, 18, Color.white, FontStyles.Normal, new Vector2(-180, y), TextAlignmentOptions.MidlineLeft, new Vector2(160, 24));
            var ddObj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_Dropdown));
            var rt = ddObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(60, y - 2);
            rt.sizeDelta = new Vector2(240, 28);
            ddObj.GetComponent<Image>().color = new Color(0.18f, 0.19f, 0.23f, 1f);
            var dd = ddObj.GetComponent<TMP_Dropdown>();
            dd.options = new List<TMP_Dropdown.OptionData>();
            foreach (var o in options) dd.options.Add(new TMP_Dropdown.OptionData(o));
            dd.value = 0;
            dd.transform.SetParent(parent, false);
            y -= 40;
            return dd;
        }

        private Toggle CreateToggle(Transform parent, string label, ref float y, bool defaultValue)
        {
            var tgObj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Toggle));
            var rt = tgObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(200, 24);
            var toggle = tgObj.GetComponent<Toggle>();
            toggle.isOn = defaultValue;
            CreateText(tgObj.transform, "Label", label, 18, Color.white, FontStyles.Normal, new Vector2(20, 0), TextAlignmentOptions.MidlineLeft, new Vector2(160, 24));
            tgObj.transform.SetParent(parent, false);
            y -= 36;
            return toggle;
        }

        private void CreateButton(Transform parent, string label, Vector2 pos, Vector2 size, bool primary, Action onClick)
        {
            var btnObj = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(pos.x, pos.y);
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

        public void Show() => _root?.SetActive(true);
        public void Hide() => _root?.SetActive(false);
    }
}
