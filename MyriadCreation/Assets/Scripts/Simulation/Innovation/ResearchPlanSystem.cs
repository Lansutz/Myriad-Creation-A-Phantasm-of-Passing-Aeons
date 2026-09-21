using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core.Events;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Planning;
using CivilizationEvolution.Simulation.WorldState;

namespace CivilizationEvolution.Simulation.Innovation
{
    /// <summary>
    /// 个人革新计划系统：负责“实践 → 突破 → 研究/验证 → 社会正式解锁”。
    /// PlanSystem 只负责生命周期，本系统负责革新领域规则。
    /// </summary>
    public sealed class ResearchPlanSystem
    {
        private readonly GameWorld _world;
        private readonly PlanSystem _plans;
        private readonly InnovationKnowledgeSystem _knowledge = new InnovationKnowledgeSystem();
        private readonly Dictionary<int, ResearchPlanData> _data = new Dictionary<int, ResearchPlanData>();
        private readonly ResearchPlanExecutor _executor;
        private readonly SimulationEventBus _events;
        private readonly HashSet<int> _practiceDirtyCharacters = new HashSet<int>();

        public const float BaseBreakthroughChancePerDay = 0.0005f;
        public const float PracticeScale = 0.01f;
        public const float ScholarshipScale = 0.008f;
        public const float ExperienceScale = 0.005f;
        public const float MaxBreakthroughChancePerDay = 0.03f;

        public InnovationKnowledgeSystem Knowledge => _knowledge;

        public ResearchPlanSystem(GameWorld world, PlanSystem plans)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _plans = plans ?? throw new ArgumentNullException(nameof(plans));
            _events = _world.SimulationEvents;
            _executor = new ResearchPlanExecutor(this);
            _plans.RegisterExecutor(_executor);
            _events.Subscribe<PracticeRecordedEvent>(OnPracticeRecorded);
        }

        private void OnPracticeRecorded(PracticeRecordedEvent evt)
        {
            RecordPractice(evt.characterId, evt.innovationId, evt.amount);
        }

        public void Dispose()
        {
            _events.Unsubscribe<PracticeRecordedEvent>(OnPracticeRecorded);
            _plans.UnregisterExecutor(PlanType.Research);
        }

        internal CharacterData GetCharacter(int characterId) => _world.Characters?.GetCharacter(characterId);

        public float RecordPractice(int characterId, int innovationId, float amount)
        {
            float recorded = _knowledge.RecordPractice(characterId, innovationId, amount);
            if (recorded > 0f && characterId >= 0)
                _practiceDirtyCharacters.Add(characterId);
            return recorded;
        }

        /// <summary>
        /// 取得并清空本轮发生真实实践的角色集合。
        /// 生产/建造/采掘等行为系统写入实践后，突破判定只检查这些角色。
        /// </summary>
        public int DrainPracticeDirtyCharacters(List<int> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            int count = _practiceDirtyCharacters.Count;
            foreach (int characterId in _practiceDirtyCharacters)
                destination.Add(characterId);
            _practiceDirtyCharacters.Clear();
            return count;
        }

        public int GetMastery(int characterId, int innovationId)
            => _knowledge.GetMastery(characterId, innovationId);

        /// <summary>
        /// 每日运行个人突破层。
        /// 随机数只决定“今天是否发生突破”，候选革新由个人知识边界决定。
        /// 研究计划随后进入统一 PlanSystem 执行。
        /// </summary>
        public int DailyTick(float deltaDays = 1f)
        {
            if (_world.Characters == null || deltaDays <= 0f) return 0;

            int discoveries = 0;
            foreach (var character in _world.Characters.GetAllCharacters())
            {
                if (character == null || !character.isAlive || character.realmId < 0) continue;
                if (TryDailyBreakthrough(character.characterId, deltaDays)) discoveries++;
            }
            return discoveries;
        }

        /// <summary>每日个人突破判定。</summary>
        public bool TryDailyBreakthrough(int characterId, float deltaDays = 1f)
        {
            var character = GetCharacter(characterId);
            var innovations = _world.Innovations;
            if (character == null || !character.isAlive || innovations == null || character.realmId < 0) return false;

            var candidates = _knowledge.GetDiscoveryCandidates(character, innovations, character.realmId);
            if (candidates.Count == 0) return false;

            // 不随机选择一个任意革新；实践最相关者才是当前突破方向。
            InnovationDef candidate = null;
            float bestRelevance = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                float relevance = GetPracticeRelevance(character.characterId, candidates[i]);
                if (relevance > bestRelevance)
                {
                    bestRelevance = relevance;
                    candidate = candidates[i];
                }
            }

            if (candidate == null || bestRelevance <= 0f) return false;
            if (HasActiveResearchPlan(character.characterId, candidate.innovationId)) return false;

            float chance = CalculateBreakthroughChance(character, bestRelevance, deltaDays);
            if (UnityEngine.Random.value > chance) return false;

            var plan = CreateResearchPlan(character, candidate, bestRelevance);
            if (plan == null) return false;

            // 发现不是正式解锁；发现后自动进入研究计划生命周期。
            return StartResearchPlan(plan.planId);
        }

        /// <summary>检查角色是否已经在研究同一革新，避免重复计划。</summary>
        public bool HasActiveResearchPlan(int characterId, int innovationId)
        {
            var plans = _plans.GetPlans(PlanType.Research);
            for (int i = 0; i < plans.Count; i++)
            {
                var plan = plans[i];
                if (plan.IsTerminal) continue;
                if (!_data.TryGetValue(plan.planId, out var data)) continue;
                if (data.characterId == characterId && data.innovationId == innovationId) return true;
            }
            return false;
        }

        /// <summary>创建个人突破后的研究/验证计划；此时绝不正式解锁革新。</summary>
        public Plan CreateResearchPlan(CharacterData character, InnovationDef innovation, float relevance = 1f)
        {
            if (character == null || innovation == null || character.realmId < 0) return null;
            if (_world.Innovations == null || _world.Innovations.HasInnovation(character.realmId, innovation.innovationId)) return null;
            if (HasActiveResearchPlan(character.characterId, innovation.innovationId)) return null;

            var plan = _plans.CreatePlan(
                PlanType.Research,
                character.characterId,
                innovation.innovationId,
                _world.currentDay,
                "研究：" + innovation.GetName(),
                "个人突破后的研究、验证与工艺固化。",
                EstimateResearchDays(character, innovation, relevance));

            plan.ownerId = character.realmId;
            plan.participants.Add(new PlanParticipant(character.characterId, "discoverer"));
            plan.requirements.Add(new PlanRequirement("practice_relevance", Mathf.Max(0f, relevance)));

            _data[plan.planId] = new ResearchPlanData
            {
                planId = plan.planId,
                characterId = character.characterId,
                realmId = character.realmId,
                innovationId = innovation.innovationId,
                relevance = Mathf.Clamp01(relevance),
                discovered = true
            };
            return plan;
        }

        public bool StartResearchPlan(int planId)
        {
            if (!_plans.AcceptPlan(planId)) return false;
            if (!_plans.BeginPreparation(planId)) return false;
            return _plans.BeginExecution(planId);
        }

        public bool TryGetResearchData(int planId, out ResearchPlanData data)
            => _data.TryGetValue(planId, out data);

        /// <summary>正式解锁后的持续学习：L1→L2→L3，每次调用最多提升一级。</summary>
        public bool Learn(int characterId, int innovationId, int targetLevel = InnovationKnowledgeSystem.MasteryLevel1)
        {
            var character = GetCharacter(characterId);
            if (character == null || character.realmId < 0 || _world.Innovations == null) return false;
            if (!_world.Innovations.HasInnovation(character.realmId, innovationId)) return false;
            return _knowledge.Learn(characterId, innovationId, targetLevel);
        }

        private float GetPracticeRelevance(int characterId, InnovationDef def)
        {
            float practice = _knowledge.GetPractice(characterId, def.innovationId);
            float maxPrereqPractice = 0f;
            if (def.prerequisites != null)
                foreach (int prereq in def.prerequisites)
                    maxPrereqPractice = Mathf.Max(maxPrereqPractice, _knowledge.GetPractice(characterId, prereq));
            return Mathf.Clamp01((practice + maxPrereqPractice * 0.5f) * PracticeScale);
        }

        private static float CalculateBreakthroughChance(CharacterData character, float relevance, float deltaDays)
        {
            float scholarship = Mathf.Clamp01(character.scholarship / 100f);
            float experience = Mathf.Clamp01((character.age - 15f) / 50f);
            float chance = BaseBreakthroughChancePerDay
                * (0.25f + relevance)
                * (0.5f + scholarship * ScholarshipScale * 10f)
                * (0.75f + experience * ExperienceScale * 10f)
                * Mathf.Max(0.1f, deltaDays);
            return Mathf.Clamp(chance, 0f, MaxBreakthroughChancePerDay);
        }

        private static float EstimateResearchDays(CharacterData character, InnovationDef innovation, float relevance)
        {
            float scholarship = Mathf.Clamp(character.scholarship, 5f, 100f);
            float cost = Mathf.Max(1f, innovation.researchCost);
            float rate = 0.25f + scholarship / 100f;
            rate *= 0.75f + Mathf.Clamp01(relevance) * 0.5f;
            return Mathf.Max(5f, cost / rate);
        }

        /// <summary>
        /// 临时兼容桥：旧 InnovationTree 仍负责写入社会革新集合。
        /// 研究计划完成前不会调用这里。
        /// </summary>
        internal bool TryFormalizeResearch(ResearchPlanData data)
        {
            if (data == null || _world.Innovations == null) return false;
            if (_world.Innovations.HasInnovation(data.realmId, data.innovationId)) return true;
            if (_world.Innovations.GetCurrentResearch(data.realmId) != null) return false;
            if (!_world.Innovations.StartResearch(data.realmId, data.innovationId)) return false;

            var def = _world.Innovations.GetInnovation(data.innovationId);
            if (def == null) return false;
            _world.Innovations.DailyTick(data.realmId, Mathf.Max(1f, def.researchCost));
            return _world.Innovations.HasInnovation(data.realmId, data.innovationId);
        }
    }

    [Serializable]
    public sealed class ResearchPlanData
    {
        public int planId;
        public int characterId;
        public int realmId;
        public int innovationId;
        public float relevance;
        public bool discovered;
        public bool formallyUnlocked;
        public float verificationProgress;
    }

    internal sealed class ResearchPlanExecutor : IPlanExecutor
    {
        private readonly ResearchPlanSystem _system;
        public PlanType Type => PlanType.Research;

        public ResearchPlanExecutor(ResearchPlanSystem system) => _system = system;

        public PlanExecutionResult Execute(Plan plan, float deltaDays)
        {
            if (!_system.TryGetResearchData(plan.planId, out var data))
                return PlanExecutionResult.Fail("research_data_missing");
            var character = _system.GetCharacter(data.characterId);
            if (character == null || !character.isAlive)
                return PlanExecutionResult.Fail("researcher_unavailable");

            float scholarship = Mathf.Clamp(character.scholarship / 100f, 0.05f, 1f);
            float rate = (0.004f + scholarship * 0.012f)
                * (0.75f + data.relevance * 0.5f) * deltaDays;
            float next = Mathf.Clamp01(data.verificationProgress + rate);

            // 正式解锁必须在验证完成后发生；旧桥暂时无法写入时保持在 99.9%。
            if (next >= 1f && !_system.TryFormalizeResearch(data))
                next = 0.999f;

            data.verificationProgress = next;
            float delta = Mathf.Max(0f, next - plan.progress);
            if (next >= 1f && data.formallyUnlocked)
                return PlanExecutionResult.Complete("research_verified", "验证与固化");
            return PlanExecutionResult.Continue(delta, "研究与验证");
        }

    }
}
