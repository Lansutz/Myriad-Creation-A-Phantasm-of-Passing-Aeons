using System;
using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
public sealed class SimulationCommandBus
{
	private readonly Dictionary<Type, object> _handlers = new Dictionary<Type, object>();

	public void Register<TCommand>(ISimulationCommandHandler<TCommand> handler) where TCommand : ISimulationCommand
	{
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		_handlers[typeof(TCommand)] = handler;
	}

	public bool Unregister<TCommand>() where TCommand : ISimulationCommand
	{
		return _handlers.Remove(typeof(TCommand));
	}

	public CommandResult Send<TCommand>(TCommand command) where TCommand : ISimulationCommand
	{
		if (!_handlers.TryGetValue(typeof(TCommand), out var value))
		{
			return CommandResult.Failed("handler_not_registered");
		}
		return ((ISimulationCommandHandler<TCommand>)value).Handle(command);
	}

	public void Clear()
	{
		_handlers.Clear();
	}
}
}
