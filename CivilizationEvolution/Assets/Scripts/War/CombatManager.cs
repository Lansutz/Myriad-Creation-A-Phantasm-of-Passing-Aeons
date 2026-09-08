using CivilizationEvolution.Diplomacy;
using CivilizationEvolution.Map;
using System.Collections.Generic;
using System;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Disaster;

namespace CivilizationEvolution.War
{
    public class CombatManager
    {
        private readonly TileData[] _tiles;
        private readonly Dictionary<int, UnitDef> _unitDefs;
        private readonly SeaLandGenerator _seaLand;

        public CombatManager(TileData[] tiles, Dictionary<int, UnitDef> unitDefs, SeaLandGenerator seaLand)
        {
            _tiles = tiles;
            _unitDefs = unitDefs;
            _seaLand = seaLand;
        }

        /// <summary>
        /// 解决一场战斗（分阶段推进 + 战斗结束机制）。
        /// 设计原则（用户定稿）：
        /// 1. 组织度过低 OR 总伤亡率过高 → 部队暂时失去战斗力
        /// 2. 组织度过低 → 增加对方追击效率
        /// 3. 瞬时伤亡率高 → 瞬间使组织度下降
        /// 4. 总人数不足原有的30% → 战斗结束
        /// 5. 逃不逃跑取决于指挥官（能力、士气、训练度）
        /// </summary>
        public BattleResult ResolveBattle(Army attacker, Army defender)
        {
            var result = new BattleResult();

            // 战斗开始：记录原始兵力
            attacker.RecordBattleStartManpower(_unitDefs);
            defender.RecordBattleStartManpower(_unitDefs);

            float attackerPower = attacker.CalculateCombatPower(_unitDefs, _tiles[attacker.currentTileIndex]);
            float defenderPower = defender.CalculateCombatPower(_unitDefs, _tiles[defender.currentTileIndex]);
            defenderPower *= 1.2f; // 防守方地形加成
            attackerPower *= 1f + UnityEngine.Random.Range(-0.1f, 0.2f);
            defenderPower *= 1f + UnityEngine.Random.Range(-0.1f, 0.2f);

            float powerRatio = attackerPower / Mathf.Max(1f, defenderPower);
            result.attackerWins = powerRatio > 1.1f;

            // ===== 分阶段战斗推进（最多5个阶段，每阶段检查战斗结束）=====
            const int MaxPhases = 5;
            bool battleEnded = false;
            Army loser = result.attackerWins ? defender : attacker;
            Army winner = result.attackerWins ? attacker : defender;

            for (int phase = 0; phase < MaxPhases && !battleEnded; phase++)
            {
                // 每阶段伤亡率（总伤亡率分摊到各阶段）
                float phaseFactor = 1f / (MaxPhases - phase); // 后期阶段伤亡更大

                float attackerLossRate = result.attackerWins
                    ? Mathf.Clamp(0.1f / powerRatio, 0.05f, 0.3f) * phaseFactor
                    : Mathf.Clamp(0.2f * powerRatio, 0.1f, 0.5f) * phaseFactor;
                float defenderLossRate = result.attackerWins
                    ? Mathf.Clamp(0.2f * powerRatio, 0.1f, 0.5f) * phaseFactor
                    : Mathf.Clamp(0.1f / powerRatio, 0.05f, 0.3f) * phaseFactor;

                float atkLosses = ApplyLosses(attacker, attackerLossRate);
                float defLosses = ApplyLosses(defender, defenderLossRate);
                result.attackerLosses += atkLosses;
                result.defenderLosses += defLosses;

                // 更新伤亡率
                attacker.UpdateCasualtyRates(_unitDefs, atkLosses);
                defender.UpdateCasualtyRates(_unitDefs, defLosses);

                // 瞬时伤亡率冲击：瞬间使组织度下降
                attacker.ApplyInstantCasualtyOrgShock();
                defender.ApplyInstantCasualtyOrgShock();

                // 组织度自然损耗（训练度越高掉得越慢）
                float atkTrainingReduction = 1f - (attacker.training / 100f) * Army.TrainingOrgLossReductionMax;
                float defTrainingReduction = 1f - (defender.training / 100f) * Army.TrainingOrgLossReductionMax;
                attacker.organization = Mathf.Max(0f, attacker.organization - 8f * atkTrainingReduction);
                defender.organization = Mathf.Max(0f, defender.organization - 8f * defTrainingReduction);

                // 检查是否暂时失去战斗力
                attacker.CheckCombatIneffective();
                defender.CheckCombatIneffective();

                // 追击效率：如果对方失去战斗力，造成额外伤亡
                if (defender.isCombatIneffective)
                {
                    float pursuit = attacker.CalculatePursuitEfficiency(defender);
                    float extraLoss = ApplyLosses(defender, 0.1f * pursuit);
                    result.defenderLosses += extraLoss;
                    defender.UpdateCasualtyRates(_unitDefs, extraLoss);
                }
                if (attacker.isCombatIneffective)
                {
                    float pursuit = defender.CalculatePursuitEfficiency(attacker);
                    float extraLoss = ApplyLosses(attacker, 0.1f * pursuit);
                    result.attackerLosses += extraLoss;
                    attacker.UpdateCasualtyRates(_unitDefs, extraLoss);
                }

                // 检查战斗是否结束（任一方人数不足原有的30%）
                if (attacker.CheckBattleEnd(_unitDefs) || defender.CheckBattleEnd(_unitDefs))
                {
                    battleEnded = true;
                    result.battleEndedByManpower = true;
                }

                // 如果败方已失去战斗力且人数不足，战斗结束
                if (loser.isCombatIneffective && loser.CheckBattleEnd(_unitDefs))
                {
                    battleEnded = true;
                }
            }

            // ===== 战斗结束：组织度士气变化 + 逃跑判定 =====
            if (result.attackerWins)
            {
                attacker.organization = Mathf.Max(0f, attacker.organization - 5f);
                attacker.morale = Mathf.Min(100f, attacker.morale + 5f);
                defender.organization = Mathf.Max(0f, defender.organization - 20f);
                defender.morale = Mathf.Max(0f, defender.morale - 20f);

                // 败方逃跑判定（取决于指挥官）
                result.defenderRetreated = defender.ShouldRetreat();
                if (result.defenderRetreated)
                {
                    defender.state = GameEnums.CombatState.Retreating;
                    // 逃跑时检查溃败踩踏事件
                    var stampede = StampedeSystem.CheckMilitaryStampede(defender, _tiles, _unitDefs, 50f, 0);
                    if (stampede != null)
                    {
                        result.defenderStampede = stampede;
                        result.defenderLosses += stampede.casualties;
                    }
                }
                else
                    defender.state = GameEnums.CombatState.Dead; // 不逃跑则被歼灭
            }
            else
            {
                attacker.organization = Mathf.Max(0f, attacker.organization - 20f);
                attacker.morale = Mathf.Max(0f, attacker.morale - 20f);
                defender.organization = Mathf.Max(0f, defender.organization - 5f);
                defender.morale = Mathf.Min(100f, defender.morale + 5f);

                result.attackerRetreated = attacker.ShouldRetreat();
                if (result.attackerRetreated)
                {
                    attacker.state = GameEnums.CombatState.Retreating;
                    // 逃跑时检查溃败踩踏事件
                    var stampede = StampedeSystem.CheckMilitaryStampede(attacker, _tiles, _unitDefs, 50f, 0);
                    if (stampede != null)
                    {
                        result.attackerStampede = stampede;
                        result.attackerLosses += stampede.casualties;
                    }
                }
                else
                    attacker.state = GameEnums.CombatState.Dead;
            }

            return result;
        }

        private float ApplyLosses(Army army, float lossRate)
        {
            float totalLost = 0f;
            var keys = new List<int>(army.unitCounts.Keys);
            foreach (int unitId in keys)
            {
                float lost = army.unitCounts[unitId] * lossRate;
                army.unitCounts[unitId] = Mathf.Max(0f, army.unitCounts[unitId] - lost);
                totalLost += lost;

                if (army.unitCounts[unitId] <= 0f)
                    army.unitCounts.Remove(unitId);
            }
            return totalLost;
        }

        // ===== 战争闭环（用户定稿：分数累计/白和/胜利判定——接入主循环） =====

        /// <summary>胜利分数阈值（达到即获胜）</summary>
        public const float VictoryScore = 100f;

        /// <summary>
        /// 每日战争推进：同地块敌对军队自动交战（ResolveBattle），
        /// 胜方按 WarRules.scoreBattle 加分（×兵力规模系数 0.5~2）
        /// </summary>
        public void DailyTick(Dictionary<int, Army> armies, List<WarState> wars,
            WarRules rules, int day)
        {
            if (wars == null || armies == null) return;

            // 1. 交战检测：遍历战争双方军队，同地块且敌对→战斗
            var activeWars = new List<WarState>(wars.FindAll(w => !w.ended));
            foreach (var war in activeWars)
            {
                foreach (var pair in armies)
                {
                    var army = pair.Value;
                    int owner = army.ownerRealmId;
                    if (owner != war.attackerId && owner != war.defenderId) continue;
                    if (army.state == GameEnums.CombatState.Dead) continue;

                    // 找敌方军队（同地块）
                    foreach (var otherPair in armies)
                    {
                        if (otherPair.Key == pair.Key) continue;
                        var enemy = otherPair.Value;
                        int enemyOwner = enemy.ownerRealmId;
                        if (enemyOwner != war.attackerId && enemyOwner != war.defenderId) continue;
                        if (enemyOwner == owner) continue;
                        if (enemy.state == GameEnums.CombatState.Dead) continue;
                        if (enemy.currentTileIndex != army.currentTileIndex) continue;

                        // 交战
                        var result = ResolveBattle(army, enemy);
                        war.lastBattleDay = day;
                        if (result.attackerWins)
                        {
                            float scale = Mathf.Clamp(enemy.GetTotalManpower(_unitDefs) / Mathf.Max(1f, army.GetTotalManpower(_unitDefs)), 0.5f, 2f);
                            war.AddScore(owner, rules.scoreBattle * scale);
                        }
                        else
                        {
                            float scale = Mathf.Clamp(army.GetTotalManpower(_unitDefs) / Mathf.Max(1f, enemy.GetTotalManpower(_unitDefs)), 0.5f, 2f);
                            war.AddScore(enemyOwner, rules.scoreBattle * scale);
                        }
                        break; // 一队一天只打一场
                    }
                }
            }
        }

        /// <summary>
        /// 战争结束判定：
        /// ①任一方分数 ≥ VictoryScore → 胜利
        /// ②双方分数均 < 白和阈值 且 开战超过 peaceMinYears → 白和（allowWhitePeace）
        /// 返回本日新结束的战争（已标记 ended/winnerId/outcome）
        /// </summary>
        public static List<WarState> UpdateWarOutcomes(List<WarState> wars, WarRules rules, int day)
        {
            var ended = new List<WarState>();
            if (wars == null) return ended;

            foreach (var war in wars)
            {
                if (war.ended) continue;

                // ① 胜利判定
                if (war.attackerScore >= VictoryScore)
                {
                    war.ended = true; war.winnerId = war.attackerId; war.outcome = "victory";
                    ended.Add(war); continue;
                }
                if (war.defenderScore >= VictoryScore)
                {
                    war.ended = true; war.winnerId = war.defenderId; war.outcome = "victory";
                    ended.Add(war); continue;
                }

                // ② 白和判定（双方低分 + 开战超年限 + 规则允许）
                if (rules != null && rules.allowWhitePeace
                    && war.attackerScore < rules.peaceWhiteScore * VictoryScore
                    && war.defenderScore < rules.peaceWhiteScore * VictoryScore
                    && day - war.startDay >= rules.peaceMinYears * 365)
                {
                    war.ended = true; war.winnerId = -1; war.outcome = "white_peace";
                    ended.Add(war);
                }
            }
            return ended;
        }
    }
}
