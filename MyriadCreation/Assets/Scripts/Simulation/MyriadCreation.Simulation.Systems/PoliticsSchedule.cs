using System;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Simulation.Systems
{
public static class PoliticsSchedule
{
	public const string ScheduleId = "politics.daily";

	public static void Register(ISimulationScheduler scheduler, PoliticsSimulationSystem system)
	{
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if (system == null)
		{
			throw new ArgumentNullException("system");
		}
		scheduler.Register("politics.daily", SimulationCadence.Daily, 15, system.DailyTick);
	}
}
}
