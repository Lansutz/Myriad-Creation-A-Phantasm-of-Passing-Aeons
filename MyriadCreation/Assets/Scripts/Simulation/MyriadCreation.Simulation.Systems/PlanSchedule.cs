using System;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Simulation.Systems
{
public static class PlanSchedule
{
	public const string ScheduleId = "plans.daily";

	public static void Register(ISimulationScheduler scheduler, PlanSystem plans)
	{
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if (plans == null)
		{
			throw new ArgumentNullException("plans");
		}
		scheduler.Register("plans.daily", SimulationCadence.Daily, 30, (SimulationTickContext context) =>
		{
			plans.DailyTick(context.day, context.deltaDays);
		});
	}
}
}
