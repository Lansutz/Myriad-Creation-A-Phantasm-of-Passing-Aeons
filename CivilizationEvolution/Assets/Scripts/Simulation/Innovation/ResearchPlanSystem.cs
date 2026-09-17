using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Planning;
using CivilizationEvolution.Simulation.WorldState;

namespace CivilizationEvolution.Simulation.Innovation
{
    /// <summary>研究计划：个人突破后的研究/验证，正式解锁后才进入社会革新集合。</summary>
    public sealed class ResearchPlanSystem
    {
        private readonly GameWorld _world;
        private readonly PlanSystem _plans;
        private readonly InnovationKnowledgeSystem _knowledge = new InnovationKnowledgeSystem();
        private readonly Dictionary<int, ResearchPlanData> _data = new Dictionary<int, ResearchPlanData>();
        private readonly ResearchPlanExecutor _executor;

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
            _executor = new ResearchPlanExecutor(this);
            _plans.RegisterExecutor(_executor);
        }

        internal CharacterData GetCharacter(int characterId)
            => _world.Characters?.GetCharacter(characterId);

        public float RecordPractice(int characterId, int innovationId, float amount)
            => _knowledge.RecordPractice(characterId, innovationId, amount);

        public int GetMastery(int characterId, int innovationId)
            => _knowledge.GetMastery(characterId, innovationId);

        /// <summary>每日个人突破判定。随机只决定“是否今天发生”，候选始终受个人知识边界限制。</summary>
        public bool TryDailyBreakthrough(int characterId, float deltaDays = 1f)
        {
            var character = GetCharacter(characterId);
            var innovations = _world.Innovations;
            if (character == null || !character.isAlive || innovations == null || character.realmId < 0)
                return false;

            var candidates = _knowledge.GetDiscoveryCandidates(character, innovations, character.realmId);
            if (candidates.Count == 0) return false;

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

            float chance = CalculateBreakthroughChance(character, bestRelevance, deltaDays);
            if (UnityEngine.Random.value > chance) return false;
            return CreateResearchPlan(character, candidate, bestRelevance) != null;
        }

        /// <summary>创建个人突破后的研究/验证计划；不直接解锁革新。</summary>
        public Plan CreateResearchPlan(CharacterData character, InnovationDef innovation, float relevance = 1f)
        {
            if (character == null || innovation == null || character.realmId < 0) return null;
            if (_world.Innovations == null || _world.Innovations.HasInnovation(character.realmId, innovation.innovationId)) return null;

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

        /// <summary>正式解锁后的持续学习：L1 能用，L2 构成后续学习基础，L3 为成熟工艺。</summary>
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
            {
                foreach (int prereq in def.prerequisites)
                    maxPrereqPractice = Mathf.Max(maxPrereqPractice, _knowledge.GetPractice(characterId, prereq));
            }
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

        internal bool CompleteResearchPlan(ResearchPlanData data)
        {
            if (data == null || _world.Innovations == null) return false;
            if (!_world.Innovations.TryCompleteResearch(data.realmId, data.innovationId)) return false;
            data.formallyUnlocked = true;
            return true;
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

        public float Execute(Plan plan, float deltaDays)
        {
            if (!_system.TryGetResearchData(plan.planId, out var data)) return 0f;
            var character = _system.GetCharacter(data.characterId);
            if (character == null || !character.isAlive) return 0f;

            float scholarship = Mathf.Clamp(character.scholarship / 100f, 0.05f, 1f);
            float rate = (0.004f + scholarship * 0.012f)
                * (0.75f + data.relevance * 0.5f) * deltaDays;
            data.verificationProgress = Mathf.Clamp01(data.verificationProgress + rate);
            return rate;
        }

        public void OnPlanEnded(Plan plan)
        {
            if (plan.state != PlanState.Completed) return;
            if (!_system.TryGetResearchData(plan.planId, out var data)) return;
            _system.CompleteResearchPlan(data);
        }
    }
}
