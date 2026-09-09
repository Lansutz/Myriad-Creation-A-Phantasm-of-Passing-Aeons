using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Map
{
 /// 聚落控制/影响力范围系统。 /// 设计原则： /// 1. 每个聚落有等级(Ⅰ-Ⅴ)和辐射范围 /// 2. 同等级聚落不互相控制（平等关系） /// 3. 高等级聚落对辐射范围内低等级聚落有控制/影响力 /// 4. 控制进度随时间增加，速度取决于驻扎部队的质量和数量 /// 5. 打下高等级聚落后，随时间推移其下属聚落控制逐步流失 /// 6. 不驻扎部队 → 控制进度倒退，最终叛乱/独立 /// 7. 虹吸效应通过税收和贸易自然表现（下属聚落向控制聚落缴纳额外税收，贸易优先经过） /// 模拟早期文明的"城市节点控制"模式： /// 早期通过城市节点实现控制，不是明确边界；高等级城市控制周围低等级聚落    public static class SettlementControlSystem
    {
 /// <summary>控制进度每日基础增长速度</summary>        public const float BaseControlSpeed = 0.5f;
 /// <summary>控制进度完成阈值（到100即完成控制）</summary>        public const float ControlCompleteThreshold = 100f;
 /// <summary>无驻扎时控制进度每日倒退速度</summary>        public const float NoGarrisonDecaySpeed = 1f;
 /// <summary>宗主被占领后，下属聚落控制流失加速系数</summary>        public const float SuzerainOccupiedDecayMultiplier = 3f;
 /// <summary>控制稳固后流失速度减半</summary>        public const float StableDecayMultiplier = 0.5f;

 /// 每日Tick：更新所有聚落的控制关系和进度        public static void DailyTick(
            Dictionary<int, BurgData> burgs,
            TileData[] tiles,
            int mapWidth,
            int mapHeight,
            Dictionary<int, CivilizationEvolution.War.Army> armies = null)
        {
            if (burgs == null || burgs.Count == 0) return;

 // 1. 更新每个聚落的驻扎部队信息            UpdateGarrisonInfo(burgs, tiles, armies);

 // 2. 扫描控制关系（高等级 → 低等级）            ScanControlRelationships(burgs, tiles, mapWidth, mapHeight);

 // 3. 更新控制进度            UpdateControlProgress(burgs, tiles);

 // 4. 检查控制完成/流失            CheckControlCompletion(burgs);
        }

 /// <summary>更新每个聚落的驻扎部队信息</summary>        private static void UpdateGarrisonInfo(
            Dictionary<int, BurgData> burgs,
            TileData[] tiles,
            Dictionary<int, CivilizationEvolution.War.Army> armies)
        {
 // 先重置            foreach (var burg in burgs.Values)
            {
                burg.garrisonQuantity = burg.garrison; // 用BurgData自带的garrison字段
                burg.garrisonQuality = 50f; // 默认质量
            }

 // 如果有军队数据，统计驻扎在聚落地块的军队            if (armies != null)
            {
                foreach (var army in armies.Values)
                {
                    if (army.state == CivilizationEvolution.Core.GameEnums.CombatState.Dead) continue;
 // 找军队所在地块的聚落                    foreach (var burg in burgs.Values)
                    {
                        if (burg.tileIndex == army.currentTileIndex && tiles[burg.tileIndex].ownerRealmId == army.ownerRealmId)
                        {
                            burg.garrisonQuantity += (int)army.GetTotalManpower(null); // 简化
                            burg.garrisonQuality = Mathf.Max(burg.garrisonQuality, army.training);
                        }
                    }
                }
            }
        }

 /// <summary>扫描控制关系：高等级聚落控制辐射范围内低等级聚落</summary>        private static void ScanControlRelationships(
            Dictionary<int, BurgData> burgs,
            TileData[] tiles,
            int mapWidth,
            int mapHeight)
        {
 // 按等级从高到低排序，高等级先建立控制            var sortedBurgs = new List<BurgData>(burgs.Values);
            sortedBurgs.Sort((a, b) => b.settlementLevel.CompareTo(a.settlementLevel));

            foreach (var controller in sortedBurgs)
            {
                if (!controller.IsMajorSettlement) continue;
                if (controller.controllerBurgId >= 0) continue; // 自己被控制，不再控制别人

                int radius = GetInfluenceRadius(controller);
                int cx = controller.tileIndex % mapWidth;
                int cy = controller.tileIndex / mapWidth;

 // 扫描辐射范围内的聚落                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (dx * dx + dy * dy > radius * radius) continue;
                        int px = cx + dx, py = cy + dy;
                        if (px < 0 || px >= mapWidth || py < 0 || py >= mapHeight) continue;
                        int tileIdx = py * mapWidth + px;

 // 找这个地块上的聚落                        foreach (var target in burgs.Values)
                        {
                            if (target.burgId == controller.burgId) continue;
                            if (target.tileIndex != tileIdx) continue;
                            if (target.controllerBurgId >= 0) continue; // 已被控制
                            if (tiles[target.tileIndex].ownerRealmId != tiles[controller.tileIndex].ownerRealmId) continue; // 不同政权不控制

 // 等级判定：高等级控制低等级，同等级不控制                            if (target.settlementLevel < controller.settlementLevel)
                            {
 // 建立控制关系（如果还没有）                                if (!controller.controlProgress.ContainsKey(target.burgId))
                                {
                                    controller.controlProgress[target.burgId] = 0f;
                                    if (!controller.controlledBurgIds.Contains(target.burgId))
                                        controller.controlledBurgIds.Add(target.burgId);
                                }
                            }
                        }
                    }
                }
            }
        }

 /// <summary>更新控制进度</summary>        private static void UpdateControlProgress(Dictionary<int, BurgData> burgs, TileData[] tiles)
        {
            foreach (var controller in burgs.Values)
            {
                if (controller.controlledBurgIds.Count == 0) continue;

 // 宗主是否被敌方占领                bool suzerainOccupied = false; // 宗主被占领判定在调用方传入

                foreach (int targetId in controller.controlledBurgIds)
                {
                    if (!burgs.ContainsKey(targetId)) continue;
                    if (!controller.controlProgress.ContainsKey(targetId)) continue;

                    var target = burgs[targetId];
                    float progress = controller.controlProgress[targetId];

 // 计算控制速度                    float speed = CalculateControlSpeed(controller, target, tiles);

                    if (suzerainOccupied)
                    {
 // 宗主被占领：控制加速流失                        float decay = NoGarrisonDecaySpeed * SuzerainOccupiedDecayMultiplier;
                        if (target.isControlStable) decay *= StableDecayMultiplier;
                        progress = Mathf.Max(0f, progress - decay);
                    }
                    else if (controller.garrisonQuantity <= 0)
                    {
 // 无驻扎：控制进度倒退                        float decay = NoGarrisonDecaySpeed;
                        if (target.isControlStable) decay *= StableDecayMultiplier;
                        progress = Mathf.Max(0f, progress - decay);
                    }
                    else
                    {
 // 有驻扎：控制进度增长                        progress = Mathf.Min(ControlCompleteThreshold, progress + speed);
                    }

                    controller.controlProgress[targetId] = progress;

 // 进度归零 → 失去控制                    if (progress <= 0f)
                    {
                        target.controllerBurgId = -1;
                        target.isControlStable = false;
                        controller.controlProgress.Remove(targetId);
                    }
                }
            }
        }

 /// <summary>计算控制速度（取决于驻扎部队质量和数量）</summary>        private static float CalculateControlSpeed(BurgData controller, BurgData target, TileData[] tiles)
        {
            float speed = BaseControlSpeed;

 // 驻扎部队数量修正：越多越快（上限200%）            float quantityFactor = Mathf.Clamp(1f + controller.garrisonQuantity / 500f, 1f, 3f);
            speed *= quantityFactor;

 // 驻扎部队质量修正：训练度越高越快（上限150%）            float qualityFactor = 0.7f + controller.garrisonQuality / 100f * 0.8f;
            speed *= qualityFactor;

 // 等级差修正：等级差越大控制越快            int levelDiff = controller.settlementLevel - target.settlementLevel;
            speed *= 1f + levelDiff * 0.2f;

 // 距离修正：越近越快 // (简化，已在辐射范围内)
 // 文化修正：同文化控制更快（简化，暂不实现）
            return speed;
        }

 /// <summary>检查控制完成</summary>        private static void CheckControlCompletion(Dictionary<int, BurgData> burgs)
        {
            foreach (var controller in burgs.Values)
            {
                foreach (int targetId in controller.controlledBurgIds)
                {
                    if (!burgs.ContainsKey(targetId)) continue;
                    if (!controller.controlProgress.ContainsKey(targetId)) continue;

                    if (controller.controlProgress[targetId] >= ControlCompleteThreshold)
                    {
                        var target = burgs[targetId];
                        target.controllerBurgId = controller.burgId;
                        target.isControlStable = true;
                    }
                }
            }
        }

 /// <summary>获取聚落影响力半径（按等级）</summary>        public static int GetInfluenceRadius(BurgData burg)
        {
 // Ⅰ=1, Ⅱ=2, Ⅲ=3, Ⅳ=4, Ⅴ=5            return Mathf.Clamp((int)burg.settlementLevel, 1, 5);
        }

 /// <summary>获取聚落的宗主（控制它的高等级聚落）</summary>        public static BurgData GetSuzerain(BurgData burg, Dictionary<int, BurgData> burgs)
        {
            if (burg.controllerBurgId < 0) return null;
            return burgs.GetValueOrDefault(burg.controllerBurgId);
        }

 /// <summary>获取聚落的税收虹吸修正（下属聚落向宗主缴纳额外税收）</summary>        public static float GetTaxSiphonModifier(BurgData burg, Dictionary<int, BurgData> burgs)
        {
            var suzerain = GetSuzerain(burg, burgs);
            if (suzerain == null) return 1f;
 // 下属聚落向宗主缴纳10%额外税收            return 0.9f;
        }

 /// <summary>获取聚落的贸易虹吸修正（下属聚落贸易优先经过宗主）</summary>        public static float GetTradeSiphonModifier(BurgData burg, Dictionary<int, BurgData> burgs)
        {
            var suzerain = GetSuzerain(burg, burgs);
            if (suzerain == null) return 1f;
 // 下属聚落贸易力量20%流向宗主            return 0.8f;
        }
    }
}
