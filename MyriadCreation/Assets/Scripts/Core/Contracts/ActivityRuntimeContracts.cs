using System;
using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
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

    /// <summary>Marker for a request that may change simulation state.</summary>
    public interface ISimulationCommand { }

    /// <summary>Marker for a read-only simulation request.</summary>
    public interface ISimulationQuery<TResult> { }

    public readonly struct CommandResult
    {
        public readonly bool Success;
        public readonly string Code;
        public readonly string Message;

        public CommandResult(bool success, string code = null, string message = null)
        {
            Success = success;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public static CommandResult Succeeded(string code = "ok")
            => new CommandResult(true, code);

        public static CommandResult Failed(string code, string message = null)
            => new CommandResult(false, code, message);
    }

    public interface ISimulationCommandHandler<in TCommand>
        where TCommand : ISimulationCommand
    {
        CommandResult Handle(TCommand command);
    }

    public interface ISimulationQueryHandler<in TQuery, TResult>
        where TQuery : ISimulationQuery<TResult>
    {
        TResult Handle(TQuery query);
    }

    /// <summary>
    /// Stable write boundary. Callers depend on command contracts rather than GameWorld internals.
    /// </summary>
    public sealed class SimulationCommandBus
    {
        private readonly Dictionary<Type, object> _handlers = new Dictionary<Type, object>();

        public void Register<TCommand>(ISimulationCommandHandler<TCommand> handler)
            where TCommand : ISimulationCommand
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers[typeof(TCommand)] = handler;
        }

        public bool Unregister<TCommand>() where TCommand : ISimulationCommand
            => _handlers.Remove(typeof(TCommand));

        public CommandResult Send<TCommand>(TCommand command)
            where TCommand : ISimulationCommand
        {
            if (!_handlers.TryGetValue(typeof(TCommand), out var raw))
                return CommandResult.Failed("handler_not_registered");

            return ((ISimulationCommandHandler<TCommand>)raw).Handle(command);
        }

        public void Clear() => _handlers.Clear();
    }

    /// <summary>
    /// Stable read boundary. Queries do not expose domain implementation details.
    /// </summary>
    public sealed class SimulationQueryBus
    {
        private readonly Dictionary<Type, object> _handlers = new Dictionary<Type, object>();

        public void Register<TQuery, TResult>(ISimulationQueryHandler<TQuery, TResult> handler)
            where TQuery : ISimulationQuery<TResult>
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers[typeof(TQuery)] = handler;
        }

        public bool Unregister<TQuery, TResult>() where TQuery : ISimulationQuery<TResult>
            => _handlers.Remove(typeof(TQuery));

        public TResult Ask<TQuery, TResult>(TQuery query)
            where TQuery : ISimulationQuery<TResult>
        {
            if (!_handlers.TryGetValue(typeof(TQuery), out var raw))
                throw new InvalidOperationException(
                    "Query handler is not registered: " + typeof(TQuery).FullName);

            return ((ISimulationQueryHandler<TQuery, TResult>)raw).Handle(query);
        }

        public bool TryAsk<TQuery, TResult>(TQuery query, out TResult result)
            where TQuery : ISimulationQuery<TResult>
        {
            if (!_handlers.TryGetValue(typeof(TQuery), out var raw))
            {
                result = default(TResult);
                return false;
            }

            result = ((ISimulationQueryHandler<TQuery, TResult>)raw).Handle(query);
            return true;
        }

        public void Clear() => _handlers.Clear();
    }

}
