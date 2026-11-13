using System;
using System.Collections.Generic;
using MyriadCreation.Core.Events;

namespace MyriadCreation.Simulation.Scheduling
{
public sealed class SimulationEventDirtyBridge : IDisposable
{
	private readonly SimulationEventBus _events;

	private readonly SimulationDirtySet _dirty;

	private readonly List<Action> _subscriptions = new List<Action>();

	private bool _disposed;

	private SimulationEventDirtyBridge(SimulationEventBus events, SimulationDirtySet dirty)
	{
		_events = events;
		_dirty = dirty;
	}

	public static SimulationEventDirtyBridge Create(SimulationEventBus events, SimulationDirtySet dirty)
	{
		if (events == null)
		{
			throw new ArgumentNullException("events");
		}
		if (dirty == null)
		{
			throw new ArgumentNullException("dirty");
		}
		return new SimulationEventDirtyBridge(events, dirty);
	}

	public SimulationEventDirtyBridge Register<TEvent>(string dirtyKey, Func<TEvent, bool> when = null)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("SimulationEventDirtyBridge");
		}
		if (string.IsNullOrWhiteSpace(dirtyKey))
		{
			throw new ArgumentException("Dirty key is required.", "dirtyKey");
		}
		Action<TEvent> handler = (TEvent evt) =>
		{
			if (when == null || when(evt))
			{
				_dirty.Mark(dirtyKey);
			}
		};
		_events.Subscribe(handler);
		_subscriptions.Add(() =>
		{
			_events.Unsubscribe(handler);
		});
		return this;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		foreach (Action subscription in _subscriptions)
		{
			subscription();
		}
		_subscriptions.Clear();
	}
}
}
