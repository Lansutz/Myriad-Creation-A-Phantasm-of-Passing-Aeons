using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.AI
{
    /// <summary>
    /// AI决策系统
    /// 负责AI政权的军事、经济、外交、内政决策
    /// 基于政体效果、性格、局势综合判断
    /// </summary>
    public class AIDecisionSystem
    {
        private GameWorld _world;
        private Dictionary<int, AIState> _aiStates = new Dictionary<int, AIState>();
        private float _decisionTimer = 0f;
        private const float DecisionInterval = 10f; // 每10秒做一次重大决策

        public AIDecisionSystem(GameWorld world)
        {
            _world = world;
        }

        [Serializable]
        public class AIState
        {
            public int realmId;
            public float aggression = 0.5f;       // 侵略性 0~1
            public float expansionism = 0.5f;     // 扩张性 0~1
            public float diplomacyFocus = 0.5f;   // 外交倾向 0~1
            public float economyFocus = 0.5f;      // 经济倾向 0~1
            public float militaryFocus = 0.5f;     // 军事倾向 0~1
            public List<int> rivals = new List<int>();      // 竞争对手
            public List<int> allies = new List<int>();      // 盟友
            public List<int> threatenedBy = new List<int>(); // 威胁来源
            public AIStance currentStance = AIStance.Peaceful;
            public int targetRealmId = -1;        // 当前目标政权
            public float warReadiness = 0f;        // 战争准备度 0~1
            public float stabilityConcern = 0f;    // 稳定度担忧
        }

        public enum AIStance
        {
            Peaceful,       // 和平发展
            Defensive,      // 防御姿态
            Expansionist,   // 扩张姿态
            Aggressive,     // 侵略姿态
            Diplomatic,     // 外交姿态
            Economic        // 经济发展
        }

        /// <summary>初始化AI状态</summary>
        public void InitializeAI(int realmId)
        {
            if (_aiStates.ContainsKey(realmId)) return;

            var state = new AIState { realmId = realmId };
            var realm = GetRealm(realmId);

            // 基于政体效果初始化AI倾向
            if (realm != null && realm.composition != null)
            {
                var effects = GovernmentEffects.CalculateEffects(realm.composition);
                state.expansionism = Mathf.Clamp01(0.5f + effects.expansionism);
                state.aggression = Mathf.Clamp01(0.5f + effects.aggressiveDiplomacy / 20f);
                state.diplomacyFocus = Mathf.Clamp01(0.5f + effects.allianceDesirability / 20f);
                state.economyFocus = Mathf.Clamp01(0.5f + effects.tradeEfficiency / 4f);
                state.militaryFocus = Mathf.Clamp01(0.5f + effects.armyMorale / 2f);
            }

            _aiStates[realmId] = state;
            Debug.Log($"[AI] 政权 {realm?.realmName} AI初始化: 扩张性={state.expansionism:F2}, 侵略性={state.aggression:F2}");
        }

        /// <summary>每Tick更新</summary>
        public void Tick(float deltaTime)
        {
            if (_world == null || _world.realms == null) return;

            _decisionTimer += deltaTime;
            if (_decisionTimer < DecisionInterval) return;
            _decisionTimer = 0f;

            foreach (var kv in _world.realms)
            {
                var realm = kv.Value;
                if (realm == null) continue;
                if (!_aiStates.ContainsKey(realm.realmId))
                    InitializeAI(realm.realmId);

                UpdateAIState(realm);
                MakeDecisions(realm);
            }
        }

        /// <summary>更新AI状态（基于当前局势）</summary>
        private void UpdateAIState(RealmData realm)
        {
            var state = _aiStates[realm.realmId];

            // 基于稳定度调整
            if (realm.stability < 30f)
            {
                state.stabilityConcern = 1f;
                state.currentStance = AIStance.Peaceful;
            }
            else if (realm.stability < 60f)
            {
                state.stabilityConcern = 0.5f;
            }
            else
            {
                state.stabilityConcern = 0f;
            }

            // 基于财政调整
            if (realm.treasury < 200f)
            {
                state.economyFocus = Mathf.Min(1f, state.economyFocus + 0.2f);
                state.currentStance = AIStance.Economic;
            }

            // 基于威胁评估调整
            UpdateThreatAssessment(realm, state);

            // 基于战争准备度调整姿态
            if (state.warReadiness > 0.8f && state.aggression > 0.5f)
            {
                state.currentStance = AIStance.Aggressive;
            }
            else if (state.threatenedBy.Count > 0)
            {
                state.currentStance = AIStance.Defensive;
            }
        }

        /// <summary>威胁评估</summary>
        private void UpdateThreatAssessment(RealmData realm, AIState state)
        {
            state.threatenedBy.Clear();
            state.rivals.Clear();

            foreach (var kv in _world.realms)
            {
                var other = kv.Value;
                if (other == null || other.realmId == realm.realmId) continue;

                // 检查是否接壤
                if (IsBordering(realm.realmId, other.realmId))
                {
                    // 接壤的政权是潜在竞争对手
                    state.rivals.Add(other.realmId);

                    // 评估威胁
                    float threat = CalculateThreat(realm, other);
                    if (threat > 0.6f)
                    {
                        state.threatenedBy.Add(other.realmId);
                    }
                }
            }
        }

        /// <summary>计算威胁等级</summary>
        private float CalculateThreat(RealmData self, RealmData other)
        {
            float threat = 0f;

            // 军事力量对比
            float militaryRatio = GetMilitaryPower(other.realmId) / Mathf.Max(1f, GetMilitaryPower(self.realmId));
            threat += Mathf.Clamp01(militaryRatio - 0.5f) * 0.4f;

            // 对方侵略性
            if (_aiStates.TryGetValue(other.realmId, out var otherAI))
            {
                threat += otherAI.aggression * 0.3f;
                threat += otherAI.expansionism * 0.3f;
            }

            // 外交关系
            // threat += (100 - relation) / 200f; // 需要外交关系系统

            return Mathf.Clamp01(threat);
        }

        /// <summary>做出决策</summary>
        private void MakeDecisions(RealmData realm)
        {
            var state = _aiStates[realm.realmId];

            // 稳定度优先
            if (state.stabilityConcern > 0.5f)
            {
                MakeInternalDecisions(realm, state);
                return;
            }

            // 根据当前姿态做决策
            switch (state.currentStance)
            {
                case AIStance.Peaceful:
                    MakeInternalDecisions(realm, state);
                    MakeDiplomaticDecisions(realm, state);
                    break;
                case AIStance.Defensive:
                    MakeDefensiveDecisions(realm, state);
                    MakeDiplomaticDecisions(realm, state);
                    break;
                case AIStance.Expansionist:
                case AIStance.Aggressive:
                    MakeMilitaryDecisions(realm, state);
                    break;
                case AIStance.Diplomatic:
                    MakeDiplomaticDecisions(realm, state);
                    break;
                case AIStance.Economic:
                    MakeEconomicDecisions(realm, state);
                    break;
            }
        }

        /// <summary>内政决策</summary>
        private void MakeInternalDecisions(RealmData realm, AIState state)
        {
            // 提高稳定度
            if (realm.stability < 50f)
            {
                // 降低税收
                if (realm.taxSystem != null)
                {
                    // realm.taxSystem.baseTaxRate = Mathf.Max(0.1f, realm.taxSystem.baseTaxRate - 0.05f);
                }
                // 增加庆典支出
                realm.treasury = Mathf.Max(0f, realm.treasury - 50f);
                realm.stability = Mathf.Min(100f, realm.stability + 5f);
            }

            // 发展经济
            if (state.economyFocus > 0.6f && realm.treasury > 500f)
            {
                // 投资建筑
                // BuildEconomicBuilding(realm);
            }
        }

        /// <summary>经济决策</summary>
        private void MakeEconomicDecisions(RealmData realm, AIState state)
        {
            // 增加税收
            if (realm.taxSystem != null && realm.stability > 60f)
            {
                // realm.taxSystem.baseTaxRate = Mathf.Min(0.5f, realm.taxSystem.baseTaxRate + 0.02f);
            }

            // 发展贸易
            // ImproveTradeRoutes(realm);
        }

        /// <summary>外交决策</summary>
        private void MakeDiplomaticDecisions(RealmData realm, AIState state)
        {
            // 寻找盟友
            if (state.allies.Count < 2 && state.diplomacyFocus > 0.5f)
            {
                foreach (var rivalId in state.rivals)
                {
                    // 不与竞争对手结盟
                    continue;
                }

                // 寻找非竞争对手的政权结盟
                foreach (var kv in _world.realms)
                {
                    var other = kv.Value;
                    if (other == null || other.realmId == realm.realmId) continue;
                    if (state.rivals.Contains(other.realmId)) continue;
                    if (state.allies.Contains(other.realmId)) continue;

                    // 有共同威胁时更可能结盟
                    if (HasCommonThreat(realm.realmId, other.realmId))
                    {
                        // ProposeAlliance(realm.realmId, other.realmId);
                        state.allies.Add(other.realmId);
                        break;
                    }
                }
            }

            // 改善与邻国关系
            // ImproveRelations(realm);
        }

        /// <summary>防御决策</summary>
        private void MakeDefensiveDecisions(RealmData realm, AIState state)
        {
            // 增加军事预算
            state.militaryFocus = Mathf.Min(1f, state.militaryFocus + 0.1f);

            // 建造堡垒
            // BuildFortifications(realm);

            // 寻求盟友
            if (state.allies.Count == 0)
            {
                MakeDiplomaticDecisions(realm, state);
            }

            // 提高战争准备度
            state.warReadiness = Mathf.Min(1f, state.warReadiness + 0.1f);
        }

        /// <summary>军事决策</summary>
        private void MakeMilitaryDecisions(RealmData realm, AIState state)
        {
            // 选择目标
            if (state.targetRealmId < 0)
            {
                state.targetRealmId = SelectWarTarget(realm, state);
            }

            if (state.targetRealmId >= 0)
            {
                // 准备战争
                state.warReadiness = Mathf.Min(1f, state.warReadiness + 0.15f);

                // 战争准备完成，发动战争
                if (state.warReadiness >= 1f)
                {
                    // DeclareWar(realm.realmId, state.targetRealmId);
                    Debug.Log($"[AI] 政权 {realm.realmName} 对政权 {state.targetRealmId} 发动战争");
                    state.warReadiness = 0f;
                    state.currentStance = AIStance.Defensive; // 战后恢复
                }
            }
        }

        /// <summary>选择战争目标</summary>
        private int SelectWarTarget(RealmData realm, AIState state)
        {
            int bestTarget = -1;
            float bestScore = 0f;

            foreach (var rivalId in state.rivals)
            {
                var rival = GetRealm(rivalId);
                if (rival == null) continue;

                float score = 0f;

                // 军事优势
                float myPower = GetMilitaryPower(realm.realmId);
                float enemyPower = GetMilitaryPower(rivalId);
                if (myPower > enemyPower * 1.2f)
                    score += 0.4f;

                // 领土价值
                // score += CalculateTerritoryValue(rivalId) * 0.3f;

                // 对方虚弱程度
                if (rival.stability < 40f) score += 0.2f;
                if (rival.treasury < 200f) score += 0.1f;

                // 侵略性加成
                score += state.aggression * 0.2f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = rivalId;
                }
            }

            return bestScore > 0.5f ? bestTarget : -1;
        }

        // ===== 辅助方法 =====

        private RealmData GetRealm(int realmId)
        {
            if (_world != null && realmId >= 0 && realmId < _world.realms.Count)
                return _world.realms[realmId];
            return null;
        }

        private bool IsBordering(int realmA, int realmB)
        {
            // 简化实现，实际需要检查地块接壤
            return false;
        }

        private float GetMilitaryPower(int realmId)
        {
            // 简化实现，实际需要计算军队总战力
            var realm = GetRealm(realmId);
            if (realm == null) return 0f;
            return 100f + realm.treasury / 10f;
        }

        private bool HasCommonThreat(int realmA, int realmB)
        {
            if (!_aiStates.TryGetValue(realmA, out var stateA)) return false;
            if (!_aiStates.TryGetValue(realmB, out var stateB)) return false;

            foreach (var threat in stateA.threatenedBy)
            {
                if (stateB.threatenedBy.Contains(threat))
                    return true;
            }
            return false;
        }

        /// <summary>获取AI状态</summary>
        public AIState GetAIState(int realmId)
        {
            return _aiStates.TryGetValue(realmId, out var state) ? state : null;
        }
    }
}
