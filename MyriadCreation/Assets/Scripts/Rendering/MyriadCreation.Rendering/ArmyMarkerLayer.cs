using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using UnityEngine;

namespace MyriadCreation.Rendering
{
    /// <summary>
    /// 军队标记层（P1.5）：在 MapPlane 上为每支军队画一个程序化箭头标记。
    ///  · 颜色按政权 id 确定性分配，交战/溃退另有覆盖色；
    ///  · 位置每帧跟随 Army.currentTileIndex（行军途中也跟得上，不重建整层）；
    ///  · 受 MapLayerConfig.showArmyMarkers 开关控制，默认关；
    ///  · 与 SettlementIconLayer 同构：代码创建、场景零改动、Rendering 只依赖 Core.Contracts。
    /// </summary>
    public sealed partial class ArmyMarkerLayer : MonoBehaviour
    {
        public const int MaxMarkers = 512;

        [SerializeField] private float markerScale = 0.85f;
        [SerializeField] private float markerHeightAboveMap = 0.18f;

        private MapRenderer _renderer;
        private IMapArmyView _armyView;
        private Transform _root;
        private Camera _camera;

        private readonly List<MarkerEntry> _markers = new List<MarkerEntry>();
        private readonly List<MapArmyInfo> _snapshot = new List<MapArmyInfo>();

        private int _lastArmyCount = -1;
        private bool _visible = true;
        private bool _lastOverlayEnabled;

        private static readonly Color CombatColor = new Color(0.95f, 0.25f, 0.22f, 1f);
        private static readonly Color RoutedColor = new Color(0.45f, 0.45f, 0.45f, 0.95f);
        private static readonly Color BesiegingColor = new Color(0.95f, 0.62f, 0.18f, 1f);

        private sealed class MarkerEntry
        {
            public GameObject Go;
            public SpriteRenderer Renderer;
            public int ArmyId = -1;
            public int TileIndex = -1;
            public Color BaseColor;
        }

        // ==================== 创建/绑定 ====================

        public static ArmyMarkerLayer Ensure(MapRenderer renderer)
        {
            if (renderer == null) return null;
            var layer = renderer.GetComponent<ArmyMarkerLayer>();
            if (layer == null)
            {
                layer = renderer.gameObject.AddComponent<ArmyMarkerLayer>();
            }
            layer.Initialize(renderer);
            return layer;
        }

        private void Initialize(MapRenderer renderer)
        {
            _renderer = renderer;
            if (_root == null)
            {
                var rootGo = new GameObject("ArmyMarkers");
                rootGo.transform.SetParent(transform, false);
                _root = rootGo.transform;
            }
            if (_camera == null)
            {
                _camera = renderer != null && renderer.MapCamera != null ? renderer.MapCamera : Camera.main;
            }
            if (_armyView != null) Rebuild();
        }

        public void SetArmyView(IMapArmyView view)
        {
            _armyView = view;
            Rebuild();
        }

        // ==================== 对外只读状态 ====================

        public int MarkerCount => _markers.Count;

        public int VisibleMarkerCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _markers.Count; i++)
                {
                    if (_markers[i].Go != null && _markers[i].Go.activeSelf) count++;
                }
                return count;
            }
        }

        public bool IsVisible => _visible;

        // ==================== 重建 ====================

        public void Rebuild()
        {
            if (_renderer == null) _renderer = GetComponent<MapRenderer>();
            if (_renderer == null) return;

            var armyView = _armyView != null ? _armyView : _renderer.ArmyView;
            _armyView = armyView;
            _lastArmyCount = armyView != null ? armyView.ArmyCount : 0;

            _snapshot.Clear();
            if (armyView != null)
            {
                var armies = armyView.GetArmies();
                for (int i = 0; i < armies.Count; i++)
                {
                    var army = armies[i];
                    if (army == null || army.tileIndex < 0) continue;
                    _snapshot.Add(army);
                }
            }

            EnsurePoolSize(_snapshot.Count);
            for (int i = 0; i < _snapshot.Count; i++)
            {
                ApplyToEntry(_markers[i], _snapshot[i]);
            }
            for (int i = _snapshot.Count; i < _markers.Count; i++)
            {
                if (_markers[i].Go != null) _markers[i].Go.SetActive(false);
            }
            RefreshOverlayState();
        }

        private void EnsurePoolSize(int needed)
        {
            if (needed > MaxMarkers)
            {
                Debug.LogWarning($"[ArmyMarkerLayer] 军队数 {needed} 超过标记池上限 {MaxMarkers}，已截断");
                needed = MaxMarkers;
            }
            while (_markers.Count < needed)
            {
                var go = new GameObject($"ArmyMarker_{_markers.Count}");
                go.transform.SetParent(_root != null ? _root : transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetArrowSprite();
                if (sr.sharedMaterial == null)
                {
                    var shader = Shader.Find("Sprites/Default");
                    if (shader != null) sr.material = new Material(shader);
                }
                sr.sortingOrder = 1500;
                _markers.Add(new MarkerEntry { Go = go, Renderer = sr, ArmyId = -1 });
            }
        }

        private void ApplyToEntry(MarkerEntry entry, MapArmyInfo army)
        {
            entry.ArmyId = army.armyId;
            entry.TileIndex = army.tileIndex;
            entry.BaseColor = ColorOf(army);

            entry.Go.name = $"ArmyMarker_{army.armyId}";
            entry.Go.transform.position = _renderer.TileToWorld(army.tileIndex) + Vector3.up * markerHeightAboveMap;
            if (_camera != null) entry.Go.transform.rotation = _camera.transform.rotation;

            if (entry.Renderer == null) entry.Renderer = entry.Go.GetComponent<SpriteRenderer>();
            entry.Renderer.color = entry.BaseColor;
            float size = markerScale * Mathf.Lerp(0.75f, 1.25f, Mathf.Clamp01(army.manpower / 2000f));
            if (army.combatState == 3) size *= 1.2f;
            entry.Go.transform.localScale = Vector3.one * size;
            entry.Go.SetActive(_visible);
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

            bool overlayEnabled = _renderer.IsOverlayEnabled(MapOverlayLayer.ArmyMarkers);
            var armyView = _renderer.ArmyView;
            bool changed = !ReferenceEquals(armyView, _armyView);
            if (armyView == null)
            {
                changed = _lastArmyCount != 0;
            }
            else
            {
                changed = changed || armyView.ArmyCount != _lastArmyCount;
            }
            if (changed || overlayEnabled != _lastOverlayEnabled) Rebuild();

            if (_visible) RefreshPositions();
        }

        /// <summary>位置/状态轻量刷新：不重建对象，只更新已存在标记的坐标与颜色。</summary>
        private void RefreshPositions()
        {
            if (_armyView == null || _renderer == null) return;
            var armies = _armyView.GetArmies();
            for (int i = 0; i < armies.Count && i < _markers.Count; i++)
            {
                var army = armies[i];
                var entry = _markers[i];
                if (army == null || entry.Go == null) continue;
                if (army.tileIndex >= 0 && army.tileIndex != entry.TileIndex)
                {
                    entry.TileIndex = army.tileIndex;
                    entry.Go.transform.position = _renderer.TileToWorld(army.tileIndex) + Vector3.up * markerHeightAboveMap;
                }
                Color color = ColorOf(army);
                if (color != entry.BaseColor)
                {
                    entry.BaseColor = color;
                    if (entry.Renderer != null) entry.Renderer.color = color;
                }
                if (_camera != null) entry.Go.transform.rotation = _camera.transform.rotation;
            }
        }

        private void RefreshOverlayState()
        {
            _lastOverlayEnabled = _renderer != null && _renderer.IsOverlayEnabled(MapOverlayLayer.ArmyMarkers);
            _visible = _lastOverlayEnabled;
            for (int i = 0; i < _markers.Count; i++)
            {
                if (_markers[i].Go != null) _markers[i].Go.SetActive(_visible && _markers[i].ArmyId >= 0);
            }
        }

        // ==================== 样式 ====================

        private static Color ColorOf(MapArmyInfo army)
        {
            if (army == null) return Color.white;
            switch (army.combatState)
            {
                case 3: return CombatColor;      // InCombat
                case 5: return RoutedColor;      // Routed
                case 2: return BesiegingColor;   // Besieging
                default:
                {
                    float h = Mathf.Repeat(0.071f * army.realmId + 0.13f, 1f);
                    return Color.HSVToRGB(h, 0.72f, 0.95f);
                }
            }
        }

        // ==================== 程序化 Sprite ====================

        private static Sprite _arrowSprite;

        private static Sprite GetArrowSprite()
        {
            if (_arrowSprite != null) return _arrowSprite;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "ArmyMarker_Arrow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float nx = (px + 0.5f - half) / half;
                    float ny = (py + 0.5f - half) / half;
                    // 向上的箭头：上半是三角，下半是尾杆
                    bool head = ny >= -0.05f && ny <= 0.85f && Mathf.Abs(nx) <= (0.85f - ny) * 0.9f;
                    bool shaft = ny < -0.05f && ny >= -0.75f && Mathf.Abs(nx) <= 0.24f;
                    bool inside = head || shaft;
                    pixels[py * size + px] = new Color32(255, 255, 255, inside ? (byte)240 : (byte)0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            _arrowSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            return _arrowSprite;
        }
    }
}
