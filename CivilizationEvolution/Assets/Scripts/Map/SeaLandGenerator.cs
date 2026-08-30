using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 海陆生成与重算系统
    /// 画笔修改地形后增量重算海洋地块属性与连通性
    /// 海洋三级划分：海岸带 / 近海大陆架 / 远洋深海
    /// </summary>
    public class SeaLandGenerator
    {
        private readonly WorldConfig _config;
        private readonly TileData[] _tiles;
        private readonly int _width;
        private readonly int _height;

        private Dictionary<int, List<int>> _seaConnectGroups = new Dictionary<int, List<int>>();
        private int _nextSeaConnectId = 0;
        private float[] _fragmentNoise;

        public SeaLandGenerator(WorldConfig config, TileData[] tiles, int width, int height)
        {
            _config = config;
            _tiles = tiles;
            _width = width;
            _height = height;
            _fragmentNoise = new float[tiles.Length];
        }

        public void SetFragmentNoise(float[] noise)
        {
            if (noise != null && noise.Length == _tiles.Length)
                _fragmentNoise = noise;
        }

        /// <summary>全量重算海陆属性</summary>
        public void RecalculateAll()
        {
            float seaThreshold = CalculateSeaLevelThresholdFromDistribution();

            for (int i = 0; i < _tiles.Length; i++)
                RecalculateSingleTile(i, seaThreshold);

            ApplyLandFragmentation(seaThreshold);
            ApplyCoastFragmentation();
            RecalculateSeaConnectivity();

            Debug.Log($"[SeaLand] 全量重算：陆地{GetTotalLandTiles()}/海洋{GetTotalSeaTiles()}/连通海域{_seaConnectGroups.Count}");
        }

        /// <summary>脏区局部重算</summary>
        public void RecalculateDirty(HashSet<int> dirtyIndices)
        {
            if (dirtyIndices == null || dirtyIndices.Count == 0) return;

            float seaThreshold = _cachedSeaThreshold; // 画笔只改高度，海平面沿用全量重算的分位阈值
            bool connectivityChanged = false;

            var expandedDirty = new HashSet<int>(dirtyIndices);
            foreach (int idx in dirtyIndices)
                foreach (int n in GetNeighbourIndices(idx))
                    expandedDirty.Add(n);

            foreach (int idx in expandedDirty)
            {
                bool wasLand = _tiles[idx].isLand;
                RecalculateSingleTile(idx, seaThreshold);
                if (wasLand != _tiles[idx].isLand)
                    connectivityChanged = true;
            }

            foreach (int idx in expandedDirty)
                RecalculateAdjacencyForTile(idx);

            if (connectivityChanged)
                RecalculateSeaConnectivity();
        }

        /// <summary>缓存的海平面阈值（增量重算复用；全量重算时按高度场分布刷新）</summary>
        private float _cachedSeaThreshold;

        /// <summary>
        /// 基于当前实际高度场分布计算海平面阈值，使 landAmount 滑块真正对应陆地占比。
        /// 固定阈值会因高度场分布不均而使陆地比例严重失真；改为取高度分位数：高于分位线为陆地。
        /// seaLevel 以 0.5 为中性，升高淹没低地、降低露出海床。
        /// </summary>
        private float CalculateSeaLevelThresholdFromDistribution()
        {
            var heights = new List<float>(_tiles.Length);
            for (int i = 0; i < _tiles.Length; i++)
                if (_tiles[i].exists) heights.Add(_tiles[i].elevation01);

            if (heights.Count == 0) { _cachedSeaThreshold = 0f; return 0f; }
            heights.Sort();

            float effectiveLand = Mathf.Clamp(_config.landAmount + (0.5f - _config.seaLevel) * 0.5f, 0.02f, 0.95f);
            float quantile = 1f - effectiveLand;
            int idx = Mathf.Clamp(Mathf.RoundToInt(quantile * (heights.Count - 1)), 0, heights.Count - 1);
            _cachedSeaThreshold = heights[idx];
            return _cachedSeaThreshold;
        }

        /// <summary>单地块海陆属性重算，输出5字段</summary>
        private void RecalculateSingleTile(int index, float seaThreshold)
        {
            ref TileData tile = ref _tiles[index];

            // 不存在的地块（虚空/地图外）重置海陆属性
            if (!tile.exists)
            {
                tile.isLand = false;
                tile.isCoast = false;
                tile.oceanTier = GameEnums.OceanTier.None;
                tile.oceanDepth01 = 0f;
                tile.waterAdjacentWeight = 0f;
                tile.seaConnectId = -1;
                return;
            }

            float rawHeight = tile.elevation01;
            float adjusted = rawHeight - seaThreshold;

            tile.elevation01 = Mathf.Clamp(adjusted, -1f, 1f);
            tile.isLand = adjusted > 0f;
            tile.oceanDepth01 = tile.isLand ? 0f : Mathf.Clamp01(Mathf.Abs(adjusted) / 0.7f);

            int seaN = 0, landN = 0;
            foreach (int n in GetNeighbourIndices(index))
            {
                if (!_tiles[n].exists) continue; // 跳过不存在的邻接
                if (_tiles[n].isLand) landN++; else seaN++;
            }
            int total = seaN + landN;

            if (tile.isLand)
            {
                tile.waterAdjacentWeight = total > 0 ? (float)seaN / total : 0f;
                tile.isCoast = seaN > 0;
                tile.oceanTier = GameEnums.OceanTier.Land;
            }
            else
            {
                float shelfThreshold = 0.05f + _config.oceanBuffer * 0.35f;
                tile.isCoast = tile.oceanDepth01 < 0.08f || landN > 0;
                tile.waterAdjacentWeight = total > 0 ? 1f - (float)landN / total : 1f;
                tile.oceanTier = tile.oceanDepth01 <= shelfThreshold
                    ? GameEnums.OceanTier.NearSea
                    : GameEnums.OceanTier.DeepSea;
            }
        }

        /// <summary>陆地破碎度：边缘低海拔陆地按概率侵蚀为海</summary>
        private void ApplyLandFragmentation(float seaThreshold)
        {
            if (_config.landFragment <= 0.01f) return;

            for (int i = 0; i < _tiles.Length; i++)
            {
                if (!_tiles[i].exists || !_tiles[i].isLand || _tiles[i].elevation01 > 0.15f) continue;

                float noise = _fragmentNoise[i];
                float erosionChance = _config.landFragment * 0.6f * noise * _tiles[i].waterAdjacentWeight;

                if (Random.value < erosionChance)
                {
                    _tiles[i].isLand = false;
                    _tiles[i].elevation01 = -Mathf.Abs(_tiles[i].elevation01) * 0.5f;
                    _tiles[i].oceanDepth01 = Mathf.Clamp01(Mathf.Abs(_tiles[i].elevation01) / 0.7f);
                    _tiles[i].oceanTier = GameEnums.OceanTier.NearSea;
                }
            }
            RecalculateAdjacencyForAll();
        }

        /// <summary>海岸破碎度：细化海岸线形成河口、海湾、岬角</summary>
        private void ApplyCoastFragmentation()
        {
            if (_config.coastFragment <= 0.01f) return;

            for (int i = 0; i < _tiles.Length; i++)
            {
                if (!_tiles[i].exists || !_tiles[i].isCoast) continue;
                float noise = _fragmentNoise[i];
                float fragmentChance = _config.coastFragment * 0.4f * noise;

                if (_tiles[i].isLand && Random.value < fragmentChance)
                {
                    _tiles[i].isLand = false;
                    _tiles[i].elevation01 = -0.03f;
                    _tiles[i].oceanDepth01 = 0.05f;
                    _tiles[i].oceanTier = GameEnums.OceanTier.NearSea;
                }
                else if (!_tiles[i].isLand && Random.value < fragmentChance * 0.5f)
                {
                    _tiles[i].isLand = true;
                    _tiles[i].elevation01 = 0.02f;
                    _tiles[i].oceanDepth01 = 0f;
                    _tiles[i].oceanTier = GameEnums.OceanTier.Land;
                }
            }
            RecalculateAdjacencyForAll();
        }

        private void RecalculateAdjacencyForAll()
        {
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i].exists)
                    RecalculateAdjacencyForTile(i);
            }
        }

        private void RecalculateAdjacencyForTile(int index)
        {
            ref TileData tile = ref _tiles[index];
            if (!tile.exists) return;

            int seaN = 0, landN = 0;
            foreach (int n in GetNeighbourIndices(index))
            {
                if (!_tiles[n].exists) continue;
                if (_tiles[n].isLand) landN++; else seaN++;
            }
            int total = seaN + landN;

            if (tile.isLand)
            {
                tile.waterAdjacentWeight = total > 0 ? (float)seaN / total : 0f;
                tile.isCoast = seaN > 0;
            }
            else
            {
                tile.isCoast = tile.oceanDepth01 < 0.08f || landN > 0;
                tile.waterAdjacentWeight = total > 0 ? 1f - (float)landN / total : 1f;
                float shelfThreshold = 0.05f + _config.oceanBuffer * 0.35f;
                tile.oceanTier = tile.oceanDepth01 <= shelfThreshold
                    ? GameEnums.OceanTier.NearSea
                    : GameEnums.OceanTier.DeepSea;
            }
        }

        /// <summary>海洋连通性全量重算（洪水填充）</summary>
        private void RecalculateSeaConnectivity()
        {
            _seaConnectGroups.Clear();
            _nextSeaConnectId = 0;
            bool[] visited = new bool[_tiles.Length];

            for (int i = 0; i < _tiles.Length; i++)
            {
                if (!_tiles[i].exists || _tiles[i].isLand || visited[i]) continue;

                var group = new List<int>();
                var queue = new Queue<int>();
                queue.Enqueue(i);
                visited[i] = true;
                int connectId = _nextSeaConnectId++;

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    group.Add(current);
                    _tiles[current].seaConnectId = connectId;

                    foreach (int neighbour in GetNeighbourIndices(current))
                    {
                        if (!visited[neighbour] && _tiles[neighbour].exists && !_tiles[neighbour].isLand)
                        {
                            visited[neighbour] = true;
                            queue.Enqueue(neighbour);
                        }
                    }
                }
                _seaConnectGroups[connectId] = group;
            }
        }

        /// <summary>
        /// 六边形网格6邻格（even-r偏移坐标）
        /// 偶数行：左上(-1,-1) 右上(0,-1) 左(-1,0) 右(1,0) 左下(-1,1) 右下(0,1)
        /// 奇数行：左上(0,-1) 右上(1,-1) 左(-1,0) 右(1,0) 左下(0,1) 右下(1,1)
        /// </summary>
        public IEnumerable<int> GetNeighbourIndices(int index)
        {
            int x = index % _width;
            int y = index / _width;
            bool evenRow = y % 2 == 0;

            int[] dx, dy;
            if (evenRow)
            {
                dx = new int[] { -1, 0, -1, 1, -1, 0 };
                dy = new int[] { -1, -1, 0, 0, 1, 1 };
            }
            else
            {
                dx = new int[] { 0, 1, -1, 1, 0, 1 };
                dy = new int[] { -1, -1, 0, 0, 1, 1 };
            }

            for (int i = 0; i < 6; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                // 环绕支持（修复：原实现忽略 wrapX/wrapY，与 WorldConfig 默认 wrapX=true 矛盾）
                if (_config.wrapX) nx = ((nx % _width) + _width) % _width;
                else if (nx < 0 || nx >= _width) continue;
                if (_config.wrapY) ny = ((ny % _height) + _height) % _height;
                else if (ny < 0 || ny >= _height) continue;

                yield return ny * _width + nx;
            }
        }

        /// <summary>六边形距离计算</summary>
        public int GetHexDistance(int a, int b)
        {
            int ax = a % _width, ay = a / _width;
            int bx = b % _width, by = b / _width;

            int acx = ax - (ay - (ay & 1)) / 2;
            int acz = ay;
            int acy = -acx - acz;

            int bcx = bx - (by - (by & 1)) / 2;
            int bcz = by;
            int bcy = -bcx - bcz;

            return (Mathf.Abs(acx - bcx) + Mathf.Abs(acy - bcy) + Mathf.Abs(acz - bcz)) / 2;
        }

        // ===== 统计查询 =====
        public int GetTotalLandTiles()
        {
            int count = 0;
            for (int i = 0; i < _tiles.Length; i++)
                if (_tiles[i].exists && _tiles[i].isLand) count++;
            return count;
        }

        public int GetTotalSeaTiles()
        {
            int count = 0;
            for (int i = 0; i < _tiles.Length; i++)
                if (_tiles[i].exists && !_tiles[i].isLand) count++;
            return count;
        }
        public int GetConnectedSeaCount() => _seaConnectGroups.Count;

        public int GetSeaGroupSize(int connectId)
        {
            return _seaConnectGroups.TryGetValue(connectId, out var group) ? group.Count : 0;
        }

        public bool AreSeaTilesConnected(int a, int b)
        {
            if (_tiles[a].isLand || _tiles[b].isLand) return false;
            return _tiles[a].seaConnectId == _tiles[b].seaConnectId;
        }
    }
}
