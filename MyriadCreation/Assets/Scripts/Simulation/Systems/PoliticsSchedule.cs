using System;
using MyriadCreation.Core.Simulation;

namespace MyriadCreation.Simulation.Systems
{
    public static class PoliticsSchedule
    {
        public const string ScheduleId = "politics.daily";

        public static void Register(ISimulationScheduler scheduler, PoliticsSimulationSystem system)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (system == null) throw new ArgumentNullException(nameof(system));

            scheduler.Register(ScheduleId, SimulationCadence.Daily, 15, system.DailyTick);
        }
    }
}
