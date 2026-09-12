using System;

namespace CivilizationEvolution.Core
{
    /// <summary>地图尺寸预设（宽x高，总地块数）</summary>
    public enum MapSizePreset
    {
        Large,     // 1024x512 = 524,288 地块（最小）
        Huge,      // 2048x1024 = 2,097,152 地块（默认-平衡）
        Reference, // 1920x1080 = 2,073,600 地块（对齐参考项目）
        Enormous   // 3072x1536 = 4,718,592 地块（最大-需16GB+内存）
    }

    /// <summary>地图环绕模式——决定边界是否连通</summary>
    public enum MapWrapMode
    {
        Flat,        // 平面：四边都不连通，标准矩形地图
        Cylindrical, // 柱面：左右连通（东西环绕），上下不连通-模拟地球
        Toroidal     // 环面：左右上下全连通
    }
}
