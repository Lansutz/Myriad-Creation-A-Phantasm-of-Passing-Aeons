using System;
using CivilizationEvolution.Core.Simulation;

namespace CivilizationEvolution.Simulation.Settlement
{
    public static class SettlementSchedule
    {
        public const string ControlScheduleId = "settlement-control.daily";
        public const string EvolutionScheduleId = "settlement-evolution.daily";
        public const string RecoveryScheduleId = "settlement-recovery.daily";
        public const string AbandonmentScheduleId = "settlement-abandonment.daily";
        public const string MapActorsScheduleId = "settlement.map-actors.daily";
        public const string CampsScheduleId = "settlement.camps.daily";

        public static void Register(
            ISimulationScheduler scheduler,
            Action control,
            Action mapActors,
            Action camps,
            Action evolution,
            Action recovery,
            Action abandonment)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (control == null) throw new ArgumentNullException(nameof(control));
            if (mapActors == null) throw new ArgumentNullException(nameof(mapActors));
            if (camps == null) throw new ArgumentNullException(nameof(camps));
            if (evolution == null) throw new ArgumentNullException(nameof(evolution));
            if (recovery == null) throw new ArgumentNullException(nameof(recovery));
            if (abandonment == null) throw new ArgumentNullException(nameof(abandonment));

            scheduler.Register(ControlScheduleId, SimulationCadence.Daily, 16, _ => control());
            scheduler.Register(MapActorsScheduleId, SimulationCadence.Daily, 17, _ => mapActors());
            scheduler.Register(CampsScheduleId, SimulationCadence.Daily, 18, _ => camps());
            scheduler.Register(EvolutionScheduleId, SimulationCadence.Daily, 19, _ => evolution());
            scheduler.Register(RecoveryScheduleId, SimulationCadence.Daily, 20, _ => recovery());
            scheduler.Register(AbandonmentScheduleId, SimulationCadence.Daily, 21, _ => abandonment());
        }
    }
}