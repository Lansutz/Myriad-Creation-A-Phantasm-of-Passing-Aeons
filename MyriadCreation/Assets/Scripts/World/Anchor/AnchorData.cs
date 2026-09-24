using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyriadCreation.World.Anchor
{
    /// <summary>
    /// 锚点（Anchor）——地图上的空间支点。
    /// 独立于地块之上，是一个可以被点击、放置、删除、移动的位置实体。
    /// 锚点只管空间：它在哪、它控制哪个地块、它和其他锚点的控制关系。
    /// 锚点上面的聚落内容（人口、建筑、经济、政治角色）全部在 SettlementData 里，
    /// 通过 anchorId 关联。
    /// </summary>
    [Serializable]
    public class AnchorData
    {
        /// <summary>锚点ID（全局唯一）</summary>
        public int anchorId;

        /// <summary>所属省份</summary>
        public int provinceId;

        /// <summary>所在地块（单元格索引）——锚点落在哪块地上，那块地就是"活的"</summary>
        public int tileIndex;

        /// <summary>地块内精确坐标（0~1，用于像素级定位）</summary>
        public float x;
        public float y;

        // ===== 锚点间控制关系 =====
        /// <summary>控制本锚点的上级锚点ID（-1=独立）</summary>
        public int controllerAnchorId = -1;

        /// <summary>本锚点控制的下级锚点ID列表</summary>
        [NonSerialized] public List<int> controlledAnchorIds = new List<int>();

        /// <summary>对各下属锚点的控制进度（anchorId -> 0~100）</summary>
        [NonSerialized] public Dictionary<int, float> controlProgress = new Dictionary<int, float>();

        /// <summary>影响力半径（地块数）</summary>
        public int influenceRadius = 1;

        /// <summary>控制是否稳固（进度到100后变为稳固，流失更慢）</summary>
        public bool isControlStable = false;

        /// <summary>关联瓶颈地块ID（关口/渡口/海峡的控制节点，-1表示无）</summary>
        public int bottleneckTileIndex = -1;
    }
}
