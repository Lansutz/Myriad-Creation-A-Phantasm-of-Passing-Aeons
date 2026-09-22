using System;
using CivilizationEvolution.Core.Simulation;

namespace CivilizationEvolution.Core.Simulation
{
    public static class SimulationTimeSchedule
    {
        public const string ScheduleId = "simulation.time.daily";

        public static void Register(ISimulationScheduler scheduler, Action advance)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (advance == null) throw new ArgumentNullException(nameof(advance));
            scheduler.Register(ScheduleId, SimulationCadence.Daily, 31, _ => advance());
        }
    }
}