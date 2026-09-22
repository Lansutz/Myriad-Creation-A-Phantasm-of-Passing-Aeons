using System;
using CivilizationEvolution.Core.Simulation;

namespace CivilizationEvolution.Simulation.Events
{
    public static class EventSchedule
    {
        public const string ScheduleId = "events.daily";

        public static void Register(ISimulationScheduler scheduler, Action tick)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (tick == null) throw new ArgumentNullException(nameof(tick));
            scheduler.Register(ScheduleId, SimulationCadence.Daily, 29, _ => tick());
        }
    }
}