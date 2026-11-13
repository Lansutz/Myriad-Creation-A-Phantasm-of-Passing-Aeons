using System;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Simulation.Systems
{
public static class SettlementSchedule
{
	public const string ControlScheduleId = "settlement-control.daily";

	public const string EvolutionScheduleId = "settlement-evolution.daily";

	public const string RecoveryScheduleId = "settlement-recovery.daily";

	public const string AbandonmentScheduleId = "settlement-abandonment.daily";

	public const string ProductionScheduleId = "settlement-production.daily";

	public const string MapActorsScheduleId = "settlement.map-actors.daily";

	public const string CampsScheduleId = "settlement.camps.daily";

	public const string TypologyScheduleId = "settlement-typology.daily";

	public static void Register(SimulationScheduler scheduler, SettlementSimulationSystem system)
	{
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if (system == null)
		{
			throw new ArgumentNullException("system");
		}
		scheduler.Register("settlement-control.daily", SimulationCadence.Daily, 16, system.ControlDailyTick);
		scheduler.Register("settlement.map-actors.daily", SimulationCadence.Daily, 17, system.MapActorsDailyTick);
		scheduler.Register("settlement.camps.daily", SimulationCadence.Daily, 18, system.CampsDailyTick);
		scheduler.Register("settlement-evolution.daily", SimulationCadence.Daily, 19, system.EvolutionDailyTick);
		scheduler.Register("settlement-recovery.daily", SimulationCadence.Daily, 20, system.RecoveryDailyTick);
		scheduler.Register("settlement-abandonment.daily", SimulationCadence.Daily, 21, system.AbandonmentDailyTick);
		scheduler.Register("settlement-production.daily", SimulationCadence.Daily, 22, system.ProductionDailyTick);
		scheduler.Register("settlement-typology.daily", SimulationCadence.Daily, 23, system.TypologyDailyTick);
	}
}
}
