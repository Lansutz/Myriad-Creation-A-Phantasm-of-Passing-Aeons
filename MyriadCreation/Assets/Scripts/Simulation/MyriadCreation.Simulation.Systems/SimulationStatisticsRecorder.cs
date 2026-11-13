using System;
using MyriadCreation.Core.Events;

namespace MyriadCreation.Simulation.Systems
{
public sealed class SimulationStatisticsRecorder : IDisposable
{
	private readonly SimulationEventBus _events;

	private readonly Action<WarDeclaredEvent> _onWarDeclared;

	private readonly Action<WarEndedEvent> _onWarEnded;

	private readonly Action<EconomyDayTickedEvent> _onEconomyTick;

	private readonly Action<SeasonChangedEvent> _onSeasonChanged;

	public int WarsDeclared { get; private set; }

	public int WarsEnded { get; private set; }

	public int EconomyTicks { get; private set; }

	public int SeasonsChanged { get; private set; }

	public SimulationStatisticsRecorder(SimulationEventBus events)
	{
		_events = events ?? throw new ArgumentNullException("events");
		_onWarDeclared = (WarDeclaredEvent _) =>
		{
			WarsDeclared++;
		};
		_onWarEnded = (WarEndedEvent _) =>
		{
			WarsEnded++;
		};
		_onEconomyTick = (EconomyDayTickedEvent _) =>
		{
			EconomyTicks++;
		};
		_onSeasonChanged = (SeasonChangedEvent _) =>
		{
			SeasonsChanged++;
		};
		_events.Subscribe(_onWarDeclared);
		_events.Subscribe(_onWarEnded);
		_events.Subscribe(_onEconomyTick);
		_events.Subscribe(_onSeasonChanged);
	}

	public void Dispose()
	{
		_events.Unsubscribe(_onWarDeclared);
		_events.Unsubscribe(_onWarEnded);
		_events.Unsubscribe(_onEconomyTick);
		_events.Unsubscribe(_onSeasonChanged);
	}
}
}
