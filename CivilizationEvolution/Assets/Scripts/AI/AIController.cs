using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Diplomacy;
using CivilizationEvolution.War;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Tech;
using CivilizationEvolution.Character;

namespace CivilizationEvolution.AI
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
            EconomyManager economy,
            InnovationTree innovations,
            CivilizationEvolution.Character.CharacterManager characters = null)
        {
            _decisionTimer++;

 // 日常行为
            DailyActions(realms, tiles, economy, innovations);

 // 劫掠机会（低烈度冲突——好战 AI 对敌对政权劫掠——屠城计数）
            TryRaid(realms, tiles, diplomacy, characters);

 // 重大决策
            if (_decisionTimer >= DecisionInterval)
            {
                _decisionTimer = 0f;
                MakeMajorDecision(realms, tiles, diplomacy);
            }
        }

        private int _raidCooldown = 0;
        private const int RaidInterval = 45; // 每 45 天可劫掠一次

 /// 劫掠决策（敌对状态下的低烈度冲突——RaidSettlement 的 AI 调用方）：
 /// 好战性格[aggression/expansionBias]驱动——屠城[Massacre]低概率
 /// [高侵略+随机]——成功屠城→执行政权统治者 massacres++（绰号判定数据）
        private void TryRaid(Dictionary<int, RealmData> realms, TileData[] tiles,
            DiplomacyManager diplomacy, CivilizationEvolution.Character.CharacterManager characters)
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

            var result = diplomacy.RaidSettlement(realmId, targetId, targetTile, type, tiles);
            if (result.success)
            {
                _raidCooldown = 0;
 // 屠城→执行政权统治者 massacres++（绰号[屠夫/恐怖者]判定数据）
                if (type == GameEnums.RaidType.Massacre && characters != null)
                {
                    var ruler = characters.FindRulerOfRealm(realmId);
                    if (ruler != null) ruler.achievements.massacres++;
                }
            }
        }

 /// <summary>日常行为</summary>
        private void DailyActions(
            Dictionary<int, RealmData> realms,
            TileData[] tiles,
            EconomyManager economy,
            InnovationTree innovations)
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
                        innovations.StartResearch(realmId, preferred.innovationId);
                    else
                        innovations.StartResearch(realmId, available[0].innovationId);
                }
            }

 // 研究进度
            float researchRate = CalculateResearchRate(realm, tiles);
            innovations.DailyTick(realmId, researchRate);

 // 经济管理（简化：调整税率）
            ManageEconomy(realm, tiles);
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

 /// <summary>经济管理</summary>
        private void ManageEconomy(RealmData realm, TileData[] tiles)
        {
 // 简化AI：根据国库调整税率
            if (realm.treasury < 100f)
            {
 // 国库空虚，加税
                realm.taxSystem.agriculturalTax = Mathf.Min(0.4f, realm.taxSystem.agriculturalTax + 0.01f);
            }
            else if (realm.treasury > 5000f)
            {
 // 国库充裕，减税收买人心
                realm.taxSystem.agriculturalTax = Mathf.Max(0.05f, realm.taxSystem.agriculturalTax - 0.005f);
            }
        }

 /// <summary>重大决策</summary>
        private void MakeMajorDecision(
            Dictionary<int, RealmData> realms,
            TileData[] tiles,
            DiplomacyManager diplomacy)
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
            ExecuteGoal(bestGoal, realm, realms, tiles, diplomacy);
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
            Dictionary<int, RealmData> realms, TileData[] tiles, DiplomacyManager diplomacy)
        {
            switch (goal)
            {
                case AIGoal.ExpandTerritory:
 // 寻找弱邻宣战
                    FindWeakNeighborAndDeclareWar(realm, realms, tiles, diplomacy);
                    break;
                case AIGoal.ImproveEconomy:
 // 经济建设（简化：增加发展度）
                    ImproveEconomy(realm, tiles);
                    break;
                case AIGoal.Diplomacy:
 // 外交行动
                    DoDiplomacy(realm, realms, diplomacy);
                    break;
                case AIGoal.Consolidate:
 // 巩固统治
                    ConsolidateRealm(realm, tiles);
                    break;
                case AIGoal.MilitaryBuildUp:
 // 军事建设（简化）
                    realm.treasury = Mathf.Max(0, realm.treasury - 50f);
                    break;
            }
        }

 /// <summary>寻找弱邻宣战</summary>
        private void FindWeakNeighborAndDeclareWar(RealmData realm,
            Dictionary<int, RealmData> realms, TileData[] tiles, DiplomacyManager diplomacy)
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
                diplomacy.DeclareWar(realmId, weakest.realmId, "领土扩张");
                Debug.Log($"[AI] 政权 {realmId} 对政权 {weakest.realmId} 宣战");
            }
        }

 /// <summary>经济建设</summary>
        private void ImproveEconomy(RealmData realm, TileData[] tiles)
        {
            foreach (int idx in realm.coreTiles)
            {
                if (idx >= 0 && idx < tiles.Length && realm.treasury > 50f)
                {
                    tiles[idx].development = Mathf.Min(1f, tiles[idx].development + 0.01f);
                    realm.treasury -= 10f;
                }
            }
        }

 /// <summary>外交行动</summary>
        private void DoDiplomacy(RealmData realm, Dictionary<int, RealmData> realms, DiplomacyManager diplomacy)
        {
            foreach (var other in realms.Values)
            {
                if (other.realmId == realmId) continue;
                var rel = diplomacy.GetRelation(realmId, other.realmId);
                if (rel == null) continue;

 // 关系好的提议结盟
                if (rel.relation > 40f && !rel.isAtWar)
                {
                    diplomacy.ProposeAlliance(realmId, other.realmId, AllianceType.DefensiveAlliance);
                }

 // 关系差的送礼物改善
                if (rel.relation < -20f && realm.treasury > 500f)
                {
                    diplomacy.SendGift(realmId, other.realmId, 100f);
                }
            }
        }

 /// <summary>巩固统治</summary>
        private void ConsolidateRealm(RealmData realm, TileData[] tiles)
        {
            foreach (int idx in realm.coreTiles)
            {
                if (idx >= 0 && idx < tiles.Length)
                {
                    tiles[idx].stability = Mathf.Min(100f, tiles[idx].stability + 1f);
                    tiles[idx].order = Mathf.Min(100f, tiles[idx].order + 0.5f);
                }
            }
        }

 // ===== 查询接口 =====
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
