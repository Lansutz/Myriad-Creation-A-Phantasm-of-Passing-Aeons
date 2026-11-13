using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>P1.5 海军日结写侧：补给/士气/组织/训练；舰船编制由 Navy 自身维护。</summary>
public static class NavySchedule
{
	public const string ScheduleId = "navy.daily";

	public const float SupplyPerShipPerDay = 0.05f;

	public static void Register(ISimulationScheduler scheduler, Dictionary<int, Navy> navies)
	{
		if (scheduler == null) throw new ArgumentNullException("scheduler");
		if (navies == null) throw new ArgumentNullException("navies");
		scheduler.Register(ScheduleId, SimulationCadence.Daily, 16, (SimulationTickContext _) =>
		{
			foreach (Navy navy in navies.Values)
			{
				if (navy == null || navy.state == MyriadCreation.Core.Enums.GameEnums.CombatState.Dead)
				{
					continue;
				}
				navy.DailyTick(SupplyPerShipPerDay);
			}
		});
	}
}
}
