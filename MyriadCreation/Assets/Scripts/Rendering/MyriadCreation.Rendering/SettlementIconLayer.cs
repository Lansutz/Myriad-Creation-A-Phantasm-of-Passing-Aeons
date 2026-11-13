using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Rendering
{
    /// <summary>
    /// 聚落锚点图标层（Map 1）：
    /// 在 MapPlane 上为每个聚落/锚点显示程序化 Sprite 图标（不依赖美术资源）：
    ///  · 形状按类型区分：首都=星形、港口=三角、城市=菱形、村镇=圆形、据点=方块；
    ///  · 颜色按聚落等级（LevelI~V）区分，首都用金色覆盖；
    ///  · LOD：相机拉远（orthographicSize 大于地图高度的一定比例）时只显示"大城市"
    ///    （首都/港口/城市/LevelIII+），拉近显示全部；
    ///  · 左键点击图标 → 只做选中高亮（放大+提亮），**不打开任何 UI 面板**；
    ///  · 世界切换/读档（MapRenderer.BindWorld / GameWorld 引用变化）后自动重建。
    /// 代码创建：由 MapRenderer 在 Start/BindWorld 时通过 <see cref="Ensure"/> 挂到 MapPlane 上，
    /// 不需要在场景里手工摆放（场景零改动）。
    /// </summary>
    public sealed partial class SettlementIconLayer : MonoBehaviour
    {
        /// <summary>图标池上限（防御异常数据，避免无限生成）。</summary>
        public const int MaxIcons = 2048;

        /// <summary>LOD 阈值：相机 orthographicSize ≥ 地图世界高度 × 该比例 → 远景（只显示大城市）。</summary>
        [SerializeField] private float farLodRatio = 0.35f;

        /// <summary>选中图标的放大倍率。</summary>
        [SerializeField] private float selectedScale = 1.35f;

        // ===== 运行时状态 =====
        private MapRenderer _renderer;
        private IMapSettlementView _settlementView;
        private Transform _root;
        private Camera _camera;

        private readonly List<IconEntry> _icons = new List<IconEntry>();
        private readonly List<int> _visibleAnchors = new List<int>();
        private readonly HashSet<int> _visibleAnchorSet = new HashSet<int>();
        private readonly List<PendingSettlement> _pending = new List<PendingSettlement>();

        private int _selectedAnchorId = -1;
        private bool _isFarLod;
        private float _lastOrthoSize = float.NaN;
        private int _lastSettlementCount = -1;
        private int _lastAnchorCount = -1;

        // ===== 程序化 Sprite 缓存（形状 → 白色 Sprite，颜色由 SpriteRenderer.color 着色）=====
        private static readonly Dictionary<IconShape, Sprite> SpriteCache = new Dictionary<IconShape, Sprite>();

        private static readonly Color[] LevelColors =
        {
            new Color(0.72f, 0.82f, 0.95f, 0.95f), // LevelI 浅蓝灰
            new Color(0.45f, 0.85f, 0.50f, 0.95f), // LevelII 绿
            new Color(0.95f, 0.80f, 0.30f, 0.95f), // LevelIII 琥珀
            new Color(0.95f, 0.52f, 0.25f, 0.95f), // LevelIV 橙
            new Color(0.82f, 0.38f, 0.92f, 0.95f), // LevelV 紫
        };

        private static readonly Color CapitalColor = new Color(1f, 0.86f, 0.22f, 1f);
        private static readonly Color PortTint = new Color(0.35f, 0.75f, 1f, 0.98f);

        private const float IconHeightAboveMap = 0.12f;

        private struct PendingSettlement
        {
            public int AnchorId;
            public int TileIndex;
            public int Level;
            public bool Major;
            public IconShape Shape;
            public Color Color;
            public float Scale;
        }

        private sealed class IconEntry
        {
            public GameObject Go;
            public SpriteRenderer Renderer;
            public int AnchorId;
            public int TileIndex;
            public bool Major;
            public Color BaseColor;
            public float BaseScale;
            public Vector3 WorldPosition;
        }

        private enum IconShape
        {
            Circle,
            Diamond,
            Star,
            Triangle,
            Square,
        }

        // ==================== 创建/绑定 ====================

        /// <summary>确保 MapPlane 上有图标层（没有就代码创建）；供 MapRenderer 与测试使用。</summary>
        public static SettlementIconLayer Ensure(MapRenderer renderer)
        {
            if (renderer == null) return null;
            var layer = renderer.GetComponent<SettlementIconLayer>();
            if (layer == null)
            {
                layer = renderer.gameObject.AddComponent<SettlementIconLayer>();
            }
            layer.Initialize(renderer);
            return layer;
        }

        private void Initialize(MapRenderer renderer)
        {
            _renderer = renderer;
            if (_root == null)
            {
                var rootGo = new GameObject("SettlementIcons");
                rootGo.transform.SetParent(transform, false);
                _root = rootGo.transform;
            }
            if (_camera == null)
            {
                _camera = renderer != null && renderer.MapCamera != null ? renderer.MapCamera : Camera.main;
            }
            if (_settlementView != null) Rebuild();
        }

        /// <summary>绑定只读聚落视图并重建图标（B7-part2；由 MapRenderer/GameManager 注入）。</summary>
        public void SetSettlementView(IMapSettlementView view)
        {
            _settlementView = view;
            Rebuild();
        }

        // ==================== 对外只读状态（测试/UI 用） ====================

        /// <summary>图标总数（为所有有效锚点聚落建立的图标数，不受 LOD 影响）。</summary>
        public int IconCount => _icons.Count;

        /// <summary>当前 LOD 下可见的锚点 id 列表（只读视图，内部复用同一 List）。</summary>
        public IReadOnlyList<int> VisibleAnchors => _visibleAnchors;

        /// <summary>当前 LOD 下可见图标数。</summary>
        public int VisibleIconCount => _visibleAnchors.Count;

        /// <summary>当前选中的聚落锚点 id；-1 = 未选中。</summary>
        public int SelectedSettlementAnchorId => _selectedAnchorId;

        /// <summary>当前是否处于远景 LOD（只显示大城市）。</summary>
        public bool IsFarLod => _isFarLod;

        /// <summary>
        /// 图标被点击并成功选中时触发，参数为聚落锚点 id。
        /// 本层只负责"选中/高亮"；打开详情面板等 UI 行为由上层（GameManager）订阅本事件完成，
        /// 因此 Rendering 程序集不依赖 UI 程序集。
        /// </summary>
        public event System.Action<int> SettlementClicked;

        // ==================== 重建 ====================

        /// <summary>按当前世界 settlements/anchors 重建图标池（幂等，可反复调用）。</summary>
        public void Rebuild()
        {
            if (_renderer == null) _renderer = GetComponent<MapRenderer>();
            if (_renderer == null) return;

            var settlementView = _settlementView != null ? _settlementView : _renderer.SettlementView;
            _settlementView = settlementView;
            _lastSettlementCount = settlementView != null ? settlementView.SettlementCount : 0;
            _lastAnchorCount = settlementView != null ? settlementView.AnchorCount : 0;

            _pending.Clear();
            if (settlementView != null)
            {
                foreach (var settlement in settlementView.GetSettlements())
                {
                    int anchorId = settlement.anchorId;
                    if (anchorId < 0) continue;
                    int tileIndex = settlement.tileIndex;
                    if (tileIndex < 0) continue;

                    bool major = IsMajor(settlement);
                    IconShape shape = ShapeOf(settlement);
                    Color color = ColorOf(settlement);
                    float scale = ScaleOf(settlement);
                    _pending.Add(new PendingSettlement
                    {
                        AnchorId = anchorId,
                        TileIndex = tileIndex,
                        Level = settlement.settlementLevel,
                        Major = major,
                        Shape = shape,
                        Color = color,
                        Scale = scale,
                    });
                }
            }

            EnsurePoolSize(_pending.Count);
            for (int i = 0; i < _pending.Count; i++)
            {
                ApplyToEntry(_icons[i], _pending[i]);
            }
            // 多余图标隐藏（不销毁，避免反复生成 GC）
            for (int i = _pending.Count; i < _icons.Count; i++)
            {
                if (_icons[i].Go != null) _icons[i].Go.SetActive(false);
            }

            // 选中的锚点已不存在 → 清除选中
            if (_selectedAnchorId >= 0)
            {
                bool stillExists = false;
                for (int i = 0; i < _pending.Count; i++)
                {
                    if (_pending[i].AnchorId == _selectedAnchorId) { stillExists = true; break; }
                }
                if (!stillExists) _selectedAnchorId = -1;
            }

            ApplySelectionVisual();
            _lastOrthoSize = float.NaN; // 强制下一帧重算 LOD/朝向
            RefreshVisibility();
        }

        private void EnsurePoolSize(int needed)
        {
            if (needed > MaxIcons)
            {
                Debug.LogWarning($"[SettlementIconLayer] 聚落数 {needed} 超过图标池上限 {MaxIcons}，已截断");
                needed = MaxIcons;
            }
            while (_icons.Count < needed)
            {
                var go = new GameObject($"SettlementIcon_{_icons.Count}");
                go.transform.SetParent(_root != null ? _root : transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetSprite(IconShape.Circle);
                var material = sr.sharedMaterial;
                if (material == null)
                {
                    var shader = Shader.Find("Sprites/Default");
                    if (shader != null) sr.material = new Material(shader);
                }
                _icons.Add(new IconEntry { Go = go, Renderer = sr, AnchorId = -1 });
            }
        }

        private void ApplyToEntry(IconEntry entry, PendingSettlement pending)
        {
            entry.AnchorId = pending.AnchorId;
            entry.TileIndex = pending.TileIndex;
            entry.Major = pending.Major;
            entry.BaseColor = pending.Color;
            entry.BaseScale = pending.Scale;
            entry.WorldPosition = _renderer.TileToWorld(pending.TileIndex) + Vector3.up * IconHeightAboveMap;

            entry.Go.name = $"SettlementIcon_{pending.AnchorId}";
            entry.Go.transform.position = entry.WorldPosition;

            if (entry.Renderer == null) entry.Renderer = entry.Go.GetComponent<SpriteRenderer>();
            entry.Renderer.sprite = GetSprite(pending.Shape);
            entry.Renderer.color = pending.Color;
            entry.Renderer.sortingOrder = 1000 + (pending.Major ? 100 : 0) + Mathf.Clamp(pending.Level, 0, 4);
            entry.Go.transform.localScale = Vector3.one * pending.Scale;
            entry.Go.SetActive(true);
        }

        // ==================== LOD 可见性 ====================

        /// <summary>按当前相机尺寸重算 LOD（远景只显示大城市；选中项始终可见）。</summary>
        public void RefreshVisibility()
        {
            if (_camera == null) _camera = _renderer != null && _renderer.MapCamera != null ? _renderer.MapCamera : Camera.main;

            float worldHeight = _renderer != null ? _renderer.WorldHeight : 0f;
            bool far = false;
            if (_camera != null && worldHeight > 0f)
            {
                far = _camera.orthographicSize >= worldHeight * Mathf.Max(0.01f, farLodRatio);
                _lastOrthoSize = _camera.orthographicSize;
                // 图标是朝向相机的 billboard（相机 45° 固定俯视；双面 Sprite 着色器保证可见）
                var rot = _camera.transform.rotation;
                for (int i = 0; i < _icons.Count; i++)
                {
                    if (_icons[i].Go != null) _icons[i].Go.transform.rotation = rot;
                }
            }
            _isFarLod = far;

            _visibleAnchors.Clear();
            _visibleAnchorSet.Clear();
            for (int i = 0; i < _icons.Count; i++)
            {
                var icon = _icons[i];
                if (icon.AnchorId < 0) continue;
                bool visible = !far || icon.Major || icon.AnchorId == _selectedAnchorId;
                if (icon.Go != null && icon.Go.activeSelf != visible) icon.Go.SetActive(visible);
                if (visible)
                {
                    _visibleAnchors.Add(icon.AnchorId);
                    _visibleAnchorSet.Add(icon.AnchorId);
                }
            }
        }

        // ==================== 选中（不打开 UI） ====================

        /// <summary>
        /// 屏幕坐标 → 选中聚落（只高亮，不打开任何 UI 面板）。
        /// 先按地块精确匹配，未命中时最多检查 1 圈邻接地块（图标有一定视觉半径）。
        /// </summary>
        public void SelectAtScreen(Vector2 screenPosition)
        {
            if (_renderer == null || _settlementView == null) return;

            int anchorId = ResolveAnchorAtScreen(screenPosition);
            SetSelected(anchorId);

            // 成功选中才通知上层（打开聚落详情面板）；未命中（-1）只清除选中、不触发。
            if (anchorId >= 0) SettlementClicked?.Invoke(anchorId);
        }

        /// <summary>清除选中。</summary>
        public void ClearSelection() => SetSelected(-1);

        private int ResolveAnchorAtScreen(Vector2 screenPosition)
        {
            int tile = _renderer.ScreenToTile(screenPosition);
            if (tile < 0) return -1;

            if (TryGetAnchorAtTile(tile, out int anchorId) && IsSelectableAnchor(anchorId)) return anchorId;

            // 邻接 1 圈回退（需求：anchor tile 匹配/最近半径）
            int mw = Mathf.Max(1, _renderer.MapWidth);
            int mh = Mathf.Max(1, _renderer.MapHeight);
            int x = tile % mw;
            int y = tile / mw;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx < 0 || nx >= mw || ny < 0 || ny >= mh) continue; // 越界（环绕地图在邻接回退里不做，保持简单）
                    int neighbor = ny * mw + nx;
                    if (TryGetAnchorAtTile(neighbor, out int neighborAnchor) && IsSelectableAnchor(neighborAnchor))
                        return neighborAnchor;
                }
            }
            return -1;
        }

        private bool TryGetAnchorAtTile(int tileIndex, out int anchorId)
        {
            anchorId = -1;
            return _settlementView != null && _settlementView.TryGetAnchorAtTile(tileIndex, out anchorId);
        }

        private bool IsSelectableAnchor(int anchorId)
        {
            // 远景 LOD 下不可见的小聚落不可点（选中项本身始终保留）；近景全部可点
            if (_isFarLod) return _visibleAnchorSet.Contains(anchorId) || anchorId == _selectedAnchorId;
            return true;
        }

        private void SetSelected(int anchorId)
        {
            if (_selectedAnchorId == anchorId) return;
            _selectedAnchorId = anchorId;
            ApplySelectionVisual();
            RefreshVisibility(); // 选中项在远景下也强制可见
        }

        private void ApplySelectionVisual()
        {
            for (int i = 0; i < _icons.Count; i++)
            {
                var icon = _icons[i];
                if (icon.Go == null || icon.Renderer == null) continue;
                bool selected = icon.AnchorId >= 0 && icon.AnchorId == _selectedAnchorId;
                if (selected)
                {
                    icon.Go.transform.localScale = Vector3.one * (icon.BaseScale * Mathf.Max(1f, selectedScale));
                    icon.Renderer.color = Color.Lerp(icon.BaseColor, Color.white, 0.55f);
                    icon.Renderer.sortingOrder = 2000;
                }
                else
                {
                    icon.Go.transform.localScale = Vector3.one * icon.BaseScale;
                    icon.Renderer.color = icon.BaseColor;
                    icon.Renderer.sortingOrder = 1000 + (icon.Major ? 100 : 0) + (icon.AnchorId >= 0 ? 10 : 0);
                }
            }
        }

        // ==================== 每帧维护 ====================

        private void Update()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<MapRenderer>();
                if (_renderer == null) return;
                Initialize(_renderer);
            }

            // 视图/聚落集合变化 → 重建（新开局、读档、聚落被毁/新建）
            var settlementView = _renderer.SettlementView;
            if (!ReferenceEquals(settlementView, _settlementView) || HasSettlementSetChanged(settlementView)) Rebuild();

            // 相机缩放变化 → 重算 LOD
            var cam = _camera != null ? _camera : (_renderer.MapCamera != null ? _renderer.MapCamera : Camera.main);
            if (cam != null && !Mathf.Approximately(cam.orthographicSize, _lastOrthoSize)) RefreshVisibility();

            // 左键点击图标 → 只选中高亮，不调用任何 UI 打开接口
            if (Input.GetMouseButtonDown(0)) SelectAtScreen(Input.mousePosition);
        }

        private bool HasSettlementSetChanged(IMapSettlementView view)
        {
            if (view == null) return _lastSettlementCount != 0;
            return view.SettlementCount != _lastSettlementCount || view.AnchorCount != _lastAnchorCount;
        }

        // ==================== 图标样式 ====================

        private static bool IsMajor(MapSettlementInfo settlement)
        {
            if (settlement == null) return false;
            if (settlement.isCapital || settlement.isPort) return true;
            if (settlement.settlementForm == 1) return true;
            return settlement.settlementLevel >= 2;
        }

        private static IconShape ShapeOf(MapSettlementInfo settlement)
        {
            if (settlement == null) return IconShape.Circle;
            if (settlement.isCapital) return IconShape.Star;
            if (settlement.isPort) return IconShape.Triangle;
            if (settlement.settlementForm == 1) return IconShape.Diamond;
            if (settlement.siteMode == 1) return IconShape.Square;
            return IconShape.Circle;
        }

        private static Color ColorOf(MapSettlementInfo settlement)
        {
            if (settlement == null) return LevelColors[0];
            if (settlement.isCapital) return CapitalColor;
            int idx = settlement.settlementLevel;
            Color c = idx >= 0 && idx < LevelColors.Length ? LevelColors[idx] : LevelColors[0];
            if (settlement.isPort) c = Color.Lerp(c, PortTint, 0.55f);
            return c;
        }

        private static float ScaleOf(MapSettlementInfo settlement)
        {
            if (settlement == null) return 0.8f;
            if (settlement.isCapital) return 1.5f;
            int level = Mathf.Clamp(settlement.settlementLevel, 0, 4);
            float baseScale = 0.85f + level * 0.12f;
            if (settlement.settlementForm == 1) baseScale += 0.1f;
            return baseScale;
        }

        // ==================== 程序化 Sprite ====================

        private static Sprite GetSprite(IconShape shape)
        {
            if (SpriteCache.TryGetValue(shape, out var sprite) && sprite != null) return sprite;
            sprite = CreateShapeSprite(shape, 64);
            SpriteCache[shape] = sprite;
            return sprite;
        }

        private static Sprite CreateShapeSprite(IconShape shape, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"SettlementIcon_{shape}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    // 像素中心归一化到 [-1,1]
                    float nx = (px + 0.5f - half) / half;
                    float ny = (py + 0.5f - half) / half;
                    bool inside = InsideShape(shape, nx, ny);
                    var c = new Color32(255, 255, 255, inside ? (byte)235 : (byte)0);
                    pixels[py * size + px] = c;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static bool InsideShape(IconShape shape, float x, float y)
        {
            float r2 = x * x + y * y;
            switch (shape)
            {
                case IconShape.Circle:
                    return r2 <= 0.80f * 0.80f;
                case IconShape.Diamond:
                    return Mathf.Abs(x) + Mathf.Abs(y) <= 0.86f;
                case IconShape.Square:
                    return Mathf.Abs(x) <= 0.62f && Mathf.Abs(y) <= 0.62f;
                case IconShape.Triangle:
                {
                    if (y > 0.80f || y < -0.82f) return false;
                    float halfWidth = 0.82f * (1f - (y + 0.82f) / 1.62f);
                    return Mathf.Abs(x) <= halfWidth;
                }
                case IconShape.Star:
                {
                    float r = Mathf.Sqrt(r2);
                    float angle = Mathf.Atan2(y, x);
                    float t = Mathf.Repeat(angle / (2f * Mathf.PI) * 5f + 0.25f, 1f); // 顶尖朝上
                    float spike = 1f - Mathf.Abs(2f * t - 1f);                       // 0..1..0
                    float outer = 0.86f;
                    float inner = 0.40f;
                    return r <= Mathf.Lerp(inner, outer, spike);
                }
                default:
                    return r2 <= 0.80f * 0.80f;
            }
        }
    }
}
