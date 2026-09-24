using System;
using MyriadCreation.Core.Simulation;

namespace MyriadCreation.Simulation.Population
{
    public static class PopulationSchedule
    {
        public const string ScheduleId = "population.daily";

        public static void Register(ISimulationScheduler scheduler, Action tick)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (tick == null) throw new ArgumentNullException(nameof(tick));
            scheduler.Register(ScheduleId, SimulationCadence.Daily, 14, _ => tick());
        }
    }
}