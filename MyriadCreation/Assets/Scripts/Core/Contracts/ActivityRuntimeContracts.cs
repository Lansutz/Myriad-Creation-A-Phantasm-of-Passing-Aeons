using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Core.Contracts
{
    /// <summary>
    /// A concrete behavior currently performed by a Plan.
    /// Activity is runtime behavior, not Plan lifecycle state.
    /// </summary>
    public interface ISimulationActivity
    {
        string ActivityId { get; }
        string DefinitionId { get; }
        string StateCode { get; }
    }

    /// <summary>
    /// A domain operation that can change simulation state.
    /// Domain-specific rules remain outside Core.
    /// </summary>
    public interface ISimulationProcess
    {
        string ProcessId { get; }
        SimulationProcessResult Execute(in SimulationProcessContext context);
    }

    public readonly struct SimulationProcessContext
    {
        public readonly int Day;
        public readonly float DeltaDays;
        public readonly object Activity;
        public readonly object WorldSnapshot;

        public SimulationProcessContext(
            int day,
            float deltaDays,
            object activity = null,
            object worldSnapshot = null)
        {
            Day = day;
            DeltaDays = deltaDays;
            Activity = activity;
            WorldSnapshot = worldSnapshot;
        }
    }

    /// <summary>
    /// Result of a Process. A process reports facts about what happened;
    /// the runtime decides how the containing Activity/Plan transitions.
    /// </summary>
    public readonly struct SimulationProcessResult
    {
        public readonly SimulationProcessOutcome Outcome;
        public readonly float ProgressDelta;
        public readonly string ResultCode;
        public readonly string ResultSummary;
        public readonly IReadOnlyList<object> Facts;

        public SimulationProcessResult(
            SimulationProcessOutcome outcome,
            float progressDelta = 0f,
            string resultCode = "",
            string resultSummary = "",
            IReadOnlyList<object> facts = null)
        {
            Outcome = outcome;
            ProgressDelta = progressDelta;
            ResultCode = resultCode ?? string.Empty;
            ResultSummary = resultSummary ?? string.Empty;
            Facts = facts;
        }

        public static SimulationProcessResult Continue(float progressDelta = 0f)
            => new SimulationProcessResult(SimulationProcessOutcome.Continue, progressDelta);

        public static SimulationProcessResult Complete(string resultCode = "", string resultSummary = "")
            => new SimulationProcessResult(
                SimulationProcessOutcome.Complete,
                0f,
                resultCode,
                resultSummary);

        public static SimulationProcessResult Wait(string resultCode = "", string resultSummary = "")
            => new SimulationProcessResult(
                SimulationProcessOutcome.Wait,
                0f,
                resultCode,
                resultSummary);

        public static SimulationProcessResult Fail(string resultCode = "", string resultSummary = "")
            => new SimulationProcessResult(
                SimulationProcessOutcome.Fail,
                0f,
                resultCode,
                resultSummary);

        public static SimulationProcessResult Cancel(string resultCode = "", string resultSummary = "")
            => new SimulationProcessResult(
                SimulationProcessOutcome.Cancel,
                0f,
                resultCode,
                resultSummary);
    }

    public enum SimulationProcessOutcome
    {
        Continue = 0,
        Complete = 1,
        Wait = 2,
        Fail = 3,
        Cancel = 4
    }

    /// <summary>
    /// A read-only condition used by an Activity to decide whether execution can resume.
    /// Waiting is represented by an Activity condition, never by PlanState.
    /// </summary>
    public interface ISimulationCondition
    {
        string ConditionId { get; }
        bool Evaluate(in SimulationConditionContext context);
    }

    public readonly struct SimulationConditionContext
    {
        public readonly int Day;
        public readonly float DeltaDays;
        public readonly object Activity;
        public readonly object WorldSnapshot;

        public SimulationConditionContext(
            int day,
            float deltaDays,
            object activity = null,
            object worldSnapshot = null)
        {
            Day = day;
            DeltaDays = deltaDays;
            Activity = activity;
            WorldSnapshot = worldSnapshot;
        }
    }

    /// <summary>
    /// Describes why an Activity is waiting and how it can be resumed.
    /// The condition is intentionally generic; domains provide implementations.
    /// </summary>
    public readonly struct SimulationWaitCondition
    {
        public readonly string ConditionId;
        public readonly string Description;
        public readonly bool PollOnSchedule;

        public SimulationWaitCondition(
            string conditionId,
            string description = "",
            bool pollOnSchedule = true)
        {
            ConditionId = conditionId ?? string.Empty;
            Description = description ?? string.Empty;
            PollOnSchedule = pollOnSchedule;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(ConditionId);
    }

    /// <summary>
    /// A transition emitted by an Activity runtime.
    /// This is deliberately generic so PlanSystem does not switch on domain types.
    /// </summary>
    public readonly struct SimulationActivityTransition
    {
        public readonly string NextActivityId;
        public readonly string ReasonCode;

        public SimulationActivityTransition(string nextActivityId, string reasonCode = "")
        {
            NextActivityId = nextActivityId ?? string.Empty;
            ReasonCode = reasonCode ?? string.Empty;
        }

        public bool HasNextActivity => !string.IsNullOrWhiteSpace(NextActivityId);
    }
}
