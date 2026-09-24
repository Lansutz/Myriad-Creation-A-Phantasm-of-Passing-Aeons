using System;
using MyriadCreation.Core.Simulation;

namespace MyriadCreation.Simulation.Characters
{
    public static class SuccessionSchedule
    {
        public const string ScheduleId = "succession.daily";

        public static void Register(ISimulationScheduler scheduler, Action tick)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (tick == null) throw new ArgumentNullException(nameof(tick));
            scheduler.Register(ScheduleId, SimulationCadence.Daily, 26, _ => tick());
        }
    }
}