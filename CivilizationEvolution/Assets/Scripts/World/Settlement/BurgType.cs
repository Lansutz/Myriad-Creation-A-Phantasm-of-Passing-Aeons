using System;

namespace CivilizationEvolution.Map
{
    public enum BurgType
    {
        Village,   // 村庄（最低级，多数 Burg）
        Town,      // 集镇（有一定发展度）
        City,      // 城市（高发展度，省中心）
        Port,      // 港口（沿海/沿河，贸易节点）
        Capital,   // 首都（政权首都，特殊 Burg）
        Fortress   // 要塞（军事据点）
    }

    /// <summary>根据 BurgType 推断初始 SettlementType（纯数据逻辑，放在 World 层避免反向依赖）。</summary>
    public static class BurgTypeInferrer
    {
        public static SettlementType InferSettlementType(BurgType burgType)
        {
            switch (burgType)
            {
                case BurgType.Fortress: return SettlementType.Fort;
                case BurgType.City:
                case BurgType.Port:
                case BurgType.Capital: return SettlementType.City;
                default: return SettlementType.Village;
            }
        }
    }
}
