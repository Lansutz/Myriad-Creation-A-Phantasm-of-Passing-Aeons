using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Simulation;
using MyriadCreation.Simulation.Characters;
using MyriadCreation.Simulation.Diplomacy;
using MyriadCreation.Simulation.Innovation;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.AI
{
    public static class AISchedule
    {
        public const string ScheduleId = "ai.daily";

        public static void Register(
            ISimulationScheduler scheduler,
            AIManager ai,
            Func<Dictionary<int, RealmData>> realms,
            Func<TileData[]> tiles,
            DiplomacyManager diplomacy,
            InnovationTree innovation,
            CharacterManager characters,
            Action missionaryTick,
            AIIntentExecutor executor)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (ai == null) throw new ArgumentNullException(nameof(ai));
            if (realms == null) throw new ArgumentNullException(nameof(realms));
            if (tiles == null) throw new ArgumentNullException(nameof(tiles));
            if (diplomacy == null) throw new ArgumentNullException(nameof(diplomacy));
            if (innovation == null) throw new ArgumentNullException(nameof(innovation));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (missionaryTick == null) throw new ArgumentNullException(nameof(missionaryTick));
            if (executor == null) throw new ArgumentNullException(nameof(executor));

            scheduler.Register(ScheduleId, SimulationCadence.Daily, 28, _ =>
            {
                missionaryTick();
                ai.SyncRulers(characters);
                ai.DailyTick(realms(), tiles(), diplomacy, innovation, characters);
                ai.ExecutePendingIntents(executor);
            });
        }
    }
}