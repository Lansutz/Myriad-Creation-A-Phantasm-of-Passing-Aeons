using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Core.Contracts
{
    /// <summary>Marker for a request that may change simulation state.</summary>
    public interface ISimulationCommand { }

    /// <summary>Marker for a read-only simulation request.</summary>
    public interface ISimulationQuery<TResult> { }

    public readonly struct CommandResult
    {
        public readonly bool success;
        public readonly string code;
        public readonly string message;

        public CommandResult(bool success, string code = null, string message = null)
        {
            this.success = success;
            this.code = code ?? string.Empty;
            this.message = message ?? string.Empty;
        }

        public static CommandResult Success(string code = "ok") => new CommandResult(true, code);
        public static CommandResult Failure(string code, string message = null) => new CommandResult(false, code, message);
    }

    public interface ISimulationCommandHandler<in TCommand> where TCommand : ISimulationCommand
    {
        CommandResult Handle(TCommand command);
    }

    public interface ISimulationQueryHandler<in TQuery, TResult> where TQuery : ISimulationQuery<TResult>
    {
        TResult Handle(TQuery query);
    }

    /// <summary>
    /// Stable command boundary. Callers know the command contract, not the target system's implementation.
    /// </summary>
    public sealed class SimulationCommandBus
    {
        private readonly Dictionary<Type, object> _handlers = new Dictionary<Type, object>();

        public void Register<TCommand>(ISimulationCommandHandler<TCommand> handler) where TCommand : ISimulationCommand
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers[typeof(TCommand)] = handler;
        }

        public bool Unregister<TCommand>() where TCommand : ISimulationCommand => _handlers.Remove(typeof(TCommand));

        public CommandResult Send<TCommand>(TCommand command) where TCommand : ISimulationCommand
        {
            if (!_handlers.TryGetValue(typeof(TCommand), out var raw))
                return CommandResult.Failure("handler_not_registered");
            return ((ISimulationCommandHandler<TCommand>)raw).Handle(command);
        }

        public void Clear() => _handlers.Clear();
    }

    /// <summary>
    /// Stable read boundary. Queries must not mutate simulation state.
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

        public TResult Ask<TQuery, TResult>(TQuery query) where TQuery : ISimulationQuery<TResult>
        {
            if (!_handlers.TryGetValue(typeof(TQuery), out var raw))
                throw new InvalidOperationException("Query handler is not registered: " + typeof(TQuery).FullName);
            return ((ISimulationQueryHandler<TQuery, TResult>)raw).Handle(query);
        }

        public bool TryAsk<TQuery, TResult>(TQuery query, out TResult result) where TQuery : ISimulationQuery<TResult>
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
