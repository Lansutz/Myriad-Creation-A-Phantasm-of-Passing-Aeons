using System;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Simulation.Systems
{
public static class PopulationSchedule
{
	public const string ScheduleId = "population.daily";

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
		scheduler.Register("population.daily", SimulationCadence.Daily, 14, (SimulationTickContext _) =>
		{
			tick();
		});
	}
}
}
