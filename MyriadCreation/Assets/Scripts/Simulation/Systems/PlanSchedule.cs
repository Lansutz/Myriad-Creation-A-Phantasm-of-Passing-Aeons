using System;
using MyriadCreation.Core.Simulation;

namespace MyriadCreation.Simulation.Systems
{
    public static class PlanSchedule
    {
        public const string ScheduleId = "plans.daily";

        public static void Register(ISimulationScheduler scheduler, PlanSystem plans)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (plans == null) throw new ArgumentNullException(nameof(plans));

            scheduler.Register(
                ScheduleId,
                SimulationCadence.Daily,
                30,
                context => plans.DailyTick(context.day, context.deltaDays));
        }
    }
}