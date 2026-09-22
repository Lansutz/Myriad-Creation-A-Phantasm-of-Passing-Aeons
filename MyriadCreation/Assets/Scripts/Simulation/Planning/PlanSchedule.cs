using System;
using CivilizationEvolution.Core.Simulation;

namespace CivilizationEvolution.Simulation.Planning
{
    public static class PlanSchedule
    {
        public const string ScheduleId = "plans.daily";

        public static void Register(ISimulationScheduler scheduler, PlanSystem plans, Func<float> delta)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (plans == null) throw new ArgumentNullException(nameof(plans));
            if (delta == null) throw new ArgumentNullException(nameof(delta));
            scheduler.Register(ScheduleId, SimulationCadence.Daily, 30, _ => plans.DailyTick(delta()));
        }
    }
}