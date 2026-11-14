using UnityEngine;

namespace MyriadCreation.Rendering
{
    /// <summary>
    /// 等高线判定：高程跨过同一间隔时落线。纯函数，便于测试；线宽由调用方决定。
    /// </summary>
    public static class TerrainContour
    {
        public static bool Crosses(float elevation, float westOrNorth, float interval)
        {
            interval = Mathf.Max(0.01f, interval);
            return Mathf.FloorToInt(elevation / interval) != Mathf.FloorToInt(westOrNorth / interval);
        }

        public static bool ShouldDraw(float elevation, float west, float north, float interval)
        {
            return Crosses(elevation, west, interval) || Crosses(elevation, north, interval);
        }
    }
}
