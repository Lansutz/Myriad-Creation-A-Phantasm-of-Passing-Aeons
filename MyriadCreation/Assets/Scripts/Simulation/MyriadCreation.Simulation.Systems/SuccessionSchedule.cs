using System;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Simulation.Systems
{
public static class SuccessionSchedule
{
	public const string ScheduleId = "succession.daily";

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
		scheduler.Register("succession.daily", SimulationCadence.Daily, 26, (SimulationTickContext _) =>
		{
			tick();
		});
	}
}
}
