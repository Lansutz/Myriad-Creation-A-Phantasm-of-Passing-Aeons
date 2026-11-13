using System;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Simulation.Systems
{
public static class BuildingSchedule
{
	public const string ScheduleId = "building.daily";

	public static void Register(ISimulationScheduler scheduler, BuildingSystem buildings)
	{
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if (buildings == null)
		{
			throw new ArgumentNullException("buildings");
		}
		scheduler.Register("building.daily", SimulationCadence.Daily, 13, (SimulationTickContext _) =>
		{
			buildings.DailyTick();
		});
	}
}
}
