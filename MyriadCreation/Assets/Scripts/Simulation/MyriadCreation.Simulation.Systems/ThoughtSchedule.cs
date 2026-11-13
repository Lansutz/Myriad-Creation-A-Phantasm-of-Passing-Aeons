using System;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Simulation.Systems
{
public static class ThoughtSchedule
{
	public const string ScheduleId = "thought.daily";

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
		scheduler.Register("thought.daily", SimulationCadence.Daily, 27, (SimulationTickContext _) =>
		{
			tick();
		});
	}
}
}
