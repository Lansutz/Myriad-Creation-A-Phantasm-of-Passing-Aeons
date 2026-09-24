using System;
using MyriadCreation.Core.Simulation;

namespace MyriadCreation.Simulation.Systems
{
    /// <summary>
    /// Economy-owned scheduler registration. GameWorld composes this module but does not
    /// define the economy cadence or execution contract itself.
    /// </summary>
    public static class EconomySchedule
    {
        public const string ScheduleId = "economy.daily";

        public static void Register(ISimulationScheduler scheduler, EconomyManager economy)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (economy == null) throw new ArgumentNullException(nameof(economy));

            scheduler.Register(
                ScheduleId,
                SimulationCadence.Daily,
                12,
                _ => economy.DailyTick());
        }
    }
}
