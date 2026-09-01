#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using CivilizationEvolution.Core;
using CivilizationEvolution.Render;
using CivilizationEvolution.UI;
using CivilizationEvolution.Map;

namespace CivilizationEvolution.EditorTools
{
    /// <summary>
    /// 文明演化 · 编辑器一键搭建工具
    /// 顶部菜单 Civilization Evolution / ...
    /// </summary>
    public static class CivilizationEvolutionMenu
    {
        private const string MenuRoot = "Civilization Evolution/";
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string DefaultConfigPath = "Assets/ScriptableObjects/DefaultWorldConfig.asset";

        // UI 主题（统一色板/圆角/Tint，见 CivilizationEvolution.UI.UITheme）
        private static readonly Color PanelColor = UITheme.PanelBg;
        private static readonly Color TextColor = UITheme.TextMain;
        private static readonly Font UiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        [MenuItem(MenuRoot + "1. 一键搭建游戏场景", false, 1)]
        public static void BuildGameScene()
        {
            // ---- GameManager（持久化单例）----
            var gmGo = new GameObject("GameManager");
            if (gmGo.GetComponent<GameManager>() == null)
                gmGo.AddComponent<GameManager>();

            // ---- GameWorld（挂载世界配置资产）----
            var worldGo = new GameObject("GameWorld");
            var world = worldGo.AddComponent<GameWorld>();
            var defaultConfig = AssetDatabase.LoadAssetAtPath<WorldConfig>(DefaultConfigPath);
            if (defaultConfig != null)
            {
                world.config = defaultConfig;
            }
            else
            {
                Debug.LogWarning($"[CE菜单] 未找到默认配置资产 {DefaultConfigPath}，运行时将使用内置默认值。可先执行菜单2创建。");
            }

            // ---- MapPlane + MapRenderer ----
            var mapGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            mapGo.name = "MapPlane";
            Object.DestroyImmediate(mapGo.GetComponent<Collider>());
            var renderer = mapGo.AddComponent<MapRenderer>();
            SetField(renderer, "world", world);

            // ---- CameraRig ----
            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            if (camGo.GetComponent<Camera>() == null) camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(64f, 100f, 32f);
            camGo.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 50f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.15f, 0.25f);
            if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();

            // ---- Directional Light ----
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // ---- MapEditor ----
            var editor = worldGo.AddComponent<CivilizationEvolution.Map.MapEditor>();
            SetField(editor, "world", world);
            SetField(editor, "mapRenderer", renderer);
            SetField(editor, "mainCamera", cam);

            // ---- Canvas + UIManager（含完整UGUI）----
            var canvasGo = new GameObject("UICanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            var ui = canvasGo.AddComponent<UIManager>();
            SetField(ui, "world", world);
            SetField(ui, "mapRenderer", renderer);
            BuildUgui(canvasGo.transform, ui);

            // ---- EventSystem（UGUI交互必需）----
            EnsureEventSystem();

            // ---- Bootstrap ----
            var bootstrapGo = new GameObject("Bootstrap");
            var bootstrap = bootstrapGo.AddComponent<Bootstrap>();
            SetField(bootstrap, "mainCamera", cam);
            SetField(bootstrap, "directionalLight", light);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = bootstrapGo;
            Debug.Log("[CE菜单] 游戏场景搭建完成：GameManager/GameWorld/地图/相机/灯光/编辑器/完整UGUI/Bootstrap 已就绪，点击Play即可运行。");
        }

        [MenuItem(MenuRoot + "2. 创建世界配置资产", false, 2)]
        public static void CreateWorldConfigAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<WorldConfig>(DefaultConfigPath);
            if (existing != null)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = existing;
                Debug.Log("[CE菜单] 默认世界配置资产已存在，已在Project窗口高亮。");
                return;
            }
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            var cfg = ScriptableObject.CreateInstance<WorldConfig>();
            AssetDatabase.CreateAsset(cfg, DefaultConfigPath);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = cfg;
            Debug.Log($"[CE菜单] 已创建世界配置资产：{DefaultConfigPath}");
        }

        [MenuItem(MenuRoot + "3. 保存当前场景到 Main.unity", false, 3)]
        public static void SaveActiveScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), MainScenePath);
            Debug.Log("[CE菜单] 场景已保存。");
        }

        [MenuItem(MenuRoot + "5. 就地升级 Dropdown 展开模板", false, 5)]
        public static void UpgradeExistingDropdown()
        {
            var dd = Object.FindFirstObjectByType<Dropdown>(FindObjectsInactive.Include);
            if (dd == null)
            {
                Debug.LogWarning("[CE菜单] 场景中未找到 Dropdown，请先执行菜单\"1. 一键搭建游戏场景\"。");
                return;
            }
            if (dd.template != null)
                Object.DestroyImmediate(dd.template.gameObject);
            dd.itemText = null;
            BuildDropdownTemplate(dd, dd.transform);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[CE菜单] Dropdown 展开模板已升级为完整层级（Viewport/Content/Item+Toggle），请保存场景。");
        }

        [MenuItem(MenuRoot + "6. 就地升级 UI 样式（主题色/圆角/按钮反馈）", false, 6)]
        public static void UpgradeGlobalStyle()
        {
            // ---- 按钮：统一圆角 + ColorTint ----
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in buttons)
            {
                if (b.targetGraphic is Image img)
                {
                    img.sprite = UITheme.RoundedButtonSprite;
                    img.type = Image.Type.Sliced;
                    img.color = UITheme.ButtonNormal;
                }
                UITheme.ApplyButtonTint(b);
            }

            // ---- Dropdown：统一圆角 + ColorTint ----
            var dropdowns = Object.FindObjectsByType<Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var dd in dropdowns)
            {
                if (dd.targetGraphic is Image img)
                {
                    img.sprite = UITheme.RoundedButtonSprite;
                    img.type = Image.Type.Sliced;
                    img.color = UITheme.ButtonNormal;
                }
                UITheme.ApplyButtonTint(dd);
            }

            // ---- 面板：圆角 + 主题色 ----
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas != null)
            {
                foreach (var panelName in new[] { "TopBar", "SpeedGroup", "TileInfoPanel", "EventLogPanel" })
                {
                    var t = canvas.transform.Find(panelName);
                    if (t == null) continue;
                    var img = t.GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = UITheme.RoundedPanelSprite;
                        img.type = Image.Type.Sliced;
                        img.color = UITheme.PanelBg;
                    }
                }

                // ---- 事件日志标题（旧场景缺失则补建）----
                var logPanelT = canvas.transform.Find("EventLogPanel");
                if (logPanelT != null && logPanelT.Find("EventLogTitle") == null)
                {
                    var lt = CreateText("EventLogTitle", logPanelT, "事件日志", 14);
                    lt.color = UITheme.TextDim;
                    lt.alignment = TextAnchor.MiddleLeft;
                    SetAnchor(lt.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -8), Vector2.zero);
                    var logTextT = logPanelT.Find("EventLogText");
                    if (logTextT != null)
                    {
                        var rt = (RectTransform)logTextT;
                        rt.offsetMax = new Vector2(-8, -24);
                        rt.offsetMin = new Vector2(8, 4);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[CE菜单] UI 样式已升级：按钮/下拉圆角+悬停反馈，面板主题色圆角，事件日志标题。请保存场景。");
        }

        [MenuItem(MenuRoot + "7. 就地添加角色面板", false, 7)]
        public static void AddCharacterPanelToExistingScene()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogWarning("[CE菜单] 未找到 Canvas，请先执行菜单 1 一键搭建游戏场景。");
                return;
            }
            if (canvas.transform.Find("CharacterPanel") != null)
            {
                Debug.Log("[CE菜单] 角色面板已存在。");
                return;
            }

            var ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            if (ui == null)
            {
                Debug.LogWarning("[CE菜单] 未找到 UIManager，请先执行菜单 1 一键搭建游戏场景。");
                return;
            }

            // 顶部入口按钮（TopBar 存在则补建）
            var topBar = canvas.transform.Find("TopBar");
            if (topBar != null && topBar.Find("CharOpenBtn") == null)
                SetField(ui, "charOpenButton", CreateButton("CharOpenBtn", topBar, "角色"));

            BuildCharacterPanel(canvas.transform, ui);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[CE菜单] 角色面板已就地添加，请保存场景。");
        }

        [MenuItem(MenuRoot + "8. 就地添加社会政治面板", false, 8)]
        public static void AddSocietyPanelToExistingScene()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogWarning("[CE菜单] 未找到 Canvas，请先执行菜单 1 一键搭建游戏场景。");
                return;
            }
            if (canvas.transform.Find("SocietyPanel") != null)
            {
                Debug.Log("[CE菜单] 社会政治面板已存在。");
                return;
            }

            var ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            if (ui == null)
            {
                Debug.LogWarning("[CE菜单] 未找到 UIManager，请先执行菜单 1 一键搭建游戏场景。");
                return;
            }

            // 顶部入口按钮（TopBar 存在则补建）
            var topBar = canvas.transform.Find("TopBar");
            if (topBar != null && topBar.Find("SocietyOpenBtn") == null)
                SetField(ui, "societyOpenButton", CreateButton("SocietyOpenBtn", topBar, "社会"));

            BuildSocietyPanel(canvas.transform, ui);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[CE菜单] 社会政治面板已就地添加，请保存场景。");
        }

        [MenuItem(MenuRoot + "9. 就地添加音乐播放器", false, 9)]
        public static void AddMusicPanelToExistingScene()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogWarning("[CE菜单] 未找到 Canvas，请先执行菜单 1 一键搭建游戏场景。");
                return;
            }
            if (canvas.transform.Find("MusicPanel") != null)
            {
                Debug.Log("[CE菜单] 音乐播放器已存在。");
                return;
            }

            var ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            if (ui == null)
            {
                Debug.LogWarning("[CE菜单] 未找到 UIManager，请先执行菜单 1 一键搭建游戏场景。");
                return;
            }

            var topBar = canvas.transform.Find("TopBar");
            if (topBar != null && topBar.Find("MusicOpenBtn") == null)
                SetField(ui, "musicOpenButton", CreateButton("MusicOpenBtn", topBar, "音乐"));

            BuildMusicPanel(canvas.transform, ui);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[CE菜单] 音乐播放器已就地添加，请保存场景。");
        }

        /// <summary>构建音乐播放器面板（曲目列表+控制+音量；供一键搭建与就地升级共用）</summary>
        private static void BuildMusicPanel(Transform canvas, UIManager ui)
        {
            var musPanel = CreatePanel("MusicPanel", canvas).gameObject;
            SetAnchor(musPanel.GetComponent<RectTransform>(),
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-1160, -12), new Vector2(420, 440));
            var cv = musPanel.AddComponent<VerticalLayoutGroup>();
            cv.spacing = 6; cv.padding = new RectOffset(12, 12, 10, 10);
            cv.childForceExpandWidth = true;

            // 标题行
            var titleRow = new GameObject("MusTitleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            titleRow.transform.SetParent(musPanel.transform, false);
            var trLayout = titleRow.GetComponent<HorizontalLayoutGroup>();
            trLayout.spacing = 6; trLayout.childAlignment = TextAnchor.MiddleCenter;
            trLayout.childForceExpandWidth = false;

            var title = CreateText("MusTitle", titleRow.transform, "音乐播放器", 18);
            title.color = UITheme.Accent;
            title.fontStyle = FontStyle.Bold;
            SetField(ui, "musicCloseButton", CreateButton("MusCloseBtn", titleRow.transform, "✕"));

            // 控制行：播放/暂停/上一首/下一首
            var ctrlRow = new GameObject("MusCtrlRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            ctrlRow.transform.SetParent(musPanel.transform, false);
            var ctrlLayout = ctrlRow.GetComponent<HorizontalLayoutGroup>();
            ctrlLayout.spacing = 6; ctrlLayout.childAlignment = TextAnchor.MiddleCenter;
            ctrlLayout.childForceExpandWidth = false;
            SetField(ui, "musicPrevButton", CreateButton("MusPrevBtn", ctrlRow.transform, "⏮"));
            SetField(ui, "musicPlayButton", CreateButton("MusPlayBtn", ctrlRow.transform, "▶"));
            SetField(ui, "musicPauseButton", CreateButton("MusPauseBtn", ctrlRow.transform, "⏸"));
            SetField(ui, "musicNextButton", CreateButton("MusNextBtn", ctrlRow.transform, "⏭"));

            // 音量滑块
            var volRow = new GameObject("MusVolRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            volRow.transform.SetParent(musPanel.transform, false);
            var volLayout = volRow.GetComponent<HorizontalLayoutGroup>();
            volLayout.spacing = 6; volLayout.childAlignment = TextAnchor.MiddleLeft;
            volLayout.childForceExpandWidth = false;
            var volLabel = CreateText("MusVolLabel", volRow.transform, "音量", 14);
            volLabel.color = UITheme.TextDim;
            var sliderGo = new GameObject("MusVolSlider", typeof(RectTransform));
            sliderGo.transform.SetParent(volRow.transform, false);
            var slider = sliderGo.AddComponent<UnityEngine.UI.Slider>();
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0.6f;
            SetField(ui, "musicVolumeSlider", slider);

            // 曲目列表（滚动）
            var scroll = musPanel.AddComponent<ScrollRect>();
            var musText = CreateText("MusicText", musPanel.transform, "（打开加载曲目）", 15);
            SetAnchor(musText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            musText.rectTransform.offsetMax = new Vector2(-8, -30);
            musText.rectTransform.offsetMin = new Vector2(8, 4);
            musText.verticalOverflow = VerticalWrapMode.Overflow;
            musText.alignment = TextAnchor.UpperLeft;
            scroll.content = musText.rectTransform;

            SetField(ui, "musicPanel", musPanel);
            SetField(ui, "musicText", musText);
            musPanel.SetActive(false); // 默认隐藏（顶栏"音乐"按钮打开）
        }

        // ================= UGUI 代码构建 =================

        /// <summary>构建整套界面并通过反射注入UIManager</summary>
        private static void BuildUgui(Transform canvas, UIManager ui)
        {
            // ---- 顶部信息栏（顶部横向）----
            var topBar = CreatePanel("TopBar", canvas);
            SetAnchor(topBar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -44), Vector2.zero);
            var topLayout = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 24; topLayout.padding = new RectOffset(16, 16, 8, 8);
            topLayout.childAlignment = TextAnchor.MiddleLeft; topLayout.childForceExpandWidth = false;

            var gameTitle = CreateText("GameTitle", topBar, GameConstants.GameNameShort, 20);
            gameTitle.color = UITheme.Accent;
            gameTitle.fontStyle = FontStyle.Bold;
            SetField(ui, "dateText", CreateText("DateText", topBar, "第 0 年 第 0 天", 20));
            SetField(ui, "treasuryText", CreateText("TreasuryText", topBar, "国库：0", 20));
            SetField(ui, "populationText", CreateText("PopulationText", topBar, "人口：0", 20));
            SetField(ui, "realmNameText", CreateText("RealmNameText", topBar, "未选择势力", 20));

            // ---- 速度控制 + 显示模式（顶部右侧）----
            var speedGroup = CreatePanel("SpeedGroup", canvas);
            SetAnchor(speedGroup, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-360, -44), Vector2.zero);
            var hLayout = speedGroup.gameObject.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 6; hLayout.padding = new RectOffset(6, 6, 6, 6);
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            SetField(ui, "pauseButton", CreateButton("PauseBtn", speedGroup, "||"));
            SetField(ui, "speed1Button", CreateButton("Speed1Btn", speedGroup, "x1"));
            SetField(ui, "speed2Button", CreateButton("Speed2Btn", speedGroup, "x2"));
            SetField(ui, "speed3Button", CreateButton("Speed3Btn", speedGroup, "x5"));
            var dropdownGo = new GameObject("DisplayModeDropdown", typeof(RectTransform));
            dropdownGo.transform.SetParent(speedGroup, false);
            var ddImage = dropdownGo.AddComponent<Image>();
            ddImage.color = UITheme.ButtonNormal;
            ddImage.sprite = UITheme.RoundedButtonSprite;
            ddImage.type = Image.Type.Sliced;
            var dd = dropdownGo.AddComponent<Dropdown>();
            dd.targetGraphic = ddImage;
            UITheme.ApplyButtonTint(dd);
            var ddCaption = CreateText("Label", dropdownGo.transform, "地形", 16);
            ddCaption.alignment = TextAnchor.MiddleCenter;
            ddCaption.rectTransform.anchorMin = Vector2.zero;
            ddCaption.rectTransform.anchorMax = Vector2.one;
            ddCaption.rectTransform.offsetMin = new Vector2(8, 0);
            ddCaption.rectTransform.offsetMax = new Vector2(-12, 0);
            var ddCaptionLe = ddCaption.GetComponent<LayoutElement>();
            ddCaptionLe.minWidth = 0;
            dd.captionText = ddCaption;
            var ddLe = dropdownGo.AddComponent<LayoutElement>();
            ddLe.preferredWidth = 150; ddLe.preferredHeight = 32;
            BuildDropdownTemplate(dd, dropdownGo.transform);
            SetField(ui, "displayModeDropdown", dd);

            // ---- 地块详情面板（右侧）----
            var tilePanel = CreatePanel("TileInfoPanel", canvas).gameObject;
            SetAnchor(tilePanel.GetComponent<RectTransform>(),
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-280, 0), new Vector2(270, 360));
            var vLayout = tilePanel.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 8; vLayout.padding = new RectOffset(12, 12, 12, 12);
            vLayout.childForceExpandWidth = true;
            SetField(ui, "tileInfoPanel", tilePanel);
            SetField(ui, "tileNameText", CreateText("TileName", tilePanel.transform, "地块", 22));
            SetField(ui, "tileTerrainText", CreateText("TileTerrain", tilePanel.transform, "地形：-", 18));
            SetField(ui, "tileClimateText", CreateText("TileClimate", tilePanel.transform, "气候：-", 18));
            SetField(ui, "tileBiomeText", CreateText("TileBiome", tilePanel.transform, "群系：-", 18));
            SetField(ui, "tilePopulationText", CreateText("TilePop", tilePanel.transform, "人口：-", 18));
            SetField(ui, "tileEconomyText", CreateText("TileEconomy", tilePanel.transform, "经济：-", 18));
            tilePanel.SetActive(false);

            // ---- 事件日志（左下）----
            var logPanel = CreatePanel("EventLogPanel", canvas).gameObject;
            SetAnchor(logPanel.GetComponent<RectTransform>(),
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(16, 16), new Vector2(420, 220));
            var logTitle = CreateText("EventLogTitle", logPanel.transform, "事件日志", 14);
            logTitle.color = UITheme.TextDim;
            logTitle.alignment = TextAnchor.MiddleLeft;
            SetAnchor(logTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -8), Vector2.zero);
            var scroll = logPanel.AddComponent<ScrollRect>();
            var logText = CreateText("EventLogText", logPanel.transform, "", 16);
            SetAnchor(logText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            logText.rectTransform.offsetMax = new Vector2(-8, -24);
            logText.rectTransform.offsetMin = new Vector2(8, 4);
            logText.verticalOverflow = VerticalWrapMode.Overflow;
            logText.alignment = TextAnchor.UpperLeft;
            scroll.content = logText.rectTransform;
            SetField(ui, "eventLogPanel", logPanel);
            SetField(ui, "eventLogText", logText);
            SetField(ui, "eventLogScroll", scroll);

            // ---- 顶部：角色面板入口按钮（标题右侧）----
            var charOpenBtn = CreateButton("CharOpenBtn", topBar, "角色");
            SetField(ui, "charOpenButton", charOpenBtn);

            // ---- 角色面板（右侧，地块面板上方）----
            BuildCharacterPanel(canvas, ui);

            // ---- 社会政治面板（右侧）----
            BuildSocietyPanel(canvas, ui);
        }

        /// <summary>构建社会政治面板（阶层画像/派系/政体变迁；供一键搭建与就地升级共用）</summary>
        private static void BuildSocietyPanel(Transform canvas, UIManager ui)
        {
            var socPanel = CreatePanel("SocietyPanel", canvas).gameObject;
            SetAnchor(socPanel.GetComponent<RectTransform>(),
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-720, -12), new Vector2(420, 600));
            var cv = socPanel.AddComponent<VerticalLayoutGroup>();
            cv.spacing = 6; cv.padding = new RectOffset(12, 12, 10, 10);
            cv.childForceExpandWidth = true;

            // 标题行：标题 + 关闭按钮
            var titleRow = new GameObject("SocTitleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            titleRow.transform.SetParent(socPanel.transform, false);
            var trLayout = titleRow.GetComponent<HorizontalLayoutGroup>();
            trLayout.spacing = 6; trLayout.childAlignment = TextAnchor.MiddleCenter;
            trLayout.childForceExpandWidth = false;

            var title = CreateText("SocTitle", titleRow.transform, "社会政治", 18);
            title.color = UITheme.Accent;
            title.fontStyle = FontStyle.Bold;
            SetField(ui, "societyCloseButton", CreateButton("SocCloseBtn", titleRow.transform, "✕"));

            // 滚动正文
            var scroll = socPanel.AddComponent<ScrollRect>();
            var socText = CreateText("SocietyText", socPanel.transform, "（打开面板刷新）", 15);
            SetAnchor(socText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            socText.rectTransform.offsetMax = new Vector2(-8, -30);
            socText.rectTransform.offsetMin = new Vector2(8, 4);
            socText.verticalOverflow = VerticalWrapMode.Overflow;
            socText.alignment = TextAnchor.UpperLeft;
            socText.horizontalOverflow = HorizontalWrapMode.Wrap;
            scroll.content = socText.rectTransform;

            SetField(ui, "societyPanel", socPanel);
            SetField(ui, "societyText", socText);
            socPanel.SetActive(false); // 默认隐藏（顶栏"社会"按钮打开）
        }

        /// <summary>构建角色面板（含入口按钮；供一键搭建与就地升级共用）</summary>
        private static void BuildCharacterPanel(Transform canvas, UIManager ui)
        {
            var charPanel = CreatePanel("CharacterPanel", canvas).gameObject;
            SetAnchor(charPanel.GetComponent<RectTransform>(),
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-280, -12), new Vector2(280, 560));
            var cv = charPanel.AddComponent<VerticalLayoutGroup>();
            cv.spacing = 6; cv.padding = new RectOffset(12, 12, 10, 10);
            cv.childForceExpandWidth = true;

            // 标题行：◀ 名称 ▶（水平排布）
            var titleRow = new GameObject("CharTitleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            titleRow.transform.SetParent(charPanel.transform, false);
            var trLayout = titleRow.GetComponent<HorizontalLayoutGroup>();
            trLayout.spacing = 6; trLayout.childAlignment = TextAnchor.MiddleCenter;
            trLayout.childForceExpandWidth = false;
            SetField(ui, "charPrevButton", CreateButton("CharPrev", titleRow.transform, "◀"));
            var charName = CreateText("CharName", titleRow.transform, "角色", 20);
            charName.alignment = TextAnchor.MiddleCenter;
            charName.GetComponent<LayoutElement>().minWidth = 140;
            SetField(ui, "charNameText", charName);
            SetField(ui, "charNextButton", CreateButton("CharNext", titleRow.transform, "▶"));

            // 状态行（政权/在世/威望等级/统治类型/精神疾病）
            SetField(ui, "charStatusText", CreateText("CharStatus", charPanel.transform, "", 16));
            // 六维 + 容量型/上限型数值
            SetField(ui, "charStatsText", CreateText("CharStats", charPanel.transform, "", 16));
            // 人格七维
            SetField(ui, "charPersonalityText", CreateText("CharPersonality", charPanel.transform, "", 16));
            // 写实人格描述（小字）
            var charDesc = CreateText("CharDesc", charPanel.transform, "", 14);
            charDesc.color = UITheme.TextDim;
            SetField(ui, "charDescText", charDesc);
            // DNA 可见层（外貌/天赋/隐疾——不暴露基因型）
            var charDna = CreateText("CharDna", charPanel.transform, "", 14);
            charDna.color = UITheme.TextDim;
            SetField(ui, "charDnaText", charDna);
            // 关闭
            SetField(ui, "charCloseButton", CreateButton("CharClose", charPanel.transform, "关闭"));
            charPanel.SetActive(false);
        }

        /// <summary>构建 UGUI Dropdown 完整展开模板（Template/Viewport/Content/Item+Toggle）</summary>
        /// <remarks>UGUI Dropdown 展开时按名称查找 "Item"/"Item Label"，层级与组件缺一不可</remarks>
        private static void BuildDropdownTemplate(Dropdown dd, Transform dropdownRoot)
        {
            // ---- Template：下拉列表容器（背景 + 滚动）----
            var templateGo = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateGo.transform.SetParent(dropdownRoot, false);
            var templateRt = (RectTransform)templateGo.transform;
            templateRt.anchorMin = new Vector2(0, 0);
            templateRt.anchorMax = new Vector2(1, 0);
            templateRt.pivot = new Vector2(0.5f, 1f);
            templateRt.sizeDelta = new Vector2(0, 150f);
            templateGo.GetComponent<Image>().color = UITheme.PanelSolid;
            templateGo.GetComponent<Image>().sprite = UITheme.RoundedPanelSprite;
            templateGo.GetComponent<Image>().type = Image.Type.Sliced;
            var scroll = templateGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            // ---- Viewport：可视区（裁剪）----
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(templateGo.transform, false);
            var viewportRt = (RectTransform)viewportGo.transform;
            StretchRect(viewportRt);
            viewportGo.GetComponent<Image>().color = Color.clear;

            // ---- Content：选项容器 ----
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = (RectTransform)contentGo.transform;
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0, 28f);
            scroll.viewport = viewportRt;
            scroll.content = contentRt;

            // ---- Item：单个选项（Toggle + 背景 + 勾选 + 文本）----
            var itemGo = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            itemGo.transform.SetParent(contentGo.transform, false);
            var itemRt = (RectTransform)itemGo.transform;
            itemRt.anchorMin = new Vector2(0, 0.5f);
            itemRt.anchorMax = new Vector2(1, 0.5f);
            itemRt.sizeDelta = new Vector2(0, 28f);
            var toggle = itemGo.GetComponent<Toggle>();
            toggle.isOn = true;
            UITheme.ApplyButtonTint(toggle);

            var bgGo = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(itemGo.transform, false);
            StretchRect((RectTransform)bgGo.transform);
            var bgImage = bgGo.GetComponent<Image>();
            bgImage.color = new Color(0.15f, 0.2f, 0.3f, 0.85f);
            toggle.targetGraphic = bgImage;

            var checkGo = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
            checkGo.transform.SetParent(itemGo.transform, false);
            StretchRect((RectTransform)checkGo.transform);
            var checkImage = checkGo.GetComponent<Image>();
            checkImage.color = new Color(0.55f, 0.8f, 1f, 1f);
            toggle.graphic = checkImage;

            // ---- 绑定 ----
            dd.itemText = BuildTemplateText("Item Label", itemGo.transform, "选项");
            dd.template = templateRt;
            templateGo.SetActive(false);
        }

        /// <summary>模板内文本（不带 LayoutElement，避免选项行被撑宽）</summary>
        private static Text BuildTemplateText(string name, Transform parent, string content)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = UiFont; txt.text = content; txt.fontSize = 16;
            txt.color = TextColor; txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            var rt = (RectTransform)go.transform;
            StretchRect(rt);
            rt.offsetMin = new Vector2(8, 0);
            rt.offsetMax = new Vector2(-8, 0);
            return txt;
        }

        /// <summary>拉伸填满父级</summary>
        private static void StretchRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static RectTransform CreatePanel(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = PanelColor;
            img.sprite = UITheme.RoundedPanelSprite;
            img.type = Image.Type.Sliced;
            return go.GetComponent<RectTransform>();
        }

        private static Text CreateText(string name, Transform parent, string content, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = UiFont; txt.text = content; txt.fontSize = fontSize;
            txt.color = TextColor; txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            var le = go.AddComponent<LayoutElement>(); le.minWidth = 120; le.preferredHeight = fontSize + 8;
            return txt;
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = UITheme.ButtonNormal;
            img.sprite = UITheme.RoundedButtonSprite;
            img.type = Image.Type.Sliced;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            UITheme.ApplyButtonTint(btn);
            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var txt = txtGo.AddComponent<Text>();
            txt.font = UiFont; txt.text = label; txt.fontSize = 16;
            txt.color = TextColor; txt.alignment = TextAnchor.MiddleCenter;
            txt.rectTransform.anchorMin = Vector2.zero; txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = Vector2.zero; txt.rectTransform.offsetMax = Vector2.zero;
            var le = go.AddComponent<LayoutElement>(); le.minWidth = 44; le.preferredWidth = 52; le.preferredHeight = 32;
            return btn;
        }

        private static void SetAnchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        /// <summary>反射给[SerializeField] private字段赋值</summary>
        private static void SetField(Object target, string fieldName, Object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) { field.SetValue(target, value); EditorUtility.SetDirty(target); }
            else Debug.LogWarning($"[CE菜单] {target.GetType().Name} 未找到私有字段 {fieldName}");
        }
    }
}
#endif
