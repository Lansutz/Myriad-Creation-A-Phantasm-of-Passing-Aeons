using System;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Simulation.Systems
{
public static class ResearchPlanSchedule
{
	public const string ScheduleId = "research.breakthrough.daily";

	public static void Register(ISimulationScheduler scheduler, ResearchPlanSystem research)
	{
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if (research == null)
		{
			throw new ArgumentNullException("research");
		}
		scheduler.Register("research.breakthrough.daily", SimulationCadence.Daily, 29, (SimulationTickContext context) =>
		{
			research.ProcessPracticeBreakthroughs(context.deltaDays);
		});
	}
}
}
