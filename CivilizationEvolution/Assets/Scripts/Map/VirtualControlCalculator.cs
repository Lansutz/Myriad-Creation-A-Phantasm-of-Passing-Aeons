using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Map
{
 /// 三角形（三个控制点索引 + 外接圆）
    public struct ControlTriangle
    {
        public int a, b, c; // 控制点索引
        public Vector2 circumcenter;
        public float circumradiusSq;
        public int ownerRealmId;
        public bool isValid;

        public ControlTriangle(int a_, int b_, int c_, Vector2[] points)
        {
            this.a = a_; this.b = b_; this.c = c_;
            circumcenter = Vector2.zero;
            circumradiusSq = 0f;
            ownerRealmId = -1;
            isValid = false;
            CalculateCircumcircle(points);
        }

        private void CalculateCircumcircle(Vector2[] points)
        {
            Vector2 p1 = points[a], p2 = points[b], p3 = points[c];
            float ax = p1.x, ay = p1.y, bx = p2.x, by = p2.y, cx = p3.x, cy = p3.y;
            float d = 2f * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
            if (Mathf.Abs(d) < 1e-10f) { isValid = false; return; }
            float ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d;
            float uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d;
            circumcenter = new Vector2(ux, uy);
            circumradiusSq = (ux - ax) * (ux - ax) + (uy - ay) * (uy - ay);
            isValid = true;
        }

        public bool InCircumcircle(Vector2 p)
        {
            float dx = p.x - circumcenter.x, dy = p.y - circumcenter.y;
            return dx * dx + dy * dy <= circumradiusSq + 1e-6f;
        }

        public float GetMinAngleDeg(Vector2[] points)
        {
            Vector2 p1 = points[a], p2 = points[b], p3 = points[c];
            float ab = (p2 - p1).magnitude, bc = (p3 - p2).magnitude, ca = (p1 - p3).magnitude;
            float angleA = Mathf.Acos(Mathf.Clamp((ab * ab + ca * ca - bc * bc) / (2f * ab * ca), -1f, 1f)) * Mathf.Rad2Deg;
            float angleB = Mathf.Acos(Mathf.Clamp((ab * ab + bc * bc - ca * ca) / (2f * ab * bc), -1f, 1f)) * Mathf.Rad2Deg;
            return Mathf.Min(angleA, angleB, 180f - angleA - angleB);
        }
    }

 /// 虚控制范围计算结果（渲染用）
    public class VirtualControlResult
    {
 /// <summary>有效三角形列表（Delaunay + 角度 + 同政权）</summary>
        public List<ControlTriangle> triangles = new List<ControlTriangle>();
 /// <summary>虚控制地块集合（地块索引 → 政权ID）</summary>
        public Dictionary<int, int> virtualControlTiles = new Dictionary<int, int>();
 /// <summary>控制点之间的连接边</summary>
        public List<(int a, int b)> connections = new List<(int, int)>();
    }

 /// 三角虚控制范围计算器（渲染层工具）。
 /// 设计原则：
 /// 1. 控制点 = 有主要聚落(Burg)的地块，且属于同一政权
 /// 2. 任意三个同政权控制点 → Delaunay 三角剖分
 /// 3. 三点共线/角度太小(＜15°) → 无效，不形成控制
 /// 4. 有效三角形内部 = 虚控制范围（影响力，半透明显示，非实际占领）
 /// 5. 两点之间：中间有该政权占领地 → 连接；没有 → 不连接（飞地）
 /// 6. 这是政权图层渲染和显示层面的，不是复杂模拟系统
 /// 模拟早期文明的"点-线-面"控制模式：
 /// 点=聚落，线=聚落间联系，面=三个聚落形成的三角形影响力范围
    public static class VirtualControlCalculator
    {
 /// <summary>有效三角形最小角度（度数）</summary>
        public const float MinTriangleAngleDeg = 15f;

 /// <summary>计算虚控制范围（渲染用）</summary>
        public static VirtualControlResult Calculate(
            Dictionary<int, BurgData> burgs,
            TileData[] tiles,
            int mapWidth,
            int mapHeight)
        {
            var result = new VirtualControlResult();
            if (burgs == null || burgs.Count == 0 || tiles == null) return result;

 // 1. 收集有效控制点（同政权分组）
            var controlPointsByRealm = new Dictionary<int, List<(int burgId, Vector2 pos)>>();
            foreach (var kv in burgs)
            {
                var burg = kv.Value;
                if (!burg.IsMajorSettlement) continue;
                if (burg.tileIndex < 0 || burg.tileIndex >= tiles.Length) continue;
                int realmId = tiles[burg.tileIndex].ownerRealmId;
                if (realmId < 0) continue;
                if (!controlPointsByRealm.ContainsKey(realmId))
                    controlPointsByRealm[realmId] = new List<(int, Vector2)>();
                int x = burg.tileIndex % mapWidth;
                int y = burg.tileIndex / mapWidth;
                controlPointsByRealm[realmId].Add((kv.Key, new Vector2(x, y)));
            }

 // 2. 对每个政权的控制点进行 Delaunay 三角剖分
            foreach (var kv in controlPointsByRealm)
            {
                int realmId = kv.Key;
                var points = kv.Value;
                if (points.Count < 3) continue;

                var pointArray = new Vector2[points.Count];
                for (int i = 0; i < points.Count; i++) pointArray[i] = points[i].pos;

                var triangles = BowyerWatson(pointArray);

 // 3. 有效三角形判定 + 虚控制范围标记
                foreach (var tri in triangles)
                {
                    if (!tri.isValid) continue;
                    if (tri.GetMinAngleDeg(pointArray) < MinTriangleAngleDeg) continue;

                    var triWithOwner = tri;
                    triWithOwner.ownerRealmId = realmId;
                    result.triangles.Add(triWithOwner);

                    MarkTriangleInteriorTiles(triWithOwner, pointArray, mapWidth, mapHeight, realmId, result);
                }

 // 4. 两点连接判定
                CalculateConnections(points, tiles, mapWidth, mapHeight, realmId, result);
            }

            return result;
        }

 /// <summary>Bowyer-Watson Delaunay 三角剖分</summary>
        private static List<ControlTriangle> BowyerWatson(Vector2[] points)
        {
            var triangles = new List<ControlTriangle>();
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in points) { minX = Mathf.Min(minX, p.x); minY = Mathf.Min(minY, p.y); maxX = Mathf.Max(maxX, p.x); maxY = Mathf.Max(maxY, p.y); }
            float dx = maxX - minX, dy = maxY - minY, deltaMax = Mathf.Max(dx, dy) * 10f;
            float midX = (minX + maxX) * 0.5f, midY = (minY + maxY) * 0.5f;

            var superPoints = new List<Vector2>(points)
            {
                new Vector2(midX - deltaMax, midY - deltaMax),
                new Vector2(midX + deltaMax, midY - deltaMax),
                new Vector2(midX, midY + deltaMax)
            };
            var superArray = superPoints.ToArray();
            int n = points.Length;
            triangles.Add(new ControlTriangle(n, n + 1, n + 2, superArray));

            for (int i = 0; i < n; i++)
            {
                var badTriangles = new List<ControlTriangle>();
                foreach (var tri in triangles) if (tri.InCircumcircle(points[i])) badTriangles.Add(tri);

                var polygon = new List<(int a, int b)>();
                foreach (var tri in badTriangles)
                {
                    var edges = new (int, int)[] { (tri.a, tri.b), (tri.b, tri.c), (tri.c, tri.a) };
                    foreach (var edge in edges)
                    {
                        bool shared = false;
                        foreach (var other in badTriangles)
                        {
                            if (other.Equals(tri)) continue;
                            var otherEdges = new (int, int)[] { (other.a, other.b), (other.b, other.c), (other.c, other.a) };
                            foreach (var oe in otherEdges)
                            {
                                if ((edge.Item1 == oe.Item1 && edge.Item2 == oe.Item2) || (edge.Item1 == oe.Item2 && edge.Item2 == oe.Item1)) { shared = true; break; }
                            }
                            if (shared) break;
                        }
                        if (!shared) polygon.Add(edge);
                    }
                }

                foreach (var bad in badTriangles) triangles.Remove(bad);
                foreach (var edge in polygon)
                {
                    var newTri = new ControlTriangle(edge.Item1, edge.Item2, i, superArray);
                    if (newTri.isValid) triangles.Add(newTri);
                }
            }

            triangles.RemoveAll(t => t.a >= n || t.b >= n || t.c >= n);
            return triangles;
        }

 /// <summary>标记三角形内部地块为虚控制范围</summary>
        private static void MarkTriangleInteriorTiles(
            ControlTriangle tri, Vector2[] points, int mapWidth, int mapHeight,
            int realmId, VirtualControlResult result)
        {
            Vector2 p1 = points[tri.a], p2 = points[tri.b], p3 = points[tri.c];
            int minX = Mathf.Max(0, (int)Mathf.Min(p1.x, p2.x, p3.x) - 1);
            int maxX = Mathf.Min(mapWidth - 1, (int)Mathf.Max(p1.x, p2.x, p3.x) + 1);
            int minY = Mathf.Max(0, (int)Mathf.Min(p1.y, p2.y, p3.y) - 1);
            int maxY = Mathf.Min(mapHeight - 1, (int)Mathf.Max(p1.y, p2.y, p3.y) + 1);

            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    if (PointInTriangle(p, p1, p2, p3))
                    {
                        int tileIdx = y * mapWidth + x;
                        if (!result.virtualControlTiles.ContainsKey(tileIdx))
                            result.virtualControlTiles[tileIdx] = realmId;
                    }
                }
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3) =>
            (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

 /// <summary>计算两点之间的连接（中间是否有该政权占领地）</summary>
        private static void CalculateConnections(
            List<(int burgId, Vector2 pos)> points, TileData[] tiles,
            int mapWidth, int mapHeight, int realmId, VirtualControlResult result)
        {
            for (int i = 0; i < points.Count; i++)
                for (int j = i + 1; j < points.Count; j++)
                {
                    if (HasOccupiedTileAlongLine(points[i].pos, points[j].pos, tiles, mapWidth, mapHeight, realmId))
                        result.connections.Add((points[i].burgId, points[j].burgId));
                }
        }

        private static bool HasOccupiedTileAlongLine(
            Vector2 p1, Vector2 p2, TileData[] tiles, int mapWidth, int mapHeight, int realmId)
        {
            float dist = Vector2.Distance(p1, p2);
            int steps = Mathf.Max(2, (int)dist);
            for (int s = 1; s < steps; s++)
            {
                float t = (float)s / steps;
                int x = (int)(p1.x + (p2.x - p1.x) * t);
                int y = (int)(p1.y + (p2.y - p1.y) * t);
                if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight) continue;
                int idx = y * mapWidth + x;
                if (idx >= 0 && idx < tiles.Length && tiles[idx].ownerRealmId == realmId) return true;
            }
            return false;
        }
    }
}
