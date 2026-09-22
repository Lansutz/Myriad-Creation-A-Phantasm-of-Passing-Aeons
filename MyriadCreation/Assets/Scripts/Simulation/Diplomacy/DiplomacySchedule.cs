using System;
using CivilizationEvolution.Core.Simulation;

namespace CivilizationEvolution.Simulation.Diplomacy
{
    public static class DiplomacySchedule
    {
        public const string ScheduleId = "diplomacy.daily";

        public static void Register(ISimulationScheduler scheduler, DiplomacyManager diplomacy)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (diplomacy == null) throw new ArgumentNullException(nameof(diplomacy));
            scheduler.Register(ScheduleId, SimulationCadence.Daily, 23, _ => diplomacy.DailyTick());
        }
    }
}