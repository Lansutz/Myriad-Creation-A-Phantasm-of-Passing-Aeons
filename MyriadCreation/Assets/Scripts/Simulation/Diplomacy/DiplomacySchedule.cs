using System;
using MyriadCreation.Core.Simulation;

namespace MyriadCreation.Simulation.Diplomacy
{
    public static class DiplomacySchedule
    {
        public const string ScheduleId = "diplomacy.daily";

        public static void Register(ISimulationScheduler scheduler, DiplomacyManager diplomacy, Func<int> day)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (diplomacy == null) throw new ArgumentNullException(nameof(diplomacy));
            if (day == null) throw new ArgumentNullException(nameof(day));
            scheduler.Register(ScheduleId, SimulationCadence.Daily, 23, _ =>
            {
                diplomacy.CurrentDay = day();
                diplomacy.DailyTick();
            });
        }
    }
}