using System;

namespace MyriadCreation.Save.Contracts
{
    /// <summary>市场库存条目（纯字段）。</summary>
    [Serializable]
    public class MarketStockEntry
    {
        public int goodsId;
        public float amount;
    }

    /// <summary>
    /// 栖居点市场存档 DTO（**纯字段**；库存/需求/供给；容量随仓储块走，但也存一份兜底）。
    /// 映射（Capture/ToMarket）在 `MyriadCreation.World.Settlement.SettlementMarketSaveMapping`。
    /// </summary>
    [Serializable]
    public class SettlementMarketSaveData
    {
        public float capacity;
        public MarketStockEntry[] inventory;
        public MarketStockEntry[] demand;
        public MarketStockEntry[] supply;
    }
}
