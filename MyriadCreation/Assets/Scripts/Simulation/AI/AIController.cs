using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Diplomacy;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Innovation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.Warfare;
using CivilizationEvolution.Simulation.WorldState;









namespace CivilizationEvolution.Simulation.AI
{
 /// AI政权行为系统
 /// 每个AI政权有独立的AI控制器，基于效用函数做决策
    [System.Serializable]
    public class AIController
    {
        public int realmId;
        public AIPersonality personality;

 // AI状态
        private float _decisionTimer = 0f;
        private const float DecisionInterval = 30f; // 每30天做一次重大决策

 // 当前目标
        private AIGoal _currentGoal = AIGoal.None;
        private int _targetRealmId = -1;
#pragma warning disable 0414 // 预留：AI 目标地块（未来目标系统使用——保留不删）
        private int _targetTileIndex = -1;
#pragma warning restore 0414

        public AIController(int realmId, AIPersonality personality)
        {
            this.realmId = realmId;
            this.personality = personality;
        }

 /// <summary>每日AI Tick</summary>
        public void DailyTick(
            Dictionary<int, RealmData> realms,
            TileData[] tiles,
            DiplomacyManager diplomacy,
            InnovationTree innovations,
            CivilizationEvolution.Simulation.Characters.CharacterManager characters = null)
        {
            _pendingIntents.Clear();
            _decisionTimer++;

 // 日常行为
            SelectDailyIntent(realms, tiles, innovations, _pendingIntents);

 // 劫掠机会（低烈度冲突——好战 AI 对敌对政权劫掠——屠城计数）
            TryRaid(realms, tiles, diplomacy, characters, _pendingIntents);

 // 重大决策
            if (_decisionTimer >= DecisionInterval)
            {
                _decisionTimer = 0f;
                MakeMajorDecision(realms, tiles, diplomacy, _pendingIntents);
            }
        }

        /// <summary>供革新领域查询的研究速率；不推进任何革新状态。</summary>
        public float GetResearchRate(RealmData realm, TileData[] tiles)
        {
            return CalculateResearchRate(realm, tiles);
        }

        private int _raidCooldown = 0;
        private readonly List<AIIntent> _pendingIntents = new List<AIIntent>();

        public IReadOnlyList<AIIntent> DrainIntents()
        {
            var result = new List<AIIntent>(_pendingIntents);
            _pendingIntents.Clear();
            return result;
        }
        private const int RaidInterval = 45; // 每 45 天可劫掠一次

 /// 劫掠决策（敌对状态下的低烈度冲突——RaidSettlement 的 AI 调用方）：
 /// 好战性格[aggression/expansionBias]驱动——屠城[Massacre]低概率
 /// [高侵略+随机]——成功屠城→执行政权统治者 massacres++（绰号判定数据）
        private void TryRaid(Dictionary<int, RealmData> realms, TileData[] tiles,
            DiplomacyManager diplomacy, CivilizationEvolution.Simulation.Characters.CharacterManager characters, List<AIIntent> intents)
        {
            _raidCooldown++;
            if (_raidCooldown < RaidInterval) return;
 // 性格门槛：侵略或扩张偏好达标才劫掠
            if (personality.aggression < 0.45f && personality.expansionBias < 0.55f) return;
            if (realms == null || !realms.ContainsKey(realmId)) return;

 // 找敌对政权（hostility ≥50——含开战? 劫掠限非战争敌对）
            var relations = diplomacy.GetAllRelations();
            int targetId = -1;
            foreach (var kv in relations)
            {
                var rel = kv.Value;
                if (rel == null || rel.isAtWar) continue;
                int other = rel.realmAId == realmId ? rel.realmBId : rel.realmBId == realmId ? rel.realmAId : -1;
                if (other < 0 || other == realmId) continue;
                if (rel.IsHostile && !rel.isAtWar) { targetId = other; break; }
            }
            if (targetId < 0 || tiles == null) return;

 // 目标政权地块（首块属于目标的）
            int targetTile = -1;
            foreach (var t in tiles)
            {
                if (t.ownerRealmId == targetId) { targetTile = t.tileIndex; break; }
            }
            if (targetTile < 0) return;

 // 类型：高侵略+低概率=屠城（恐怖威慑）；否则普通劫掠
            GameEnums.RaidType type = GameEnums.RaidType.VillageRaid;
            if (personality.aggression > 0.6f && UnityEngine.Random.value < 0.12f)
                type = GameEnums.RaidType.Massacre;

            intents.Add(new AIIntent(AIIntentType.RaidSettlement, realmId, targetId, targetTile, raidType: type));
            _raidCooldown = 0;
        }

 /// <summary>日常行为</summary>
        private void SelectDailyIntent(
            Dictionary<int, RealmData> realms,
            TileData[] tiles,
            InnovationTree innovations,
            List<AIIntent> intents)
        {
            if (!realms.TryGetValue(realmId, out var realm)) return;

 // 研究革新
            var currentResearch = innovations.GetCurrentResearch(realmId);
            if (currentResearch == null || currentResearch.innovationId == 0)
            {
                var available = innovations.GetAvailableInnovations(realmId);
                if (available.Count > 0)
                {
 // 根据人格选择研究方向（偏好大类：技术/思维/制度/传统）
                    var preferred = available.Find(i =>
                        personality.preferredDomains.Contains(i.Domain));
                    if (preferred.innovationId != 0)
                        intents.Add(new AIIntent(AIIntentType.StartResearch, realmId, innovationId: preferred.innovationId));
                    else
                        intents.Add(new AIIntent(AIIntentType.StartResearch, realmId, innovationId: available[0].innovationId));
                }
            }

 // 研究进度
 // 研究推进已迁移到 InnovationResearchSchedule；AI 这里只负责研究选择/意图。

 // 经济领域的实际状态推进由 EconomySchedule 负责；AI 不在此处修改国库/税率。
        }

 /// <summary>计算研究速率</summary>
        private float CalculateResearchRate(RealmData realm, TileData[] tiles)
        {
            float rate = 0.5f; // 基础速率

 // 学识高的统治者加成 // 简化：用发展度加成
            float totalDevelopment = 0f;
            int tileCount = 0;
            foreach (int tileIdx in realm.coreTiles)
            {
                if (tileIdx >= 0 && tileIdx < tiles.Length)
                {
                    totalDevelopment += tiles[tileIdx].development;
                    tileCount++;
                }
            }
            if (tileCount > 0)
                rate *= 1f + (totalDevelopment / tileCount);

            return rate * personality.researchMultiplier;
        }

 /// <summary>重大决策</summary>
        private void MakeMajorDecision(
            Dictionary<int, RealmData> realms,
            TileData[] tiles,
            DiplomacyManager diplomacy,
            List<AIIntent> intents)
        {
            if (!realms.TryGetValue(realmId, out var realm)) return;

 // 计算各选项效用
            var utilities = new Dictionary<AIGoal, float>();

            utilities[AIGoal.ExpandTerritory] = CalculateExpansionUtility(realm, tiles, diplomacy);
            utilities[AIGoal.ImproveEconomy] = CalculateEconomyUtility(realm, tiles);
            utilities[AIGoal.Diplomacy] = CalculateDiplomacyUtility(realm, realms, diplomacy);
            utilities[AIGoal.Consolidate] = CalculateConsolidationUtility(realm, tiles);
            utilities[AIGoal.MilitaryBuildUp] = CalculateMilitaryUtility(realm, tiles);

 // 选择效用最高的目标
            AIGoal bestGoal = AIGoal.None;
            float bestUtility = 0f;
            foreach (var kv in utilities)
            {
                if (kv.Value > bestUtility)
                {
                    bestUtility = kv.Value;
                    bestGoal = kv.Key;
                }
            }

            _currentGoal = bestGoal;

 // 执行决策
            ExecuteGoal(bestGoal, realm, realms, tiles, diplomacy, intents);
        }

 /// <summary>计算扩张效用</summary>
        private float CalculateExpansionUtility(RealmData realm, TileData[] tiles, DiplomacyManager diplomacy)
        {
            float utility = personality.expansionBias * 50f;

 // 领土少则扩张意愿高
            utility += Mathf.Max(0, 10 - realm.coreTiles.Count) * 5f;

 // 国库充裕则扩张意愿高
            utility += Mathf.Min(realm.treasury / 1000f, 20f);

 // 有弱邻则扩张意愿高 // 简化：随机因素
            utility += UnityEngine.Random.Range(0f, 20f);

            return utility;
        }

 /// <summary>计算经济效用</summary>
        private float CalculateEconomyUtility(RealmData realm, TileData[] tiles)
        {
            float utility = personality.economicBias * 40f;

 // 国库空虚则经济建设意愿高
            utility += Mathf.Max(0, 500f - realm.treasury) / 50f;

 // 发展度低则建设意愿高
            float avgDevelopment = 0f;
            int count = 0;
            foreach (int idx in realm.coreTiles)
            {
                if (idx >= 0 && idx < tiles.Length)
                {
                    avgDevelopment += tiles[idx].development;
                    count++;
                }
            }
            if (count > 0)
                utility += (1f - avgDevelopment / count) * 30f;

            return utility;
        }

 /// <summary>计算外交效用</summary>
        private float CalculateDiplomacyUtility(RealmData realm, Dictionary<int, RealmData> realms, DiplomacyManager diplomacy)
        {
            float utility = personality.diplomaticBias * 40f;

 // 强敌环伺则外交意愿高 // 简化：检查是否有敌对国家
            foreach (var other in realms.Values)
            {
                if (other.realmId == realmId) continue;
                var rel = diplomacy.GetRelation(realmId, other.realmId);
                if (rel != null && rel.isAtWar)
                    utility += 20f;
                if (rel != null && rel.relation < -30f)
                    utility += 10f;
            }

            return utility;
        }

 /// <summary>计算巩固效用</summary>
        private float CalculateConsolidationUtility(RealmData realm, TileData[] tiles)
        {
            float utility = 0f;

 // 稳定度低则巩固意愿高
            float avgStability = 0f;
            int count = 0;
            foreach (int idx in realm.coreTiles)
            {
                if (idx >= 0 && idx < tiles.Length)
                {
                    avgStability += tiles[idx].stability;
                    count++;
                }
            }
            if (count > 0)
                utility += (100f - avgStability / count) * 0.5f;

 // 叛乱风险高则巩固意愿高
            utility += realm.CalculateRebellionRisk() * 0.3f;

            return utility;
        }

 /// <summary>计算军事建设效用</summary>
        private float CalculateMilitaryUtility(RealmData realm, TileData[] tiles)
        {
            float utility = personality.militaryBias * 40f;

 // 有战争则军事建设意愿高 // 简化：随机因素
            utility += UnityEngine.Random.Range(0f, 15f);

            return utility;
        }

 /// <summary>执行目标</summary>
        private void ExecuteGoal(AIGoal goal, RealmData realm,
            Dictionary<int, RealmData> realms, TileData[] tiles, DiplomacyManager diplomacy, List<AIIntent> intents)
        {
            switch (goal)
            {
                case AIGoal.ExpandTerritory:
 // 寻找弱邻宣战
                    FindWeakNeighborAndDeclareWar(realm, realms, tiles, diplomacy, intents);
                    break;
                case AIGoal.ImproveEconomy:
 // 经济建设（简化：增加发展度）
                    intents.Add(new AIIntent(AIIntentType.ImproveEconomy, realmId));
                    break;
                case AIGoal.Diplomacy:
 // 外交行动
                    DoDiplomacy(realm, realms, diplomacy, intents);
                    break;
                case AIGoal.Consolidate:
 // 巩固统治
                    intents.Add(new AIIntent(AIIntentType.ConsolidateRealm, realmId));
                    break;
                case AIGoal.MilitaryBuildUp:
 // 军事建设（简化）
                    intents.Add(new AIIntent(AIIntentType.MilitaryBuildUp, realmId));
                    break;
            }
        }

 /// <summary>寻找弱邻宣战</summary>
        private void FindWeakNeighborAndDeclareWar(RealmData realm,
            Dictionary<int, RealmData> realms, TileData[] tiles, DiplomacyManager diplomacy, List<AIIntent> intents)
        {
            RealmData weakest = null;
            float weakestScore = float.MaxValue;

            foreach (var other in realms.Values)
            {
                if (other.realmId == realmId) continue;
                var rel = diplomacy.GetRelation(realmId, other.realmId);
                if (rel == null || rel.isAtWar) continue;
                if (rel.relation > 30f) continue; // 关系好不打

 // 评估对方实力（简化：用领土数量）
                float score = other.coreTiles.Count;
                if (score < weakestScore && score < realm.coreTiles.Count)
                {
                    weakestScore = score;
                    weakest = other;
                }
            }

            if (weakest != null && realm.treasury > 200f)
            {
                intents.Add(new AIIntent(AIIntentType.DeclareWar, realmId, weakest.realmId));
            }
        }

 // 经济建设、巩固与军事建设已经转为 AIIntent，由执行阶段处理。\n\n // ===== 查询接口 =====
        public AIGoal GetCurrentGoal() => _currentGoal;
        public int GetTargetRealm() => _targetRealmId;

 /// 人格→AI 偏置同步（借鉴 MPD 的 ai_* 人格值：人格直接驱动决策，且随漂移实时变化）
 /// 映射：大胆→扩张/冒险；贪婪→经济敛财；荣誉→守信外交；报复→好战；
 /// 悲悯→厌战；理性→谨慎
        public void SyncPersonality(CharacterData ruler)
        {
            if (ruler == null) return;
            if (personality.fixedArchetype) return; // 固定原型——不随性格漂移
            float b = (ruler.boldness + 100f) / 200f;        // 大胆 0-1
            float g = (ruler.greed + 100f) / 200f;           // 贪婪
            float h = (ruler.honor + 100f) / 200f;           // 荣誉
            float v = (ruler.vengefulness + 100f) / 200f;    // 报复
            float c = (ruler.compassion + 100f) / 200f;      // 悲悯
            float r = (ruler.rationality + 100f) / 200f;     // 理性

            var p = personality; // struct 局部修改后回写
            p.expansionBias = Mathf.Clamp(0.4f + (b - 0.5f) * 0.6f + (v - 0.5f) * 0.3f, 0.05f, 0.95f);
            p.economicBias = Mathf.Clamp(0.4f + (g - 0.5f) * 0.8f, 0.05f, 0.95f);
            p.diplomaticBias = Mathf.Clamp(0.4f + (h - 0.5f) * 0.6f + (r - 0.5f) * 0.3f, 0.05f, 0.95f);
            p.militaryBias = Mathf.Clamp(0.35f + (v - 0.5f) * 0.5f + (b - 0.5f) * 0.3f - (c - 0.5f) * 0.3f, 0.05f, 0.95f);
            p.aggression = Mathf.Clamp(0.4f + (b - 0.5f) * 0.5f + (v - 0.5f) * 0.4f - (c - 0.5f) * 0.4f, 0.05f, 0.95f);
            p.riskTolerance = Mathf.Clamp(0.5f + (b - 0.5f) * 0.5f - (r - 0.5f) * 0.4f, 0.05f, 0.95f);
 // 原型名涌现（按当前偏好最高维分类——被后世记住的形象——非标签驱动）
            p.personalityName = ClassifyName(p);
            personality = p;
        }

 /// <summary>原型命名（按偏好最高维分类——涌现结果）</summary>
        private static string ClassifyName(AIPersonality p)
        {
            float[] dims = { p.expansionBias, p.economicBias, p.diplomaticBias, p.militaryBias, p.aggression };
            int maxIdx = 0;
            for (int i = 1; i < dims.Length; i++)
                if (dims[i] > dims[maxIdx]) maxIdx = i;
            string[] names = { "征服王", "建设王", "外交家", "军事统帅", "侵略者" };
            return names[maxIdx];
        }
    }

 /// <summary>AI人格</summary>

 /// AI管理器
 /// 管理所有AI政权的控制器
}
