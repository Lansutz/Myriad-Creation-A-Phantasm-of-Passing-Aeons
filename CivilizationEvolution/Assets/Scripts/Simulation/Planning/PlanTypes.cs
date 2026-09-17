using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Planning
{
    /// <summary>
    /// 统一计划类型。具体领域系统通过此类型注册自己的执行逻辑，PlanSystem 只负责生命周期与调度。
    /// </summary>
    public enum PlanType
    {
        Research,
        Intrigue,
        Engineering,
        Construction,
        Exploration,
        Trade,
        Diplomacy,
        Migration,
        Military,
        Custom
    }

    /// <summary>统一计划生命周期。</summary>
    public enum PlanState
    {
        Proposed,
        Accepted,
        Preparing,
        Executing,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>计划执行阶段。子系统可以只使用自己需要的阶段。</summary>
    public enum PlanPhase
    {
        Preparation,
        Execution,
        Verification,
        Resolution
    }

    /// <summary>计划中的参与者。</summary>
    [Serializable]
    public struct PlanParticipant
    {
        public int characterId;
        public string role;

        public PlanParticipant(int characterId, string role = null)
        {
            this.characterId = characterId;
            this.role = role ?? string.Empty;
        }
    }

    /// <summary>计划对某种抽象资源的需求。具体资源语义由领域子系统解释。</summary>
    [Serializable]
    public struct PlanRequirement
    {
        public string key;
        public float amount;

        public PlanRequirement(string key, float amount)
        {
            this.key = key ?? string.Empty;
            this.amount = amount;
        }
    }

    /// <summary>
    /// 统一计划数据。
    /// 注意：Plan 不是“行动结果”，而是持续存在的一项意图/工作过程。
    /// </summary>
    [Serializable]
    public sealed class Plan
    {
        public int planId;
        public PlanType type;
        public PlanState state;
        public PlanPhase phase;
        public int initiatorId = -1;
        public int ownerId = -1;
        public int targetId = -1;
        public int parentPlanId = -1;
        public string title = string.Empty;
        public string description = string.Empty;
        public float progress;
        public float elapsedDays;
        public float estimatedDays;
        public int createdDay;
        public int lastStateChangeDay;

        public readonly List<PlanParticipant> participants = new List<PlanParticipant>();
        public readonly List<PlanRequirement> requirements = new List<PlanRequirement>();
        public readonly List<int> childPlanIds = new List<int>();

        /// <summary>领域系统写入的轻量结果/原因标识，避免 PlanSystem 依赖具体领域类型。</summary>
        public string resultCode = string.Empty;

        public Plan(int planId, PlanType type, int initiatorId, int targetId, int createdDay)
        {
            this.planId = planId;
            this.type = type;
            this.state = PlanState.Proposed;
            this.phase = PlanPhase.Preparation;
            this.initiatorId = initiatorId;
            this.targetId = targetId;
            this.createdDay = createdDay;
            this.lastStateChangeDay = createdDay;
        }

        public bool IsTerminal => state == PlanState.Completed
            || state == PlanState.Failed
            || state == PlanState.Cancelled;
    }

    /// <summary>
    /// 计划执行器接口。研究、阴谋、工程等子系统实现自己的执行逻辑，不把领域规则塞进 PlanSystem。
    /// </summary>
    public interface IPlanExecutor
    {
        PlanType Type { get; }
        float Execute(Plan plan, float deltaDays);
        void OnPlanEnded(Plan plan);
    }
}
