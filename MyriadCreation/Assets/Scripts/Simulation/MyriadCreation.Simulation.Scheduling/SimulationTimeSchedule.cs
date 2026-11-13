using System;

namespace MyriadCreation.Simulation.Scheduling
{
public static class SimulationTimeSchedule
{
	public const string ScheduleId = "simulation.time.daily";

	public static void Register(ISimulationScheduler scheduler, Action advance)
	{
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if (advance == null)
		{
			throw new ArgumentNullException("advance");
		}
		scheduler.Register("simulation.time.daily", SimulationCadence.Daily, 31, (SimulationTickContext _) =>
		{
			advance();
		});
	}
}
}
