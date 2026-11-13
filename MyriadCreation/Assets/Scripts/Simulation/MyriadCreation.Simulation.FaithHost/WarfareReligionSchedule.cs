using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.FaithHost
{
public static class WarfareReligionSchedule
{
	public const string ScheduleId = "warfare-religion.daily";

	public static void Register(ISimulationScheduler scheduler, CombatManager combat, Dictionary<int, Army> armies, List<WarState> wars, WarRules rules, Func<int> day, Action religionTick, Action holyWarSettlementTick, Action warOutcomeTick)
	{
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if (combat == null)
		{
			throw new ArgumentNullException("combat");
		}
		if (armies == null)
		{
			throw new ArgumentNullException("armies");
		}
		if (wars == null)
		{
			throw new ArgumentNullException("wars");
		}
		if (rules == null)
		{
			throw new ArgumentNullException("rules");
		}
		if (day == null)
		{
			throw new ArgumentNullException("day");
		}
		if (religionTick == null)
		{
			throw new ArgumentNullException("religionTick");
		}
		if (holyWarSettlementTick == null)
		{
			throw new ArgumentNullException("holyWarSettlementTick");
		}
		if (warOutcomeTick == null)
		{
			throw new ArgumentNullException("warOutcomeTick");
		}
		scheduler.Register("warfare-religion.daily", SimulationCadence.Daily, 24, (SimulationTickContext _) =>
		{
			combat.DailyTick(armies, wars, rules, day());
			religionTick();
			holyWarSettlementTick();
			warOutcomeTick();
		});
	}
}
}
