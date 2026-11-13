using System;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Simulation.Systems
{
public static class DiplomacySchedule
{
	public const string ScheduleId = "diplomacy.daily";

	public static void Register(ISimulationScheduler scheduler, DiplomacyManager diplomacy, Func<int> day)
	{
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if (diplomacy == null)
		{
			throw new ArgumentNullException("diplomacy");
		}
		if (day == null)
		{
			throw new ArgumentNullException("day");
		}
		scheduler.Register("diplomacy.daily", SimulationCadence.Daily, 23, (SimulationTickContext _) =>
		{
			diplomacy.CurrentDay = day();
			diplomacy.DailyTick();
		});
	}
}
}
