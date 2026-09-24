using System;
using MyriadCreation.Core.Simulation;

namespace MyriadCreation.Simulation.Systems
{
    public static class CharacterSchedule
    {
        public const string ScheduleId = "characters.daily";

        public static void Register(
            ISimulationScheduler scheduler,
            CharacterManager characters,
            Func<int> day,
            Func<int> year)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (day == null) throw new ArgumentNullException(nameof(day));
            if (year == null) throw new ArgumentNullException(nameof(year));
            scheduler.Register(ScheduleId, SimulationCadence.Daily, 25, _ => characters.DailyTick(day(), year()));
        }
    }
}