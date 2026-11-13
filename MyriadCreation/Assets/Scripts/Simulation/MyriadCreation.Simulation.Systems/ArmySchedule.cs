using System;
using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Simulation.State;
using MyriadCreation.World.Terrain;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>P1.5 军队日结：补给/组织/训练与行军推进；行军后由 GameWorld 做敌境占领判定。</summary>
public static class ArmySchedule
{
	public const string ScheduleId = "army.daily";

	public static void Register(ISimulationScheduler scheduler, Dictionary<int, Army> armies, Dictionary<int, UnitDef> unitDefs, TileData[] tiles, SeaLandGenerator seaLand, Action<Army> afterTick)
	{
		if (scheduler == null) throw new ArgumentNullException("scheduler");
		if (armies == null) throw new ArgumentNullException("armies");
		if (unitDefs == null) throw new ArgumentNullException("unitDefs");
		if (tiles == null) throw new ArgumentNullException("tiles");
		scheduler.Register(ScheduleId, SimulationCadence.Daily, 15, (SimulationTickContext _) =>
		{
			foreach (Army army in armies.Values)
			{
				if (army == null || army.state == GameEnums.CombatState.Dead)
				{
					continue;
				}
				army.DailyTick(unitDefs, tiles, seaLand);
				afterTick?.Invoke(army);
			}
		});
	}
}
}
