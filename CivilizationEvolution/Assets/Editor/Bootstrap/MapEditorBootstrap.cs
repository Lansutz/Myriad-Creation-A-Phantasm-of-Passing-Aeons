using System;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;
using CivilizationEvolution.Render;
using CivilizationEvolution.UI;
using UnityEngine;
using UnityEngine.UI;
using MapEditor = CivilizationEvolution.Render.MapEditor;

namespace CivilizationEvolution.Bootstrap
{
 /// 地图编辑器场景引导脚本
 /// 挂在场景空物体上，Awake时自动组装：
 /// Canvas → 左侧工具面板(EditorUIPanel) + 右侧参数面板(MapGenerationPanel)
 /// GameWorld → MapRenderer → 相机
 /// 面板回调连接到GameWorld的GenerateTerrainWithConfig/CalculateClimate/RecalculateHydrology
    public class MapEditorBootstrap : MonoBehaviour
    {
        [Header("地图尺寸（运行时可被GenConfig覆盖）")]
        public int mapWidth = 256;
        public int mapHeight = 128;

        [Header("相机设置")]
        public float cameraSize = 60f;
        public Color backgroundColor = new Color(0.05f, 0.07f, 0.10f, 1f);

        [Header("启动模式")]
        [Tooltip("true=启动时全海空白地图（不自动生成地形）；false=自动生成默认地形")]
        public bool startWithEmptyOcean = false;

        [Header("加载存档")]
        [Tooltip("不为空时，启动后自动加载指定存档文件名（不含.json后缀）")]
        public string loadSaveFileName = null;

 // 运行时创建的引用
        private GameWorld _world;
        private MapRenderer _renderer;
        private SphericalMapRenderer _sphericalRenderer;
        private MapEditor _mapEditor;
        private EditorUIPanel _toolPanel;
        private MapGenerationPanel _genPanel;
        private Camera _camera;

        void Awake()
        {
            CreateCamera();
            CreateWorld();
            CreateRenderer();
            CreateSphericalRenderer();
            CreateMapEditor();
            CreateCanvasAndPanels();
            ConnectPanelCallbacks();

            Debug.Log("[MapEditorBootstrap] 地图编辑器场景组装完成");
        }

        void Start()
        {
            if (_world == null) return;

 // 加载存档模式：优先于其他启动模式
            if (!string.IsNullOrEmpty(loadSaveFileName))
            {
                LoadSavedMap(loadSaveFileName);
                return;
            }

            if (startWithEmptyOcean)
            {
 // 全海空白地图模式：初始化 tiles 为全海，不生成地形
                InitializeEmptyOcean();
                Debug.Log("[MapEditorBootstrap] 全海空白地图已初始化（等待玩家编辑/生成）");
            }
            else
            {
 // 正常模式：自动生成默认地图
                _world.GenerateTerrainWithConfig();
                _world.CalculateClimate();
                _renderer?.ForceRefresh();
                Debug.Log("[MapEditorBootstrap] 默认地图已生成");
            }
        }

 /// <summary>加载已保存的地图（地形+省份+聚落）</summary>
        private void LoadSavedMap(string fileName)
        {
            try
            {
                var saveSystem = new MapSaveSystem(_world, _renderer);
                bool ok = saveSystem.LoadMap(fileName);
                if (ok)
                {
                    _renderer?.ForceRefresh();
                    Debug.Log($"[MapEditorBootstrap] 存档已加载: {fileName}");
                }
                else
                {
                    Debug.LogError($"[MapEditorBootstrap] 存档加载失败: {fileName}，回退到全海空白地图");
                    InitializeEmptyOcean();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapEditorBootstrap] 存档加载异常: {e.Message}，回退到全海空白地图");
                InitializeEmptyOcean();
            }
        }

 /// <summary>初始化全海空白地图（所有地块为海洋，等待玩家编辑）</summary>
        private void InitializeEmptyOcean()
        {
            int total = mapWidth * mapHeight;
            _world.tiles = new TileData[total];
            for (int i = 0; i < total; i++)
            {
                _world.tiles[i] = new TileData
                {
                    tileIndex = i,
                    exists = true,
                    isLand = false,
                    isCoast = false,
                    elevation01 = 0f,
                    slopeDegree = 0f,
                    ownerRealmId = -1,
                    occupyingRealmId = -1,
                    provinceId = -1,
                    regionId = -1,
                    passable = true,
                    movementCost = 1f,
                    oceanTier = GameEnums.OceanTier.FarSea,
                    oceanDepth01 = 0.5f,
                    biome = GameEnums.BiomeType.AbyssalPlain,
                    populationBlocks = new System.Collections.Generic.List<PopulationBlock>(),
                    buildingLevels = new int[0],
                };
            }
            _renderer?.ForceRefresh();
        }

 // ===== 创建相机 =====
        private void CreateCamera()
        {
            var camGo = new GameObject("EditorCamera");
            _camera = camGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = cameraSize;
            _camera.backgroundColor = backgroundColor;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

 // 相机控制器（平移缩放）
            var controller = camGo.AddComponent<EditorCameraController>();
            controller.targetCamera = _camera;
        }

 // ===== 创建GameWorld =====
        private void CreateWorld()
        {
            var worldGo = new GameObject("GameWorld");
            _world = worldGo.AddComponent<GameWorld>();
            _world.mapWidth = mapWidth;
            _world.mapHeight = mapHeight;
 // GenConfig使用默认值，面板会绑定它
        }

 // ===== 创建MapRenderer =====
        private void CreateRenderer()
        {
            var rendererGo = new GameObject("MapRenderer");
            rendererGo.transform.SetParent(_world.transform, false);
            _renderer = rendererGo.AddComponent<MapRenderer>();
            _renderer.BindWorld(_world);
        }

 // ===== 创建SphericalMapRenderer（默认隐藏） =====
        private void CreateSphericalRenderer()
        {
            var sphereGo = new GameObject("SphericalMapRenderer");
            sphereGo.transform.SetParent(_world.transform, false);
            _sphericalRenderer = sphereGo.AddComponent<SphericalMapRenderer>();
            _sphericalRenderer.planarRenderer = _renderer;
            _sphericalRenderer.radius = 50f;
            sphereGo.SetActive(false); // 默认平面模式
        }

 // ===== 创建MapEditor（画笔工具，普通类非MonoBehaviour） =====
        private void CreateMapEditor()
        {
            _mapEditor = new MapEditor(_world, _renderer);
        }

 // ===== 创建Canvas和UI面板 =====
        private void CreateCanvasAndPanels()
        {
 // Canvas
            var canvasGo = new GameObject("EditorCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

 // EventSystem
            var eventGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

 // 左侧工具面板
            var toolPanelGo = new GameObject("ToolPanel");
            toolPanelGo.transform.SetParent(canvasGo.transform, false);
            _toolPanel = toolPanelGo.AddComponent<EditorUIPanel>();
            _toolPanel.Initialize(_renderer, _mapEditor);
            _toolPanel.ShowPanel();

 // 右侧参数面板
            var genPanelGo = new GameObject("GenPanel");
            genPanelGo.transform.SetParent(canvasGo.transform, false);
            _genPanel = genPanelGo.AddComponent<MapGenerationPanel>();
            _genPanel.Initialize(
                _world.GenConfig,
                () => { _world.GenerateTerrainWithConfig(); _renderer.ForceRefresh(); },
                () => { _world.CalculateClimate(); _renderer.ForceRefresh(); },
                () => { _world.RecalculateHydrology(); _renderer.ForceRefresh(); },
                mode =>
                {
                    bool isSpherical = (MapRenderer.MapProjectionMode)mode == MapRenderer.MapProjectionMode.Spherical;
                    _renderer.gameObject.SetActive(!isSpherical);
                    _sphericalRenderer.gameObject.SetActive(isSpherical);
                    if (isSpherical)
                    {
                        _sphericalRenderer.RefreshTexture();
                        _camera.orthographic = false; // 球形用透视相机
                        _camera.fieldOfView = 60f;
                        _camera.transform.position = new Vector3(0f, 0f, -120f);
                    }
                    else
                    {
                        _camera.orthographic = true; // 平面用正交相机
                        _camera.orthographicSize = cameraSize;
                        _camera.transform.position = new Vector3(0f, 0f, -10f);
                    }
                    _renderer.SetProjectionMode((MapRenderer.MapProjectionMode)mode);
                }
            );
            _genPanel.Show();

 // 右下角速度控制面板
            CreateSpeedControl(canvasGo.transform);
        }

 /// <summary>右下角速度控制UI（暂停/1x/2x/3x）</summary>
        private void CreateSpeedControl(Transform canvasParent)
        {
            var panelObj = new GameObject("SpeedControl", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var prt = panelObj.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(1f, 0f);
            prt.anchorMax = new Vector2(1f, 0f);
            prt.pivot = new Vector2(1f, 0f);
            prt.anchoredPosition = new Vector2(-20, 20);
            prt.sizeDelta = new Vector2(220, 44);
            panelObj.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.92f);
            panelObj.transform.SetParent(canvasParent, false);

            string[] speeds = { "暂停", "1x", "2x", "3x" };
            float[] speedValues = { 0f, 1f, 2f, 3f };
            float btnW = 50f, gap = 4f;
            float startX = -(speeds.Length * btnW + (speeds.Length - 1) * gap) * 0.5f + btnW * 0.5f;

            for (int i = 0; i < speeds.Length; i++)
            {
                var btnObj = new GameObject(speeds[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                var brt = btnObj.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.5f, 0.5f);
                brt.anchorMax = new Vector2(0.5f, 0.5f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.anchoredPosition = new Vector2(startX + i * (btnW + gap), 0);
                brt.sizeDelta = new Vector2(btnW, 32);
                btnObj.GetComponent<Image>().color = new Color(0.18f, 0.19f, 0.23f, 1f);
                var btn = btnObj.GetComponent<Button>();
                int idx = i;
                btn.onClick.AddListener(() =>
                {
                    var gm = CivilizationEvolution.Core.GameManager.Instance;
                    if (gm != null) gm.SetGameSpeed(speedValues[idx]);
                });

                var textObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
                var trt = textObj.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
                var tmp = textObj.GetComponent<TMPro.TextMeshProUGUI>();
                tmp.text = speeds[i];
                tmp.fontSize = 16;
                tmp.color = new Color(0.85f, 0.85f, 0.88f, 1f);
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                textObj.transform.SetParent(btnObj.transform, false);

                btnObj.AddComponent<UIButtonHover>();
                btnObj.transform.SetParent(panelObj.transform, false);
            }
        }

 // ===== 连接面板回调 =====
        private void ConnectPanelCallbacks()
        {
 // 面板回调已在Initialize时连接 // 这里可以添加额外的事件绑定
            Debug.Log("[MapEditorBootstrap] 面板回调已连接");
        }

 // ===== 公共访问器 =====
        public GameWorld World => _world;
        public MapRenderer Renderer => _renderer;
        public MapEditor MapEditor => _mapEditor;
        public EditorUIPanel ToolPanel => _toolPanel;
        public MapGenerationPanel GenPanel => _genPanel;
    }

 /// 编辑器相机控制器（平移+缩放）
 /// 右键拖拽平移，滚轮缩放
}
