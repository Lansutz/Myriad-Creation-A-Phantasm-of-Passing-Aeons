using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CivilizationEvolution.Core;
using CivilizationEvolution.Render;
using CivilizationEvolution.Race;
using CivilizationEvolution.Character;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.UI
{
 /// UIManager.MapMode —— 地图模式切换（分类/子模式/按钮生成/高亮）（partial class，与 UIManager.cs 共享字段与组件引用）
    public partial class UIManager : MonoBehaviour
    {

 /// <summary>类别切换：填充子项选项（无子项隐藏子 Dropdown）</summary>
        private void OnMapCategoryChanged(int index)
        {
            _mapCategory = index;
            if (displayModeSubDropdown != null)
            {
                if (MapModeSubs.TryGetValue(index, out var subs))
                {
                    displayModeSubDropdown.ClearOptions();
                    displayModeSubDropdown.AddOptions(subs);
                    displayModeSubDropdown.gameObject.SetActive(true);
                    displayModeSubDropdown.value = 0;
                    _mapSub = 0;
                }
                else
                {
                    displayModeSubDropdown.gameObject.SetActive(false);
                    _mapSub = 0;
                }
            }
            ApplyMapMode();
        }


        private void OnMapSubModeChanged(int index)
        {
            _mapSub = index;
            ApplyMapMode();
        }


        private void ApplyMapMode()
        {
            if (mapRenderer == null) return;
            if (ModeMap.TryGetValue((_mapCategory, _mapSub), out var mode))
                mapRenderer.SetDisplayMode(mode);
        }


 /// <summary>折叠/展开地图模式栏（顶部按钮——箭头下拉式）</summary>
        private void ToggleMapModeBar()
        {
 // CK3 式模式面板（替代旧 Dropdown——保留旧栏兼容隐藏）
            if (mapModePanel != null)
            {
                bool open = !mapModePanel.activeSelf;
                mapModePanel.SetActive(open);
                if (open) RefreshMapModeList();
                return;
            }
            if (mapModeBar != null) mapModeBar.SetActive(!mapModeBar.activeSelf);
        }


 /// <summary>模式面板按钮列表（类别+展开子项——radio 高亮）</summary>
        private void RefreshMapModeList()
        {
            if (mapModeListRoot == null) return;
            foreach (Transform child in mapModeListRoot)
                Destroy(child.gameObject);

            for (int cat = 0; cat < MapModeCategories.Count; cat++)
            {
                bool hasSub = MapModeSubs.TryGetValue(cat, out var subs);
                bool current = cat == _modeCat;
                bool expanded = current && _modeSubOpen && hasSub;

 // 类别按钮（当前高亮——有子显示 ▸/▾）
                string label = hasSub
                    ? (expanded ? "▾ " : "▸ ") + MapModeCategories[cat]
                    : MapModeCategories[cat];
                var btn = CreateMapModeButton(label, mapModeListRoot, cat, -1);
                HighlightMapMode(btn, current && !hasSub);
                if (current && hasSub && !_modeSubOpen)
                {
 // 当前类别默认选中其子 0（模式已生效——标签右侧标当前子名）
                    if (subs != null && subs.Count > 0)
                        label += " · " + subs[_modeSub];
                }

 // 子项（展开显示——缩进）
                if (expanded && subs != null)
                {
                    for (int si = 0; si < subs.Count; si++)
                    {
                        var subBtn = CreateMapModeButton("    " + subs[si], mapModeListRoot, cat, si);
                        HighlightMapMode(subBtn, si == _modeSub);
                    }
                }
            }
        }


        private Button CreateMapModeButton(string label, Transform parent, int cat, int sub)
        {
            var go = new GameObject("ModeBtn_" + cat + "_" + sub, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.sprite = UITheme.RoundedButtonSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color32(30, 38, 52, 230);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var txt = txtGo.AddComponent<TMPro.TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 13;
            txt.color = UITheme.TextMain;
            txt.font = TMPFontUtility.GetChineseFont();
            txt.alignment = TMPro.TextAlignmentOptions.Left;
            var rt = txt.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(10, 0); rt.offsetMax = new Vector2(-4, 0);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 26; le.minWidth = 150;

            if (sub < 0)
            {
 // 类别点击：切换（有子=展开/折叠——无子=直接切）
                btn.onClick.AddListener(() =>
                {
                    bool hasSub = MapModeSubs.ContainsKey(cat);
                    if (hasSub)
                    {
                        if (_modeCat == cat) { _modeSubOpen = !_modeSubOpen; }
                        else { _modeCat = cat; _modeSub = 0; _modeSubOpen = true; }
                        SetMapMode(_modeCat, _modeSub);
                    }
                    else
                    {
                        _modeCat = cat; _modeSub = 0; _modeSubOpen = false;
                        SetMapMode(cat, 0);
                    }
                    RefreshMapModeList();
                });
            }
            else
            {
                int c = cat; int si = sub;
                btn.onClick.AddListener(() =>
                {
                    _modeCat = c; _modeSub = si; _modeSubOpen = false;
                    SetMapMode(c, si);
                    RefreshMapModeList();
                });
            }
            return btn;
        }


        private static void HighlightMapMode(Button btn, bool on)
        {
            if (btn == null) return;
            var img = btn.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
                img.color = on ? new Color32(58, 88, 130, 255) : new Color32(30, 38, 52, 230);
        }


 /// <summary>设置地图模式（类别+子项→displayMode——统一入口——Dropdown 兼容）</summary>
        private void SetMapMode(int cat, int sub)
        {
            if (ModeMap.TryGetValue((cat, sub), out var mode))
            {
                var mr = FindAnyObjectByType<Render.MapRenderer>();
                if (mr != null) mr.SetDisplayMode(mode);
                _modeCat = cat; _modeSub = sub;
            }
        }

    }
}
