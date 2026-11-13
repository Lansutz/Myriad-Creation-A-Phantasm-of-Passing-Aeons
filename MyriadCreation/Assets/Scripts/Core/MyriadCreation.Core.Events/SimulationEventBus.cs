using System;
using System.Collections.Generic;

namespace MyriadCreation.Core.Events
{
public sealed class SimulationEventBus
{
	private readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

	public void Subscribe<T>(Action<T> handler)
	{
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		Type typeFromHandle = typeof(T);
		if (_handlers.TryGetValue(typeFromHandle, out var value))
		{
			_handlers[typeFromHandle] = Delegate.Combine(value, handler);
		}
		else
		{
			_handlers[typeFromHandle] = handler;
		}
	}

	public void Unsubscribe<T>(Action<T> handler)
	{
		if (handler == null)
		{
			return;
		}
		Type typeFromHandle = typeof(T);
		if (_handlers.TryGetValue(typeFromHandle, out var value))
		{
			Delegate obj = Delegate.Remove(value, handler);
			if ((object)obj == null)
			{
				_handlers.Remove(typeFromHandle);
			}
			else
			{
				_handlers[typeFromHandle] = obj;
			}
		}
	}

	public void Publish<T>(T evt)
	{
		if (_handlers.TryGetValue(typeof(T), out var value))
		{
			((Action<T>)value)(evt);
		}
	}

	public void Clear()
	{
		_handlers.Clear();
	}
}
}
