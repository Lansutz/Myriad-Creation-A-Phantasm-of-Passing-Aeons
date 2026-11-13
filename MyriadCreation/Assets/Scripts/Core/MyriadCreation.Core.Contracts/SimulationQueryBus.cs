using System;
using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
public sealed class SimulationQueryBus
{
	private readonly Dictionary<Type, object> _handlers = new Dictionary<Type, object>();

	public void Register<TQuery, TResult>(ISimulationQueryHandler<TQuery, TResult> handler) where TQuery : ISimulationQuery<TResult>
	{
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		_handlers[typeof(TQuery)] = handler;
	}

	public bool Unregister<TQuery, TResult>() where TQuery : ISimulationQuery<TResult>
	{
		return _handlers.Remove(typeof(TQuery));
	}

	public TResult Ask<TQuery, TResult>(TQuery query) where TQuery : ISimulationQuery<TResult>
	{
		if (!_handlers.TryGetValue(typeof(TQuery), out var value))
		{
			throw new InvalidOperationException("Query handler is not registered: " + typeof(TQuery).FullName);
		}
		return ((ISimulationQueryHandler<TQuery, TResult>)value).Handle(query);
	}

	public bool TryAsk<TQuery, TResult>(TQuery query, out TResult result) where TQuery : ISimulationQuery<TResult>
	{
		if (!_handlers.TryGetValue(typeof(TQuery), out var value))
		{
			result = default;
			return false;
		}
		result = ((ISimulationQueryHandler<TQuery, TResult>)value).Handle(query);
		return true;
	}

	public void Clear()
	{
		_handlers.Clear();
	}
}
}
