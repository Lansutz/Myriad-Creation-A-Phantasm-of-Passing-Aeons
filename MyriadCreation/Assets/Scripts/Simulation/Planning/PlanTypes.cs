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
        public string targetKind = string.Empty;
        public string purpose = string.Empty;
        public string title = string.Empty;
        public string description = string.Empty;
        public float progress;
        public float elapsedDays;
        public float estimatedDays;
        public int createdDay;
        public int lastStateChangeDay;

        public readonly List<PlanParticipant> participants = new List<PlanParticipant>();
        public readonly List<PlanRequirement> requirements = new List<PlanRequirement>();

        /// <summary>领域系统写入的轻量结果/原因标识，避免 PlanSystem 依赖具体领域类型。</summary>
        public string currentActivity = string.Empty;
        public string resultCode = string.Empty;
        public string resultSummary = string.Empty;

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

    /// <summary>统一的计划执行结果；PlanSystem 只解释结果，不理解领域规则。</summary>
    public enum PlanExecutionOutcome
    {
        Continue,
        Complete,
        Fail,
        Cancel
    }

    [Serializable]
    public readonly struct PlanExecutionResult
    {
        public readonly PlanExecutionOutcome outcome;
        public readonly float progressDelta;
        public readonly string currentActivity;
        public readonly string resultCode;

        public PlanExecutionResult(
            PlanExecutionOutcome outcome,
            float progressDelta = 0f,
            string currentActivity = null,
            string resultCode = null)
        {
            this.outcome = outcome;
            this.progressDelta = progressDelta;
            this.currentActivity = currentActivity ?? string.Empty;
            this.resultCode = resultCode ?? string.Empty;
        }

        public static PlanExecutionResult Continue(
            float progressDelta = 0f, string currentActivity = null)
            => new PlanExecutionResult(
                PlanExecutionOutcome.Continue, progressDelta, currentActivity);

        public static PlanExecutionResult Complete(
            string resultCode = "completed", string currentActivity = null)
            => new PlanExecutionResult(
                PlanExecutionOutcome.Complete, 0f, currentActivity, resultCode);

        public static PlanExecutionResult Fail(string resultCode = "failed")
            => new PlanExecutionResult(
                PlanExecutionOutcome.Fail, 0f, null, resultCode);

        public static PlanExecutionResult Cancel(string resultCode = "cancelled")
            => new PlanExecutionResult(
                PlanExecutionOutcome.Cancel, 0f, null, resultCode);
    }

    /// <summary>
    /// 计划执行器接口。领域系统只提供“当前计划怎么推进”，不管理统一生命周期。
    /// </summary>
    public interface IPlanExecutor
    {
        PlanType Type { get; }
        PlanExecutionResult Execute(Plan plan, float deltaDays);
    }
}
