using System;

namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// 城墙的一块横向段（沿墙长方向切开的一段）。
    /// 一个城墙块可以有多个段，一个聚落可以有多个城墙块；段不是独立城墙块。
    /// </summary>
    [Serializable]
    public class WallSegment
    {
        /// <summary>该段实际长度（米）；0 表示由所在城墙块长度分摊/估算。</summary>
        public float length;

        /// <summary>完好度 0..1；缺省 1。</summary>
        public float condition = 1f;

        /// <summary>该段塔楼数。</summary>
        public int towerCount;

        /// <summary>该段是否有城门。</summary>
        public bool hasGate;

        /// <summary>该段护城河等级（0..5）。</summary>
        public int moatLevel;
    }
}
