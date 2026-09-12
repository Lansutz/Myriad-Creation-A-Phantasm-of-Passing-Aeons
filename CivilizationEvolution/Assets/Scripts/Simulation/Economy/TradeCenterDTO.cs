
using System.Collections.Generic;
using System;

using CivilizationEvolution.Core.Dto;
namespace CivilizationEvolution.Simulation.Economy
{
    [Serializable]
    public class TradeCenterDTO
    {
        public int regionId;
        public string centerName;
        public int centerTileIndex;
        public List<IntFloatEntry> inventory = new List<IntFloatEntry>();
        public float inventoryCapacity;
        public List<TradeRoute> tradeRoutes = new List<TradeRoute>();
        public List<IntFloatEntry> localDemand = new List<IntFloatEntry>();
        public List<IntFloatEntry> localSupply = new List<IntFloatEntry>();

        public static TradeCenterDTO FromTradeCenter(TradeCenter tc)
        {
            var dto = new TradeCenterDTO
            {
                regionId = tc.regionId,
                centerName = tc.centerName,
                centerTileIndex = tc.centerTileIndex,
                inventoryCapacity = tc.inventoryCapacity,
                tradeRoutes = tc.tradeRoutes
            };
            if (tc.inventory != null)
                foreach (var kv in tc.inventory) dto.inventory.Add(new IntFloatEntry(kv.Key, kv.Value));
            if (tc.localDemand != null)
                foreach (var kv in tc.localDemand) dto.localDemand.Add(new IntFloatEntry(kv.Key, kv.Value));
            if (tc.localSupply != null)
                foreach (var kv in tc.localSupply) dto.localSupply.Add(new IntFloatEntry(kv.Key, kv.Value));
            return dto;
        }

        public TradeCenter ToTradeCenter()
        {
            var tc = new TradeCenter
            {
                regionId = regionId,
                centerName = centerName,
                centerTileIndex = centerTileIndex,
                inventoryCapacity = inventoryCapacity,
                tradeRoutes = tradeRoutes
            };
            foreach (var e in inventory) tc.inventory[e.key] = e.value;
            foreach (var e in localDemand) tc.localDemand[e.key] = e.value;
            foreach (var e in localSupply) tc.localSupply[e.key] = e.value;
            return tc;
        }
    }
}
