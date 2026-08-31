using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Military
{
    /// <summary>
    /// 军队寻路系统
    /// 基于A*算法，考虑地形、坡度、海拔、建筑、外交通行管制
    /// 支持左右连通/上下联通的环形地图
    /// </summary>
    public class PathfindingSystem
    {
        private GameWorld _world;
        private int _width;
        private int _height;
        private bool _wrapX = true;  // 左右连通
        private bool _wrapY = false; // 上下联通（一般不连通，极地不可通行）

        // 8方向移动
        private static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] DY = { 0, 0, 1, -1, 1, -1, 1, -1 };
        private static readonly float[] DCost = { 1f, 1f, 1f, 1f, 1.414f, 1.414f, 1.414f, 1.414f };

        public PathfindingSystem(GameWorld world, int width, int height, bool wrapX = true, bool wrapY = false)
        {
            _world = world;
            _width = width;
            _height = height;
            _wrapX = wrapX;
            _wrapY = wrapY;
        }

        /// <summary>寻路参数</summary>
        public class PathfindingParams
        {
            public int armyRealmId = -1;           // 军队所属政权
            public bool isNavy = false;             // 是否海军
            public float maxSlope = 45f;            // 最大可通行坡度（度）
            public float maxElevation = 5000f;      // 最大可通行海拔
            public bool canPassEnemyTerritory = false; // 是否可通过敌方领土（需要军事通行权）
            public bool canPassImpassable = false;   // 是否可通过不可通行地区
            public Dictionary<int, float> tileCostOverrides = new Dictionary<int, float>(); // 自定义地块成本
        }

        /// <summary>寻路结果</summary>
        public class PathResult
        {
            public List<int> path = new List<int>(); // 地块索引列表
            public float totalCost = 0f;
            public bool success = false;
            public string failReason = "";
        }

        /// <summary>执行寻路</summary>
        public PathResult FindPath(int startTile, int endTile, PathfindingParams param)
        {
            var result = new PathResult();

            if (startTile == endTile)
            {
                result.path.Add(startTile);
                result.success = true;
                return result;
            }

            // A*算法
            var openSet = new PriorityQueue<int, float>();
            var cameFrom = new Dictionary<int, int>();
            var gScore = new Dictionary<int, float>();
            var closedSet = new HashSet<int>();

            gScore[startTile] = 0f;
            openSet.Enqueue(startTile, Heuristic(startTile, endTile));

            int maxIterations = _width * _height * 2;
            int iterations = 0;

            while (openSet.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                var current = openSet.Dequeue();

                if (current == endTile)
                {
                    // 重建路径
                    result.path = ReconstructPath(cameFrom, current);
                    result.totalCost = gScore[current];
                    result.success = true;
                    return result;
                }

                closedSet.Add(current);

                foreach (var (neighbor, cost) in GetNeighbors(current, param))
                {
                    if (closedSet.Contains(neighbor)) continue;

                    float tentativeG = gScore[current] + cost;

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        float f = tentativeG + Heuristic(neighbor, endTile);
                        openSet.Enqueue(neighbor, f);
                    }
                }
            }

            result.failReason = iterations >= maxIterations ? "超出最大迭代次数" : "无法到达目标";
            return result;
        }

        /// <summary>获取邻居地块及通行成本</summary>
        private List<(int tile, float cost)> GetNeighbors(int tile, PathfindingParams param)
        {
            var neighbors = new List<(int, float)>();
            int x = tile % _width;
            int y = tile / _width;

            for (int i = 0; i < 8; i++)
            {
                int nx = x + DX[i];
                int ny = y + DY[i];

                // 处理环形地图
                if (_wrapX)
                {
                    if (nx < 0) nx += _width;
                    if (nx >= _width) nx -= _width;
                }
                else if (nx < 0 || nx >= _width) continue;

                if (_wrapY)
                {
                    if (ny < 0) ny += _height;
                    if (ny >= _height) ny -= _height;
                }
                else if (ny < 0 || ny >= _height) continue;

                int neighborTile = ny * _width + nx;

                // 检查可通行性
                float moveCost = CalculateMoveCost(tile, neighborTile, param, DCost[i]);
                if (moveCost < 0) continue; // 不可通行

                neighbors.Add((neighborTile, moveCost));
            }

            return neighbors;
        }

        /// <summary>计算移动成本（返回负数表示不可通行）</summary>
        private float CalculateMoveCost(int fromTile, int toTile, PathfindingParams param, float baseCost)
        {
            if (_world == null) return baseCost;

            // 获取地块数据
            var tileData = GetTileData(toTile);
            if (tileData == null) return -1;

            // 海军特殊处理
            if (param.isNavy)
            {
                // 海军只能在海洋地块移动
                if (!tileData.isOcean && !tileData.isCoastal)
                    return -1;
                return baseCost * (tileData.isOcean ? 1f : 1.5f);
            }

            // 陆军不可通行检查
            if (!param.canPassImpassable && tileData.isImpassable)
                return -1;

            // 坡度检查
            if (tileData.slope > param.maxSlope)
                return -1;

            // 海拔检查
            if (tileData.elevation > param.maxElevation)
                return -1;

            // 海洋地块陆军不可通行（除非有运输船）
            if (tileData.isOcean)
                return -1;

            float cost = baseCost;

            // 地形成本修正
            cost *= GetTerrainCostMultiplier(tileData);

            // 坡度成本修正
            cost *= (1f + tileData.slope / 45f);

            // 海拔成本修正
            if (tileData.elevation > 2000f)
                cost *= 1.5f;

            // 外交通行管制检查
            if (param.armyRealmId >= 0)
            {
                int tileOwner = GetTileOwner(toTile);
                if (tileOwner >= 0 && tileOwner != param.armyRealmId)
                {
                    var ownerRealm = GetRealm(tileOwner);
                    if (ownerRealm != null)
                    {
                        var controlLevel = ownerRealm.movementControl;
                        if (controlLevel == GameEnums.MovementControlLevel.Strict)
                        {
                            // 严格管制需要军事通行权
                            if (!ownerRealm.militaryAccessGranted.Contains(param.armyRealmId))
                                return -1; // 无通行权，不可通过
                            cost *= 1.2f;
                        }
                        else if (controlLevel == GameEnums.MovementControlLevel.Limited)
                        {
                            cost *= 1.5f;
                        }

                        // 敌对领土额外成本
                        if (IsAtWar(param.armyRealmId, tileOwner))
                        {
                            if (!param.canPassEnemyTerritory)
                                return -1;
                            cost *= 2f; // 敌方领土移动成本翻倍
                        }
                    }
                }
            }

            // 建筑影响
            cost *= GetBuildingCostMultiplier(toTile, param);

            // 自定义成本覆盖
            if (param.tileCostOverrides.TryGetValue(toTile, out var overrideCost))
                cost = overrideCost;

            return cost;
        }

        /// <summary>地形成本倍率</summary>
        private float GetTerrainCostMultiplier(TileData tile)
        {
            if (tile.isMountain) return 3f;
            if (tile.isHills) return 1.8f;
            if (tile.isForest) return 1.5f;
            if (tile.isSwamp) return 2.5f;
            if (tile.isDesert) return 1.8f;
            if (tile.isPlains) return 1f;
            if (tile.isCoastal) return 1.2f;
            return 1.5f;
        }

        /// <summary>建筑成本倍率</summary>
        private float GetBuildingCostMultiplier(int tile, PathfindingParams param)
        {
            // 堡垒/要塞增加敌方移动成本
            var buildings = GetBuildingsAtTile(tile);
            float multiplier = 1f;
            foreach (var b in buildings)
            {
                if (b.isFortification)
                {
                    int owner = b.ownerRealmId;
                    if (owner != param.armyRealmId)
                        multiplier *= 1.5f; // 敌方堡垒增加移动成本
                    else
                        multiplier *= 0.8f; // 己方堡垒减少移动成本（补给支援）
                }
            }
            return multiplier;
        }

        /// <summary>启发式函数（曼哈顿距离，考虑环形地图）</summary>
        private float Heuristic(int a, int b)
        {
            int ax = a % _width, ay = a / _width;
            int bx = b % _width, by = b / _width;

            int dx = Math.Abs(ax - bx);
            if (_wrapX) dx = Math.Min(dx, _width - dx);

            int dy = Math.Abs(ay - by);
            if (_wrapY) dy = Math.Min(dy, _height - dy);

            return dx + dy;
        }

        /// <summary>重建路径</summary>
        private List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
        {
            var path = new List<int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            return path;
        }

        // ===== 辅助方法（需要根据实际GameWorld结构调整） =====

        private TileData GetTileData(int tileIndex)
        {
            // 简化实现，实际需要从GameWorld获取
            return null;
        }

        private int GetTileOwner(int tileIndex)
        {
            // 简化实现
            return -1;
        }

        private RealmData GetRealm(int realmId)
        {
            if (_world != null && realmId >= 0 && realmId < _world.realms.Count)
                return _world.realms[realmId];
            return null;
        }

        private bool IsAtWar(int realmA, int realmB)
        {
            // 简化实现，实际需要检查战争状态
            return false;
        }

        private List<BuildingData> GetBuildingsAtTile(int tileIndex)
        {
            // 简化实现
            return new List<BuildingData>();
        }
    }

    /// <summary>地块数据（简化版，实际应从GameWorld获取）</summary>
    public class TileData
    {
        public bool isOcean;
        public bool isCoastal;
        public bool isMountain;
        public bool isHills;
        public bool isForest;
        public bool isSwamp;
        public bool isDesert;
        public bool isPlains;
        public bool isImpassable;
        public float slope;
        public float elevation;
    }

    /// <summary>建筑数据（简化版）</summary>
    public class BuildingData
    {
        public int ownerRealmId;
        public bool isFortification;
        public string buildingType;
    }

    /// <summary>简单优先队列</summary>
    public class PriorityQueue<T, TPriority> where TPriority : IComparable<TPriority>
    {
        private List<(T item, TPriority priority)> _elements = new List<(T, TPriority)>();
        public int Count => _elements.Count;

        public void Enqueue(T item, TPriority priority)
        {
            _elements.Add((item, priority));
            int i = _elements.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (_elements[parent].priority.CompareTo(_elements[i].priority) <= 0) break;
                (_elements[parent], _elements[i]) = (_elements[i], _elements[parent]);
                i = parent;
            }
        }

        public T Dequeue()
        {
            if (_elements.Count == 0) throw new InvalidOperationException("Queue empty");
            T result = _elements[0].item;
            int last = _elements.Count - 1;
            _elements[0] = _elements[last];
            _elements.RemoveAt(last);

            int i = 0;
            while (true)
            {
                int left = 2 * i + 1;
                int right = 2 * i + 2;
                int smallest = i;

                if (left < _elements.Count && _elements[left].priority.CompareTo(_elements[smallest].priority) < 0)
                    smallest = left;
                if (right < _elements.Count && _elements[right].priority.CompareTo(_elements[smallest].priority) < 0)
                    smallest = right;

                if (smallest == i) break;
                (_elements[i], _elements[smallest]) = (_elements[smallest], _elements[i]);
                i = smallest;
            }

            return result;
        }
    }
}
