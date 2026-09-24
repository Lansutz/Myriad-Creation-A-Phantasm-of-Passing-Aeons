using System;
using MyriadCreation.Core.Simulation;

namespace MyriadCreation.Simulation.Culture
{
    public static class CultureSchedule
    {
        public const string ScheduleId = "culture-stage.monthly";

        public static void Register(ISimulationScheduler scheduler, Action tick)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (tick == null) throw new ArgumentNullException(nameof(tick));
            scheduler.Register(ScheduleId, SimulationCadence.Monthly, 22, _ => tick());
        }
    }
}