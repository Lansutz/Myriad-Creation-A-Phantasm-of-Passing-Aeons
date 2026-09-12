using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;
using CivilizationEvolution.War;

namespace CivilizationEvolution.Disaster
{
 /// 踩踏事件类型
    public enum StampedeType
    {
 /// <summary>军队溃败踩踏（逃跑时混乱导致）</summary>
        MilitaryRout,
 /// <summary>城市恐慌踩踏（围城/灾难/骚乱时人群拥挤）</summary>
        UrbanPanic,
 /// <summary>集会踩踏（大型集会/庆典时）</summary>
        Gathering
    }

 /// 踩踏事件数据
    [Serializable]
    public class StampedeEvent
    {
        public StampedeType type;
        public int tileIndex;
        public int realmId;
        public int armyId = -1;       // 军队踩踏时的军队ID
        public int burgId = -1;       // 城市踩踏时的聚落ID
        public float severity;         // 严重度 0~1
        public int casualties;         // 伤亡人数
        public int durationDays;       // 持续天数
        public int startDay;
        public string description;
    }

 /// 踩踏事件系统。
 /// 设计原则：
 /// 1. 军队溃败踩踏：逃跑状态 + 组织度过低 + 瞬时伤亡率高 → 触发踩踏，额外非战斗伤亡
 /// 2. 城市恐慌踩踏：围城/灾难/骚乱 + 人口密度过高 → 触发踩踏，人口减少+稳定度下降
 /// 3. 影响因素：训练度越高踩踏概率越低（精锐更有秩序），指挥官能力越高概率越低，狭窄地形更容易
 /// 4. 踩踏是"事件"，不是持续状态，触发后造成一次性伤亡+短期影响
    public static class StampedeSystem
    {
 // ===== 军队溃败踩踏参数 ===== /// <summary>军队踩踏基础触发概率（组织度<20%且逃跑时）</summary>
        public const float MilitaryBaseChance = 0.3f;
 /// <summary>训练度对踩踏概率的减免（训练度100时减免60%）</summary>
        public const float TrainingStampedeReductionMax = 0.6f;
 /// <summary>瞬时伤亡率对踩踏概率的加成（每10%+15%概率）</summary>
        public const float InstantCasualtyChanceBonus = 1.5f;
 /// <summary>军队踩踏基础伤亡率（占当前兵力的比例）</summary>
        public const float MilitaryBaseCasualtyRate = 0.05f;
 /// <summary>踩踏导致的组织度额外下降</summary>
        public const float StampedeOrgLoss = 10f;
 /// <summary>踩踏导致的士气额外下降</summary>
        public const float StampedeMoraleLoss = 15f;

 // ===== 城市恐慌踩踏参数 ===== /// <summary>城市踩踏基础触发概率（围城/灾难时）</summary>
        public const float UrbanBaseChance = 0.15f;
 /// <summary>城市踩踏人口密度阈值（超过此密度概率增加）</summary>
        public const float UrbanDensityThreshold = 5000f;
 /// <summary>城市踩踏基础伤亡率（占城市人口比例）</summary>
        public const float UrbanBaseCasualtyRate = 0.02f;

 /// 检查军队溃败踩踏（在逃跑判定后调用）
 /// <returns>踩踏事件（null=未触发）</returns>
        public static StampedeEvent CheckMilitaryStampede(
            Army army, TileData[] tiles, Dictionary<int, UnitDef> unitDefs,
            float commanderAbility = 50f, int currentDay = 0)
        {
            if (army == null) return null;
 // 只有逃跑状态才可能触发踩踏
            if (army.state != GameEnums.CombatState.Retreating) return null;
 // 组织度过低
            if (army.organization > Army.IneffectiveOrgThreshold) return null;

 // 计算触发概率
            float chance = MilitaryBaseChance;
 // 训练度减免
            float trainingReduction = (army.training / 100f) * TrainingStampedeReductionMax;
            chance *= (1f - trainingReduction);
 // 瞬时伤亡率加成
            if (army.instantCasualtyRate > 0.1f)
                chance *= 1f + (army.instantCasualtyRate - 0.1f) * InstantCasualtyChanceBonus;
 // 指挥官能力减免（能力越高越能维持秩序）
            chance *= (1f - (commanderAbility / 100f) * 0.3f);
 // 地形修正（狭窄地形/山地更容易踩踏）
            if (tiles[army.currentTileIndex].slopeDegree > 30f)
                chance *= 1.3f;

            chance = Mathf.Clamp01(chance);

            if (UnityEngine.Random.value > chance) return null;

 // 触发踩踏
            var stampede = new StampedeEvent
            {
                type = StampedeType.MilitaryRout,
                tileIndex = army.currentTileIndex,
                realmId = army.ownerRealmId,
                armyId = army.armyId,
                severity = Mathf.Clamp01(0.3f + army.instantCasualtyRate + (Army.IneffectiveOrgThreshold - army.organization) / 100f),
                startDay = currentDay,
                durationDays = 1,
                description = $"军队溃败踩踏：组织度崩溃导致混乱，士兵互相践踏"
            };

 // 计算伤亡
            float totalManpower = army.GetTotalManpower(unitDefs);
            float casualtyRate = MilitaryBaseCasualtyRate * stampede.severity * (1f + army.instantCasualtyRate);
            stampede.casualties = Mathf.RoundToInt(totalManpower * casualtyRate);

 // 应用效果
            ApplyMilitaryStampedeEffects(army, stampede, unitDefs);

            return stampede;
        }

 /// <summary>应用军队踩踏效果</summary>
        private static void ApplyMilitaryStampedeEffects(
            Army army, StampedeEvent stampede, Dictionary<int, UnitDef> unitDefs)
        {
 // 按比例减少各兵种
            float totalManpower = army.GetTotalManpower(unitDefs);
            if (totalManpower <= 0) return;
            float lossRatio = stampede.casualties / totalManpower;

            var keys = new List<int>(army.unitCounts.Keys);
            foreach (int unitId in keys)
            {
                army.unitCounts[unitId] = Mathf.Max(0f, army.unitCounts[unitId] * (1f - lossRatio));
                if (army.unitCounts[unitId] <= 0f)
                    army.unitCounts.Remove(unitId);
            }

 // 组织度和士气额外下降
            army.organization = Mathf.Max(0f, army.organization - StampedeOrgLoss * stampede.severity);
            army.morale = Mathf.Max(0f, army.morale - StampedeMoraleLoss * stampede.severity);

 // 更新伤亡率
            army.UpdateCasualtyRates(unitDefs, stampede.casualties);
        }

 /// 检查城市恐慌踩踏（围城/灾难/骚乱时）
        public static StampedeEvent CheckUrbanStampede(
            BurgData burg, TileData[] tiles, bool isUnderSiege, bool hasDisaster,
            float cityStability, int currentDay = 0)
        {
            if (burg == null) return null;
 // 只有主要聚落（城市）才可能发生大规模踩踏
            if (!burg.IsMajorSettlement) return null;
 // 人口密度阈值
            if (burg.population < UrbanDensityThreshold) return null;
 // 需要触发条件：围城 或 灾难 或 稳定度过低
            if (!isUnderSiege && !hasDisaster && cityStability > 50f) return null;

 // 计算触发概率
            float chance = UrbanBaseChance;
            if (isUnderSiege) chance *= 2f;
            if (hasDisaster) chance *= 1.5f;
            if (cityStability < 30f) chance *= 1.5f;
 // 人口密度越高概率越高
            chance *= 0.5f + (burg.population / UrbanDensityThreshold) * 0.5f;
            chance = Mathf.Clamp01(chance);

            if (UnityEngine.Random.value > chance) return null;

 // 触发踩踏
            var stampede = new StampedeEvent
            {
                type = StampedeType.UrbanPanic,
                tileIndex = burg.tileIndex,
                realmId = tiles[burg.tileIndex].ownerRealmId,
                burgId = burg.burgId,
                severity = Mathf.Clamp01(0.2f + (isUnderSiege ? 0.3f : 0f) + (hasDisaster ? 0.2f : 0f) + (100f - cityStability) / 200f),
                startDay = currentDay,
                durationDays = 1,
                description = $"城市恐慌踩踏：{burg.burgName}人群拥挤导致混乱"
            };

 // 计算伤亡
            float casualtyRate = UrbanBaseCasualtyRate * stampede.severity;
            stampede.casualties = Mathf.RoundToInt(burg.population * casualtyRate);

 // 应用效果
            ApplyUrbanStampedeEffects(burg, tiles, stampede);

            return stampede;
        }

 /// <summary>应用城市踩踏效果</summary>
        private static void ApplyUrbanStampedeEffects(BurgData burg, TileData[] tiles, StampedeEvent stampede)
        {
 // 人口减少
            burg.population = Mathf.Max(0f, burg.population - stampede.casualties);
 // 地块稳定度下降
            if (burg.tileIndex >= 0 && burg.tileIndex < tiles.Length)
            {
                tiles[burg.tileIndex].stability = Mathf.Max(0f, tiles[burg.tileIndex].stability - 5f * stampede.severity);
            }
 // 发展度轻微下降
            burg.development = Mathf.Max(0f, burg.development - 1f * stampede.severity);
        }

 /// <summary>获取踩踏事件的描述文本</summary>
        public static string GetStampedeDescription(StampedeEvent stampede)
        {
            return stampede.type switch
            {
                StampedeType.MilitaryRout => $"【军队溃败踩踏】伤亡{stampede.casualties}人，组织度和士气大幅下降",
                StampedeType.UrbanPanic => $"【城市恐慌踩踏】{stampede.casualties}人伤亡，城市稳定度下降",
                StampedeType.Gathering => $"【集会踩踏】{stampede.casualties}人伤亡",
                _ => $"【踩踏事件】伤亡{stampede.casualties}人"
            };
        }
    }
}
