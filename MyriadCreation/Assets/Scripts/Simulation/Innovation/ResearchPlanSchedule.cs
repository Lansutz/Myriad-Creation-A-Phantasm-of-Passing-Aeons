using System;
using CivilizationEvolution.Core.Simulation;

namespace CivilizationEvolution.Simulation.Innovation
{
    /// <summary>
    /// Research is scheduled as a domain runtime system, not as a GameWorld callback.
    /// It consumes the practice facts accumulated by production and other processes.
    /// </summary>
    public static class ResearchPlanSchedule
    {
        public const string ScheduleId = "research.breakthrough.daily";

        public static void Register(ISimulationScheduler scheduler, ResearchPlanSystem research)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (research == null) throw new ArgumentNullException(nameof(research));
            scheduler.Register(ScheduleId, SimulationCadence.Daily, 29, context =>
                research.ProcessPracticeBreakthroughs(context.DeltaDays));
        }
    }
}
