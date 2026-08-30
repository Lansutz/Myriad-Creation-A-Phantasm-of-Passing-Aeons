using System;
using System.Collections.Generic;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;
using UnityEngine;

namespace CivilizationEvolution.Render
{
    /// <summary>
    /// 地图编辑器工具类型
    /// 对齐 FantasyMapSimulator: CustomMapImageLayer / MapBrush / cursorPixelPosition / CeilToPixelGrid
    /// </summary>
    public enum EditorTool
    {
        None,           // 无（观察模式）
        TerrainLand,    // 地形画笔：造陆
        TerrainSea,     // 地形画笔：填海
        TerrainMountain,// 地形画笔：山地
        TerrainPlain,   // 地形画笔：平原
        ProvincePaint,  // 省份画笔：绘制省份归属
        ProvinceErase,  // 省份橡皮擦：清除省份归属
        BurgPlace,      // 子地块放置：在指定位置放置Burg
        BurgRemove      // 子地块移除
    }

    /// <summary>
    /// 地图编辑器
    /// 运行时像素级地图编辑：地形绘制、省份边界调整、子地块放置
    /// 支持画笔大小、撤销/重做、绘制后自动重算省份/海洋/Burg
    /// </summary>
    public class MapEditor
    {
        private readonly GameWorld _world;
        private readonly MapRenderer _renderer;

        // ===== 编辑器状态 =====
        public bool IsEditMode { get; private set; }
        public EditorTool CurrentTool { get; private set; } = EditorTool.None;
        public int BrushSize { get; set; } = 1; // 画笔半径（1=1x1, 2=3x3, 3=5x5）
        public int SelectedProvinceId { get; set; } = -1; // 省份画笔选中的省份ID
        public BurgType SelectedBurgType { get; set; } = BurgType.City; // 子地块放置类型

        // ===== 撤销/重做 =====
        private class EditAction
        {
            public string description;
            public List<(int tileIndex, TileData oldData, TileData newData)> tileChanges = new();
            public List<(int burgId, BurgData oldBurg, BurgData newBurg)> burgChanges = new();
            public List<int> addedBurgIds = new();
            public List<int> removedBurgIds = new();
        }
        private readonly Stack<EditAction> _undoStack = new Stack<EditAction>();
        private readonly Stack<EditAction> _redoStack = new Stack<EditAction>();
        private const int MaxUndoSteps = 50;

        // ===== 绘制状态 =====
        private bool _isPainting = false;
        private EditAction _currentAction;
        private HashSet<int> _paintedTiles = new HashSet<int>(); // 本次绘制已修改的地块（防重复）

        // ===== 事件 =====
        public event Action OnEditModeChanged;
        public event Action OnToolChanged;
        public event Action OnMapEdited;

        public MapEditor(GameWorld world, MapRenderer renderer)
        {
            _world = world;
            _renderer = renderer;
        }

        // ===== 模式切换 =====
        public void ToggleEditMode()
        {
            IsEditMode = !IsEditMode;
            if (!IsEditMode)
            {
                _isPainting = false;
                _currentAction = null;
            }
            OnEditModeChanged?.Invoke();
            Debug.Log($"[MapEditor] 编辑模式: {(IsEditMode ? "开启" : "关闭")}");
        }

        public void SetTool(EditorTool tool)
        {
            CurrentTool = tool;
            _isPainting = false;
            _currentAction = null;
            OnToolChanged?.Invoke();
        }

        // ===== 绘制入口（由MapRenderer的鼠标事件调用）=====
        public void OnPaintStart(int tileIndex)
        {
            if (!IsEditMode || CurrentTool == EditorTool.None) return;
            if (tileIndex < 0 || tileIndex >= _world.tiles.Length) return;

            _isPainting = true;
            _paintedTiles.Clear();
            _currentAction = new EditAction { description = GetToolDescription() };
            PaintAt(tileIndex);
        }

        public void OnPaintDrag(int tileIndex)
        {
            if (!_isPainting) return;
            if (tileIndex < 0 || tileIndex >= _world.tiles.Length) return;
            PaintAt(tileIndex);
        }

        public void OnPaintEnd()
        {
            if (!_isPainting) return;
            _isPainting = false;

            if (_currentAction != null &&
                (_currentAction.tileChanges.Count > 0 || _currentAction.burgChanges.Count > 0 ||
                 _currentAction.addedBurgIds.Count > 0 || _currentAction.removedBurgIds.Count > 0))
            {
                PushUndo(_currentAction);
                _redoStack.Clear();
                OnMapEdited?.Invoke();
            }
            _currentAction = null;
            _paintedTiles.Clear();

            // 绘制结束后重算相关系统
            RecalculateAfterEdit();
        }

        // ===== 核心绘制逻辑 =====
        private void PaintAt(int centerTile)
        {
            int cx = centerTile % _world.mapWidth;
            int cy = centerTile / _world.mapWidth;
            int r = BrushSize;

            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    // 圆形画笔（方形画笔的话去掉距离判断）
                    if (dx * dx + dy * dy > r * r) continue;

                    int px = cx + dx;
                    int py = cy + dy;

                    // 地图环绕：先环绕再边界检查（柱面=左右环绕，环面=全环绕，平面=不环绕）
                    if (_world.wrapMode == MapWrapMode.Cylindrical || _world.wrapMode == MapWrapMode.Toroidal)
                        px = (px + _world.mapWidth) % _world.mapWidth;
                    if (_world.wrapMode == MapWrapMode.Toroidal)
                        py = (py + _world.mapHeight) % _world.mapHeight;

                    // 平面模式下的边界检查（环绕模式下坐标已合法）
                    if (_world.wrapMode == MapWrapMode.Flat)
                    {
                        if (px < 0 || px >= _world.mapWidth || py < 0 || py >= _world.mapHeight) continue;
                    }

                    int tileIndex = py * _world.mapWidth + px;
                    if (_paintedTiles.Contains(tileIndex)) continue;
                    _paintedTiles.Add(tileIndex);

                    ApplyToolToTile(tileIndex);
                }
            }
        }

        private void ApplyToolToTile(int tileIndex)
        {
            ref TileData tile = ref _world.tiles[tileIndex];
            TileData oldData = tile; // 拷贝旧值用于撤销

            bool changed = false;
            switch (CurrentTool)
            {
                case EditorTool.TerrainLand:
                    if (!tile.isLand)
                    {
                        tile.isLand = true;
                        tile.elevation01 = Mathf.Max(tile.elevation01, 0.5f);
                        tile.seaConnectId = -1;
                        changed = true;
                    }
                    break;

                case EditorTool.TerrainSea:
                    if (tile.isLand)
                    {
                        tile.isLand = false;
                        tile.elevation01 = Mathf.Min(tile.elevation01, 0.35f);
                        tile.provinceId = -1;
                        tile.ownerRealmId = -1;
                        changed = true;
                    }
                    break;

                case EditorTool.TerrainMountain:
                    if (tile.isLand && tile.elevation01 < 0.75f)
                    {
                        tile.elevation01 = 0.75f + UnityEngine.Random.Range(0f, 0.15f);
                        changed = true;
                    }
                    break;

                case EditorTool.TerrainPlain:
                    if (tile.isLand && tile.elevation01 > 0.55f)
                    {
                        tile.elevation01 = 0.48f + UnityEngine.Random.Range(0f, 0.07f);
                        changed = true;
                    }
                    break;

                case EditorTool.ProvincePaint:
                    if (tile.isLand && SelectedProvinceId >= 0)
                    {
                        if (tile.provinceId != SelectedProvinceId)
                        {
                            tile.provinceId = SelectedProvinceId;
                            changed = true;
                        }
                    }
                    break;

                case EditorTool.ProvinceErase:
                    if (tile.provinceId >= 0)
                    {
                        tile.provinceId = -1;
                        changed = true;
                    }
                    break;

                case EditorTool.BurgPlace:
                    // 只在中心点放置Burg（画笔范围内的第一个陆地地块）
                    if (tile.isLand && !HasBurgAt(tileIndex))
                    {
                        PlaceBurgAt(tileIndex);
                        return; // Burg放置不记录tileChange
                    }
                    break;

                case EditorTool.BurgRemove:
                    RemoveBurgAt(tileIndex);
                    return;
            }

            if (changed)
            {
                _currentAction.tileChanges.Add((tileIndex, oldData, tile));
            }
        }

        // ===== Burg 放置/移除 =====
        private bool HasBurgAt(int tileIndex)
        {
            if (_world.burgs == null) return false;
            foreach (var b in _world.burgs.Values)
                if (b.tileIndex == tileIndex) return true;
            return false;
        }

        private void PlaceBurgAt(int tileIndex)
        {
            if (_world.burgs == null) _world.burgs = new Dictionary<int, BurgData>();

            int newId = 0;
            foreach (var k in _world.burgs.Keys) if (k >= newId) newId = k + 1;

            ref TileData tile = ref _world.tiles[tileIndex];
            var burg = new BurgData
            {
                burgId = newId,
                burgName = GenerateBurgName(tile, SelectedBurgType),
                type = SelectedBurgType,
                provinceId = tile.provinceId,
                tileIndex = tileIndex,
                x = 0.5f,
                y = 0.5f,
                isCoastal = tile.isCoast,
                isPort = SelectedBurgType == BurgType.Port,
                development = SelectedBurgType == BurgType.City ? 30f : 15f,
                population = SelectedBurgType == BurgType.City ? 1000f : 300f,
                buildLevel = SelectedBurgType == BurgType.City ? 2 : 1
            };

            _world.burgs[newId] = burg;
            _currentAction.addedBurgIds.Add(newId);
            Debug.Log($"[MapEditor] 放置子地块: {burg.DisplayName} at tile#{tileIndex}");
        }

        private void RemoveBurgAt(int tileIndex)
        {
            if (_world.burgs == null) return;
            int? toRemove = null;
            foreach (var kv in _world.burgs)
            {
                if (kv.Value.tileIndex == tileIndex)
                {
                    toRemove = kv.Key;
                    break;
                }
            }
            if (toRemove.HasValue)
            {
                var burg = _world.burgs[toRemove.Value];
                _currentAction.burgChanges.Add((toRemove.Value, burg, null));
                _world.burgs.Remove(toRemove.Value);
                _currentAction.removedBurgIds.Add(toRemove.Value);
                Debug.Log($"[MapEditor] 移除子地块: {burg.burgName} (id={toRemove.Value})");
            }
        }

        // ===== 绘制后重算 =====
        private void RecalculateAfterEdit()
        {
            // 1. 重算省份成员列表
            if (_world.provinces != null)
            {
                foreach (var p in _world.provinces.Values)
                    p.memberTiles.Clear();

                for (int i = 0; i < _world.tiles.Length; i++)
                {
                    if (_world.tiles[i].provinceId >= 0 &&
                        _world.provinces.TryGetValue(_world.tiles[i].provinceId, out var prov))
                    {
                        prov.memberTiles.Add(i);
                    }
                }
            }

            // 2. 重算海洋连通（简化：标记海洋地块）
            // 完整的flood fill在SeaLandGenerator中，这里只做基础标记
            for (int i = 0; i < _world.tiles.Length; i++)
            {
                if (!_world.tiles[i].isLand)
                {
                    _world.tiles[i].provinceId = -1;
                    _world.tiles[i].ownerRealmId = -1;
                }
            }

            // 3. 强制刷新地图渲染
            if (_renderer != null)
                _renderer.ForceRefresh();

            Debug.Log("[MapEditor] 绘制后重算完成");
        }

        // ===== 撤销/重做 =====
        public void Undo()
        {
            if (_undoStack.Count == 0) return;
            var action = _undoStack.Pop();
            ApplyActionReverse(action);
            _redoStack.Push(action);
            RecalculateAfterEdit();
            OnMapEdited?.Invoke();
            Debug.Log($"[MapEditor] 撤销: {action.description}");
        }

        public void Redo()
        {
            if (_redoStack.Count == 0) return;
            var action = _redoStack.Pop();
            ApplyActionForward(action);
            _undoStack.Push(action);
            RecalculateAfterEdit();
            OnMapEdited?.Invoke();
            Debug.Log($"[MapEditor] 重做: {action.description}");
        }

        private void ApplyActionForward(EditAction action)
        {
            foreach (var (idx, _, newData) in action.tileChanges)
                _world.tiles[idx] = newData;
            foreach (var id in action.addedBurgIds)
            {
                // 重新添加（从burgChanges中找）
                foreach (var (bid, burg, _) in action.burgChanges)
                    if (bid == id && burg != null)
                        _world.burgs[bid] = burg;
            }
            foreach (var id in action.removedBurgIds)
                _world.burgs.Remove(id);
        }

        private void ApplyActionReverse(EditAction action)
        {
            foreach (var (idx, oldData, _) in action.tileChanges)
                _world.tiles[idx] = oldData;
            foreach (var id in action.addedBurgIds)
                _world.burgs.Remove(id);
            foreach (var (bid, burg, _) in action.burgChanges)
                if (burg != null && action.removedBurgIds.Contains(bid))
                    _world.burgs[bid] = burg;
        }

        private void PushUndo(EditAction action)
        {
            _undoStack.Push(action);
            if (_undoStack.Count > MaxUndoSteps)
            {
                // 移除最旧的（Stack不支持直接移除底部，用临时栈）
                var temp = new Stack<EditAction>();
                while (_undoStack.Count > 1)
                    temp.Push(_undoStack.Pop());
                _undoStack.Clear();
                while (temp.Count > 0)
                    _undoStack.Push(temp.Pop());
            }
        }

        // ===== 工具描述 =====
        private string GetToolDescription()
        {
            return CurrentTool switch
            {
                EditorTool.TerrainLand => "造陆",
                EditorTool.TerrainSea => "填海",
                EditorTool.TerrainMountain => "造山",
                EditorTool.TerrainPlain => "平整",
                EditorTool.ProvincePaint => $"绘制省份#{SelectedProvinceId}",
                EditorTool.ProvinceErase => "擦除省份",
                EditorTool.BurgPlace => $"放置{SelectedBurgType}",
                EditorTool.BurgRemove => "移除子地块",
                _ => "编辑"
            };
        }

        private static string GenerateBurgName(TileData tile, BurgType type)
        {
            string prefix = tile.elevation01 > 0.55f ? "山" : tile.isCoast ? "海" : "原";
            string mid = tile.annualPrecipMm > 900f ? "润" : tile.annualPrecipMm < 300f ? "干" : "丰";
            string suffix = type switch
            {
                BurgType.City => "城",
                BurgType.Port => "港",
                BurgType.Fortress => "寨",
                BurgType.Town => "镇",
                BurgType.Capital => "京",
                _ => "村"
            };
            return prefix + mid + suffix;
        }

        // ===== 状态查询 =====
        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;
        public bool IsPainting => _isPainting;

        /// <summary>获取画笔覆盖的地块列表（用于渲染预览）</summary>
        public List<int> GetBrushTileIndices(int centerTile)
        {
            var result = new List<int>();
            int cx = centerTile % _world.mapWidth;
            int cy = centerTile / _world.mapWidth;
            int r = BrushSize;
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dy * dy > r * r) continue;
                    int px = cx + dx;
                    int py = cy + dy;
                    if (px < 0 || px >= _world.mapWidth || py < 0 || py >= _world.mapHeight) continue;
                    result.Add(py * _world.mapWidth + px);
                }
            }
            return result;
        }

        // ===== 海洋地块重算 =====
        /// <summary>
        /// 重新计算所有海洋地块的等级（海岸/近海/中海/远海/深海）
        /// 使用多源BFS从陆地出发计算每个海洋格到最近陆地的距离
        /// 绘制完陆地后调用此方法，用于海军和贸易系统
        /// </summary>
        public void RecalculateOceanZones()
        {
            int w = _world.mapWidth;
            int h = _world.mapHeight;
            int total = w * h;

            // 距离数组：-1表示未访问，陆地格距离为0
            int[] dist = new int[total];
            for (int i = 0; i < total; i++) dist[i] = -1;

            // BFS队列：先把所有陆地格入队（距离0）
            var queue = new Queue<int>();
            for (int i = 0; i < total; i++)
            {
                if (_world.tiles[i].isLand)
                {
                    dist[i] = 0;
                    queue.Enqueue(i);
                }
            }

            // 4方向邻居（上下左右）
            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };

            // 多源BFS
            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                int cx = cur % w;
                int cy = cur / w;

                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + dx[d];
                    int ny = cy + dy[d];

                    // 左右环绕（柱面/环面）
                    if (_world.wrapMode == MapWrapMode.Cylindrical || _world.wrapMode == MapWrapMode.Toroidal)
                        nx = (nx + w) % w;
                    if (_world.wrapMode == MapWrapMode.Toroidal)
                        ny = (ny + h) % h;

                    // 平面模式边界检查
                    if (_world.wrapMode == MapWrapMode.Flat)
                    {
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                    }

                    int nidx = ny * w + nx;
                    if (dist[nidx] == -1)
                    {
                        dist[nidx] = dist[cur] + 1;
                        queue.Enqueue(nidx);
                    }
                }
            }

            // 根据距离设置海洋等级
            int coastCount = 0, nearCount = 0, midCount = 0, farCount = 0, deepCount = 0;
            for (int i = 0; i < total; i++)
            {
                ref TileData tile = ref _world.tiles[i];
                if (tile.isLand)
                {
                    tile.oceanTier = GameEnums.OceanTier.Land;
                    tile.isCoast = false;
                    tile.oceanDepth01 = 0f;
                    continue;
                }

                int d = dist[i];
                tile.isCoast = (d == 1); // 紧邻陆地的海洋格

                if (d <= 1)
                {
                    tile.oceanTier = GameEnums.OceanTier.Coast;
                    tile.oceanDepth01 = 0.1f;
                    coastCount++;
                }
                else if (d <= 3)
                {
                    tile.oceanTier = GameEnums.OceanTier.NearSea;
                    tile.oceanDepth01 = 0.25f;
                    nearCount++;
                }
                else if (d <= 6)
                {
                    tile.oceanTier = GameEnums.OceanTier.MidSea;
                    tile.oceanDepth01 = 0.45f;
                    midCount++;
                }
                else if (d <= 11)
                {
                    tile.oceanTier = GameEnums.OceanTier.FarSea;
                    tile.oceanDepth01 = 0.7f;
                    farCount++;
                }
                else
                {
                    tile.oceanTier = GameEnums.OceanTier.DeepSea;
                    tile.oceanDepth01 = Mathf.Clamp01(0.85f + d * 0.01f);
                    deepCount++;
                }
            }

            Debug.Log($"[MapEditor] 海洋重算完成: 海岸{coastCount} 近海{nearCount} 中海{midCount} 远海{farCount} 深海{deepCount}");
        }

        /// <summary>获取指定海洋等级的地块数量</summary>
        public int GetOceanTileCount(GameEnums.OceanTier tier)
        {
            int count = 0;
            for (int i = 0; i < _world.tiles.Length; i++)
            {
                if (_world.tiles[i].oceanTier == tier) count++;
            }
            return count;
        }
    }
}
