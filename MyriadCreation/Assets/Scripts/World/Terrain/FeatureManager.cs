using System.Collections.Generic;

using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;


namespace CivilizationEvolution.World.Terrain
{
    /// <summary>
    /// 特征锁定管理器（参考 Azgaar FMG 的 Features / Zones lock 机制）。
    /// 玩家手绘的岛屿、湖泊、大陆等区域可以被锁定，
    /// 锁定后程序化地形生成不会覆盖这些地块的高程和海陆属性。
    /// 下游的气候/水文/群系仍会重新计算（因为它们依赖地形）。
    /// </summary>
    public class FeatureManager
    {
        private readonly TileData[] _tiles;
        private readonly int _width;
        private readonly int _height;

        /// <summary>锁定的地块索引集合（运行时缓存，避免遍历全图）</summary>
        private readonly HashSet<int> _lockedSet = new HashSet<int>();

        /// <summary>命名特征区域（可选，用于UI显示和存档）</summary>
        private readonly Dictionary<int, FeatureRegion> _features = new Dictionary<int, FeatureRegion>();
        private int _nextFeatureId = 1;

        public FeatureManager(TileData[] tiles, int width, int height)
        {
            _tiles = tiles;
            _width = width;
            _height = height;
            // 从 tiles 重建锁定集合（读档后）
            for (int i = 0; i < tiles.Length; i++)
                if (tiles[i].locked) _lockedSet.Add(i);
        }

        /// <summary>锁定数量</summary>
        public int LockedCount => _lockedSet.Count;

        /// <summary>是否有任何锁定地块</summary>
        public bool HasLocked => _lockedSet.Count > 0;

        /// <summary>锁定单个地块</summary>
        public void LockTile(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= _tiles.Length) return;
            _tiles[tileIndex].locked = true;
            _lockedSet.Add(tileIndex);
        }

        /// <summary>解锁单个地块</summary>
        public void UnlockTile(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= _tiles.Length) return;
            _tiles[tileIndex].locked = false;
            _lockedSet.Remove(tileIndex);
        }

        /// <summary>切换锁定状态</summary>
        public void ToggleLock(int tileIndex)
        {
            if (IsLocked(tileIndex)) UnlockTile(tileIndex);
            else LockTile(tileIndex);
        }

        /// <summary>查询是否锁定</summary>
        public bool IsLocked(int tileIndex) => _lockedSet.Contains(tileIndex);

        /// <summary>锁定一个区域（地块索引列表）</summary>
        public void LockRegion(IEnumerable<int> tileIndices, string name = null)
        {
            var region = new FeatureRegion
            {
                featureId = _nextFeatureId++,
                name = name ?? $"区域{_nextFeatureId}",
                memberTiles = new List<int>(tileIndices)
            };
            _features[region.featureId] = region;
            foreach (int idx in region.memberTiles) LockTile(idx);
            Debug.Log($"[FeatureManager] 锁定区域 '{region.name}'：{region.memberTiles.Count} 地块");
        }

        /// <summary>解锁指定特征区域</summary>
        public void UnlockFeature(int featureId)
        {
            if (!_features.TryGetValue(featureId, out var region)) return;
            foreach (int idx in region.memberTiles) UnlockTile(idx);
            _features.Remove(featureId);
        }

        /// <summary>解锁全部</summary>
        public void UnlockAll()
        {
            foreach (int idx in _lockedSet) _tiles[idx].locked = false;
            _lockedSet.Clear();
            _features.Clear();
            Debug.Log("[FeatureManager] 已解锁全部地块");
        }

        /// <summary>获取所有锁定地块索引</summary>
        public IReadOnlyCollection<int> GetLockedTiles() => _lockedSet;

        /// <summary>
        /// 在地形生成前调用：保存所有锁定地块的高程和海陆属性。
        /// 返回保存的快照，生成后调用 RestoreLockedTiles 恢复。
        /// </summary>
        public LockedSnapshot SaveLockedSnapshot()
        {
            var snapshot = new LockedSnapshot(_lockedSet.Count);
            foreach (int idx in _lockedSet)
            {
                snapshot.indices.Add(idx);
                snapshot.elevations.Add(_tiles[idx].elevation01);
                snapshot.isLand.Add(_tiles[idx].isLand);
            }
            return snapshot;
        }

        /// <summary>
        /// 在地形生成后调用：恢复锁定地块的高程和海陆属性。
        /// 注意：坡度/海岸/温度/降水等派生属性会在后续步骤重新计算。
        /// </summary>
        public void RestoreLockedTiles(LockedSnapshot snapshot)
        {
            if (snapshot == null || snapshot.indices.Count == 0) return;
            for (int i = 0; i < snapshot.indices.Count; i++)
            {
                int idx = snapshot.indices[i];
                if (idx < 0 || idx >= _tiles.Length) continue;
                _tiles[idx].elevation01 = snapshot.elevations[i];
                _tiles[idx].isLand = snapshot.isLand[i];
                // 重新设置海洋层级
                if (!_tiles[idx].isLand)
                {
                    float seaLevel = 0.5f; // 近似，实际由生成器决定
                    _tiles[idx].oceanTier = (_tiles[idx].elevation01 < seaLevel * 0.3f) ? GameEnums.OceanTier.DeepSea :
                                            (_tiles[idx].elevation01 < seaLevel * 0.7f) ? GameEnums.OceanTier.NearSea :
                                            GameEnums.OceanTier.Coast;
                    _tiles[idx].oceanDepth01 = Mathf.Max(0f, (seaLevel - _tiles[idx].elevation01) / seaLevel);
                }
                else
                {
                    _tiles[idx].oceanTier = GameEnums.OceanTier.Land;
                    _tiles[idx].oceanDepth01 = 0f;
                }
            }
            Debug.Log($"[FeatureManager] 已恢复 {snapshot.indices.Count} 个锁定地块的地形");
        }

        /// <summary>锁定快照（生成前后保存/恢复用）</summary>
        public class LockedSnapshot
        {
            public List<int> indices;
            public List<float> elevations;
            public List<bool> isLand;
            public LockedSnapshot(int capacity)
            {
                indices = new List<int>(capacity);
                elevations = new List<float>(capacity);
                isLand = new List<bool>(capacity);
            }
        }

        /// <summary>命名特征区域</summary>
        public class FeatureRegion
        {
            public int featureId;
            public string name;
            public List<int> memberTiles;
        }
    }
}
