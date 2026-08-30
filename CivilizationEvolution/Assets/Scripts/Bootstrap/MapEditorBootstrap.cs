using CivilizationEvolution.Core;
using CivilizationEvolution.Map;
using CivilizationEvolution.Render;
using CivilizationEvolution.UI;
using UnityEngine;
using UnityEngine.UI;
using MapEditor = CivilizationEvolution.Render.MapEditor;

namespace CivilizationEvolution.Setup
{
    /// <summary>
    /// 地图编辑器场景引导脚本
    /// 挂在场景空物体上，Awake时自动组装：
    ///   Canvas → 左侧工具面板(EditorUIPanel) + 右侧参数面板(MapGenerationPanel)
    ///   GameWorld → MapRenderer → 相机
    /// 面板回调连接到GameWorld的GenerateTerrainWithConfig/CalculateClimate/RecalculateHydrology
    /// </summary>
    public class MapEditorBootstrap : MonoBehaviour
    {
        [Header("地图尺寸（运行时可被GenConfig覆盖）")]
        public int mapWidth = 256;
        public int mapHeight = 128;

        [Header("相机设置")]
        public float cameraSize = 60f;
        public Color backgroundColor = new Color(0.05f, 0.07f, 0.10f, 1f);

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
            // 启动时自动生成一张默认地图
            if (_world != null)
            {
                _world.GenerateTerrainWithConfig();
                _world.CalculateClimate();
                _renderer?.ForceRefresh();
                Debug.Log("[MapEditorBootstrap] 默认地图已生成");
            }
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
        }

        // ===== 连接面板回调 =====
        private void ConnectPanelCallbacks()
        {
            // 面板回调已在Initialize时连接
            // 这里可以添加额外的事件绑定
            Debug.Log("[MapEditorBootstrap] 面板回调已连接");
        }

        // ===== 公共访问器 =====
        public GameWorld World => _world;
        public MapRenderer Renderer => _renderer;
        public MapEditor MapEditor => _mapEditor;
        public EditorUIPanel ToolPanel => _toolPanel;
        public MapGenerationPanel GenPanel => _genPanel;
    }

    /// <summary>
    /// 编辑器相机控制器（平移+缩放）
    /// 右键拖拽平移，滚轮缩放
    /// </summary>
    public class EditorCameraController : MonoBehaviour
    {
        public Camera targetCamera;
        public float panSpeed = 50f;
        public float zoomSpeed = 10f;
        public float minZoom = 10f;
        public float maxZoom = 200f;

        private Vector3 _lastMousePos;
        private bool _isPanning;

        void Update()
        {
            if (targetCamera == null) return;

            // 右键拖拽平移
            if (Input.GetMouseButtonDown(1))
            {
                _isPanning = true;
                _lastMousePos = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(1))
            {
                _isPanning = false;
            }
            if (_isPanning)
            {
                Vector3 delta = Input.mousePosition - _lastMousePos;
                Vector3 pan = new Vector3(-delta.x, -delta.y, 0f) * panSpeed * 0.01f;
                targetCamera.transform.Translate(pan, Space.Self);
                _lastMousePos = Input.mousePosition;
            }

            // 滚轮缩放
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                targetCamera.orthographicSize = Mathf.Clamp(
                    targetCamera.orthographicSize - scroll * zoomSpeed,
                    minZoom, maxZoom);
            }

            // WASD平移
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                Vector3 move = new Vector3(h, v, 0f) * panSpeed * Time.deltaTime;
                targetCamera.transform.Translate(move, Space.Self);
            }
        }
    }
}
