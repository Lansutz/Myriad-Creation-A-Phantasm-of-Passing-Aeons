using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;



namespace CivilizationEvolution.Simulation.Economy
{
    public class TradeCenter
    {
        public int regionId;
        public string centerName;
        public int centerTileIndex;

        [System.NonSerialized]
        public Dictionary<int, float> inventory = new Dictionary<int, float>();
        public float inventoryCapacity = 10000f;

        public List<TradeRoute> tradeRoutes = new List<TradeRoute>();

        [System.NonSerialized]
        public Dictionary<int, float> localDemand = new Dictionary<int, float>();
        [System.NonSerialized]
        public Dictionary<int, float> localSupply = new Dictionary<int, float>();

 /// <summary>获取物资价格（供需决定）</summary>
        public float GetGoodsPrice(int goodsId, Dictionary<int, GoodsDef> goodsDefs)
        {
            if (!goodsDefs.TryGetValue(goodsId, out var def)) return 1f;

            float stock = inventory.GetValueOrDefault(goodsId, 0f);
            float demand = localDemand.GetValueOrDefault(goodsId, 0f);
            float supply = localSupply.GetValueOrDefault(goodsId, 0f);

            float supplyDemandRatio = supply > 0 ? demand / Mathf.Max(0.01f, supply) : 2f;
            supplyDemandRatio = Mathf.Clamp(supplyDemandRatio, 0.2f, 5f);

            float stockFactor = Mathf.Clamp(1.5f - stock / Mathf.Max(1f, inventoryCapacity), 0.3f, 1.5f);

            return def.baseValue * supplyDemandRatio * stockFactor;
        }

        public bool AddGoods(int goodsId, float amount)
        {
            float current = inventory.GetValueOrDefault(goodsId, 0f);
            float totalCurrent = GetTotalInventory();
            float available = inventoryCapacity - totalCurrent;

            if (amount > available)
            {
                inventory[goodsId] = current + available;
                return false;
            }
            inventory[goodsId] = current + amount;
            return true;
        }

        public bool RemoveGoods(int goodsId, float amount)
        {
            float current = inventory.GetValueOrDefault(goodsId, 0f);
            if (current < amount) return false;
            inventory[goodsId] = current - amount;
            return true;
        }

        public float GetTotalInventory()
        {
            float total = 0f;
            foreach (var kv in inventory) total += kv.Value;
            return total;
        }

 /// <summary>更新本地供需（由人口和生产决定）</summary>
        public void UpdateSupplyDemand(TileData[] tiles, Dictionary<int, GoodsDef> goodsDefs, int regionTileStart, int regionTileEnd)
        {
            localSupply.Clear();
            localDemand.Clear();

            for (int i = regionTileStart; i < regionTileEnd && i < tiles.Length; i++)
            {
                if (!tiles[i].isLand) continue;

 // 农业产出
                float agriOutput = tiles[i].fertility * tiles[i].development * 0.5f;
                AddToDict(localSupply, 0, agriOutput);  // 粮食
                AddToDict(localSupply, 10, agriOutput * 0.8f); // 谷物

 // 人口消耗
                float popCount = GetRegionPopulation(tiles, i);
                AddToDict(localDemand, 0, popCount * 0.01f);  // 粮食消耗
                AddToDict(localDemand, 3, popCount * 0.002f);  // 盐消耗

 // 基建产出
                if (tiles[i].buildingLevels[1] > 0) // 手工业
                {
                    AddToDict(localSupply, 70, tiles[i].buildingLevels[1] * 0.1f); // 武器
                    AddToDict(localSupply, 31, tiles[i].buildingLevels[1] * 0.2f); // 加工木材
                }
            }
        }

        private float GetRegionPopulation(TileData[] tiles, int tileIndex)
        {
            float total = 0f;
            if (tiles[tileIndex].populationBlocks != null)
            {
                foreach (var pb in tiles[tileIndex].populationBlocks)
                    total += pb.count;
            }
            return total;
        }

        private void AddToDict(Dictionary<int, float> dict, int key, float value)
        {
            dict[key] = dict.GetValueOrDefault(key, 0f) + value;
        }
    }
}
