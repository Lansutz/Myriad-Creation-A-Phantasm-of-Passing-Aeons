using System;
using CivilizationEvolution.Core.Simulation;

namespace CivilizationEvolution.Simulation.Politics
{
    public static class PoliticsSchedule
    {
        public const string ScheduleId = "politics.daily";

        public static void Register(ISimulationScheduler scheduler, PoliticalManager politics, Action worldPolitics)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (politics == null) throw new ArgumentNullException(nameof(politics));
            if (worldPolitics == null) throw new ArgumentNullException(nameof(worldPolitics));
            scheduler.Register(ScheduleId, SimulationCadence.Daily, 15, _ =>
            {
                politics.DailyTick();
                worldPolitics();
            });
        }
    }
}