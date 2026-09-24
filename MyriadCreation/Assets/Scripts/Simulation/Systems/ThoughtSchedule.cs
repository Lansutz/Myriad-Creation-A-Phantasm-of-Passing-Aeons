using System;
using MyriadCreation.Core.Simulation;

namespace MyriadCreation.Simulation.Systems
{
    public static class ThoughtSchedule
    {
        public const string ScheduleId = "thought.daily";

        public static void Register(ISimulationScheduler scheduler, Action tick)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (tick == null) throw new ArgumentNullException(nameof(tick));
            scheduler.Register(ScheduleId, SimulationCadence.Daily, 27, _ => tick());
        }
    }
}