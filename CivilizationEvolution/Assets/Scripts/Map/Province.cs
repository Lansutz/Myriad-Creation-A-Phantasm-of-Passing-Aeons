using UnityEngine;
using System;
using System.Collections.Generic;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Map
{
 /// 沃罗诺伊细胞聚合的地块集合：政体/文化/战争/贸易的归属载体
    [Serializable]
    public class Province
    {
        public int provinceId;
        public string provinceName;
        public int centerTileIndex;      // 省中心地块（种子点位置）
        public List<int> memberTiles = new List<int>();

 /// <summary>省界判定：与任一邻域省份归属不同即为边界地块（静态——供渲染与测试）</summary>
        public static bool IsBorder(TileData[] tiles, int width, int height, int index)
        {
            if (tiles[index].provinceId < 0) return false;
            int x = index % width;
            int y = index / width;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    int ni = ny * width + nx;
                    if (!tiles[ni].isLand) continue; // 邻海不算省界
                    if (tiles[ni].provinceId != tiles[index].provinceId)
                        return true;
                }
            }
            return false;
        }
    }

 /// 1. 陆地随机种子点（数量=陆地 tile 数 / cellsPerProvince）
 /// 2. Lloyd 松弛迭代：每 tile 归最近种子 → 种子移到所属集合质心 → 循环
 /// 3. 输出省份集合（每省中心/成员/省名）

}
