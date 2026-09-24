using System;
using System.Collections.Generic;
using MyriadCreation.Core.Simulation;
using MyriadCreation.Core.Data;

namespace MyriadCreation.Simulation.Warfare
{
    public static class WarfareReligionSchedule
    {
        public const string ScheduleId = "warfare-religion.daily";

        public static void Register(
            ISimulationScheduler scheduler,
            CombatManager combat,
            Dictionary<int, Army> armies,
            List<WarState> wars,
            WarRules rules,
            Func<int> day,
            Action religionTick,
            Action holyWarSettlementTick,
            Action warOutcomeTick)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (combat == null) throw new ArgumentNullException(nameof(combat));
            if (armies == null) throw new ArgumentNullException(nameof(armies));
            if (wars == null) throw new ArgumentNullException(nameof(wars));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (day == null) throw new ArgumentNullException(nameof(day));
            if (religionTick == null) throw new ArgumentNullException(nameof(religionTick));
            if (holyWarSettlementTick == null) throw new ArgumentNullException(nameof(holyWarSettlementTick));
            if (warOutcomeTick == null) throw new ArgumentNullException(nameof(warOutcomeTick));

            scheduler.Register(ScheduleId, SimulationCadence.Daily, 24, _ =>
            {
                combat.DailyTick(armies, wars, rules, day());
                religionTick();
                holyWarSettlementTick();
                warOutcomeTick();
            });
        }
    }
}