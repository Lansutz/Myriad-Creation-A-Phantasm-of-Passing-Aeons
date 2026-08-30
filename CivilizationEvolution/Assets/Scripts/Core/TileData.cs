using System;
using System.Collections.Generic;
using UnityEngine;

namespace CivilizationEvolution.Core
{
    /// <summary>
    /// 地块完整数据结构
    /// 所有模拟数据下沉到地块级，行省仅为逻辑分组
    /// </summary>
    [System.Serializable]
    public struct TileData
    {
        // ===== 基础标识 =====
        public int tileIndex;
        public int regionId;
        /// <summary>省份归属（沃罗诺伊省区——ProvinceGenerator 生成时赋值并重置，-1=未归属）</summary>
        public int provinceId;
        public int ownerRealmId;
        public int occupyingRealmId;

        /// <summary>地块是否存在（支持任意形状地图，false=虚空/地图外）</summary>
        public bool exists;

        // ===== 地形 =====
        public float elevation01;
        public float slopeDegree;
        public float terrainShade;
        public bool isGate;
        public GameEnums.RoadLevel roadLevel;

        // ===== 海陆属性 =====
        public bool isLand;
        public bool isCoast;
        public GameEnums.OceanTier oceanTier;
        public float oceanDepth01;
        public int seaConnectId;
        public float waterAdjacentWeight;

        // ===== 气候 =====
        public float annualTemp;
        public float diurnalTempRange;
        public float annualPrecipMm;
        public float airHumidityPct;
        public float soilHumidityPct;
        public float accumulatedTemp;
        public float frostFreeDays;
        public GameEnums.ClimateZone climateZone;
        public GameEnums.BiomeType biome;

        // ===== 经济 =====
        public float fertility;
        public float development;
        public float stability;
        public float order;
        public int[] buildingLevels;

        // ===== 人口 =====
        public List<PopulationBlock> populationBlocks;

        // ===== 脏标记 =====
        public bool isTerrainDirty;
        public bool isClimateDirty;
        public bool isEconomyDirty;
    }

    /// <summary>
    /// 人口块：50自然人为一个块，浮点精细存储
    /// </summary>
    [System.Serializable]
    public struct PopulationBlock
    {
        public float count;
        public int raceId;
        public int cultureId;
        public int faithId;
        public GameEnums.SocialClass socialClass;
        public int profession;
        public float satisfaction;
        public float culturePenetration;
    }

    /// <summary>
    /// 地块网格坐标辅助
    /// 统一处理坐标转换、邻接计算、环绕（左右连通）
    /// even-r偏移六边形坐标
    /// </summary>
    public static class TileGrid
    {
        /// <summary>坐标转索引</summary>
        public static int ToIndex(int x, int y, int width) => y * width + x;

        /// <summary>索引转x坐标</summary>
        public static int ToX(int index, int width) => index % width;

        /// <summary>索引转y坐标</summary>
        public static int ToY(int index, int width) => index / width;

        /// <summary>x轴环绕（左右连通）</summary>
        public static int WrapX(int x, int width) => ((x % width) + width) % width;

        /// <summary>y轴环绕（上下连通，可选）</summary>
        public static int WrapY(int y, int height) => ((y % height) + height) % height;

        /// <summary>
        /// 获取六边形6邻接索引（even-r偏移坐标）
        /// </summary>
        /// <param name="index">中心地块索引</param>
        /// <param name="width">地图宽度</param>
        /// <param name="height">地图高度</param>
        /// <param name="wrapX">是否左右连通（x轴环绕）</param>
        /// <param name="wrapY">是否上下连通（y轴环绕）</param>
        /// <returns>有效的邻接索引列表（越界且不环绕则跳过）</returns>
        public static List<int> GetNeighbours(int index, int width, int height, bool wrapX = true, bool wrapY = false)
        {
            int x = ToX(index, width);
            int y = ToY(index, width);
            var result = new List<int>(6);

            // even-r偏移：偶数行和奇数行的邻接偏移不同
            int[,] evenRowOffsets = { { 1, 0 }, { 0, -1 }, { -1, -1 }, { -1, 0 }, { -1, 1 }, { 0, 1 } };
            int[,] oddRowOffsets = { { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, 0 }, { 0, 1 }, { 1, 1 } };

            int[,] offsets = (y % 2 == 0) ? evenRowOffsets : oddRowOffsets;

            for (int i = 0; i < 6; i++)
            {
                int nx = x + offsets[i, 0];
                int ny = y + offsets[i, 1];

                if (wrapX) nx = WrapX(nx, width);
                else if (nx < 0 || nx >= width) continue;

                if (wrapY) ny = WrapY(ny, height);
                else if (ny < 0 || ny >= height) continue;

                result.Add(ToIndex(nx, ny, width));
            }

            return result;
        }

        /// <summary>
        /// 六边形距离（支持x轴环绕）
        /// </summary>
        public static int HexDistance(int indexA, int indexB, int width, bool wrapX = true)
        {
            int ax = ToX(indexA, width);
            int ay = ToY(indexA, width);
            int bx = ToX(indexB, width);
            int by = ToY(indexB, width);

            if (wrapX)
            {
                int dx = Mathf.Abs(ax - bx);
                dx = Mathf.Min(dx, width - dx); // 环绕取最短
                ax = 0; bx = dx;
            }

            // 偏移坐标转立方坐标
            int acx = ax - (ay - (ay & 1)) / 2;
            int acz = ay;
            int acy = -acx - acz;

            int bcx = bx - (by - (by & 1)) / 2;
            int bcz = by;
            int bcy = -bcx - bcz;

            return (Mathf.Abs(acx - bcx) + Mathf.Abs(acy - bcy) + Mathf.Abs(acz - bcz)) / 2;
        }

        /// <summary>
        /// 获取圆形画笔范围内的所有地块索引
        /// </summary>
        /// <param name="centerIndex">中心索引</param>
        /// <param name="radius">半径（地块数）</param>
        /// <param name="width">地图宽度</param>
        /// <param name="height">地图高度</param>
        /// <param name="wrapX">是否左右连通</param>
        /// <returns>范围内的地块索引列表</returns>
        public static List<int> GetCircleBrush(int centerIndex, int radius, int width, int height, bool wrapX = true)
        {
            var result = new List<int>();
            int cx = ToX(centerIndex, width);
            int cy = ToY(centerIndex, width);

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;

                    if (wrapX) nx = WrapX(nx, width);
                    else if (nx < 0 || nx >= width) continue;

                    if (ny < 0 || ny >= height) continue;

                    // 用曼哈顿距离近似圆形（六边形网格）
                    int dist = (Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dx + dy)) / 2;
                    if (dist <= radius)
                    {
                        result.Add(ToIndex(nx, ny, width));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取方形画笔范围内的所有地块索引
        /// </summary>
        public static List<int> GetSquareBrush(int centerIndex, int halfSize, int width, int height, bool wrapX = true)
        {
            var result = new List<int>();
            int cx = ToX(centerIndex, width);
            int cy = ToY(centerIndex, width);

            for (int dy = -halfSize; dy <= halfSize; dy++)
            {
                for (int dx = -halfSize; dx <= halfSize; dx++)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;

                    if (wrapX) nx = WrapX(nx, width);
                    else if (nx < 0 || nx >= width) continue;

                    if (ny < 0 || ny >= height) continue;

                    result.Add(ToIndex(nx, ny, width));
                }
            }

            return result;
        }
    }

}
