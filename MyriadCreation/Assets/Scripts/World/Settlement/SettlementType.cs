using System;

namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// 定居点形态——仅用于 Burg（定居点）类别下的具体样子。
    /// 据点（Outpost）的形态由 FortSubtype 决定，不在此枚举内。
    /// 营地（Camp）的形态由规模和用途决定，不在此枚举内。
    /// 等级（settlementLevel）独立于形态。
    /// </summary>
    public enum SettlementType
    {
        Village,  // 村镇：村落、集镇，生产功能为主，防御薄弱，辐射范围小
        City      // 城：城邑、都会、大都会，区域综合型中心，功能复合
    }
}
