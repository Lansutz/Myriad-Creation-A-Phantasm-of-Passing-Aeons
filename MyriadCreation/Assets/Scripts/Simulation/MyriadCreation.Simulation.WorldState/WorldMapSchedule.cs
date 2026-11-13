using System;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Simulation.WorldState
{
public static class WorldMapSchedule
{
	public const string ScheduleId = "world.map.recalculate-dirty";

	public const string DirtyKey = "world.terrain";

	public static void Register(ISimulationScheduler scheduler, WorldMapSimulationSystem system)
	{
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if (system == null)
		{
			throw new ArgumentNullException("system");
		}
		scheduler.RegisterDirty("world.map.recalculate-dirty", SimulationCadence.Daily, 10, "world.terrain", (SimulationTickContext _) =>
		{
			system.RecalculateDirty();
		});
	}
}
}
