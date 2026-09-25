using System;

namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// 聚居点形态——定居点类别下的具体样子。
    /// 类别是定居点/据点/营地，形态是这个类别下具体是什么。
    /// 据点的形态由 FortSubtype 决定，营地的形态由规模和用途决定。
    /// 等级（settlementLevel）独立于形态——村镇可以很大，堡垒可以很小。
    /// </summary>
    public enum SettlementType
    {
        Village,  // 村镇：村落、集镇，生产功能为主，防御薄弱，辐射范围小
        City,     // 城：城邑、都会、大都会，区域综合型中心，功能复合
        Fort      // 堡：堡垒、要塞、堡寨，军事防御为核心，等级跨度完整
    }
}
