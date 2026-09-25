using System;

namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// 聚居点子类型——某种类别下的具体子类。
    /// 类别是定居点/据点/营地，子类型是这个类别下具体是什么。
    /// 等级（settlementLevel）独立于子类型——村可以很大，王帐可以很小。
    /// </summary>
    public enum SettlementType
    {
        // 定居点类别下
        Village,  // 村：小型农耕聚落，生产为主
        Town,     // 镇：有一定发展度，市集/交换节点
        City,     // 城市：区域中心，功能复合，有区划

        // 据点类别下
        Fort,     // 堡垒：军事据点，控制通道/要地。形态有好几种（关口堡/坞堡/高地堡等），由 bottleneckType 等参数区分

        // 营地类别下（预留）
        Camp      // 营地：可移动/季节性（王帐/行营/普通营地）
    }
}
