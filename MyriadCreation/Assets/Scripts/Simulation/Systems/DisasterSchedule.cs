using System;
using MyriadCreation.Core.Simulation;

namespace MyriadCreation.Simulation.Systems
{
    public static class DisasterSchedule
    {
        public const string ScheduleId = "disaster.daily";

        public static void Register(ISimulationScheduler scheduler, DisasterSystem disasters, DiseaseSystem diseases, Func<int> day, Func<int> year)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (disasters == null) throw new ArgumentNullException(nameof(disasters));
            if (diseases == null) throw new ArgumentNullException(nameof(diseases));
            if (day == null) throw new ArgumentNullException(nameof(day));
            if (year == null) throw new ArgumentNullException(nameof(year));
            scheduler.Register(ScheduleId, SimulationCadence.Daily, 11, _ =>
            {
                disasters.DailyTick(day(), year());
                diseases.DailyTick(day(), year());
            });
        }
    }
}