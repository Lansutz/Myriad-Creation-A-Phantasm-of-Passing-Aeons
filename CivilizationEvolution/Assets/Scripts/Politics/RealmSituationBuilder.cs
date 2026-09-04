using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;
using CivilizationEvolution.War;
using CivilizationEvolution.Disaster;
using CivilizationEvolution.Tech;
using CivilizationEvolution.Culture;

namespace CivilizationEvolution.Politics
{
    /// <summary>
    /// 政权情境采集器：每个政治 Tick 从各子系统采集客观指标，组装成 RealmSituation，
    /// 供 ClassNeedsSystem 评估。集中在此处对接，避免需求/社会系统直接耦合经济、战争、灾害等。
    /// </summary>
    public static class RealmSituationBuilder
    {
        // 食品货物ID（EconomySystem.ProcessConsumption 以 goodsId=0 为食品）
        const int FoodGoodsId = 0;
        // 食品库存"安全线"= 多少天消耗量（库存/日需求 达到此值视为供给充足=1）
        const float FoodReserveTargetDays = 30f;

        /// <summary>
        /// 采集一个政权的需求情境。
        /// </summary>
        /// <param name="realm">政权</param>
        /// <param name="tiles">全图地块</param>
        /// <param name="economy">经济管理器（贸易中心库存/商路，可空）</param>
        /// <param name="wars">进行中的战争列表（可空）</param>
        /// <param name="armies">全图军队（判断本土交战，可空）</param>
        /// <param name="disasters">灾害系统（可空）</param>
        /// <param name="innovations">革新树（阶层制度承认判定，可空=宽松）</param>
        public static RealmSituation Build(
            RealmData realm, TileData[] tiles,
            EconomyManager economy = null,
            List<WarState> wars = null,
            Dictionary<int, Army> armies = null,
            DisasterSystem disasters = null,
            InnovationTree innovations = null,
            IReadOnlyList<int> realmTiles = null)
        {
            var sit = new RealmSituation { realmId = realm.realmId };

            // ===== 单次遍历政权地块，汇总人口/治安/地区/灾害/占领 =====
            float totalPop = 0f, orderSum = 0f, stabilitySum = 0f;
            int landCount = 0;
            var regions = new HashSet<int>();
            float disasterAccum = 0f;
            bool occupiedHomeSoil = false;

            foreach (int idx in EnumerateRealmTiles(realm, tiles, realmTiles))
            {
                ref TileData t = ref tiles[idx];
                regions.Add(t.regionId);
                orderSum += t.order;
                stabilitySum += t.stability;
                landCount++;

                if (t.populationBlocks != null)
                    foreach (var pb in t.populationBlocks) totalPop += pb.count;

                if (disasters != null)
                {
                    var ds = disasters.GetDisastersAtTile(idx);
                    foreach (var d in ds) disasterAccum += d.severity;
                }

                if (t.occupyingRealmId != -1 && t.occupyingRealmId != t.ownerRealmId)
                    occupiedHomeSoil = true;
            }

            sit.publicOrder = landCount > 0 ? orderSum / landCount : 50f;
            float avgStability = landCount > 0 ? stabilitySum / landCount : 50f;
            sit.disasterSeverity = Mathf.Clamp(disasterAccum * 2f, 0f, 100f);

            // ===== 粮食保障：本政权涉及地区的食品库存 / (日需求×安全天数) =====
            float dailyFoodNeed = Mathf.Max(0.01f, totalPop * 0.01f);
            float foodStock = 0f;
            float routeEffSum = 0f; int routeCount = 0;
            if (economy != null)
            {
                foreach (int regionId in regions)
                {
                    var tc = economy.GetTradeCenter(regionId);
                    if (tc == null) continue;
                    foodStock += tc.inventory.GetValueOrDefault(FoodGoodsId, 0f);
                    if (tc.tradeRoutes != null)
                    {
                        foreach (var route in tc.tradeRoutes)
                        {
                            if (route.isBlocked) { routeEffSum += 0f; routeCount++; }
                            else { routeEffSum += Mathf.Clamp01(route.currentEfficiency); routeCount++; }
                        }
                    }
                }
            }
            sit.foodSecurity = Mathf.Clamp(foodStock / (dailyFoodNeed * FoodReserveTargetDays), 0f, 1.2f);
            sit.tradeFlow = routeCount > 0 ? routeEffSum / routeCount : 0.8f; // 无商路数据时给温和默认

            // ===== 战争状态 =====
            bool atWar = false, enemyOnSoil = occupiedHomeSoil;
            if (wars != null)
            {
                foreach (var w in wars)
                {
                    if (w.ended) continue;
                    if (w.attackerId != realm.realmId && w.defenderId != realm.realmId) continue;
                    atWar = true;
                    int enemyId = w.attackerId == realm.realmId ? w.defenderId : w.attackerId;
                    // 本土有敌方军队 = 兵临境内
                    if (armies != null)
                    {
                        foreach (var army in armies.Values)
                        {
                            if (army.ownerRealmId != enemyId) continue;
                            if (realm.coreTiles.Contains(army.currentTileIndex)) { enemyOnSoil = true; break; }
                        }
                    }
                }
            }
            sit.atWar = atWar;
            sit.warOnHomeSoil = enemyOnSoil;

            // ===== 税负痛感（TaxSystem 返回 -50~+10 的影响值，取反为 0~100 痛感）=====
            foreach (GameEnums.SocialClass cls in Enum.GetValues(typeof(GameEnums.SocialClass)))
            {
                float impact = realm.taxSystem != null ? realm.taxSystem.GetTaxSatisfactionImpact(cls) : 0f;
                sit.taxPain[cls] = Mathf.Clamp(-impact, 0f, 100f);
            }

            // ===== 政治通道（解析政体七维成分）=====
            sit.politicalAccess = PoliticalAccessAnalyzer.GetAllAccess(realm.composition);

            // ===== 货币稳定 =====
            if (realm.currencySystem != null)
            {
                // 通胀率越高越不稳定；币值（成色/储备）作为补充
                float val = realm.currencySystem.GetCurrencyValue();
                sit.monetaryStability = Mathf.Clamp01(1f - realm.currencySystem.inflationRate * 2f) * Mathf.Clamp01(val);
            }
            else sit.monetaryStability = 1f;

            // ===== 阶层制度承认（革新+文化）=====
            CultureData culture = null;
            if (realm.primaryCultureId >= 0 && ContentRegistry.TryGetCulture(realm.primaryCultureId, out var pack))
                culture = pack.data;
            foreach (GameEnums.SocialClass cls in Enum.GetValues(typeof(GameEnums.SocialClass)))
                sit.classRecognized[cls] = SocialClassAvailability.IsClassAvailable(cls, culture, innovations, realm.realmId);

            // ===== 合法性：稳定度 + 威望 =====
            sit.stability = realm.stability;
            sit.legitimacy = Mathf.Clamp(realm.stability * 0.6f + realm.prestige * 0.4f, 0f, 100f);

            // ===== 贵族特权保障：世袭最高权力 + 地方世袭领有 + 贵族免税 =====
            var comp = realm.composition;
            float priv = 0.3f;
            if ((SupremeSuccession)comp.supremeSuccession.primary == SupremeSuccession.Hereditary) priv += 0.3f;
            if ((LocalSuccession)comp.localSuccession.primary == LocalSuccession.Hereditary) priv += 0.25f;
            if (comp.centralInstitution.primary == (int)CentralInstitution.EldersCouncil) priv += 0.15f;
            if (realm.taxSystem != null && realm.taxSystem.taxExemptions
                    .GetValueOrDefault(GameEnums.SocialClass.NobilityClergy, false)) priv += 0.1f;
            sit.privilegeSecurity = Mathf.Clamp01(priv);

            return sit;
        }

        /// <summary>枚举政权所有领有地块（核心 + 非核心领有），去重</summary>
        static IEnumerable<int> EnumerateRealmTiles(RealmData realm, TileData[] tiles,
            IReadOnlyList<int> realmTiles = null)
        {
            // 优化 2026-09-04：调用方传入领地索引（每日一次全扫构建）——
            // 避免每政权全扫（N×tiles）——无索引时原逻辑（兼容）
            if (realmTiles != null)
            {
                foreach (int idx in realmTiles)
                    if (idx >= 0 && idx < tiles.Length) yield return idx;
                yield break;
            }
            var seen = new HashSet<int>();
            foreach (int idx in realm.coreTiles)
            {
                if (idx < 0 || idx >= tiles.Length || !seen.Add(idx)) continue;
                yield return idx;
            }
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].ownerRealmId != realm.realmId || !seen.Add(i)) continue;
                yield return i;
            }
        }
    }
}
