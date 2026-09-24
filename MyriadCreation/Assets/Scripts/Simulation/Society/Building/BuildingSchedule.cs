using System;
using MyriadCreation.Core.Events;
using MyriadCreation.Core.Simulation;

namespace MyriadCreation.Simulation.Society.Building
{
    public static class BuildingSchedule
    {
        public const string ScheduleId = "building.daily";

        public static void Register(ISimulationScheduler scheduler, BuildingSystem buildings)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (buildings == null) throw new ArgumentNullException(nameof(buildings));
            scheduler.Register(ScheduleId, SimulationCadence.Daily, 13, _ => buildings.DailyTick());
        }
    }
}
