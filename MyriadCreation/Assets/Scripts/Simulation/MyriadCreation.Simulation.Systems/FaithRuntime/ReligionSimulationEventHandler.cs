using System;
using System.Collections.Generic;
using MyriadCreation.Core.Events;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Faith;

namespace MyriadCreation.Simulation.FaithHost
{
public sealed class ReligionSimulationEventHandler : IDisposable
{
	private readonly IReadOnlyList<FaithSystem> _faithSystems;

	private readonly SimulationEventBus _events;

	public ReligionSimulationEventHandler(IReadOnlyList<FaithSystem> faithSystems, SimulationEventBus events)
	{
		_faithSystems = faithSystems ?? throw new ArgumentNullException("faithSystems");
		_events = events ?? throw new ArgumentNullException("events");
		_events.Subscribe<FaithConflictTriggeredEvent>(OnFaithConflictTriggered);
	}

	private void OnFaithConflictTriggered(FaithConflictTriggeredEvent evt)
	{
		if (evt.FaithA != evt.FaithB)
		{
			FaithSystem faithSystem = Find(evt.FaithA);
			FaithSystem faithSystem2 = Find(evt.FaithB);
			faithSystem?.AddFervor(25f);
			faithSystem2?.AddFervor(25f);
		}
	}

	private FaithSystem Find(int faithId)
	{
		for (int i = 0; i < _faithSystems.Count; i++)
		{
			FaithSystem faithSystem = _faithSystems[i];
			if (faithSystem != null && faithSystem.faithId == faithId)
			{
				return faithSystem;
			}
		}
		return null;
	}

	public void Dispose()
	{
		_events.Unsubscribe<FaithConflictTriggeredEvent>(OnFaithConflictTriggered);
	}
}
}
