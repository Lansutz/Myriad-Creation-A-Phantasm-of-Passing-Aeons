using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Diplomacy;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;

namespace CivilizationEvolution.Render
{
    /// <summary>
    /// 地图渲染器
    /// 用Mesh/Texture渲染六边形地块地图
    /// 支持多种显示模式：地形、气候、群系、政治、人口、经济
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        [Header("渲染设置")]
        [SerializeField] private int mapWidth = 128;
        [SerializeField] private int mapHeight = 64;
        [SerializeField] private float hexSize = 1f;
        private float _cameraFitSize = 50f;   // 看全图基准（SetupMapDisplay 校准）
        private float _maxZoom = 150f;        // 最大缩出（看全图——动态）
        [SerializeField] private MapDisplayMode displayMode = MapDisplayMode.Terrain;

        [Header("引用")]
        [SerializeField] private GameWorld world;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Texture2D mapTexture;

        /// <summary>省界描边颜色（Terrain 模式下省区边界）</summary>
        [SerializeField] private Color provinceBorderColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        // 地图纹理更新节流：世界数据每 1 秒才 Tick 一次，无需每帧（60fps）全量重绘 8192 像素
        private float _mapTextureTimer;
        // 纹理重绘间隔（优化 2026-09-04：世界 1 秒 1 Tick——运行 1s 足够；
        // 编辑模式涂色用 0.2s 实时——模式切换/涂色走 force 即时）
        private const float MapTextureInterval = 1f;
        private const float MapTextureIntervalEdit = 0.2f;
        private bool _forceMapRefresh = true; // 首次/切换模式时立即重绘
        // 地图编辑器
        private MapEditor _mapEditor;
        private int _hoverTile = -1; // 鼠标悬停地块（画笔预览）

        // 像素数组缓存：避免每次重绘都 new Color[8192] 产生 GC
        private Color[] _pixelBuffer;
        private static readonly Color VoidColor = new Color(0.05f, 0.05f, 0.08f, 1f);

        /// <summary>省界判定：与任一邻域省份归属不同即为边界地块</summary>
        public bool IsProvinceBorder(int tileIndex)
        {
            return Province.IsBorder(world.tiles, mapWidth, mapHeight, tileIndex);
        }

        /// <summary>当前显示模式（只读）</summary>
        public MapDisplayMode DisplayMode => displayMode;
        /// <summary>当前世界引用（只读）</summary>
        public GameWorld World => world;

        /// <summary>绑定世界并同步地图尺寸</summary>
        public void BindWorld(GameWorld target)
        {
            world = target;
            if (target != null)
            {
                mapWidth = target.mapWidth;
                mapHeight = target.mapHeight;
            }
        }

        // 颜色缓存
        private Color[] _terrainColors;
        private Color[] _climateColors;
        private Color[] _biomeColors;
        private Color[] _politicalColors;

        // 相机控制
        private Camera _mainCamera;
        private Vector3 _cameraTarget;
        private float _zoomLevel = 50f;
        private bool _isDragging = false;
        private Vector3 _lastMousePosition;

                void Start()
        {
            // 先从绑定的世界同步地图尺寸，避免纹理用默认 128×64 创建导致大地图不渲染
            if (world != null)
            {
                mapWidth = world.mapWidth;
                mapHeight = world.mapHeight;
                Debug.Log($"[MapRenderer] 从世界同步地图尺寸：{mapWidth}×{mapHeight}");
            }
            InitializeRenderer();
            InitializeColors();
            _mainCamera = Camera.main;
            _mapEditor = new MapEditor(world, this);
            _forceMapRefresh = true; // 尺寸同步后强制重绘
            // 地图显示全链路（查漏补缺：Plane 缩放至地图世界尺寸+相机对准——
            // 否则 10×10 默认 Plane+固定相机坐标=地图不可见）
            SetupMapDisplay();
        }

                void Update()
        {
            // 尺寸漂移检测（查漏补缺：Awake 时 GameWorld 可能未初始化
            // [mapWidth=0]——生成后 world.mapWidth>0——重新同步+显示就位）
            if (world != null && world.mapWidth > 0 && world.mapWidth != mapWidth)
            {
                mapWidth = world.mapWidth;
                mapHeight = world.mapHeight;
                InitializeRenderer();
                _forceMapRefresh = true;
                SetupMapDisplay();
                Debug.Log($"[MapRenderer] 尺寸重同步：{mapWidth}×{mapHeight}——显示就位");
            }

            // 相机输入需要实时响应，不节流
            HandleCameraInput();
            // 地图编辑器鼠标处理（编辑模式下左键绘制）
            HandleEditorInput();

            // 重绘节流：运行 1s[数据每秒才变]/编辑 0.2s[涂色实时]——force 即时
            _mapTextureTimer += Time.unscaledDeltaTime;
            bool editing = _mapEditor != null && _mapEditor.IsEditMode;
            float interval = editing ? MapTextureIntervalEdit : MapTextureInterval;
            if (_forceMapRefresh || _mapTextureTimer >= interval)
            {
                _mapTextureTimer = 0f;
                _forceMapRefresh = false;
                UpdateMapTexture();
            }
        }


        /// <summary>
        /// 相机位置边界限制（地图世界范围——wrap 轴不限制）：
        /// 世界宽=mapWidth×hexSize——高=mapHeight×hexSize×0.75（y 轴密度系数）
        /// </summary>
        private void ClampCameraToMap()
        {
            if (_mainCamera == null) return;
            bool wrapX = world != null && world.config.wrapX;
            bool wrapY = world != null && world.config.wrapY;
            var pos = _mainCamera.transform.position;
            float worldW = mapWidth * hexSize;
            float worldH = mapHeight * hexSize * 0.75f;
            float viewHalfW = _zoomLevel;   // 正交半宽≈zoom（保守）
            float viewHalfH = _zoomLevel;
            if (!wrapX)
                pos.x = Mathf.Clamp(pos.x, viewHalfW * 0.5f, worldW - viewHalfW * 0.5f);
            if (!wrapY)
                pos.z = Mathf.Clamp(pos.z, viewHalfH * 0.5f, worldH - viewHalfH * 0.5f);
            _mainCamera.transform.position = pos;
        }

        /// <summary>编辑器鼠标输入处理（左键按下拖动绘制，悬停更新画笔预览）</summary>
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
        /// <summary>初始化渲染器</summary>
        /// <summary>
        /// 地图显示适配（全链路关键——学 FMS 场景搭建思路）：
        /// ① MapPlane 缩放至地图世界尺寸（hexSize=1：世界宽=mapWidth——
        ///    高=mapHeight×0.75——Plane 基元 10×10 需放大）并移到世界中心
        /// ② 相机对准地图中心（俯视 60°——orthoSize 看全图）
        /// ③ 缩放范围按地图尺寸动态（大图能看全——小图能拉近）
        /// </summary>
        private void SetupMapDisplay()
        {
            if (_mainCamera == null || transform == null) return;
            float worldW = Mathf.Max(1f, mapWidth * hexSize);
            float worldH = Mathf.Max(1f, mapHeight * hexSize * 0.75f);

            // ① Plane 缩放（基元默认 10×10——UV 跟随拉伸）+ 中心对齐
            transform.localScale = new Vector3(worldW / 10f, 1f, worldH / 10f);
            transform.localPosition = new Vector3(worldW * 0.5f, 0f, worldH * 0.5f);

            // ② 相机对准地图中心（俯视——地图满屏占位：
            // fit=取宽/高适配较小者——地图充满视野不留白——CK3 式开局满屏）
            float aspect = _mainCamera.aspect > 0f ? _mainCamera.aspect : 1.777f;
            float fitSize = Mathf.Min(worldW / aspect, worldH) * 0.52f;
            _cameraFitSize = fitSize;
            _mainCamera.transform.position = new Vector3(worldW * 0.5f,
                Mathf.Max(80f, worldH * 0.9f), worldH * 0.5f + fitSize * 0.4f);
            _mainCamera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            _mainCamera.orthographic = true;
            _mainCamera.orthographicSize = fitSize;
            // 视野后移补偿（60° 俯视——看全图边缘不裁）
            _mainCamera.transform.position = new Vector3(worldW * 0.5f,
                Mathf.Max(80f, worldH * 0.9f), worldH * 0.5f);
            _zoomLevel = fitSize;
            _maxZoom = fitSize * 1.2f;
            Debug.Log($"[MapRenderer] 地图显示就位：世界 {worldW:F0}×{worldH:F0}——相机居中 size={fitSize:F0}");
        }

        private void InitializeRenderer()
        {
            // 先取现有组件（MapPlane 基元自带 MeshFilter/MeshRenderer），没有再添加；
            // 对已存在组件重复 AddComponent 在 Unity6 会返回 null，导致后续空引用
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
            mapTexture.wrapMode = (world != null && world.wrapMode != MapWrapMode.Flat)
                ? TextureWrapMode.Repeat
                : TextureWrapMode.Clamp;

            // 创建简单的平面Mesh
            var mesh = new Mesh();
            float width = mapWidth * hexSize;
            float height = mapHeight * hexSize * 0.75f;

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(0, 0, 0),
                new Vector3(width, 0, 0),
                new Vector3(0, height, 0),
                new Vector3(width, height, 0)
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

            // 设置材质
            var material = new Material(Shader.Find("Standard"));
            material.mainTexture = mapTexture;
            meshRenderer.material = material;

            Debug.Log("[MapRenderer] 渲染器初始化完成");
        }

        /// <summary>初始化颜色映射</summary>
        private void InitializeColors()
        {
            // 地形颜色（按高程）
            _terrainColors = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                float t = i / 255f;
                if (t < 0.3f) // 深海
                    _terrainColors[i] = Color.Lerp(new Color(0.05f, 0.1f, 0.3f), new Color(0.1f, 0.2f, 0.5f), t / 0.3f);
                else if (t < 0.4f) // 浅海
                    _terrainColors[i] = Color.Lerp(new Color(0.1f, 0.2f, 0.5f), new Color(0.2f, 0.4f, 0.7f), (t - 0.3f) / 0.1f);
                else if (t < 0.45f) // 海岸
                    _terrainColors[i] = new Color(0.9f, 0.85f, 0.6f);
                else if (t < 0.55f) // 平原
                    _terrainColors[i] = Color.Lerp(new Color(0.5f, 0.7f, 0.3f), new Color(0.4f, 0.6f, 0.2f), (t - 0.45f) / 0.1f);
                else if (t < 0.7f) // 丘陵
                    _terrainColors[i] = Color.Lerp(new Color(0.4f, 0.5f, 0.2f), new Color(0.5f, 0.45f, 0.3f), (t - 0.55f) / 0.15f);
                else if (t < 0.85f) // 山地
                    _terrainColors[i] = Color.Lerp(new Color(0.5f, 0.45f, 0.3f), new Color(0.6f, 0.55f, 0.5f), (t - 0.7f) / 0.15f);
                else // 雪山
                    _terrainColors[i] = Color.Lerp(new Color(0.6f, 0.55f, 0.5f), Color.white, (t - 0.85f) / 0.15f);
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

        /// <summary>更新地图纹理</summary>
                private void UpdateMapTexture()
        {
            if (world == null || world.tiles == null) return;
            if (world.tiles.Length != mapWidth * mapHeight) return;

            // 复用像素缓冲区，仅在尺寸变化时重新分配
            int pixelCount = mapWidth * mapHeight;
            if (_pixelBuffer == null || _pixelBuffer.Length != pixelCount)
                _pixelBuffer = new Color[pixelCount];

            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (!world.tiles[i].exists)
                {
                    _pixelBuffer[i] = VoidColor; // 虚空/地图外：深色
                    continue;
                }
                _pixelBuffer[i] = GetTileColor(i);
            }

            // ===== 子地块/Burg 标记绘制（在地形纹理上叠加彩色像素点）=====
            if (world.burgs != null && world.burgs.Count > 0)
            {
                foreach (var burg in world.burgs.Values)
                {
                    // 村庄太小不显示，只显示主要定居点
                    if (!burg.IsMajorSettlement) continue;
                    if (burg.tileIndex < 0 || burg.tileIndex >= pixelCount) continue;

                    Color burgColor = burg.type switch
                    {
                        BurgType.Capital => new Color(1f, 0.85f, 0.2f, 1f),   // 金色：首都
                        BurgType.City => new Color(1f, 1f, 1f, 1f),              // 白色：城市
                        BurgType.Port => new Color(0.3f, 0.6f, 1f, 1f),          // 蓝色：港口
                        BurgType.Fortress => new Color(0.9f, 0.3f, 0.2f, 1f),    // 红色：要塞
                        BurgType.Town => new Color(0.9f, 0.8f, 0.4f, 1f),        // 黄色：集镇
                        _ => new Color(0.6f, 0.6f, 0.6f, 1f)                      // 灰色
                    };

                    // 在Burg所在地块画 2x2 像素标记（大地图画3x3）
                    int bx = burg.tileIndex % mapWidth;
                    int by = burg.tileIndex / mapWidth;
                    int markSize = mapWidth > 512 ? 2 : 1;
                    for (int dy = 0; dy < markSize; dy++)
                    {
                        for (int dx = 0; dx < markSize; dx++)
                        {
                            int px = bx + dx;
                            int py = by + dy;
                            if (px >= 0 && px < mapWidth && py >= 0 && py < mapHeight)
                            {
                                int pi = py * mapWidth + px;
                                _pixelBuffer[pi] = burgColor;
                            }
                        }
                    }
                }
            }

            // 编辑器画笔预览（编辑模式下高亮鼠标悬停的画笔范围）
            if (_mapEditor != null && _mapEditor.IsEditMode && _hoverTile >= 0 && _mapEditor.CurrentTool != EditorTool.None)
            {
                var brushTiles = _mapEditor.GetBrushTileIndices(_hoverTile);
                Color previewColor = new Color(1f, 1f, 1f, 0.4f); // 半透明白色预览
                foreach (int bt in brushTiles)
                {
                    if (bt >= 0 && bt < pixelCount)
                    {
                        _pixelBuffer[bt] = Color.Lerp(_pixelBuffer[bt], previewColor, 0.5f);
                    }
                }
            }
            mapTexture.SetPixels(_pixelBuffer);

            mapTexture.Apply();
        }

        /// <summary>
        /// 地块颜色（大陆底图渲染——2026-09 用户定稿方向：大陆有纸纹/笔触
        /// 质感[参照《地图上发生的事》terrain 分色+纸感]）：
        /// 基色 + 确定性纸纹调制——低频斑驳[地形性色带]+高频颗粒[笔触感]——
        /// 海洋保持平滑（水无纸纹）——modes 也带纸感[政权色如彩绘]
        /// </summary>
        private Color GetTileColor(int tileIndex)
        {
            ref TileData tile = ref world.tiles[tileIndex];
            Color c = GetTileColorRaw(tileIndex);

            // 纸纹调制（确定性——同 seed 同画面——hash 高频颗粒 + 低频斑驳）
            if (tile.exists && tile.isLand)
            {
                int x = tileIndex % mapWidth;
                int y = tileIndex / mapWidth;
                float h1 = HashNoise(x, y, 0);         // 高频颗粒（笔触）
                float h2 = HashNoise(x / 4, y / 4, 1); // 低频斑驳（色带）
                float grain = (h1 - 0.5f) * 0.05f + (h2 - 0.5f) * 0.08f;
                c.r = Mathf.Clamp01(c.r + grain);
                c.g = Mathf.Clamp01(c.g + grain);
                c.b = Mathf.Clamp01(c.b + grain * 0.7f);
            }
            return c;
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

        /// <summary>地块基色（模式分派——biome/政权/人口/宗教……）</summary>
        private Color GetTileColorRaw(int tileIndex)
        {
            ref TileData tile = ref world.tiles[tileIndex];

            switch (displayMode)
            {
                case MapDisplayMode.Terrain:
                    // 河流优先着色（水系蓝）
                    if (tile.isRiver) return new Color(0.25f, 0.45f, 0.85f, 1f);
                    // 省界描边（与任一邻域省份不同 → 边界色）
                    if (IsProvinceBorder(tileIndex))
                        return provinceBorderColor;
                    int terrainIndex = Mathf.Clamp(Mathf.RoundToInt((tile.elevation01 + 1f) / 2f * 255f), 0, 255);
                    return _terrainColors[terrainIndex];

                case MapDisplayMode.Climate:
                    int climateIndex = Mathf.Clamp(Mathf.RoundToInt((tile.annualTemp + 55f) / 90f * 255f), 0, 255);
                    return _climateColors[climateIndex];

                case MapDisplayMode.Biome:
                    int biomeIndex = (int)tile.biome;
                    if (biomeIndex >= 0 && biomeIndex < _biomeColors.Length)
                        return _biomeColors[biomeIndex];
                    return Color.gray;

                case MapDisplayMode.Political:
                    if (tile.ownerRealmId >= 0 && tile.ownerRealmId < 16)
                        return _politicalColors[tile.ownerRealmId];
                    return new Color(0.3f, 0.3f, 0.3f);

                case MapDisplayMode.Population:
                    float pop = 0f;
                    if (tile.populationBlocks != null)
                        foreach (var pb in tile.populationBlocks)
                            pop += pb.count;
                    float popT = Mathf.Clamp(pop / 100f, 0f, 1f);
                    return Color.Lerp(new Color(0.9f, 0.9f, 0.9f), new Color(0.8f, 0.2f, 0.2f), popT);

                case MapDisplayMode.Economy:
                    float devT = Mathf.Clamp(tile.development, 0f, 1f);
                    return Color.Lerp(new Color(0.5f, 0.5f, 0.5f), new Color(1f, 0.9f, 0.3f), devT);

                case MapDisplayMode.Diplomacy:
                    return GetDiplomacyColor(tile);

                case MapDisplayMode.Alliance:
                    return GetAllianceColor(tile);

                case MapDisplayMode.Culture:
                    return GetCultureColor(tile, false);

                case MapDisplayMode.CultureBranch:
                    return GetCultureColor(tile, true);

                case MapDisplayMode.Religion:
                    return GetReligionColor(tile, ReligionMapLevel.Religion);

                case MapDisplayMode.ReligionSuccession:
                    return GetReligionColor(tile, ReligionMapLevel.Succession);

                case MapDisplayMode.ReligionTradition:
                    return GetReligionColor(tile, ReligionMapLevel.Tradition);

                default:
                    return Color.gray;
            }
        }

        // ===== 地图模式着色辅助（外交/联盟/文化/宗教） =====

        private static readonly Color WarColor = new Color(0.85f, 0.2f, 0.2f, 1f);
        private static readonly Color HostileColor = new Color(0.9f, 0.55f, 0.2f, 1f);
        private static readonly Color NeutralColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        private static readonly Color FriendlyColor = new Color(0.35f, 0.7f, 0.35f, 1f);
        private static readonly Color AllyColor = new Color(0.3f, 0.5f, 0.9f, 1f);
        private static readonly Color FactionColor = new Color(0.6f, 0.35f, 0.85f, 1f);

        /// <summary>外交关系色（玩家视角：战争/敌对/盟约/友好/中立）</summary>
        private Color GetDiplomacyColor(TileData tile)
        {
            int owner = tile.ownerRealmId;
            if (owner < 0) return new Color(0.25f, 0.25f, 0.25f);
            int player = world != null ? world.PlayerRealmId : -1;
            if (player < 0) return _politicalColors[owner % _politicalColors.Length];

            var dm = world.GetDiplomacyManager();
            if (dm == null) return _politicalColors[owner % _politicalColors.Length];
            if (owner == player) return FriendlyColor;

            var rel = dm.GetRelation(player, owner);
            if (rel == null) return NeutralColor;
            if (rel.isAtWar) return WarColor;
            if (rel.IsHostile) return HostileColor;
            if (rel.activeAlliances.Exists(a => a.isActive)) return AllyColor;
            return rel.relation >= 20f ? FriendlyColor : NeutralColor;
        }

        /// <summary>联盟阵营色（玩家盟友/阵营成员）</summary>
        private Color GetAllianceColor(TileData tile)
        {
            int owner = tile.ownerRealmId;
            if (owner < 0) return new Color(0.25f, 0.25f, 0.25f);
            int player = world != null ? world.PlayerRealmId : -1;
            if (player < 0) return _politicalColors[owner % _politicalColors.Length];
            if (owner == player) return FriendlyColor;

            var dm = world.GetDiplomacyManager();
            if (dm == null) return NeutralColor;
            var rel = dm.GetRelation(player, owner);
            if (rel == null) return NeutralColor;
            if (rel.activeAlliances.Exists(a => a.isActive && a.type == AllianceType.Faction))
                return FactionColor;
            if (rel.activeAlliances.Exists(a => a.isActive))
                return AllyColor;
            return NeutralColor;
        }

        /// <summary>文化色（branch=true 时允许分支的文化显示子文化色）</summary>
        private Color GetCultureColor(TileData tile, bool branch)
        {
            int cultureId = GetDominantBlockCulture(tile);
            if (cultureId < 0 || world == null) return NeutralColor;
            if (!world.cultures.TryGetValue(cultureId, out var culture)) return NeutralColor;

            if (branch)
            {
                // 分支文化：子文化（parentCultureId>=0）且父文化允许分支 → 子文化色
                if (culture.parentCultureId >= 0
                    && world.cultures.TryGetValue(culture.parentCultureId, out var parent)
                    && parent.allowsBranching)
                    return culture.color;
                return NeutralColor;
            }

            // 主文化：分支文化显示根文化色（同一谱系同色）
            if (culture.parentCultureId >= 0
                && world.cultures.TryGetValue(culture.parentCultureId, out var root))
                return root.color;
            return culture.color;
        }

        /// <summary>宗教色（三级谱系：宗教/宗派/传统）</summary>
        private Color GetReligionColor(TileData tile, ReligionMapLevel level)
        {
            int faithId = GetDominantBlockFaith(tile);
            if (faithId < 0) return NeutralColor;
            return ReligionCatalog.GetColor(faithId, level);
        }

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
                _mainCamera.transform.position -= new Vector3(delta.x * moveSpeed * 0.01f, 0f, delta.y * moveSpeed * 0.01f);
            }

            // WASD移动
            float moveX = Input.GetAxis("Horizontal") * _zoomLevel * 0.02f;
            float moveZ = Input.GetAxis("Vertical") * _zoomLevel * 0.02f;
            _mainCamera.transform.position += new Vector3(moveX, 0f, moveZ);

            // 相机边界（查漏补缺：拖出地图范围=黑屏——clamp 到地图世界范围——
            // wrapX/Y 环绕地图不限制对应轴[环行视觉连续]）
            ClampCameraToMap();
        }

        /// <summary>切换显示模式</summary>
                public void SetDisplayMode(MapDisplayMode mode)
        {
            displayMode = mode;
            _forceMapRefresh = true; // 切换显示模式立即重绘
            Debug.Log($"[MapRenderer] 切换显示模式：{mode}");
        }

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

        public void ForceRefresh()
        {
            _forceMapRefresh = true;
        }
        /// <summary>获取当前地图纹理（用于导出PNG）</summary>
        public Texture2D GetMapTexture() => mapTexture;

        /// <summary>获取地图编辑器实例</summary>
        public MapEditor GetMapEditor()
        {
            // 懒初始化（时序安全：UIManager 可能早于 _mapEditor 创建访问——
            // NRE 修复 2026-09-04）
            if (_mapEditor == null && world != null)
                _mapEditor = new MapEditor(world, this);
            return _mapEditor;
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
                bool wrapX = world != null && world.config.wrapX;
                if (wrapX) x = TileGrid.WrapX(x, mapWidth);
                else if (x < 0 || x >= mapWidth) return -1;

                bool wrapY = world != null && world.config.wrapY;
                if (wrapY) y = TileGrid.WrapY(y, mapHeight);
                else if (y < 0 || y >= mapHeight) return -1;

                int index = y * mapWidth + x;
                if (world != null && !world.tiles[index].exists) return -1;
                return index;
            }
            return -1;
        }
    }

    public enum MapDisplayMode
    {
        Terrain,        // 地形
        Climate,        // 气候
        Biome,          // 群系
        Political,      // 政治（常态）
        Population,     // 人口
        Economy,        // 经济
        Diplomacy,      // 外交关系（玩家视角：战争/敌对/中立/友好/盟约）
        Alliance,       // 联盟阵营（普通盟友/阵营成员）
        Culture,        // 文化（主文化）
        CultureBranch,  // 文化分支（允许分支的文化显示子文化）
        Religion,       // 宗教（根宗教）
        ReligionSuccession, // 宗教-教统（组织性单位）
        ReligionTradition // 宗教传统（礼拜仪轨/教义学派）
    }
}
