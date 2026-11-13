using System;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;






namespace MyriadCreation.Rendering
{
 /// 地图渲染器
 /// 用Mesh/Texture渲染六边形地块地图
 /// 支持多种显示模式：地形、气候、群系、政治、人口、经济
    public partial class MapRenderer : MonoBehaviour
    {
        [Header("渲染设置")]
        [SerializeField] private int mapWidth = 128;
        [SerializeField] private int mapHeight = 64;
        [SerializeField] private float hexSize = 1f;
        private float _cameraFitSize = 50f;   // 看全图基准（SetupMapDisplay 校准）
        private float _maxZoom = 150f;        // 最大缩出（看全图——动态）
        // 陆地高程归一化范围：地形生成器的陆地高程分布与旧调色板阈值不一致（实测 89% 陆地
        // 落在"山地/雪山"档，整张图发白），所以按当前世界陆地实际 min/max 做分段映射。
        private float _landElevMin = 0.05f;
        private float _landElevMax = 0.85f;
        [SerializeField] private MapDisplayMode displayMode = MapDisplayMode.Terrain;

        [Header("图层配置")]
        [Tooltip("地图图层配置：基础图层始终叠加，地图模式互斥单选，可选叠加层少量可开关")]
        [SerializeField] private MapLayerConfig _layerConfig = new MapLayerConfig();

        [Header("引用")]
        private IMapWorldView _worldView;
        private IMapSettlementView _settlementView;
        private IMapOverlayView _overlayView;
        private IMapRealmView _realmView;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Texture2D mapTexture;

 /// <summary>省界描边颜色（Terrain 模式下省区边界）</summary>
        [SerializeField] private Color provinceBorderColor = new Color(0.9f, 0.9f, 0.9f, 1f);

 // 地图纹理更新节流：世界数据每 1 秒才 Tick 一次，无需每帧（60fps）全量重绘 8192 像素
        private float _mapTextureTimer;
 // 纹理重绘间隔（优化1 秒 1 Tick——运行 1s 足够； // 编辑模式涂色用 0.2s 实时——模式切换/涂色走 force 即时）
        private const float MapTextureInterval = 1f;
        private const float MapTextureIntervalEdit = 0.2f;
        private bool _forceMapRefresh = true; // 首次/切换模式时立即重绘
        /// <summary>编辑器画笔控制器（运行时为 null；由 Editor 程序集注入）。</summary>
        private IMapEditController _mapEditor;
        public void SetMapEditController(IMapEditController controller) => _mapEditor = controller;
        private int _hoverTile = -1; // 鼠标悬停地块（画笔预览）

 // 像素数组缓存：避免每次重绘都 new Color[8192] 产生 GC
        private Color[] _pixelBuffer;
        private static readonly Color VoidColor = new Color(0.05f, 0.05f, 0.08f, 1f);

 /// <summary>省界判定：与任一邻域省份归属不同即为边界地块</summary>
        public bool IsProvinceBorder(int tileIndex)
        {
            return ViewIsProvinceBorder(tileIndex);
        }

 /// <summary>当前显示模式（只读）</summary>
        public MapDisplayMode DisplayMode => displayMode;

        /// <summary>B7：注入只读地图视图（App/GameManager 在 BindWorld 后调用）；null 时自动回退旧 GameWorld 读取。</summary>
        /// <summary>当前注入的只读地图视图（其它 Rendering 组件共用；未注入时为 null，GameManager 负责注入）。</summary>
        public IMapWorldView WorldView => _worldView;

        /// <summary>当前注入的只读聚落视图（SettlementIconLayer 用）。</summary>
        public IMapSettlementView SettlementView => _settlementView;

        public void SetWorldView(IMapWorldView view)
        {
            // 只记录只读视图，不改 mapWidth/mapHeight：
            // 地图尺寸/纹理/Mesh 的生命周期仍由 BindWorld/Start/Update 的 world 尺寸同步负责，
            // 否则在"视图先注入、世界稍后绑定"的时序下会出现纹理尺寸与像素缓冲不一致（SetPixels 越界）。
            _worldView = view;
            _settlementView = view as IMapSettlementView;
            _overlayView = view as IMapOverlayView;
            _realmView = view as IMapRealmView;
            var settlementIcons = GetComponent<SettlementIconLayer>();
            if (settlementIcons != null) settlementIcons.SetSettlementView(_settlementView);
            _forceMapRefresh = true;
        }



 // ===== 最小对外坐标/尺寸 API（聚落图标层等外部图层用；不改变既有行为） =====

 /// <summary>地图宽度（地块数，只读）</summary>
        public int MapWidth => mapWidth;
 /// <summary>地图高度（地块数，只读）</summary>
        public int MapHeight => mapHeight;
 /// <summary>单个地块的世界尺寸（相邻地块中心距在 x 上等于该值）</summary>
        public float HexSize => hexSize;
 /// <summary>地图平面在世界中的高度（z 方向）：mapHeight × hexSize × 0.75</summary>
        public float WorldHeight => Mathf.Max(1f, mapHeight * hexSize * 0.75f);
 /// <summary>地图相机（未启动/无相机时可能为 null）</summary>
        public Camera MapCamera => _mainCamera;

 /// <summary>地块索引 → 世界坐标（地块中心，地图平面 y=0）；非法索引返回地图物体位置</summary>
        public Vector3 TileToWorld(int tileIndex)
        {
            if (tileIndex < 0 || mapWidth <= 0 || mapHeight <= 0 || tileIndex >= mapWidth * mapHeight)
                return transform.position;
            int x = tileIndex % mapWidth;
            int y = tileIndex / mapWidth;
            var local = new Vector3((x + 0.5f) * hexSize, 0f, (y + 0.5f) * hexSize * 0.75f);
            return transform.TransformPoint(local);
        }



 // 颜色缓存
        private Color[] _terrainColors;
        private Color[] _climateColors;
        private Color[] _biomeColors;
        private Color[] _politicalColors;

 // ===== 双色空间政治地图（效忠树层级 vs 附庸朝贡，色相完全隔离）=====
        [Header("双色空间政治地图")]
        [Tooltip("查看政权ID（-1=玩家政权）；政治模式下以此为基准区分本土/附庸/外国")]
        [SerializeField] private int _dualSpaceViewRealmId = -1;
        [Tooltip("A色系：本国效忠树深度色阶（由深→浅，同色相蓝色系）")]
        [SerializeField] private Color[] _allegiancePalette = new Color[]
        {
            new Color(0.12f, 0.22f, 0.55f, 1f),  // 深度0：主圈（君主直辖）——最深
            new Color(0.22f, 0.40f, 0.75f, 1f),  // 深度1：直属大领主/一级行政
            new Color(0.40f, 0.60f, 0.88f, 1f),  // 深度2：次级封臣/二级行政区
            new Color(0.60f, 0.78f, 0.95f, 1f),  // 深度3：下层封臣/三级行政区
        };
        [Tooltip("B色：附庸/朝贡国专用色（灰青色——色相与A色系彻底隔离，哪怕亮度接近也一眼可辨）")]
        [SerializeField] private Color _vassalTributeColor = new Color(0.50f, 0.62f, 0.65f, 1f);
        [Tooltip("占领/争议条纹：45°斜线叠加层（不修改底色）")]
        [SerializeField] private Color _occupationStripeColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField] private int _occupationStripeSpacing = 4;  // 条纹间距（像素）

 // 相机控制
        private Camera _mainCamera;
        private Vector3 _cameraTarget;      // 注视点（地图平面上的点），相机位置由它推导
        private float _cameraDistance = 100f;
        private float _zoomLevel = 50f;
        private bool _isDragging = false;
        private Vector3 _lastMousePosition;

                void Start()
        {
 // 先从绑定视图/世界同步地图尺寸，避免纹理用默认 128×64 创建导致大地图不渲染
            if (TrySyncMapSizeFromView())
            {
                Debug.Log($"[MapRenderer] 从世界/视图同步地图尺寸：{mapWidth}×{mapHeight}");
            }
            InitializeRenderer();
            InitializeColors();
            _mainCamera = Camera.main;
            _forceMapRefresh = true; // 尺寸同步后强制重绘
 // 地图显示全链路（查漏补缺：Plane 缩放至地图世界尺寸+相机对准—— // 否则 10×10 默认 Plane+固定相机坐标=地图不可见）
            SetupMapDisplay();
 // 聚落锚点图标层：代码创建（场景零改动），随世界绑定时重建（B7 兼容壳）
            var settlementIcons = SettlementIconLayer.Ensure(this);
            if (settlementIcons != null)
            {
                settlementIcons.SetSettlementView(_settlementView);
            }
        }

                void Update()
        {
 // 尺寸漂移检测（查漏补缺：Awake 时 GameWorld 可能未初始化 // [mapWidth=0]——生成后 world.mapWidth>0——重新同步+显示就位）
            if (TrySyncMapSizeFromView())
            {
                InitializeRenderer();
                _forceMapRefresh = true;
                SetupMapDisplay();
 // 地图尺寸变化 → 聚落图标的世界坐标随之变化，重建图标层
                var settlementIcons = GetComponent<SettlementIconLayer>();
                if (settlementIcons != null) settlementIcons.Rebuild();
                Debug.Log($"[MapRenderer] 尺寸重同步：{mapWidth}×{mapHeight}——显示就位");
            }

 // 相机输入需要实时响应，不节流
            HandleCameraInput();
 // 地图编辑器鼠标处理（编辑模式下左键绘制）
            #if UNITY_EDITOR
            HandleEditorInput();
            #endif

 // 重绘节流：运行 1s[数据每秒才变]/编辑 0.2s[涂色实时]——force 即时
            _mapTextureTimer += Time.unscaledDeltaTime;
            #if UNITY_EDITOR
            bool editing = _mapEditor != null && _mapEditor.IsEditMode;
            #else
            bool editing = false;
            #endif
            float interval = editing ? MapTextureIntervalEdit : MapTextureInterval;
            if (_forceMapRefresh || _mapTextureTimer >= interval)
            {
                _mapTextureTimer = 0f;
                _forceMapRefresh = false;
                UpdateMapTexture();
            }
        }


 /// 相机位置边界限制（以注视点为基准——相机的 z 是"后退距离"，不能再按世界坐标 clamp）：
 /// 世界宽=mapWidth×hexSize——高=mapHeight×hexSize×0.75（y 轴密度系数）
        private void ClampCameraToMap()
        {
            if (_mainCamera == null) return;
            bool wrapX = ViewWrapX;
            bool wrapY = ViewWrapY;
            float worldW = Mathf.Max(1f, mapWidth * hexSize);
            float worldH = Mathf.Max(1f, mapHeight * hexSize * 0.75f);
            var target = _cameraTarget;
            if (wrapX)
                target.x = Mathf.Repeat(target.x, worldW);
            else
                target.x = Mathf.Clamp(target.x, 0f, worldW);
            if (wrapY)
                target.z = Mathf.Repeat(target.z, worldH);
            else
                target.z = Mathf.Clamp(target.z, 0f, worldH);
            _cameraTarget = target;
            ApplyCameraTransform();
        }

        /// <summary>由注视点 + 固定 45° 俯视几何推导相机位置（forward=(0,-0.707,0.707)）。</summary>
        private void ApplyCameraTransform()
        {
            if (_mainCamera == null) return;
            _mainCamera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            _mainCamera.transform.position = _cameraTarget + new Vector3(0f, _cameraDistance, -_cameraDistance);
        }

 /// <summary>编辑器鼠标输入处理（左键按下拖动绘制，悬停更新画笔预览）</summary>
        #if UNITY_EDITOR
        private void HandleEditorInput()
        {
            if (_mapEditor == null || !_mapEditor.IsEditMode) return;

            int tile = ScreenToTile(Input.mousePosition);
            _hoverTile = tile;

            if (Input.GetMouseButtonDown(0))
            {
                _mapEditor.OnPaintStart(tile);
            }
            else if (Input.GetMouseButton(0) && _mapEditor.IsPainting)
            {
                _mapEditor.OnPaintDrag(tile);
            }
            if (Input.GetMouseButtonUp(0))
            {
                _mapEditor.OnPaintEnd();
            }
        }
        #endif
 /// <summary>初始化渲染器</summary>
 /// 地图显示适配（全链路关键——学 FMS 场景搭建思路）：
 /// ① MapPlane 缩放至地图世界尺寸（hexSize=1：世界宽=mapWidth——
 /// 高=mapHeight×0.75——Plane 基元 10×10 需放大）并移到世界中心
 /// ② 相机对准地图中心（俯视 60°——orthoSize 看全图）
 /// ③ 缩放范围按地图尺寸动态（大图能看全——小图能拉近）
        private void SetupMapDisplay()
        {
            if (_mainCamera == null || transform == null) return;
            float worldW = Mathf.Max(1f, mapWidth * hexSize);
            float worldH = Mathf.Max(1f, mapHeight * hexSize * 0.75f);

 // ① 网格本身就在世界单位下生成（InitializeRenderer 用 mapWidth×hexSize 建 4 顶点平面，
 //    从原点铺到 (worldW, worldH)），所以这里**不能再按 Plane 基元 10×10 缩放**——
 //    之前同时缩放 + 平移，地图被放大 10 倍并偏到右下角（PlayMode 截图里只剩一条带）。
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;

 // ② 相机对准地图中心（俯视 45°）
 //
 // 相机欧拉 (45,0,0) 的 forward = (0,-0.707,0.707)，即"向 +z 方向俯冲"。
 // 要把地图中心放进画面正中，相机必须位于中心的 **(-z, +y)** 侧：
 //   cam = center + (0, d, -d)  →  forward 正好指向 center。
 // 旧实现把相机放在 center.z 上（甚至 +z 偏移），大图/小图都会直接看空（PlayMode 截图整幅纯背景）。
 // 正交视野：屏幕竖向对应世界 z 的投影（乘 sin45≈0.707），横向对应世界 x。
            float aspect = _mainCamera.aspect > 0f ? _mainCamera.aspect : 1.777f;
            var center = new Vector3(worldW * 0.5f, 0f, worldH * 0.5f);
            float projectedH = worldH * 0.7071f;
            float fitSize = Mathf.Max(projectedH * 0.5f, (worldW / aspect) * 0.5f) * 1.08f;
            _cameraFitSize = fitSize;
            _cameraTarget = center;

            _cameraDistance = Mathf.Max(worldH * 0.75f, 40f);
            _mainCamera.orthographic = true;
            _mainCamera.orthographicSize = fitSize;
            _zoomLevel = fitSize;
            _maxZoom = fitSize * 1.2f;
            ApplyCameraTransform();
            Debug.Log($"[MapRenderer] 地图显示就位：世界 {worldW:F0}×{worldH:F0}——相机居中 size={fitSize:F0}");
        }

        private void InitializeRenderer()
        {
 // 先取现有组件（MapPlane 基元自带 MeshFilter/MeshRenderer），没有再添加； // 对已存在组件重复 AddComponent 在 Unity6 会返回 null，导致后续空引用
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();

 // 创建地图纹理
            mapTexture = new Texture2D(mapWidth, mapHeight, TextureFormat.RGBA32, false);
            mapTexture.filterMode = FilterMode.Point;
 // 根据地图环绕模式设置纹理环绕：柱面/环面用Repeat实现左右连通视觉
            mapTexture.wrapMode = (ViewWrapMode != MapWrapMode.Flat)
                ? TextureWrapMode.Repeat
                : TextureWrapMode.Clamp;

 // 创建简单的平面Mesh
            var mesh = new Mesh();
            float width = mapWidth * hexSize;
            float height = mapHeight * hexSize * 0.75f;

            // 网格必须铺在 **XZ 平面**（y=0）：相机的 45° 俯视几何是围绕"地面平面"推导的。
            // 旧实现把地图建在 XY 平面（竖直广告牌），相机中心射线永远打不到网格中心。
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(0, 0, 0),
                new Vector3(width, 0, 0),
                new Vector3(0, 0, height),
                new Vector3(width, 0, height)
            };

            int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;

 // 设置材质：地图是"已烘焙好山体阴影/纸纹的地图纹理"，必须用 Unlit 显示原色。
 // 用 Standard 会被方向光/环境光再次照亮，实测把 (102,148,69) 的草绿冲成 (200,220,170) 的发白灰绿。
            var shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader);
            material.mainTexture = mapTexture;
            meshRenderer.material = material;

            Debug.Log("[MapRenderer] 渲染器初始化完成");
        }

 /// <summary>初始化颜色映射</summary>
        private void InitializeColors()
        {
 // 地形颜色（水域按绝对高程；陆地按"归一化高程"使用 0.45~1.0 段——见 GetTileColorRaw）
            _terrainColors = new Color[256];

            Color deepSea = new Color(0.05f, 0.11f, 0.24f);
            Color midSea = new Color(0.08f, 0.20f, 0.38f);
            Color shallowSea = new Color(0.15f, 0.34f, 0.52f);
            Color beach = new Color(0.82f, 0.77f, 0.57f);
            Color lowland = new Color(0.40f, 0.58f, 0.27f);   // 低地草甸
            Color grassland = new Color(0.50f, 0.62f, 0.29f); // 草原
            Color steppe = new Color(0.60f, 0.60f, 0.35f);    // 干草原
            Color hills = new Color(0.52f, 0.47f, 0.33f);     // 丘陵
            Color rock = new Color(0.55f, 0.52f, 0.48f);      // 山地岩石
            Color peak = new Color(0.93f, 0.94f, 0.96f);      // 雪线

            for (int i = 0; i < 256; i++)
            {
                float t = i / 255f;
                Color c;
                if (t < 0.30f)       c = Color.Lerp(deepSea, midSea, t / 0.30f);
                else if (t < 0.40f)  c = Color.Lerp(midSea, shallowSea, (t - 0.30f) / 0.10f);
                else if (t < 0.45f)  c = Color.Lerp(shallowSea, beach, (t - 0.40f) / 0.05f);
                else if (t < 0.56f)  c = Color.Lerp(lowland, grassland, (t - 0.45f) / 0.11f);
                else if (t < 0.68f)  c = Color.Lerp(grassland, steppe, (t - 0.56f) / 0.12f);
                else if (t < 0.80f)  c = Color.Lerp(steppe, hills, (t - 0.68f) / 0.12f);
                else if (t < 0.90f)  c = Color.Lerp(hills, rock, (t - 0.80f) / 0.10f);
                else                 c = Color.Lerp(rock, peak, (t - 0.90f) / 0.10f);
                _terrainColors[i] = c;
            }

 // 气候颜色（按温度）
            _climateColors = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                float t = i / 255f;
                if (t < 0.2f) // 极寒
                    _climateColors[i] = Color.Lerp(new Color(0.8f, 0.9f, 1f), new Color(0.6f, 0.8f, 1f), t / 0.2f);
                else if (t < 0.4f) // 寒冷
                    _climateColors[i] = Color.Lerp(new Color(0.6f, 0.8f, 1f), new Color(0.5f, 0.9f, 0.7f), (t - 0.2f) / 0.2f);
                else if (t < 0.6f) // 温和
                    _climateColors[i] = Color.Lerp(new Color(0.5f, 0.9f, 0.7f), new Color(0.9f, 0.9f, 0.4f), (t - 0.4f) / 0.2f);
                else if (t < 0.8f) // 温暖
                    _climateColors[i] = Color.Lerp(new Color(0.9f, 0.9f, 0.4f), new Color(1f, 0.6f, 0.2f), (t - 0.6f) / 0.2f);
                else // 炎热
                    _climateColors[i] = Color.Lerp(new Color(1f, 0.6f, 0.2f), new Color(0.8f, 0.2f, 0.1f), (t - 0.8f) / 0.2f);
            }

 // 群系颜色
            _biomeColors = new Color[Enum.GetValues(typeof(GameEnums.BiomeType)).Length];
 // ===== A系：低水沃野（农耕定居基座）=====
            _biomeColors[(int)GameEnums.BiomeType.AlluvialPlain] = new Color(0.55f, 0.7f, 0.35f);
            _biomeColors[(int)GameEnums.BiomeType.GreatRiverPlain] = new Color(0.5f, 0.68f, 0.32f);
            _biomeColors[(int)GameEnums.BiomeType.Delta] = new Color(0.45f, 0.65f, 0.4f);
            _biomeColors[(int)GameEnums.BiomeType.Interfluvial] = new Color(0.6f, 0.72f, 0.38f);
            _biomeColors[(int)GameEnums.BiomeType.WetMarshPlain] = new Color(0.4f, 0.55f, 0.45f);
            _biomeColors[(int)GameEnums.BiomeType.Swamp] = new Color(0.3f, 0.45f, 0.38f);
            _biomeColors[(int)GameEnums.BiomeType.SedimentaryBasin] = new Color(0.55f, 0.6f, 0.45f);
            _biomeColors[(int)GameEnums.BiomeType.PiedmontBasin] = new Color(0.58f, 0.65f, 0.42f);
            _biomeColors[(int)GameEnums.BiomeType.EnclosedBasin] = new Color(0.52f, 0.68f, 0.38f);
            _biomeColors[(int)GameEnums.BiomeType.InlandAridBasin] = new Color(0.7f, 0.65f, 0.45f);
            _biomeColors[(int)GameEnums.BiomeType.VolcanicAshPlain] = new Color(0.5f, 0.55f, 0.4f);
            _biomeColors[(int)GameEnums.BiomeType.PluvialFan] = new Color(0.6f, 0.68f, 0.45f);

 // ===== B系：高地硬骨（屏障、割据与海洋陆地）=====
            _biomeColors[(int)GameEnums.BiomeType.LoessPlateau] = new Color(0.65f, 0.58f, 0.4f);
            _biomeColors[(int)GameEnums.BiomeType.LoessKarst] = new Color(0.6f, 0.55f, 0.42f);
            _biomeColors[(int)GameEnums.BiomeType.FoldMountains] = new Color(0.5f, 0.45f, 0.4f);
            _biomeColors[(int)GameEnums.BiomeType.LowHills] = new Color(0.45f, 0.55f, 0.38f);
            _biomeColors[(int)GameEnums.BiomeType.HighMountains] = new Color(0.55f, 0.5f, 0.48f);
            _biomeColors[(int)GameEnums.BiomeType.BrokenPlateau] = new Color(0.52f, 0.48f, 0.42f);
            _biomeColors[(int)GameEnums.BiomeType.CoastalLowland] = new Color(0.5f, 0.65f, 0.5f);
            _biomeColors[(int)GameEnums.BiomeType.Fjord] = new Color(0.35f, 0.45f, 0.5f);
            _biomeColors[(int)GameEnums.BiomeType.KarstMountains] = new Color(0.48f, 0.45f, 0.42f);
            _biomeColors[(int)GameEnums.BiomeType.ErodedBadlands] = new Color(0.6f, 0.5f, 0.38f);
            _biomeColors[(int)GameEnums.BiomeType.VolcanicIslands] = new Color(0.4f, 0.35f, 0.3f);
            _biomeColors[(int)GameEnums.BiomeType.ContinentalIslands] = new Color(0.42f, 0.55f, 0.4f);
            _biomeColors[(int)GameEnums.BiomeType.CoralAtoll] = new Color(0.6f, 0.75f, 0.65f);
            _biomeColors[(int)GameEnums.BiomeType.ContinentalIslet] = new Color(0.45f, 0.58f, 0.42f);
            _biomeColors[(int)GameEnums.BiomeType.PlateauMarsh] = new Color(0.4f, 0.5f, 0.45f);
            _biomeColors[(int)GameEnums.BiomeType.CoastalSaltMarsh] = new Color(0.55f, 0.6f, 0.5f);
            _biomeColors[(int)GameEnums.BiomeType.ImpactCraterAtoll] = new Color(0.45f, 0.4f, 0.45f);
            _biomeColors[(int)GameEnums.BiomeType.VolcanicIslandArc] = new Color(0.42f, 0.38f, 0.35f);

 // ===== C系：极端覆盖与过渡（减速、通道与资源边界）=====
            _biomeColors[(int)GameEnums.BiomeType.IceSheet] = new Color(0.95f, 0.97f, 1f);
            _biomeColors[(int)GameEnums.BiomeType.MountainGlacier] = new Color(0.85f, 0.9f, 0.95f);
            _biomeColors[(int)GameEnums.BiomeType.Tundra] = new Color(0.7f, 0.75f, 0.7f);
            _biomeColors[(int)GameEnums.BiomeType.BorealForest] = new Color(0.3f, 0.45f, 0.3f);
            _biomeColors[(int)GameEnums.BiomeType.DeciduousForest] = new Color(0.35f, 0.6f, 0.35f);
            _biomeColors[(int)GameEnums.BiomeType.EvergreenForest] = new Color(0.25f, 0.5f, 0.3f);
            _biomeColors[(int)GameEnums.BiomeType.MonsoonForest] = new Color(0.3f, 0.55f, 0.35f);
            _biomeColors[(int)GameEnums.BiomeType.Savanna] = new Color(0.75f, 0.7f, 0.3f);
            _biomeColors[(int)GameEnums.BiomeType.TropicalRainforest] = new Color(0.15f, 0.45f, 0.2f);
            _biomeColors[(int)GameEnums.BiomeType.TropicalMonsoon] = new Color(0.25f, 0.55f, 0.25f);
            _biomeColors[(int)GameEnums.BiomeType.Mangrove] = new Color(0.3f, 0.5f, 0.4f);
            _biomeColors[(int)GameEnums.BiomeType.HotDesert] = new Color(0.85f, 0.75f, 0.45f);
            _biomeColors[(int)GameEnums.BiomeType.InlandDesert] = new Color(0.8f, 0.7f, 0.42f);
            _biomeColors[(int)GameEnums.BiomeType.ColdDesert] = new Color(0.75f, 0.72f, 0.68f);
            _biomeColors[(int)GameEnums.BiomeType.CoastalDesert] = new Color(0.78f, 0.72f, 0.5f);
            _biomeColors[(int)GameEnums.BiomeType.SemiAridShrubland] = new Color(0.7f, 0.65f, 0.4f);
            _biomeColors[(int)GameEnums.BiomeType.SaltDesert] = new Color(0.8f, 0.78f, 0.65f);
            _biomeColors[(int)GameEnums.BiomeType.DesertOasis] = new Color(0.45f, 0.6f, 0.4f);
            _biomeColors[(int)GameEnums.BiomeType.EndorheicLake] = new Color(0.4f, 0.55f, 0.65f);
            _biomeColors[(int)GameEnums.BiomeType.RiverSourceMarsh] = new Color(0.42f, 0.52f, 0.48f);
            _biomeColors[(int)GameEnums.BiomeType.AlpineMeadow] = new Color(0.55f, 0.65f, 0.5f);
            _biomeColors[(int)GameEnums.BiomeType.TemperateGrassland] = new Color(0.6f, 0.75f, 0.3f);
            _biomeColors[(int)GameEnums.BiomeType.GravelGobi] = new Color(0.65f, 0.62f, 0.55f);
            _biomeColors[(int)GameEnums.BiomeType.Yardang] = new Color(0.68f, 0.6f, 0.5f);
            _biomeColors[(int)GameEnums.BiomeType.LandBridgeIsthmus] = new Color(0.55f, 0.62f, 0.45f);

 // 政治颜色（随机生成）
            _politicalColors = new Color[16];
            for (int i = 0; i < 16; i++)
            {
                UnityEngine.Random.InitState(i * 1000);
                _politicalColors[i] = new Color(
                    UnityEngine.Random.Range(0.3f, 0.9f),
                    UnityEngine.Random.Range(0.3f, 0.9f),
                    UnityEngine.Random.Range(0.3f, 0.9f));
            }
        }

 /// <summary>确定性噪声（整数坐标 hash——纸纹颗粒——同 seed 复现）</summary>
        private static float HashNoise(int x, int y, int salt)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + salt * 2246822519);
                h = (h ^ (h >> 13)) * 1274126177;
                h = h ^ (h >> 16);
                return (h & 0xFFFF) / 65535f;
            }
        }

 // ===== 地图模式着色辅助（外交/联盟/文化/宗教） =====
        private static readonly Color WarColor = new Color(0.85f, 0.2f, 0.2f, 1f);
        private static readonly Color HostileColor = new Color(0.9f, 0.55f, 0.2f, 1f);
        private static readonly Color NeutralColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        private static readonly Color FriendlyColor = new Color(0.35f, 0.7f, 0.35f, 1f);
        private static readonly Color AllyColor = new Color(0.3f, 0.5f, 0.9f, 1f);
        private static readonly Color FactionColor = new Color(0.6f, 0.35f, 0.85f, 1f);

 /// <summary>主人口块的文化（count 最大块——地块主文化）</summary>
        private static int GetDominantBlockCulture(TileData tile)
        {
            if (tile.populationBlocks == null || tile.populationBlocks.Count == 0) return -1;
            var best = tile.populationBlocks[0];
            foreach (var pb in tile.populationBlocks)
                if (pb.count > best.count) best = pb;
            return best.cultureId;
        }

 /// <summary>主人口块的信仰（count 最大块——地块主信仰）</summary>
        private static int GetDominantBlockFaith(TileData tile)
        {
            if (tile.populationBlocks == null || tile.populationBlocks.Count == 0) return -1;
            var best = tile.populationBlocks[0];
            foreach (var pb in tile.populationBlocks)
                if (pb.count > best.count) best = pb;
            return best.faithId;
        }



 /// <summary>相机输入处理</summary>
        private void HandleCameraInput()
        {
            if (_mainCamera == null) return;

 // 缩放
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
 // 动态范围（SetupMapDisplay 校准——看全图↔贴近地块）
                float minZoom = 3f;
                float maxZoom = _maxZoom > 0f ? _maxZoom : 150f;
                _zoomLevel = Mathf.Clamp(_zoomLevel - scroll * 20f, minZoom, maxZoom);
                _mainCamera.orthographicSize = _zoomLevel;
            }

 // 拖拽平移
            if (Input.GetMouseButtonDown(2) || Input.GetMouseButtonDown(1))
            {
                _isDragging = true;
                _lastMousePosition = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(2) || Input.GetMouseButtonUp(1))
            {
                _isDragging = false;
            }

            if (_isDragging)
            {
                Vector3 delta = Input.mousePosition - _lastMousePosition;
                _lastMousePosition = Input.mousePosition;

                float moveSpeed = _zoomLevel / 50f;
                // 移动的是"注视点"：屏幕右拖 = 视线向左扫（地图内容右移）
                _cameraTarget -= new Vector3(delta.x * moveSpeed * 0.01f, 0f, delta.y * moveSpeed * 0.01f);
            }

 // WASD移动
            float moveX = Input.GetAxis("Horizontal") * _zoomLevel * 0.02f;
            float moveZ = Input.GetAxis("Vertical") * _zoomLevel * 0.02f;
            _cameraTarget += new Vector3(moveX, 0f, moveZ);

 // 相机边界（查漏补缺：拖出地图范围=黑屏——clamp 到地图世界范围—— // wrapX/Y 环绕地图不限制对应轴[环行视觉连续]）
            ClampCameraToMap();
        }

 /// <summary>切换显示模式</summary>
                public void SetDisplayMode(MapDisplayMode mode)
        {
            displayMode = mode;
            _forceMapRefresh = true; // 切换显示模式立即重绘
            Debug.Log($"[MapRenderer] 切换显示模式：{mode}");
        }

 /// <summary>图层配置（公开访问，UI可直接修改开关后调用ForceRefresh）</summary>
        public MapLayerConfig LayerConfig => _layerConfig;

 /// <summary>切换可选叠加层</summary>
        public void ToggleOverlay(MapOverlayLayer layer, bool enabled)
        {
            _layerConfig.ToggleOverlay(layer, enabled);
            _forceMapRefresh = true;
        }

 /// <summary>检查可选叠加层是否启用</summary>
        public bool IsOverlayEnabled(MapOverlayLayer layer) =>
            _layerConfig.IsOverlayEnabled(layer);

 /// <summary>强制刷新地图纹理（编辑器绘制后调用）</summary>
 /// <summary>当前投影模式</summary>
        private MapProjectionMode _projection = MapProjectionMode.Planar;

 /// <summary>设置地图投影模式（平面/球形）</summary>
        public void SetProjectionMode(MapProjectionMode mode)
        {
            _projection = mode;
            Debug.Log($"[MapRenderer] 投影模式切换为: {mode}");
 // 球形投影的具体渲染实现待完善（球面UV映射+经纬度坐标转换）
            ForceRefresh();
        }

 /// <summary>地图投影模式</summary>
        public enum MapProjectionMode
        {
            Planar,    // 平面地图（柱状投影，左右连通）
            Spherical  // 球形地图（3D球面投影）
        }

 /// <summary>当前鼠标悬停地块（画笔预览用）</summary>
        public int HoverTile => _hoverTile;
 /// <summary>屏幕坐标转地块索引（支持左右连通环绕）</summary>
        public int ScreenToTile(Vector3 screenPos)
        {
            if (_mainCamera == null) return -1;

            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                int x = Mathf.FloorToInt(hitPoint.x / hexSize);
                int y = Mathf.FloorToInt(hitPoint.z / (hexSize * 0.75f));

 // 左右连通环绕
                bool wrapX = ViewWrapX;
                if (wrapX) x = TileGrid.WrapX(x, mapWidth);
                else if (x < 0 || x >= mapWidth) return -1;

                bool wrapY = ViewWrapY;
                if (wrapY) y = TileGrid.WrapY(y, mapHeight);
                else if (y < 0 || y >= mapHeight) return -1;

                int index = y * mapWidth + x;
                if (!TryGetTileView(index, out TileData tile) || !tile.exists) return -1;
                return index;
            }
            return -1;
        }
        /// <summary>B7 兼容壳：旧 API 保留（GameManager 已改用 SetWorldView；该入口不再从 GameWorld 取数据）。</summary>
        [System.Obsolete("B7: 使用 SetWorldView(IMapWorldView)；MapRenderer 不再引用 GameWorld。")]
        public void BindWorld(object target)
        {
        }

        // ===== B7：view-only helpers（替代 WorldCompat 过渡实现）=====

        private bool TryGetTileView(int tileIndex, out TileData tile)
        {
            if (_worldView != null)
            {
                return _worldView.TryGetTile(tileIndex, out tile);
            }
            tile = default;
            return false;
        }

        private int ViewPlayerRealmId => _worldView != null ? _worldView.PlayerRealmId : -1;

        private bool ViewWrapX => _worldView != null && _worldView.WrapX;

        private bool ViewWrapY => _worldView != null && _worldView.WrapY;

        private bool HasDiplomacyManagerView => _worldView != null;

        private int ViewMapWidth => _worldView != null ? _worldView.MapWidth : mapWidth;

        private int ViewMapHeight => _worldView != null ? _worldView.MapHeight : mapHeight;

        private MapWrapMode ViewWrapMode => _worldView != null ? _worldView.WrapMode : MapWrapMode.Flat;

        private bool ViewIsProvinceBorder(int tileIndex)
        {
            return _worldView != null && _worldView.IsProvinceBorder(tileIndex);
        }

        private bool TrySyncMapSizeFromView()
        {
            if (_worldView == null)
            {
                return false;
            }
            int width = _worldView.MapWidth;
            int height = _worldView.MapHeight;
            if (width <= 0 || height <= 0 || (width == mapWidth && height == mapHeight))
            {
                return false;
            }
            mapWidth = width;
            mapHeight = height;
            return true;
        }

        private IMapSettlementView CurrentSettlementView => _settlementView;

        private IMapOverlayView CurrentOverlayView => _overlayView;

        private IMapRealmView CurrentRealmView => _realmView;

        private bool TryGetCultureView(int cultureId, out MapCultureView culture)
        {
            if (_worldView == null)
            {
                culture = null;
                return false;
            }
            return _worldView.TryGetCulture(cultureId, out culture);
        }

        private bool TryGetDiplomacyView(int viewerRealmId, int otherRealmId, out MapDiplomacyView view)
        {
            if (_worldView == null)
            {
                view = null;
                return false;
            }
            return _worldView.TryGetDiplomacyRelation(viewerRealmId, otherRealmId, out view);
        }

        private bool IsSubordinateView(int viewerRealmId, int otherRealmId)
        {
            return _worldView != null && _worldView.IsSubordinate(viewerRealmId, otherRealmId);
        }

    }

    public enum MapDisplayMode
    {
        Terrain,        // 地形
        Climate,        // 气候
        Biome,          // 群系
        Political,      // 政治（常态）
        Population,     // 人口
        Diplomacy,      // 外交关系（玩家视角：战争/敌对/中立/友好/盟约）
        Alliance,       // 联盟阵营（普通盟友/阵营成员）
        Culture,        // 文化（主文化）
        CultureBranch,  // 文化分支（允许分支的文化显示子文化）
        Religion,       // 宗教（根宗教）
        ReligionSuccession, // 宗教-教统（组织性单位）
        ReligionTradition // 宗教传统（礼拜仪轨/教义学派）
    }
}
