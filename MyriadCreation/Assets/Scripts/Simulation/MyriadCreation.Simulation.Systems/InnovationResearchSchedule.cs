using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Simulation.AI;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.Systems
{
public static class InnovationResearchSchedule
{
	public const string ScheduleId = "innovation.legacy_research.daily";

	public static void Register(ISimulationScheduler scheduler, InnovationTree innovations, AIManager ai, Func<Dictionary<int, RealmData>> realms, Func<TileData[]> tiles)
	{
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if (innovations == null)
		{
			throw new ArgumentNullException("innovations");
		}
		if (ai == null)
		{
			throw new ArgumentNullException("ai");
		}
		if (realms == null)
		{
			throw new ArgumentNullException("realms");
		}
		if (tiles == null)
		{
			throw new ArgumentNullException("tiles");
		}
		scheduler.Register("innovation.legacy_research.daily", SimulationCadence.Daily, 29, (SimulationTickContext ctx) =>
		{
			Dictionary<int, RealmData> dictionary = realms();
			TileData[] array = tiles();
			if (dictionary == null || array == null)
			{
				return;
			}
			// 日结系统按 deltaDays 折算：大跳步（daysPerTick>1）时一天只加一次会退化成 1/daysPerTick 速度。
			int deltaDays = Math.Max(1, ctx.deltaDays);
			foreach (KeyValuePair<int, RealmData> item in dictionary)
			{
				RealmData value = item.Value;
				if (value != null)
				{
					float researchRate = ai.GetResearchRate(value.realmId, value, array);
					if (researchRate > 0f)
					{
						innovations.DailyTick(value.realmId, researchRate * deltaDays);
					}
				}
			}
		});
	}
}
}
