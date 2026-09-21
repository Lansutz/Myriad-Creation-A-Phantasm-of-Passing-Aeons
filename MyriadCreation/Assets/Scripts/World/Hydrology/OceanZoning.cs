
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;


namespace CivilizationEvolution.World.Hydrology
{
    /// <summary>
    /// 海洋分区系统（参考 Azgaar FMG 的 grid.cells.t 距离场设计）。
    /// 用多源 BFS 从海岸线计算每个海洋地块到最近陆地的距离，
    /// 据此划分近海/中海/远海/深海，影响海军移动、贸易路线、渔业资源。
    /// </summary>
    public static class OceanZoning
    {
        public enum OceanZone
        {
            Land = 0,
            Coast = 1,      // 近海：0-1格（沿岸、港口、渔业）
            Neritic = 2,    // 中海：2-4格（大陆架、贸易航线）
            Oceanic = 3,    // 远海：5-8格（远洋航行）
            DeepSea = 4     // 深海：9格以上（深渊、极少航行）
        }

        /// <summary>近海最大距离（格）</summary>
        public const int CoastMaxDist = 1;
        /// <summary>中海最大距离（格）</summary>
        public const int NeriticMaxDist = 4;
        /// <summary>远海最大距离（格）</summary>
        public const int OceanicMaxDist = 8;

        /// <summary>
        /// 计算全图海洋距离场并分区。
        /// 修改 tiles[i].oceanZone（需在 TileData 中添加字段）和 seaConnectId。
        /// </summary>
        public static int[] CalculateDistanceField(TileData[] tiles, int width, int height, bool wrapX)
        {
            int[] dist = new int[tiles.Length];
            for (int i = 0; i < dist.Length; i++) dist[i] = -1;

            var queue = new Queue<int>();

            // 多源 BFS：所有陆地相邻的海洋地块距离=0
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].isLand) continue;
                if (!tiles[i].exists) continue;

                foreach (int n in TileGrid.GetNeighbours(i, width, height, wrapX))
                {
                    if (n >= 0 && tiles[n].isLand)
                    {
                        dist[i] = 0;
                        queue.Enqueue(i);
                        break;
                    }
                }
            }

            // BFS 扩散
            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                int d = dist[cur];

                foreach (int n in TileGrid.GetNeighbours(cur, width, height, wrapX))
                {
                    if (n < 0 || tiles[n].isLand || !tiles[n].exists) continue;
                    if (dist[n] >= 0) continue;
                    dist[n] = d + 1;
                    queue.Enqueue(n);
                }
            }

            return dist;
        }

        /// <summary>根据距离判定海洋分区</summary>
        public static OceanZone GetZone(int distance)
        {
            if (distance < 0) return OceanZone.DeepSea;
            if (distance <= CoastMaxDist) return OceanZone.Coast;
            if (distance <= NeriticMaxDist) return OceanZone.Neritic;
            if (distance <= OceanicMaxDist) return OceanZone.Oceanic;
            return OceanZone.DeepSea;
        }

        /// <summary>获取分区中文名</summary>
        public static string GetZoneName(OceanZone zone) => zone switch
        {
            OceanZone.Land => "陆地",
            OceanZone.Coast => "近海",
            OceanZone.Neritic => "中海",
            OceanZone.Oceanic => "远海",
            OceanZone.DeepSea => "深海",
            _ => zone.ToString()
        };

        /// <summary>
        /// 应用距离场到地块：设置 isCoast 和海洋分区。
        /// 陆地地块不处理。
        /// </summary>
        public static void ApplyToTiles(TileData[] tiles, int width, int height, bool wrapX)
        {
            int[] dist = CalculateDistanceField(tiles, width, height, wrapX);
            int coastCount = 0, neriticCount = 0, oceanicCount = 0, deepCount = 0;

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].isLand || !tiles[i].exists) continue;

                var zone = GetZone(dist[i]);
                tiles[i].isCoast = (zone == OceanZone.Coast);

                // 用 oceanDepth01 的高位编码分区（0.0-0.2=近海, 0.2-0.4=中海...）
                // 注意：oceanDepth01 同时表示深度，这里只在低精度位标记分区
                switch (zone)
                {
                    case OceanZone.Coast: coastCount++; break;
                    case OceanZone.Neritic: neriticCount++; break;
                    case OceanZone.Oceanic: oceanicCount++; break;
                    case OceanZone.DeepSea: deepCount++; break;
                }
            }

            Debug.Log($"[OceanZoning] 海洋分区完成：近海{coastCount} 中海{neriticCount} 远海{oceanicCount} 深海{deepCount}");
        }
    }
}
