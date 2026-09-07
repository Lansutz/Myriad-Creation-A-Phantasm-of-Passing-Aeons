using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Economy
{
    /// <summary>物资定义</summary>


    /// <summary>贸易中心（每个大地区一个）</summary>


    /// <summary>贸易路线</summary>


    /// <summary>商队</summary>
    /// <summary>货币系统</summary>


    /// <summary>税收系统</summary>


    /// <summary>
    /// 经济管理器
    /// 协调贸易中心、商队、货币、税收的每日运行
    /// </summary>
    public class EconomyManager
    {
        private readonly TileData[] _tiles;
        private readonly Dictionary<int, TradeCenter> _tradeCenters;
        private readonly Dictionary<int, GoodsDef> _goodsDefs;
        private readonly List<Caravan> _caravans = new List<Caravan>();
        private readonly CurrencySystem _currency;
        private readonly TaxSystem _taxSystem;
        private int _nextCaravanId = 1;

        public EconomyManager(TileData[] tiles, Dictionary<int, TradeCenter> tradeCenters,
            Dictionary<int, GoodsDef> goodsDefs, CurrencySystem currency, TaxSystem taxSystem)
        {
            _tiles = tiles;
            _tradeCenters = tradeCenters;
            _goodsDefs = goodsDefs;
            _currency = currency;
            _taxSystem = taxSystem;
        }

        /// <summary>
        /// 仓储容量随建筑更新：地区内农业建筑（粮仓——Agriculture 槽）每级 +10% 仓储容量。
        /// 仓储=地区物资仓库（本地产出+贸易品），容量决定能存多少物资
        /// </summary>
        public void UpdateStorageCapacities()
        {
            if (_tiles == null || _tradeCenters == null) return;

            foreach (var kv in _tradeCenters)
            {
                var tc = kv.Value;
                int baseCapacity = 10000;

                // 统计该地区农业建筑最高等级（粮仓类→存储容量）
                int maxAgriLevel = 0;
                for (int i = 0; i < _tiles.Length; i++)
                {
                    if (_tiles[i].regionId != tc.regionId) continue;
                    if (_tiles[i].buildingLevels != null && _tiles[i].buildingLevels[0] > maxAgriLevel)
                        maxAgriLevel = _tiles[i].buildingLevels[0];
                }

                tc.inventoryCapacity = baseCapacity * (1f + maxAgriLevel * 0.1f);
            }
        }

        private int _storageUpdateDay = -1;

        /// <summary>每日经济Tick</summary>
        public void DailyTick()
        {
            // 0. 仓储容量随建筑更新（粮仓等农业建筑→地区仓储容量；30 天限频——全扫代价高）
            if (_storageUpdateDay < 0 || _storageUpdateDay >= 30)
            {
                UpdateStorageCapacities();
                _storageUpdateDay = 0;
            }
            else _storageUpdateDay++;

            // 1. 更新所有贸易中心供需
            UpdateAllSupplyDemand();

            // 2. 计算所有贸易路线效率
            CalculateAllRouteEfficiency();

            // 3. 发起新贸易（AI自动匹配供需）
            InitiateTrades();

            // 4. 移动所有商队
            MoveCaravans();

            // 5. 物资消耗（人口、军队）
            ProcessConsumption();

            // 6. 货币通胀更新
            _currency.DailyTick();

            // 7. 保质期检查
            ProcessShelfLife();
        }

        private void UpdateAllSupplyDemand()
        {
            // 简化：每16个地块一个地区
            int tilesPerRegion = 16;
            foreach (var kv in _tradeCenters)
            {
                int start = kv.Key * tilesPerRegion;
                int end = start + tilesPerRegion;
                kv.Value.UpdateSupplyDemand(_tiles, _goodsDefs, start, end);
            }
        }

        private void CalculateAllRouteEfficiency()
        {
            foreach (var tc in _tradeCenters.Values)
            {
                foreach (var route in tc.tradeRoutes)
                {
                    route.CalculateEfficiency(_tiles);
                }
            }
        }

        /// <summary>AI自动发起贸易：寻找供需差最大的物资配对</summary>
        private void InitiateTrades()
        {
            if (_caravans.Count > 20) return; // 限制同时存在的商队数量

            foreach (var fromTC in _tradeCenters.Values)
            {
                foreach (var route in fromTC.tradeRoutes)
                {
                    if (route.isBlocked || route.currentEfficiency < 0.1f) continue;
                    if (!_tradeCenters.TryGetValue(route.toRegionId, out var toTC)) continue;

                    // 找供需差最大的物资
                    int bestGoods = -1;
                    float bestProfit = 0f;

                    foreach (var supplyKv in fromTC.localSupply)
                    {
                        int goodsId = supplyKv.Key;
                        float fromPrice = fromTC.GetGoodsPrice(goodsId, _goodsDefs);
                        float toPrice = toTC.GetGoodsPrice(goodsId, _goodsDefs);
                        float profit = (toPrice - fromPrice) * route.currentEfficiency;

                        if (profit > bestProfit && fromTC.inventory.GetValueOrDefault(goodsId, 0f) > 10f)
                        {
                            bestProfit = profit;
                            bestGoods = goodsId;
                        }
                    }

                    if (bestGoods >= 0 && bestProfit > 0.5f)
                    {
                        // 发起商队
                        float amount = Mathf.Min(100f, fromTC.inventory[bestGoods] * 0.3f);
                        if (fromTC.RemoveGoods(bestGoods, amount))
                        {
                            var caravan = new Caravan
                            {
                                caravanId = _nextCaravanId++,
                                fromRegionId = route.fromRegionId,
                                toRegionId = route.toRegionId,
                                currentNodeIndex = 0,
                                isMoving = true
                            };
                            caravan.cargo[bestGoods] = amount;
                            _caravans.Add(caravan);
                        }
                    }
                }
            }
        }

        /// <summary>移动所有商队，到达后卸货</summary>
        private void MoveCaravans()
        {
            for (int i = _caravans.Count - 1; i >= 0; i--)
            {
                var caravan = _caravans[i];
                var fromTC = _tradeCenters.GetValueOrDefault(caravan.fromRegionId);
                if (fromTC == null) { _caravans.RemoveAt(i); continue; }

                var route = fromTC.tradeRoutes.Find(r => r.toRegionId == caravan.toRegionId);
                if (route == null) { _caravans.RemoveAt(i); continue; }

                bool arrived = caravan.MoveTick(route, _tiles);

                if (arrived)
                {
                    // 到达目的地，卸货
                    if (_tradeCenters.TryGetValue(caravan.toRegionId, out var toTC))
                    {
                        foreach (var cargoKv in caravan.cargo)
                        {
                            toTC.AddGoods(cargoKv.Key, cargoKv.Value);
                        }
                    }
                    _caravans.RemoveAt(i);
                }
            }
        }

        /// <summary>物资消耗：人口消耗食品、盐</summary>
        private void ProcessConsumption()
        {
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (!_tiles[i].exists || !_tiles[i].isLand || _tiles[i].populationBlocks == null) continue;

                float totalPop = 0f;
                foreach (var pb in _tiles[i].populationBlocks)
                    totalPop += pb.count;

                if (totalPop <= 0) continue;

                // 从最近的贸易中心库存中扣除
                int regionId = _tiles[i].regionId;
                if (_tradeCenters.TryGetValue(regionId, out var tc))
                {
                    float foodNeed = totalPop * 0.01f;
                    float saltNeed = totalPop * 0.002f;

                    if (!tc.RemoveGoods(0, foodNeed))
                    {
                        // 食品不足：满意度下降
                        foreach (var pb in _tiles[i].populationBlocks)
                        {
                            // 注意：struct需要特殊处理，这里简化
                        }
                    }
                    tc.RemoveGoods(3, saltNeed);
                }
            }
        }

        /// <summary>保质期检查：过期物资损耗</summary>
        private void ProcessShelfLife()
        {
            // 简化：每日食品类物资有0.1%的损耗
            foreach (var tc in _tradeCenters.Values)
            {
                var keys = new List<int>(tc.inventory.Keys);
                foreach (int goodsId in keys)
                {
                    if (_goodsDefs.TryGetValue(goodsId, out var def) && def.hasShelfLife)
                    {
                        tc.inventory[goodsId] *= 0.999f;
                        if (tc.inventory[goodsId] < 0.01f)
                            tc.inventory.Remove(goodsId);
                    }
                }
            }
        }

        /// <summary>结算全地区税收</summary>
        public float SettleTaxes(int realmId)
        {
            float totalTax = 0f;
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (!_tiles[i].exists || !_tiles[i].isLand || _tiles[i].ownerRealmId != realmId) continue;

                float baseOutput = _tiles[i].fertility * _tiles[i].development * 10f;
                var dominantClass = GetDominantClass(_tiles[i]);
                totalTax += _taxSystem.CalculateTileTax(_tiles[i], baseOutput, dominantClass);
            }
            return totalTax;
        }

        private GameEnums.SocialClass GetDominantClass(TileData tile)
        {
            if (tile.populationBlocks == null || tile.populationBlocks.Count == 0)
                return GameEnums.SocialClass.Peasant;

            var classCounts = new Dictionary<GameEnums.SocialClass, float>();
            foreach (var pb in tile.populationBlocks)
            {
                classCounts[pb.socialClass] = classCounts.GetValueOrDefault(pb.socialClass, 0f) + pb.count;
            }

            GameEnums.SocialClass dominant = GameEnums.SocialClass.Peasant;
            float maxCount = 0f;
            foreach (var kv in classCounts)
            {
                if (kv.Value > maxCount)
                {
                    maxCount = kv.Value;
                    dominant = kv.Key;
                }
            }
            return dominant;
        }

        public IReadOnlyList<Caravan> GetActiveCaravans() => _caravans;
        public CurrencySystem GetCurrencySystem() => _currency;
        public TaxSystem GetTaxSystem() => _taxSystem;

        /// <summary>按地区获取贸易中心（角色饮食联动等外部查询用）</summary>
        public TradeCenter GetTradeCenter(int regionId)
        {
            return _tradeCenters.GetValueOrDefault(regionId);
        }
    }
}
