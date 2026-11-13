using System.Collections.Generic;
using MyriadCreation.Save.Contracts;

namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// `SettlementMarket` 与存档 DTO 的映射（**状态所有者侧**）。
    /// DTO 在 `MyriadCreation.Save.Contracts`（只依赖 Core），映射留在这里，
    /// 存档层（Infrastructure）因此不必引用 World。
    /// </summary>
    public static class SettlementMarketSaveMapping
    {
        public static SettlementMarketSaveData Capture(SettlementMarket market)
        {
            if (market == null) return null;
            return new SettlementMarketSaveData
            {
                capacity = market.capacity,
                inventory = ToEntries(market.inventory),
                demand = ToEntries(market.demand),
                supply = ToEntries(market.supply),
            };
        }

        public static SettlementMarket ToMarket(SettlementMarketSaveData data)
        {
            if (data == null) return null;
            var market = new SettlementMarket(data.capacity);
            if (data.inventory != null)
                foreach (var entry in data.inventory)
                    if (entry != null && entry.amount > 0f) market.inventory[entry.goodsId] = entry.amount;
            if (data.demand != null)
                foreach (var entry in data.demand)
                    if (entry != null && entry.amount > 0f) market.demand[entry.goodsId] = entry.amount;
            if (data.supply != null)
                foreach (var entry in data.supply)
                    if (entry != null && entry.amount > 0f) market.supply[entry.goodsId] = entry.amount;
            return market;
        }

        private static MarketStockEntry[] ToEntries(Dictionary<int, float> source)
        {
            if (source == null || source.Count == 0) return null;
            var result = new List<MarketStockEntry>();
            foreach (var pair in source)
                result.Add(new MarketStockEntry { goodsId = pair.Key, amount = pair.Value });
            return result.ToArray();
        }
    }
}
