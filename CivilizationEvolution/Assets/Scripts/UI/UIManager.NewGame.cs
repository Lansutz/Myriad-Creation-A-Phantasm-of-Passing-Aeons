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
    /// <summary>
    /// UIManager.NewGame —— 新游戏面板（世界参数滑块/尺寸预设/种子随机/开始游戏）（partial class，与 UIManager.cs 共享字段与组件引用）
    /// </summary>
    public partial class UIManager : MonoBehaviour
    {

        /// <summary>主菜单开始游戏（世界初始化——玩家操作后启动——隐藏菜单进游戏）</summary>
        /// <summary>主菜单第 2 行：编辑器（生成世界并进入编辑模式——Tab 编辑器面板）</summary>
        private void EnterEditorFromMenu()
        {
            var bootstrap = FindAnyObjectByType<CivilizationEvolution.Core.Bootstrap>();
            if (bootstrap != null && (world == null || world.tiles.Length == 0))
                bootstrap.StartNewGame();
            if (startMenuPanel != null) startMenuPanel.SetActive(false);
            // 打开编辑器面板（Tab 编辑器——编辑地图）
            if (_editorPanel != null) _editorPanel.ShowPanel();
        }


        /// <summary>世界生成面板（第 1 行"进入世界"→生成选项——FMG 式：
        /// 尺寸/种子→生成——玩家可配置而非无脑默认）</summary>
        private void OpenNewGamePanel()
        {
            if (newGamePanel == null)
            {
                // 无面板（旧场景）——直接生成（兼容）
                StartGameWithOptions();
                return;
            }
            startMenuPanel?.SetActive(false);
            _presetIndex = 0;
            _genSeed = new System.Random().Next(1, 100000);
            // 世界参数配置副本（防污染 DefaultWorldConfig 资产——改副本——
            // InitializeWorld 再 Instantiate 保参——生成链用）
            if (world != null && world.config != null)
            {
                _panelConfig = UnityEngine.Object.Instantiate(world.config);
                _panelConfig.name = "PanelWorldConfig";
                world.config = _panelConfig;
            }
            BuildWorldParameterRows();
            RefreshNewGamePanel();
            newGamePanel.SetActive(true);
        }


        /// <summary>世界参数行（动态构建——每参数：名+滑条+值——对接 WorldConfig 字段）</summary>
        private void BuildWorldParameterRows()
        {
            if (paramContentRoot == null) return;
            // 清旧行
            foreach (Transform child in paramContentRoot)
                Destroy(child.gameObject);
            if (_panelConfig == null) return;

            foreach (var (label, field, min, max, fmt) in WorldParams)
            {
                var row = new GameObject("Param_" + field, typeof(RectTransform),
                    typeof(HorizontalLayoutGroup));
                row.transform.SetParent(paramContentRoot, false);
                var hl = row.GetComponent<HorizontalLayoutGroup>();
                hl.spacing = 8; hl.childAlignment = TextAnchor.MiddleLeft;
                hl.childForceExpandWidth = true;
                var le = row.AddComponent<LayoutElement>();
                le.minHeight = 30;

                var nameTxt = MakeParamText(label);
                nameTxt.transform.SetParent(row.transform, false);
                nameTxt.GetComponent<LayoutElement>().minWidth = 86;

                var sliderGo = new GameObject("Slider", typeof(RectTransform));
                sliderGo.transform.SetParent(row.transform, false);
                var slider = sliderGo.AddComponent<Slider>();
                slider.minValue = min; slider.maxValue = max;
                var bg = sliderGo.AddComponent<UnityEngine.UI.Image>();
                bg.sprite = UITheme.RoundedPanelSprite;
                bg.type = Image.Type.Sliced;
                bg.color = new Color32(30, 38, 52, 255);
                // fill
                var fillGo = new GameObject("Fill", typeof(RectTransform));
                fillGo.transform.SetParent(sliderGo.transform, false);
                var fill = fillGo.AddComponent<UnityEngine.UI.Image>();
                fill.color = UITheme.Accent;
                slider.fillRect = (RectTransform)fillGo.transform;
                slider.targetGraphic = bg;
                // handle
                var handleGo = new GameObject("Handle", typeof(RectTransform));
                handleGo.transform.SetParent(sliderGo.transform, false);
                var handleImg = handleGo.AddComponent<UnityEngine.UI.Image>();
                handleImg.sprite = UITheme.RoundedButtonSprite;
                handleImg.type = Image.Type.Sliced;
                handleImg.color = new Color32(210, 220, 235, 255);
                slider.handleRect = (RectTransform)handleGo.transform;
                slider.direction = Slider.Direction.LeftToRight;
                var sle = sliderGo.AddComponent<LayoutElement>();
                sle.minWidth = 160; sle.flexibleWidth = 1f;

                // 值文本
                var valueTxt = MakeParamText("");
                valueTxt.transform.SetParent(row.transform, false);
                valueTxt.alignment = TMPro.TextAlignmentOptions.Right;
                valueTxt.GetComponent<LayoutElement>().minWidth = 48;

                // 初始值（从 config 读——反射）
                float cur = ReadParam(field);
                slider.value = Mathf.Clamp(cur, min, max);
                valueTxt.text = cur.ToString(fmt);

                // 联动（滑条→config 字段）
                float fMin = min, fMax = max; string fFmt = fmt; string fField = field;
                slider.onValueChanged.AddListener(v =>
                {
                    float val = Mathf.Clamp(v, fMin, fMax);
                    WriteParam(fField, val);
                    valueTxt.text = val.ToString(fFmt);
                });
            }
        }


        private TMPro.TextMeshProUGUI MakeParamText(string content)
        {
            var go = new GameObject("T", typeof(RectTransform));
            var txt = go.AddComponent<TMPro.TextMeshProUGUI>();
            txt.text = content;
            txt.fontSize = 13;
            txt.color = UITheme.TextMain;
            txt.font = TMPFontUtility.GetChineseFont();
            return txt;
        }


        /// <summary>读 WorldConfig 字段（switch——明确映射）</summary>
        private float ReadParam(string field)
        {
            if (_panelConfig == null) return 0f;
            switch (field)
            {
                case "landAmount": return _panelConfig.landAmount;
                case "landFragment": return _panelConfig.landFragment;
                case "coastFragment": return _panelConfig.coastFragment;
                case "oceanBuffer": return _panelConfig.oceanBuffer;
                case "continentScale": return _panelConfig.continentScale;
                case "mountainStrength": return _panelConfig.mountainStrength;
                case "thermalEquatorLat": return _panelConfig.thermalEquatorLat;
                case "seasonIntensity": return _panelConfig.seasonIntensity;
                default: return 0f;
            }
        }


        private void WriteParam(string field, float v)
        {
            if (_panelConfig == null) return;
            switch (field)
            {
                case "landAmount": _panelConfig.landAmount = v; break;
                case "landFragment": _panelConfig.landFragment = v; break;
                case "coastFragment": _panelConfig.coastFragment = v; break;
                case "oceanBuffer": _panelConfig.oceanBuffer = v; break;
                case "continentScale": _panelConfig.continentScale = v; break;
                case "mountainStrength": _panelConfig.mountainStrength = v; break;
                case "thermalEquatorLat": _panelConfig.thermalEquatorLat = v; break;
                case "seasonIntensity": _panelConfig.seasonIntensity = v; break;
            }
        }


        /// <summary>参数重置（默认值）</summary>
        private void ResetWorldParams()
        {
            if (_panelConfig == null) return;
            var def = WorldConfig.CreateRuntimeInstance();
            _panelConfig.landAmount = def.landAmount;
            _panelConfig.landFragment = def.landFragment;
            _panelConfig.coastFragment = def.coastFragment;
            _panelConfig.oceanBuffer = def.oceanBuffer;
            _panelConfig.continentScale = def.continentScale;
            _panelConfig.mountainStrength = def.mountainStrength;
            _panelConfig.thermalEquatorLat = def.thermalEquatorLat;
            _panelConfig.seasonIntensity = def.seasonIntensity;
            BuildWorldParameterRows(); // 重建（滑条回默认）
        }


        private void CloseNewGamePanel()
        {
            newGamePanel?.SetActive(false);
            if (startMenuPanel != null) startMenuPanel.SetActive(true);
        }


        private void SelectSize(int idx)
        {
            _presetIndex = idx;
            RefreshNewGamePanel();
        }


        private void RandomizeSeed()
        {
            _genSeed = new System.Random().Next(1, 100000);
            RefreshNewGamePanel();
        }


        private void RefreshNewGamePanel()
        {
            if (seedText != null) seedText.text = $"种子：{_genSeed}";
            // 尺寸按钮高亮（选中色——非选中正常）
            Highlight(sizeLargeButton, _presetIndex == 0);
            Highlight(sizeHugeButton, _presetIndex == 1);
            Highlight(sizeEnormousButton, _presetIndex == 2);
        }


        private static void Highlight(Button btn, bool on)
        {
            if (btn == null) return;
            var img = btn.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
                img.color = on ? new Color32(88, 120, 92, 255) : new Color32(52, 62, 78, 255);
        }


        /// <summary>生成世界（面板参数——loading 反馈——链路）</summary>
        private void StartGameWithOptions()
        {
            if (newGamePanel != null) newGamePanel.SetActive(false);
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
                if (loadingText != null)
                {
                    string sizeHint = _presetIndex switch
                    {
                        1 => "（Huge 2048×1024——约 1-3 分钟）",
                        2 => "（Enormous 3072×1536——约 5-10 分钟）",
                        _ => "（Large 1024×512——约 30-60 秒）"
                    };
                    loadingText.text = $"世界生成中…\n{sizeHint}";
                }
            }
            var bootstrap = FindAnyObjectByType<CivilizationEvolution.Core.Bootstrap>();
            if (bootstrap != null && (world == null || world.tiles == null || world.tiles.Length == 0))
            {
                float t0 = Time.realtimeSinceStartup;
                bootstrap.ConfigureAndStartNewGame(_presetIndex, _genSeed);
                UnityEngine.Debug.Log($"[进入世界] 生成完成——耗时 {Time.realtimeSinceStartup - t0:F1}s——种子 {_genSeed}");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[进入世界] 世界已存在——直接进入");
            }
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }


        private void StartGameFromMenu()
        {
            // 链路（用户核心诉求：主菜单按钮→真实进入世界——带生成反馈）：
            // 隐藏主菜单 → 显示"世界生成中"覆盖 → 同步生成（阻塞——画面冻结
            // 在提示上——完成后覆盖层隐藏→地图可见）
            if (startMenuPanel != null) startMenuPanel.SetActive(false);
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
                if (loadingText != null)
                    loadingText.text = "世界生成中…\n（首图较慢——请稍候）";
            }
            var bootstrap = FindAnyObjectByType<CivilizationEvolution.Core.Bootstrap>();
            if (bootstrap != null && (world == null || world.tiles.Length == 0))
            {
                float t0 = Time.realtimeSinceStartup;
                bootstrap.StartNewGame(); // 同步生成（建世界）
                UnityEngine.Debug.Log($"[进入世界] 世界生成完成——耗时 {Time.realtimeSinceStartup - t0:F1}s");
            }
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }

    }
}
