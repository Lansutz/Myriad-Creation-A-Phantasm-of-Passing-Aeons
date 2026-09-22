using System;
using System.Collections.Generic;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Simulation;
using CivilizationEvolution.World.Settlement;
using CivilizationEvolution.Simulation.Warfare;

namespace CivilizationEvolution.Simulation.Settlement
{
    /// <summary>
    /// Settlement domain runtime.
    /// Scheduler only supplies cadence/context; settlement rules remain inside this domain boundary.
    /// </summary>
    public sealed class SettlementSimulationSystem
    {
        private readonly Dictionary<int, BurgData> _burgs;
        private readonly TileData[] _tiles;
        private readonly int _mapWidth;
        private readonly int _mapHeight;
        private readonly Dictionary<int, Army> _armies;

        public SettlementSimulationSystem(
            Dictionary<int, BurgData> burgs,
            TileData[] tiles,
            int mapWidth,
            int mapHeight,
            Dictionary<int, Army> armies)
        {
            _burgs = burgs ?? throw new ArgumentNullException(nameof(burgs));
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _mapWidth = mapWidth;
            _mapHeight = mapHeight;
            _armies = armies;
        }

        public void DailyTick(SimulationTickContext context)
        {
            SettlementControlSystem.DailyTick(
                _burgs,
                _tiles,
                _mapWidth,
                _mapHeight,
                _armies);
        }
    }

    public static class SettlementSimulationSchedule
    {
        public const string ScheduleId = "settlement.control.daily";
        public const int Order = 20;

        public static void Register(
            SimulationScheduler scheduler,
            SettlementSimulationSystem system)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (system == null) throw new ArgumentNullException(nameof(system));

            scheduler.Register(
                ScheduleId,
                SimulationCadence.Daily,
                Order,
                system.DailyTick);
        }
    }
}
