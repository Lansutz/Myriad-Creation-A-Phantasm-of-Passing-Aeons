using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Economy
{
    /// <summary>物资定义</summary>
    [System.Serializable]
    public struct GoodsDef
    {
        public int goodsId;
        public string goodsName;
        public GameEnums.GoodsCategory category;
        public float baseValue;
        public float weight;
        public bool hasShelfLife;
        public float shelfLifeDays;
        public List<GameEnums.BiomeType> originBiomes;
        public int processedFromId;
        public float processingRatio;
    }

    /// <summary>贸易中心（每个大地区一个）</summary>
    [System.Serializable]
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

    /// <summary>贸易路线</summary>
    [System.Serializable]
    public class TradeRoute
    {
        public int fromRegionId;
        public int toRegionId;
        public List<int> nodeTileIndices = new List<int>();
        public float baseEfficiency = 1.0f;
        public float currentEfficiency = 1.0f;
        public bool isBlocked = false;

        /// <summary>计算贸易效率修正</summary>
        public void CalculateEfficiency(TileData[] tiles)
        {
            if (isBlocked || nodeTileIndices.Count == 0) { currentEfficiency = 0f; return; }

            float efficiency = baseEfficiency;

            // 道路等级
            float avgRoad = 0f;
            foreach (int idx in nodeTileIndices)
                avgRoad += (int)tiles[idx].roadLevel;
            avgRoad /= nodeTileIndices.Count;
            efficiency *= 0.6f + avgRoad * 0.15f;

            // 治安
            float avgStability = 0f;
            foreach (int idx in nodeTileIndices)
                avgStability += tiles[idx].stability;
            avgStability /= nodeTileIndices.Count;
            efficiency *= 0.5f + avgStability / 200f;

            // 距离衰减
            efficiency *= Mathf.Exp(-nodeTileIndices.Count * 0.02f);

            currentEfficiency = Mathf.Clamp(efficiency, 0f, 1.5f);
        }
    }

    /// <summary>商队</summary>
    [System.Serializable]
    public class Caravan
    {
        public int caravanId;
        public int fromRegionId;
        public int toRegionId;
        public int currentNodeIndex;
        [System.NonSerialized]
        public Dictionary<int, float> cargo = new Dictionary<int, float>();
        public float capacity = 500f;
        public float speed = 1f;
        public bool isMoving = true;
        public float moveProgress = 0f;

        public float GetCargoWeight(Dictionary<int, GoodsDef> goodsDefs)
        {
            float weight = 0f;
            foreach (var kv in cargo)
            {
                if (goodsDefs.TryGetValue(kv.Key, out var def))
                    weight += kv.Value * def.weight;
            }
            return weight;
        }

        /// <summary>商队移动Tick</summary>
        public bool MoveTick(TradeRoute route, TileData[] tiles)
        {
            if (!isMoving || route.isBlocked) return false;

            moveProgress += speed * route.currentEfficiency;
            if (moveProgress >= 1f)
            {
                moveProgress = 0f;
                currentNodeIndex++;
                if (currentNodeIndex >= route.nodeTileIndices.Count)
                {
                    isMoving = false;
                    return true; // 到达目的地
                }
            }
            return false;
        }
    }

    /// <summary>货币系统</summary>
    [System.Serializable]
    public class CurrencySystem
    {
        public GameEnums.CurrencyStage currentStage = GameEnums.CurrencyStage.Barter;
        public string currencyName = "";
        public float goldReserve = 0f;
        public float silverReserve = 0f;
        public float coinPurity = 1.0f;
        public float paperMoneyInCirculation = 0f;
        public float inflationRate = 0f;

        /// <summary>计算货币价值</summary>
        public float GetCurrencyValue()
        {
            return currentStage switch
            {
                GameEnums.CurrencyStage.Barter => 1f,
                GameEnums.CurrencyStage.Bullion => 1f,
                GameEnums.CurrencyStage.MintedCoin => coinPurity,
                GameEnums.CurrencyStage.PaperMoney => Mathf.Clamp(
                    (goldReserve + silverReserve * 0.1f) / Mathf.Max(1f, paperMoneyInCirculation),
                    0.01f, 2f),
                _ => 1f
            };
        }

        /// <summary>铸造劣币</summary>
        public void DebaseCoin(float purityReduction, float amountMinted)
        {
            coinPurity = Mathf.Max(0.1f, coinPurity - purityReduction);
            inflationRate += purityReduction * 0.5f;
        }

        /// <summary>发行纸币</summary>
        public bool IssuePaperMoney(float amount)
        {
            float reserveValue = goldReserve + silverReserve * 0.1f;
            float maxIssue = reserveValue * 3f;
            if (paperMoneyInCirculation + amount > maxIssue) return false;

            paperMoneyInCirculation += amount;
            if (paperMoneyInCirculation > reserveValue * 1.5f)
                inflationRate += (paperMoneyInCirculation / reserveValue - 1.5f) * 0.1f;
            return true;
        }

        /// <summary>每日通胀衰减</summary>
        public void DailyTick()
        {
            inflationRate = Mathf.Max(0f, inflationRate - 0.001f);
        }
    }

    /// <summary>税收系统</summary>
    [System.Serializable]
    public class TaxSystem
    {
        public float agriculturalTax = 0.1f;
        public float headTax = 0.05f;
        public float tradeTax = 0.1f;
        public float miningTax = 0.15f;
        public float craftTax = 0.1f;
        public float livestockTax = 0.08f;
        public float luxuryTax = 0.3f;
        public float saltMonopolyTax = 0.5f;
        public float wartimeSpecialTax = 0f;

        [System.NonSerialized]
        public Dictionary<GameEnums.SocialClass, bool> taxExemptions = new Dictionary<GameEnums.SocialClass, bool>();

        /// <summary>计算地块实际税收</summary>
        public float CalculateTileTax(TileData tile, float baseOutput, GameEnums.SocialClass dominantClass)
        {
            if (taxExemptions.GetValueOrDefault(dominantClass, false)) return 0f;

            float controlEfficiency = 0.3f + tile.stability / 100f * 0.7f;
            float combinedRate = agriculturalTax * 0.4f + headTax * 0.2f + tradeTax * 0.2f + craftTax * 0.1f + wartimeSpecialTax * 0.1f;

            // 最优税率区间：超过30%后边际收益递减
            float effectiveRate = combinedRate < 0.3f
                ? combinedRate
                : 0.3f + (combinedRate - 0.3f) * 0.5f;

            return baseOutput * effectiveRate * controlEfficiency;
        }

        /// <summary>计算税率对阶层好感的影响</summary>
        public float GetTaxSatisfactionImpact(GameEnums.SocialClass socialClass)
        {
            float impact = socialClass switch
            {
                GameEnums.SocialClass.Peasant => -(agriculturalTax + headTax) * 50f,
                GameEnums.SocialClass.MerchantFreeman => -(tradeTax + craftTax) * 40f,
                GameEnums.SocialClass.NobilityClergy => -(luxuryTax + livestockTax) * 30f,
                GameEnums.SocialClass.Slave => -headTax * 20f,
                _ => 0f
            };
            impact -= wartimeSpecialTax * 60f;
            return Mathf.Clamp(impact, -50f, 10f);
        }
    }

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
