using System;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Simulation.CultureHost
{
public static class CultureSchedule
{
	public const string ScheduleId = "culture-stage.monthly";

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
		scheduler.Register("culture-stage.monthly", SimulationCadence.Monthly, 22, (SimulationTickContext _) =>
		{
			tick();
		});
	}
}
}
