using System;
using System.Collections.Generic;
using UnityEngine;
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
        private const float MapTextureInterval = 0.2f; // 5 次/秒，人眼无感延迟
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
        }

                void Update()
        {
            // 相机输入需要实时响应，不节流
            HandleCameraInput();
            // 地图编辑器鼠标处理（编辑模式下左键绘制）
            HandleEditorInput();

            // 地图纹理重绘节流：0.2 秒一次或强制刷新时
            _mapTextureTimer += Time.unscaledDeltaTime;
            if (_forceMapRefresh || _mapTextureTimer >= MapTextureInterval)
            {
                _mapTextureTimer = 0f;
                _forceMapRefresh = false;
                UpdateMapTexture();
            }
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
            _biomeColors[(int)GameEnums.BiomeType.IceSheet] = new Color(0.95f, 0.97f, 1f);
            _biomeColors[(int)GameEnums.BiomeType.Tundra] = new Color(0.7f, 0.75f, 0.7f);
            _biomeColors[(int)GameEnums.BiomeType.BorealForest] = new Color(0.3f, 0.45f, 0.3f);
            _biomeColors[(int)GameEnums.BiomeType.TemperateForest] = new Color(0.3f, 0.6f, 0.3f);
            _biomeColors[(int)GameEnums.BiomeType.TemperateGrassland] = new Color(0.6f, 0.75f, 0.3f);
            _biomeColors[(int)GameEnums.BiomeType.Desert] = new Color(0.85f, 0.75f, 0.45f);
            _biomeColors[(int)GameEnums.BiomeType.Steppe] = new Color(0.7f, 0.65f, 0.4f);
            _biomeColors[(int)GameEnums.BiomeType.Savanna] = new Color(0.75f, 0.7f, 0.3f);
            _biomeColors[(int)GameEnums.BiomeType.TropicalRainforest] = new Color(0.15f, 0.45f, 0.2f);
            _biomeColors[(int)GameEnums.BiomeType.TropicalMonsoon] = new Color(0.25f, 0.55f, 0.25f);
            _biomeColors[(int)GameEnums.BiomeType.Alpine] = new Color(0.6f, 0.55f, 0.5f);
            _biomeColors[(int)GameEnums.BiomeType.Wetland] = new Color(0.3f, 0.5f, 0.45f);
            _biomeColors[(int)GameEnums.BiomeType.Volcanic] = new Color(0.4f, 0.25f, 0.2f);
            _biomeColors[(int)GameEnums.BiomeType.SaltLake] = new Color(0.8f, 0.8f, 0.7f);

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

        /// <summary>获取地块颜色</summary>
        private Color GetTileColor(int tileIndex)
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

                default:
                    return Color.gray;
            }
        }

        /// <summary>相机输入处理</summary>
        private void HandleCameraInput()
        {
            if (_mainCamera == null) return;

            // 缩放
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                _zoomLevel = Mathf.Clamp(_zoomLevel - scroll * 20f, 10f, 150f);
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
        }

        /// <summary>切换显示模式</summary>
                public void SetDisplayMode(MapDisplayMode mode)
        {
            displayMode = mode;
            _forceMapRefresh = true; // 切换显示模式立即重绘
            Debug.Log($"[MapRenderer] 切换显示模式：{mode}");
        }

        /// <summary>强制刷新地图纹理（编辑器绘制后调用）</summary>
        public void ForceRefresh()
        {
            _forceMapRefresh = true;
        }
        /// <summary>获取当前地图纹理（用于导出PNG）</summary>
        public Texture2D GetMapTexture() => mapTexture;

        /// <summary>获取地图编辑器实例</summary>
        public MapEditor GetMapEditor() => _mapEditor;

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
        Terrain,      // 地形
        Climate,      // 气候
        Biome,        // 群系
        Political,    // 政治
        Population,   // 人口
        Economy       // 经济
    }
}
