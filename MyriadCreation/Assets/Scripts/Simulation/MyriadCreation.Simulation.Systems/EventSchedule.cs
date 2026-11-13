using System;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Simulation.Systems
{
public static class EventSchedule
{
	public const string ScheduleId = "events.daily";

	public static void Register(ISimulationScheduler scheduler, Action tick)
	{
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if (tick == null)
		{
			throw new ArgumentNullException("tick");
		}
		scheduler.Register("events.daily", SimulationCadence.Daily, 29, (SimulationTickContext _) =>
		{
			tick();
		});
	}
}
}
