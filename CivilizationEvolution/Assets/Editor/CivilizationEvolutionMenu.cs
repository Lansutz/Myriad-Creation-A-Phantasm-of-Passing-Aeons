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

        // 常用UI颜色
        private static readonly Color PanelColor = new Color(0.08f, 0.10f, 0.16f, 0.85f);
        private static readonly Color TextColor = Color.white;
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
            var editor = worldGo.AddComponent<MapEditor>();
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
            var dd = dropdownGo.AddComponent<Dropdown>();
            var ddLabel = CreateText("Label", dropdownGo.transform, "地形", 16);
            dd.captionText = ddLabel;
            var ddTemplate = new GameObject("Template", typeof(RectTransform));
            ddTemplate.transform.SetParent(dropdownGo.transform, false);
            dd.template = ddTemplate.AddComponent<RectTransform>();
            var itemText = CreateText("ItemText", ddTemplate.transform, "选项", 16);
            dd.itemText = itemText;
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
            var scroll = logPanel.AddComponent<ScrollRect>();
            var logText = CreateText("EventLogText", logPanel.transform, "", 16);
            SetAnchor(logText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            logText.verticalOverflow = VerticalWrapMode.Overflow;
            logText.alignment = TextAnchor.UpperLeft;
            scroll.content = logText.rectTransform;
            SetField(ui, "eventLogPanel", logPanel);
            SetField(ui, "eventLogText", logText);
            SetField(ui, "eventLogScroll", scroll);
        }

        private static RectTransform CreatePanel(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = PanelColor;
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
            var img = go.AddComponent<Image>(); img.color = new Color(0.2f, 0.3f, 0.5f, 1);
            var btn = go.AddComponent<Button>();
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
