using System;
using System.Collections.Generic;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// Priority-Flood 水文模拟系统
    /// 基于 Priority-Flood 算法（Barnes et al. 2014, "Priority-Flood: An Optimal Depression-Filling and Watershed-Labeling Algorithm"）
    ///
    /// 功能：
    ///   1. 洼地填充（Depression Filling）：消除地形中的封闭洼地
    ///   2. 排水方向（Flow Direction）：每个点的水流去向（D8 8方向）
    ///   3. 汇水面积（Flow Accumulation）：每个点上游汇水区域大小
    ///   4. 河流提取（River Extraction）：汇水面积超过阈值的点形成河流
    ///   5. 流域划分（Watershed）：按出海口/河流终点划分集水区
    ///
    /// 性能：O(N log N)，使用最小堆（优先队列），支持百万级地块
    /// </summary>
    public class HydrologySystem
    {
        // D8 8方向偏移（even-r 六边形网格用简化8邻域）
        private static readonly int[] DX = { 0, 1, 1, 1, 0, -1, -1, -1 };
        private static readonly int[] DY = { -1, -1, 0, 1, 1, 1, 0, -1 };

        /// <summary>填充后的高程（洼地被填平）</summary>
        public float[] FilledElevation { get; private set; }
        /// <summary>排水方向（0-7，-1=无排水/海洋）</summary>
        public int[] FlowDirection { get; private set; }
        /// <summary>汇水面积（上游地块数）</summary>
        public float[] FlowAccumulation { get; private set; }
        /// <summary>是否河流（汇水面积超过阈值）</summary>
        public bool[] IsRiver { get; private set; }
        /// <summary>河流等级（Strahler stream order）</summary>
        public int[] StreamOrder { get; private set; }

        /// <summary>
    /// 最小堆（Min-Heap），用于Priority-Flood算法的优先队列
    /// 替代.NET 6+的PriorityQueue（兼容Unity的.NET版本）
    /// </summary>
    private class MinHeap
    {
        private struct HeapNode { public int item; public float priority; }
        private readonly List<HeapNode> _nodes = new List<HeapNode>();

        public int Count => _nodes.Count;

        public void Enqueue(int item, float priority)
        {
            _nodes.Add(new HeapNode { item = item, priority = priority });
            int i = _nodes.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (_nodes[parent].priority <= _nodes[i].priority) break;
                (_nodes[parent], _nodes[i]) = (_nodes[i], _nodes[parent]);
                i = parent;
            }
        }

        public int Dequeue()
        {
            int result = _nodes[0].item;
            int last = _nodes.Count - 1;
            _nodes[0] = _nodes[last];
            _nodes.RemoveAt(last);

            int i = 0;
            while (true)
            {
                int left = 2 * i + 1;
                int right = 2 * i + 2;
                int smallest = i;
                if (left < _nodes.Count && _nodes[left].priority < _nodes[smallest].priority) smallest = left;
                if (right < _nodes.Count && _nodes[right].priority < _nodes[smallest].priority) smallest = right;
                if (smallest == i) break;
                (_nodes[smallest], _nodes[i]) = (_nodes[i], _nodes[smallest]);
                i = smallest;
            }
            return result;
        }
    }

    private readonly int _width;
        private readonly int _height;
        private readonly bool _wrapX;

        public HydrologySystem(int width, int height, bool wrapX = true)
        {
            _width = width;
            _height = height;
            _wrapX = wrapX;
        }

        /// <summary>
        /// 运行完整水文模拟
        /// </summary>
        /// <param name="elevation">原始高程数组（0-1）</param>
        /// <param name="isLand">是否陆地</param>
        /// <param name="riverThreshold">河流阈值（汇水面积超过此值为河流，默认100）</param>
        public void Run(float[] elevation, bool[] isLand, float riverThreshold = 100f)
        {
            int n = _width * _height;
            FilledElevation = new float[n];
            FlowDirection = new int[n];
            FlowAccumulation = new float[n];
            IsRiver = new bool[n];
            StreamOrder = new int[n];

            // 1. 洼地填充 + 排水方向（Priority-Flood）
            PriorityFlood(elevation, isLand);

            // 2. 汇水面积计算
            CalculateFlowAccumulation();

            // 3. 河流提取
            ExtractRivers(riverThreshold);

            // 4. 河流等级（Strahler）
            CalculateStreamOrder();
        }

        /// <summary>
        /// Priority-Flood 核心算法：洼地填充 + 排水方向
        /// 使用最小堆（优先队列），从海洋/边界点开始逐步淹没
        /// </summary>
        private void PriorityFlood(float[] elevation, bool[] isLand)
        {
            int n = _width * _height;
            Array.Copy(elevation, FilledElevation, n);
            Array.Fill(FlowDirection, -1);

            // 优先队列：最小堆（按填充高程排序）
            var pq = new MinHeap();
            var processed = new bool[n];

            // 初始化：所有海洋点和边界点加入队列
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int idx = y * _width + x;
                    bool isBoundary = (x == 0 || x == _width - 1 || y == 0 || y == _height - 1);
                    if (!isLand[idx] || isBoundary)
                    {
                        processed[idx] = true;
                        pq.Enqueue(idx, FilledElevation[idx]);
                    }
                }
            }

            // Priority-Flood 主循环
            while (pq.Count > 0)
            {
                int current = pq.Dequeue();
                int cx = current % _width;
                int cy = current / _width;
                float currentElev = FilledElevation[current];

                // 处理8邻域
                for (int d = 0; d < 8; d++)
                {
                    int nx = cx + DX[d];
                    int ny = cy + DY[d];

                    // 左右环绕
                    if (_wrapX)
                    {
                        if (nx < 0) nx = _width - 1;
                        if (nx >= _width) nx = 0;
                    }
                    else if (nx < 0 || nx >= _width) continue;

                    if (ny < 0 || ny >= _height) continue;

                    int nidx = ny * _width + nx;
                    if (processed[nidx]) continue;

                    processed[nidx] = true;

                    // 洼地填充：如果邻域高程低于当前点，填平到当前点高程
                    if (FilledElevation[nidx] < currentElev)
                    {
                        FilledElevation[nidx] = currentElev;
                    }

                    // 排水方向：邻域水流向当前点（反方向）
                    FlowDirection[nidx] = (d + 4) % 8; // 反方向
                    pq.Enqueue(nidx, FilledElevation[nidx]);
                }
            }
        }

        /// <summary>
        /// 汇水面积计算
        /// 从每个点沿排水方向追溯，累加汇水量
        /// 优化：按高程降序处理，高海拔点先处理，其流量累加到下游
        /// </summary>
        private void CalculateFlowAccumulation()
        {
            int n = _width * _height;
            Array.Fill(FlowAccumulation, 1f); // 每个点初始贡献1

            // 按填充高程降序排列索引（高海拔先处理）
            var indices = new int[n];
            for (int i = 0; i < n; i++) indices[i] = i;
            Array.Sort(indices, (a, b) => FilledElevation[b].CompareTo(FilledElevation[a]));

            // 从高到低处理，每个点的流量累加到其排水目标
            foreach (int idx in indices)
            {
                int dir = FlowDirection[idx];
                if (dir < 0) continue; // 无排水（海洋/边界）

                int cx = idx % _width;
                int cy = idx / _width;
                int nx = cx + DX[dir];
                int ny = cy + DY[dir];

                if (_wrapX)
                {
                    if (nx < 0) nx = _width - 1;
                    if (nx >= _width) nx = 0;
                }
                if (ny < 0 || ny >= _height) continue;

                int nidx = ny * _width + nx;
                FlowAccumulation[nidx] += FlowAccumulation[idx];
            }
        }

        /// <summary>
        /// 河流提取：汇水面积超过阈值的点为河流
        /// </summary>
        private void ExtractRivers(float threshold)
        {
            int n = _width * _height;
            for (int i = 0; i < n; i++)
            {
                IsRiver[i] = FlowAccumulation[i] >= threshold && FlowDirection[i] >= 0;
            }
        }

        /// <summary>
        /// Strahler 河流等级计算
        /// 源头=1，同级汇合+1，不同级取最大
        /// </summary>
        private void CalculateStreamOrder()
        {
            int n = _width * _height;
            Array.Fill(StreamOrder, 0);

            // 按汇水面积升序处理（源头先处理）
            var indices = new int[n];
            for (int i = 0; i < n; i++) indices[i] = i;
            Array.Sort(indices, (a, b) => FlowAccumulation[a].CompareTo(FlowAccumulation[b]));

            foreach (int idx in indices)
            {
                if (!IsRiver[idx]) continue;

                if (StreamOrder[idx] == 0) StreamOrder[idx] = 1; // 源头

                int dir = FlowDirection[idx];
                if (dir < 0) continue;

                int cx = idx % _width;
                int cy = idx / _width;
                int nx = cx + DX[dir];
                int ny = cy + DY[dir];
                if (_wrapX)
                {
                    if (nx < 0) nx = _width - 1;
                    if (nx >= _width) nx = 0;
                }
                if (ny < 0 || ny >= _height) continue;

                int nidx = ny * _width + nx;
                if (!IsRiver[nidx]) continue;

                // Strahler规则：同级汇合+1，不同级取最大
                if (StreamOrder[nidx] == 0) StreamOrder[nidx] = StreamOrder[idx];
                else if (StreamOrder[nidx] == StreamOrder[idx]) StreamOrder[nidx]++;
                else StreamOrder[nidx] = Mathf.Max(StreamOrder[nidx], StreamOrder[idx]);
            }
        }

        /// <summary>
        /// 将水文结果写入 TileData 数组
        /// </summary>
        public void ApplyToTiles(TileData[] tiles)
        {
            int n = Math.Min(tiles.Length, IsRiver.Length);
            for (int i = 0; i < n; i++)
            {
                tiles[i].isRiver = IsRiver[i];
                // 河流等级可以存入扩展字段（当前TileData没有，可后续添加）
            }
        }

        /// <summary>
        /// 获取河流总长度（地块数）
        /// </summary>
        public int GetRiverTileCount()
        {
            int count = 0;
            foreach (var r in IsRiver) if (r) count++;
            return count;
        }

        /// <summary>
        /// 获取最大河流等级
        /// </summary>
        public int GetMaxStreamOrder()
        {
            int max = 0;
            foreach (var o in StreamOrder) if (o > max) max = o;
            return max;
        }
    }
}
