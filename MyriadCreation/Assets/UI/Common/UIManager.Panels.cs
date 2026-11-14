using MyriadCreation.App.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Enums;
using MyriadCreation.Rendering;
using MyriadCreation.UI;
using MyriadCreation.UI.Panels;
using MyriadCreation.UI.Presentation;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.UI.Common
{
public partial class UIManager
{
 // ===== 聚落“已知人物”面板（知识视图——只显示观察者已知且当前在该聚落的角色）=====
        private GameObject settlementPeoplePanel;
        private Transform settlementPeopleListRoot;
        private TMPro.TMP_Text settlementPeopleTitle;
        private UnityEngine.UI.Button settlementPeopleEntryButton;
        private string settlementPeopleSiteName = "";


        private string GetReligionName(int faithId)
        {
            var def = ReligionCatalog.Get(faithId);
            return def != null ? def.religionName : faithId.ToString();
        }

 /// <summary>懒创建入口按钮：挂在地块详情面板下沿（不改场景）。</summary>
        private void EnsureSettlementPeopleEntryButton()
        {
            if (settlementPeopleEntryButton != null || tileInfoPanel == null) return;

            var go = new GameObject("SettlementPeopleBtn", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            var rt = (RectTransform)go.transform;
            rt.SetParent(tileInfoPanel.transform, false);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -8f);
            rt.sizeDelta = new Vector2(210f, 30f);

            go.GetComponent<UnityEngine.UI.Image>().color = new Color32(52, 62, 78, 255);
            settlementPeopleEntryButton = go.GetComponent<UnityEngine.UI.Button>();
            settlementPeopleEntryButton.onClick.AddListener(OpenSettlementPeoplePanel);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var txt = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            txt.font = TMPFontUtility.GetChineseFont();
            txt.text = "此聚落人物（已知）";
            txt.fontSize = 14;
            txt.alignment = TMPro.TextAlignmentOptions.Center;
            txt.color = Color.white;
            var trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        }

 /// <summary>懒创建聚落人物面板（代码生成——不改场景文件）。</summary>
        private void EnsureSettlementPeoplePanel()
        {
            if (settlementPeoplePanel != null) return;

            var parent = transform.parent != null ? transform.parent : transform;
            var panel = new GameObject("SettlementPeoplePanel", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            var rt = (RectTransform)panel.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-560f, 0f);
            rt.sizeDelta = new Vector2(360f, 460f);
            panel.GetComponent<UnityEngine.UI.Image>().color = new Color32(20, 26, 33, 240);

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(panel.transform, false);
            settlementPeopleTitle = titleGo.AddComponent<TMPro.TextMeshProUGUI>();
            settlementPeopleTitle.font = TMPFontUtility.GetChineseFont();
            settlementPeopleTitle.fontSize = 16;
            settlementPeopleTitle.color = UITheme.Accent;
            settlementPeopleTitle.alignment = TMPro.TextAlignmentOptions.Center;
            var trt = settlementPeopleTitle.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -10f);
            trt.sizeDelta = new Vector2(-20f, 28f);

            var listGo = new GameObject("List", typeof(RectTransform));
            listGo.transform.SetParent(panel.transform, false);
            var vlg = listGo.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var lrt = vlg.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(1f, 1f);
            lrt.offsetMin = new Vector2(10f, 50f);
            lrt.offsetMax = new Vector2(-10f, -40f);
            settlementPeopleListRoot = listGo.transform;

            var closeGo = new GameObject("CloseBtn", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            var crt = (RectTransform)closeGo.transform;
            crt.SetParent(panel.transform, false);
            crt.anchorMin = new Vector2(0.5f, 0f); crt.anchorMax = new Vector2(0.5f, 0f);
            crt.pivot = new Vector2(0.5f, 0f);
            crt.anchoredPosition = new Vector2(0f, 10f);
            crt.sizeDelta = new Vector2(120f, 30f);
            closeGo.GetComponent<UnityEngine.UI.Image>().color = new Color32(80, 44, 44, 255);
            closeGo.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(CloseSettlementPeoplePanel);
            var closeTextGo = new GameObject("Text", typeof(RectTransform));
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var closeText = closeTextGo.AddComponent<TMPro.TextMeshProUGUI>();
            closeText.font = TMPFontUtility.GetChineseFont();
            closeText.text = "关闭";
            closeText.fontSize = 14;
            closeText.alignment = TMPro.TextAlignmentOptions.Center;
            closeText.color = Color.white;
            var ctrt = closeText.rectTransform;
            ctrt.anchorMin = Vector2.zero; ctrt.anchorMax = Vector2.one;
            ctrt.offsetMin = Vector2.zero; ctrt.offsetMax = Vector2.zero;

            settlementPeoplePanel = panel;
            panel.SetActive(false);
        }

        private void CloseSettlementPeoplePanel()
        {
            if (settlementPeoplePanel != null) settlementPeoplePanel.SetActive(false);
        }


 /// <summary>关闭角色面板</summary>
        public void CloseCharacterPanel()
        {
            if (_characterPanelView != null) _characterPanelView.Hide();
        }

        private void PrevCharacter()
        {
            if (_characterPanelView != null) _characterPanelView.Prev();
        }

        private void NextCharacter()
        {
            if (_characterPanelView != null) _characterPanelView.Next();
        }


 /// <summary>启动时恢复保存的显示设置（Bootstrap/GameManager 调）</summary>
        public static void RestoreDisplaySettings()
        {
            int w = PlayerPrefs.GetInt("screen_width", 0);
            int h = PlayerPrefs.GetInt("screen_height", 0);
            int mode = PlayerPrefs.GetInt("screen_mode", -1);
            if (w > 0 && h > 0)
            {
                var fm = mode switch
                {
                    0 => FullScreenMode.FullScreenWindow,
                    1 => FullScreenMode.Windowed,
                    2 => FullScreenMode.MaximizedWindow,
                    _ => FullScreenMode.FullScreenWindow
                };
                Screen.SetResolution(w, h, fm);
            }
        }


 /// <summary>查看政权总览（打开聚合面板——人口/国库/官职/宗教——
 /// 数据源=已有系统聚合——）</summary>
        private void OpenViewRealmPanel()
        {
            if (overviewPanel != null)
            {
                _viewingDivisionId = -1; // 每次打开回政权总览层
                _divisionNavStack.Clear();
                RefreshOverviewPanel();
                overviewPanel.SetActive(true);
            }
        }


 /// <summary>返回上一级（区划详情→父区划/政权总览）</summary>
        private void OnOverviewBack()
        {
            if (_viewingDivisionId < 0) { CloseOverviewPanel(); return; }
            _viewingDivisionId = _divisionNavStack.Count > 0
                ? _divisionNavStack[_divisionNavStack.Count - 1] : -1;
            if (_divisionNavStack.Count > 0) _divisionNavStack.RemoveAt(_divisionNavStack.Count - 1);
            RefreshOverviewPanel();
        }


 /// <summary>下钻到区划（按钮点击——push 当前——显示子详情）</summary>
        private void OnDivisionClicked(int divisionId)
        {
            if (_viewingDivisionId >= 0) _divisionNavStack.Add(_viewingDivisionId);
            _viewingDivisionId = divisionId;
            RefreshOverviewPanel();
        }


        private void CloseOverviewPanel()
        {
            if (overviewPanel != null) overviewPanel.SetActive(false);
        }


 /// <summary>运行时按钮（代码构建——区划下钻入口）</summary>
        private UnityEngine.UI.Button CreateRuntimeButton(string label)
        {
            var go = new GameObject("DivBtn_" + label, typeof(RectTransform));
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color32(52, 62, 78, 255);
            var btn = go.AddComponent<UnityEngine.UI.Button>();
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = Color.white;
            var lt = go.AddComponent<UnityEngine.UI.LayoutElement>();
            lt.minWidth = 90; lt.minHeight = 28;
            return btn;
        }


        private void OpenReligionPanel()
        {
            if (religionPanel != null)
            {
                RefreshReligionPanel();
                religionPanel.SetActive(true);
            }
        }


        private void CloseReligionPanel()
        {
            if (religionPanel != null) religionPanel.SetActive(false);
        }


        private void OpenSocietyPanel()
        {
            if (societyPanel != null) societyPanel.SetActive(true);
            RefreshSocietyPanel();
        }


 /// <summary>关闭社会政治面板</summary>
        private void CloseSocietyPanel()
        {
            if (societyPanel != null) societyPanel.SetActive(false);
        }


 /// <summary>打开音乐播放器面板</summary>
        private void OpenMusicPanel()
        {
            MusicPlayer();
            if (musicPanel != null) musicPanel.SetActive(true);
            RefreshMusicPanel();
        }


 /// <summary>关闭音乐播放器面板</summary>
        private void CloseMusicPanel()
        {
            if (musicPanel != null) musicPanel.SetActive(false);
        }


 /// <summary>刷新音乐面板（曲目列表+当前状态）</summary>
        private void RefreshMusicPanel()
        {
            if (musicText == null) return;
            var mp = MusicPlayer();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"── 音乐播放器 ── 音量 {mp.Volume * 100f:F0}%");
            sb.AppendLine(mp.IsPlaying ? "▶ 播放中" : "⏸ 暂停");
            for (int i = 0; i < mp.Playlist.Count; i++)
            {
                string mark = i == mp.CurrentIndex ? "▶ " : "  ";
                sb.AppendLine($"{mark}{i + 1}. {mp.Playlist[i].name}");
            }
            if (mp.Playlist.Count == 0)
                sb.AppendLine("（无曲目——请放入 Resources/Music/ 的 ogg/mp3）");
            musicText.text = sb.ToString();
        }


 /// <summary>打开家族树面板</summary>
        private void OpenFamilyTreePanel()
        {
            if (_familyTreeView == null)
            {
                return;
            }
            _familyTreeView.SetViewRealm(ViewRealmId);
            _familyTreeView.Show();
        }


 /// <summary>关闭家族树面板</summary>
        private void CloseFamilyTreePanel()
        {
            if (_familyTreeView != null) _familyTreeView.Hide();
        }

        private void PrevFamilyMember()
        {
            if (_familyTreeView != null) _familyTreeView.Prev();
        }

        private void NextFamilyMember()
        {
            if (_familyTreeView != null) _familyTreeView.Next();
        }

        // ===== 政府/法律面板入口（task-60：TopBar 动态按钮 + G 快捷键，场景零改动） =====
        private MyriadCreation.UI.Panels.GovernmentPanel _governmentPanel;
        private UnityEngine.UI.Button _governmentOpenButton;
        private int _governmentTopBarPaddingLeft = 16;
        private int _governmentTopBarPaddingRight = 16;
        private int _lastGovernmentLayoutWidth = -1;

        /// <summary>是否正在 TMP 输入框中打字（G 快捷键不打扰输入）。</summary>
        private static bool IsTypingInInputField()
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null || eventSystem.currentSelectedGameObject == null) return false;
            return eventSystem.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null;
        }

        /// <summary>P7：TopBar 上的五档速度循环按钮（1/2/5/10/30x），不依赖场景改造。</summary>
        private void EnsureSpeedCycleButton()
        {
            if (_speedCycleButton != null) return;
            Transform topBar = null;
            GameObject topBarObj = GameObject.Find("TopBar");
            if (topBarObj != null) topBar = topBarObj.transform;
            if (topBar == null && societyOpenButton != null) topBar = societyOpenButton.transform.parent;
            if (topBar == null && charOpenButton != null) topBar = charOpenButton.transform.parent;
            if (topBar == null) return;

            var go = new GameObject("SpeedCycleButton", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            var rt = (RectTransform)go.transform;
            rt.SetParent(topBar, false);
            rt.sizeDelta = new Vector2(64f, 30f);
            go.GetComponent<UnityEngine.UI.Image>().color = new Color32(52, 62, 78, 255);
            var layoutElement = go.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.preferredWidth = 64f;
            layoutElement.minWidth = 54f;
            layoutElement.preferredHeight = 30f;
            layoutElement.minHeight = 30f;

            _speedCycleButton = go.GetComponent<UnityEngine.UI.Button>();
            _speedCycleButton.onClick.AddListener(() => { UiSfx.PlayClick(); CycleGameSpeed(); });

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var txt = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            txt.font = TMPFontUtility.GetChineseFont();
            txt.text = "1x";
            txt.fontSize = 13;
            txt.alignment = TMPro.TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.raycastTarget = false;
            var trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            _speedCycleLabel = txt;

            float currentSpeed = AppServices.GameSession != null ? AppServices.GameSession.GameSpeed : 1f;
            _speedIndex = System.Array.IndexOf(SpeedOptions, currentSpeed);
            if (_speedIndex < 0)
            {
                // 旧场景默认 3x 或暂停 0f 不在五档里：标签按真实值显示，下一次点击从 1x 开始循环。
                _speedIndex = 0;
                _speedCycleLabel.text = currentSpeed <= 0f ? "暂停" : $"{currentSpeed:0}x";
            }
            else
            {
                _speedCycleLabel.text = $"{SpeedOptions[_speedIndex]:0}x";
            }
            _speedCycleButton.gameObject.SetActive(
                AppServices.GameSession == null || AppServices.GameSession.CurrentState != GameState.MainMenu);
        }

        /// <summary>懒创建 TopBar 上的「政府」按钮（若场景已有则复用引用；不改场景文件）。</summary>
        private void EnsureGovernmentButton()
        {
            if (_governmentOpenButton != null) return;

            Transform topBar = null;
            GameObject topBarObj = GameObject.Find("TopBar");
            if (topBarObj != null) topBar = topBarObj.transform;
            if (topBar == null && societyOpenButton != null) topBar = societyOpenButton.transform.parent;
            if (topBar == null && charOpenButton != null) topBar = charOpenButton.transform.parent;
            if (topBar == null) return; // 顶栏缺失（异常场景）：G 快捷键/公开方法仍可用

            var go = new GameObject("GovernmentOpenButton", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            var rt = (RectTransform)go.transform;
            rt.SetParent(topBar, false);
            rt.sizeDelta = new Vector2(86f, 30f);
            go.GetComponent<UnityEngine.UI.Image>().color = new Color32(52, 62, 78, 255);
            // 顶栏基线内边距记录：窄屏时按基线减 8（16→8），恢复宽屏时还原。
            UnityEngine.UI.HorizontalLayoutGroup topBarLayout = topBar.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (topBarLayout != null)
            {
                _governmentTopBarPaddingLeft = topBarLayout.padding.left;
                _governmentTopBarPaddingRight = topBarLayout.padding.right;
            }
            var layoutElement = go.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.preferredWidth = 86f;
            layoutElement.minWidth = 64f;
            layoutElement.preferredHeight = 30f;
            layoutElement.minHeight = 30f;

            _governmentOpenButton = go.GetComponent<UnityEngine.UI.Button>();
            _governmentOpenButton.onClick.AddListener(() => { UiSfx.PlayClick(); ToggleGovernmentPanel(); });

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var txt = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            txt.font = TMPFontUtility.GetChineseFont();
            txt.text = "政府";
            txt.fontSize = 13;
            txt.alignment = TMPro.TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.raycastTarget = false;
            var trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            // 主菜单阶段先隐藏（UIManager.Start 可能早于/晚于 GameManager.Start）；ShowGameUI 时统一显示。
            _governmentOpenButton.gameObject.SetActive(
                AppServices.GameSession == null || AppServices.GameSession.CurrentState != GameState.MainMenu);
            // 首次按当前屏宽应用宽度/字号/顶栏内边距（窄屏 <1280 缩到 64）。
            RefreshGovernmentButtonLayout(Screen.width);
        }

        /// <summary>
        /// 按屏宽刷新「政府」按钮布局（不越出 TopBar、不与 CharOpenBtn 重叠）：
        /// ≥1280 宽屏 → 86×30、字号 13、TopBar 左右内边距保持基线；
        /// &lt;1280 窄屏 → 64×30（minWidth 64）、字号 12、内边距基线减 8（最少 4）。
        /// TopBar 的 HorizontalLayoutGroup 为 childControlWidth=0 时用 sizeDelta，=1 时用 LayoutElement，
        /// 两套都同步设置；刷新后强制重建布局，保证同帧可测量。
        /// </summary>
        public void RefreshGovernmentButtonLayout(int screenWidth)
        {
            if (screenWidth <= 0) screenWidth = Screen.width;
            _lastGovernmentLayoutWidth = screenWidth;
            if (_governmentOpenButton == null) return;

            bool narrow = screenWidth > 0 && screenWidth < 1280;
            float buttonWidth = narrow ? 64f : 86f;
            var rt = (RectTransform)_governmentOpenButton.transform;
            rt.sizeDelta = new Vector2(buttonWidth, 30f);
            var layoutElement = _governmentOpenButton.GetComponent<UnityEngine.UI.LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.preferredWidth = buttonWidth;
                layoutElement.minWidth = 64f;
                layoutElement.preferredHeight = 30f;
                layoutElement.minHeight = 30f;
            }
            TMPro.TMP_Text label = _governmentOpenButton.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = narrow ? 12 : 13;
            }

            RectTransform topBar = _governmentOpenButton.transform.parent as RectTransform;
            if (topBar != null)
            {
                UnityEngine.UI.HorizontalLayoutGroup topBarLayout = topBar.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                if (topBarLayout != null)
                {
                    topBarLayout.padding.left = narrow
                        ? Mathf.Max(4, _governmentTopBarPaddingLeft - 8)
                        : _governmentTopBarPaddingLeft;
                    topBarLayout.padding.right = narrow
                        ? Mathf.Max(4, _governmentTopBarPaddingRight - 8)
                        : _governmentTopBarPaddingRight;
                }
                // 立即重建，便于测试/切屏当帧拿到最终位置与宽度。
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(topBar);
            }
        }

        /// <summary>政府/法律面板是否打开（供 ESC 优先级/外部系统查询）。</summary>
        public bool IsGovernmentPanelOpen => _governmentPanel != null && _governmentPanel.IsOpen;

        /// <summary>关闭政府/法律面板。</summary>
        public void CloseGovernmentPanel()
        {
            if (_governmentPanel != null) _governmentPanel.Hide();
        }

        /// <summary>切换政府/法律面板（顶部「政府」按钮与 G 快捷键共用）。</summary>
        public void ToggleGovernmentPanel()
        {
            EnsureGovernmentPanel();
            if (_governmentPanel == null) return;
            if (_governmentPanel.IsOpen)
            {
                _governmentPanel.Hide();
                return;
            }
            OpenGovernmentPanel();
        }

        // ===== 4d：Legacy 删除后的契约安全兜底（无 view 显示空/不抛） =====

        private void UpdateTileInfo()
        {
            if (_worldReadModel == null || _selectedTile < 0)
            {
                FillTileTexts("-", "-", "-", "-", "-", "-");
                if (viewRealmButton != null) viewRealmButton.gameObject.SetActive(false);
                return;
            }

            TileInfoDto dto = _worldReadModel.GetTileInfo(_selectedTile);
            if (dto == null)
            {
                FillTileTexts("-", "-", "-", "-", "-", "-");
                if (viewRealmButton != null) viewRealmButton.gameObject.SetActive(false);
                return;
            }

            string owner = string.IsNullOrEmpty(dto.ownerRealmName)
                ? (dto.ownerRealmId >= 0 ? "#" + dto.ownerRealmId : "无主")
                : dto.ownerRealmName;
            SetPanelText(tileNameText, owner + " · 地块 #" + dto.tileIndex + " (" + dto.x + "," + dto.y + ")");
            SetPanelText(tileTerrainText,
                "地形: " + dto.terrainName + "\n海拔: " + dto.elevation01.ToString("F2") +
                "\n坡度: " + dto.slopeDegree.ToString("F1") + "°" +
                "\n海陆: " + (dto.isLand ? "陆地" : "海洋"));
            SetPanelText(tileClimateText,
                "年均温: " + dto.annualTemp.ToString("F1") + "℃\n年降水: " + dto.annualPrecipMm.ToString("F0") +
                "mm\n温度带: " + dto.climateZoneName);
            SetPanelText(tileBiomeText,
                "群系: " + dto.biomeName + "\n肥力: " + dto.fertility.ToString("F2"));
            SetPanelText(tilePopulationText,
                "人口: " + dto.population.ToString("N0") + "\n发展: " + dto.development.ToString("F2") +
                "\n稳定: " + dto.stability.ToString("F0"));

            StringBuilder economy = new StringBuilder();
            economy.Append("省份: ").Append(string.IsNullOrEmpty(dto.provinceName) ? (dto.provinceId >= 0 ? "#" + dto.provinceId : "-") : dto.provinceName);
            economy.Append("\n聚落: ").Append(dto.settlementAnchorId >= 0 ? dto.settlementName + "（anchor " + dto.settlementAnchorId + "）" : "-");
            if (dto.resources != null && dto.resources.Count > 0)
            {
                economy.Append("\n资源:");
                foreach (TileResourceDto resource in dto.resources)
                {
                    economy.Append("\n- ").Append(resource.goodsName).Append("（丰度 ")
                        .Append(resource.abundance.ToString("F2")).Append(resource.discovered ? "" : "·未发现")
                        .Append(resource.developed ? "·已开发" : "").Append("）");
                }
            }
            else
            {
                economy.Append("\n资源: -");
            }
            SetPanelText(tileEconomyText, economy.ToString());
            if (viewRealmButton != null) viewRealmButton.gameObject.SetActive(dto.ownerRealmId >= 0);
        }

        private void OpenCharacterPanel()
        {
            if (_characterPanelView == null)
            {
                return;
            }
            _characterPanelView.SetViewRealm(ViewRealmId);
            _characterPanelView.Show();
        }

        private void FillTileTexts(string name, string terrain, string climate, string biome, string population, string economy)
        {
            SetPanelText(tileNameText, name);
            SetPanelText(tileTerrainText, terrain);
            SetPanelText(tileClimateText, climate);
            SetPanelText(tileBiomeText, biome);
            SetPanelText(tilePopulationText, population);
            SetPanelText(tileEconomyText, economy);
        }

        private static void SetPanelText(TMP_Text target, string value)
        {
            if (target != null) target.text = value;
        }

        private void RefreshOverviewPanel()
        {
            if (overviewText == null) return;
            overviewText.text = _worldReadModel != null
                ? RealmOverviewText.Build(_worldReadModel, ViewRealmId)
                : "=== 政权总览 ===\n（无数据）";
        }

        private void RefreshReligionPanel()
        {
            if (religionPanelText == null) return;
            religionPanelText.text = _worldReadModel != null
                ? ReligionPanelText.Build(_worldReadModel, ViewRealmId, -1)
                : "=== 宗教 ===\n（无数据）";
        }

        private void RefreshSocietyPanel()
        {
            if (societyText == null) return;
            societyText.text = _worldReadModel != null
                ? SocietyPanelText.Build(_worldReadModel, ViewRealmId)
                : "=== 社会 ===\n（无数据）";
        }

        private void OpenSettlementPeoplePanel()
        {
            EnsureSettlementPeoplePanel();
            if (settlementPeoplePanel != null) settlementPeoplePanel.SetActive(true);
            if (settlementPeopleTitle != null) settlementPeopleTitle.text = settlementPeopleSiteName + " · 已知人物 0";
        }

        private void EnsureGovernmentPanel()
        {
            if (_governmentPanel != null) return;

            _governmentPanel = FindAnyObjectByType<MyriadCreation.UI.Panels.GovernmentPanel>(
                FindObjectsInactive.Include);
            if (_governmentPanel != null)
            {
                ApplyLawServicesToGovernmentPanel();
                ApplyDecreeCommandToGovernmentPanel();
                _governmentPanel.SetWorldReadModel(_worldReadModel);
                Panels.Register(_governmentPanel);
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null
                ? canvas.transform
                : (transform.parent != null ? transform.parent : transform);
            var go = new GameObject("GovernmentPanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var hostRt = (RectTransform)go.transform;
            hostRt.anchorMin = Vector2.zero;
            hostRt.anchorMax = Vector2.one;
            hostRt.offsetMin = Vector2.zero;
            hostRt.offsetMax = Vector2.zero;
            _governmentPanel = go.AddComponent<MyriadCreation.UI.Panels.GovernmentPanel>();
            _governmentPanel.Build(null);
            ApplyLawServicesToGovernmentPanel();
            ApplyDecreeCommandToGovernmentPanel();
            _governmentPanel.SetWorldReadModel(_worldReadModel);
            Panels.Register(_governmentPanel);
        }

        public void OpenGovernmentPanel()
        {
            EnsureGovernmentPanel();
            if (_governmentPanel == null) return;
            _governmentPanel.SetWorldReadModel(_worldReadModel);
            _governmentPanel.Show();
        }

}
}
