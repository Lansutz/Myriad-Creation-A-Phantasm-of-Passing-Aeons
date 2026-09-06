using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Diplomacy;
using CivilizationEvolution.Map;

namespace CivilizationEvolution.War
{
    /// <summary>兵种定义</summary>
    [System.Serializable]
    public struct UnitDef
    {
        public int unitId;
        public string unitName;
        public GameEnums.UnitCategory category;
        public int tier; // 1轻型 2中型 3重型 4超重型

        // 战斗属性
        public float meleeAttack;
        public float rangedAttack;
        public float defense;
        public float morale;
        public float speed; // 地块/天
        public float supplyConsumption; // 每日补给消耗

        // 招募消耗
        [System.NonSerialized] public Dictionary<int, float> recruitCost; // goodsId -> 数量

        public float manpowerCost; // 人力消耗

        // 地形偏好
        [System.NonSerialized] public Dictionary<GameEnums.TerrainTacticType, float> terrainModifiers;


        /// <summary>
        /// 解锁前置革新（用户定稿：兵种必须有对应革新才能征募——重骑兵需马镫等）
        /// 由 AddUnitDef 赋值（struct 不能带字段初始化器）
        /// </summary>
        public List<int> requiredInnovations;
    }

    /// <summary>军团（兵力块集合）</summary>
    [System.Serializable]


    /// <summary>战斗管理器</summary>
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

        /// <summary>解决一场战斗</summary>
        public BattleResult ResolveBattle(Army attacker, Army defender)
        {
            var result = new BattleResult();

            float attackerPower = attacker.CalculateCombatPower(_unitDefs, _tiles[attacker.currentTileIndex]);
            float defenderPower = defender.CalculateCombatPower(_unitDefs, _tiles[defender.currentTileIndex]);

            // 防守方地形加成
            defenderPower *= 1.2f;

            // 将领加成（简化）
            attackerPower *= 1f + Random.Range(-0.1f, 0.2f);
            defenderPower *= 1f + Random.Range(-0.1f, 0.2f);

            float powerRatio = attackerPower / Mathf.Max(1f, defenderPower);
            result.attackerWins = powerRatio > 1.1f;

            // 伤亡计算
            float attackerLossRate = result.attackerWins
                ? Mathf.Clamp(0.1f / powerRatio, 0.05f, 0.3f)
                : Mathf.Clamp(0.2f * powerRatio, 0.1f, 0.5f);
            float defenderLossRate = result.attackerWins
                ? Mathf.Clamp(0.2f * powerRatio, 0.1f, 0.5f)
                : Mathf.Clamp(0.1f / powerRatio, 0.05f, 0.3f);

            result.attackerLosses = ApplyLosses(attacker, attackerLossRate);
            result.defenderLosses = ApplyLosses(defender, defenderLossRate);

            // 组织度和士气变化
            if (result.attackerWins)
            {
                attacker.organization = Mathf.Max(0f, attacker.organization - 10f);
                attacker.morale = Mathf.Min(100f, attacker.morale + 5f);
                defender.organization = Mathf.Max(0f, defender.organization - 30f);
                defender.morale = Mathf.Max(0f, defender.morale - 20f);
                defender.state = GameEnums.CombatState.Retreating;
            }
            else
            {
                attacker.organization = Mathf.Max(0f, attacker.organization - 30f);
                attacker.morale = Mathf.Max(0f, attacker.morale - 20f);
                attacker.state = GameEnums.CombatState.Retreating;
                defender.organization = Mathf.Max(0f, defender.organization - 10f);
                defender.morale = Mathf.Min(100f, defender.morale + 5f);
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

    /// <summary>战斗结果</summary>
    [System.Serializable]
    public struct BattleResult
    {
        public bool attackerWins;
        public float attackerLosses;
        public float defenderLosses;
    }

    /// <summary>
    /// 战争状态（战争闭环——分数累计/白和/胜利判定）
    /// 分数按 WarRules score 体系：野战胜利=scoreBattle、歼灭敌军=按规模、
    /// 占城=scoreCity 等（当前实现：战斗胜利+占领加分）
    /// </summary>
    }
