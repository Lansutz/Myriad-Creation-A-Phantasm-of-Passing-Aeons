using System;
using System.Collections.Generic;
using CivilizationEvolution.Core.Simulation;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Diplomacy;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Innovation;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Planning;
using CivilizationEvolution.Simulation.WorldState;

namespace CivilizationEvolution.Simulation.AI
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
            EconomyManager economy,
            InnovationTree innovation,
            CharacterManager characters,
            Action missionaryTick)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (ai == null) throw new ArgumentNullException(nameof(ai));
            if (realms == null) throw new ArgumentNullException(nameof(realms));
            if (tiles == null) throw new ArgumentNullException(nameof(tiles));
            if (diplomacy == null) throw new ArgumentNullException(nameof(diplomacy));
            if (economy == null) throw new ArgumentNullException(nameof(economy));
            if (innovation == null) throw new ArgumentNullException(nameof(innovation));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (missionaryTick == null) throw new ArgumentNullException(nameof(missionaryTick));

            scheduler.Register(ScheduleId, SimulationCadence.Daily, 28, _ =>
            {
                missionaryTick();
                ai.SyncRulers(characters);
                ai.DailyTick(realms(), tiles(), diplomacy, economy, innovation, characters);
            });
        }
    }
}