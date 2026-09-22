using System;
using System.Collections.Generic;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Simulation;
using CivilizationEvolution.Simulation.Warfare;

namespace CivilizationEvolution.Simulation.Settlement
{
    /// <summary>聚落领域运行时。负责聚落控制与现有聚落相关日常阶段。</summary>
    public sealed class SettlementSimulationSystem
    {
        private readonly Dictionary<int, BurgData> _burgs;
        private readonly TileData[] _tiles;
        private readonly int _mapWidth;
        private readonly int _mapHeight;
        private readonly Dictionary<int, Army> _armies;
        private readonly Action _mapActorsTick;
        private readonly Action _campsTick;
        private readonly Action _evolutionTick;
        private readonly Action _recoveryTick;
        private readonly Action _abandonmentTick;

        public SettlementSimulationSystem(
            Dictionary<int, BurgData> burgs,
            TileData[] tiles,
            int mapWidth,
            int mapHeight,
            Dictionary<int, Army> armies,
            Action mapActorsTick,
            Action campsTick,
            Action evolutionTick,
            Action recoveryTick,
            Action abandonmentTick)
        {
            _burgs = burgs ?? throw new ArgumentNullException(nameof(burgs));
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _mapWidth = mapWidth;
            _mapHeight = mapHeight;
            _armies = armies ?? throw new ArgumentNullException(nameof(armies));
            _mapActorsTick = mapActorsTick ?? throw new ArgumentNullException(nameof(mapActorsTick));
            _campsTick = campsTick ?? throw new ArgumentNullException(nameof(campsTick));
            _evolutionTick = evolutionTick ?? throw new ArgumentNullException(nameof(evolutionTick));
            _recoveryTick = recoveryTick ?? throw new ArgumentNullException(nameof(recoveryTick));
            _abandonmentTick = abandonmentTick ?? throw new ArgumentNullException(nameof(abandonmentTick));
        }

        public void ControlDailyTick(SimulationTickContext context)
        {
            SettlementControlSystem.DailyTick(_burgs, _tiles, _mapWidth, _mapHeight, _armies);
        }

        public void MapActorsDailyTick(SimulationTickContext context) => _mapActorsTick();
        public void CampsDailyTick(SimulationTickContext context) => _campsTick();
        public void EvolutionDailyTick(SimulationTickContext context) => _evolutionTick();
        public void RecoveryDailyTick(SimulationTickContext context) => _recoveryTick();
        public void AbandonmentDailyTick(SimulationTickContext context) => _abandonmentTick();
    }

    public static class SettlementSchedule
    {
        public const string ControlScheduleId = "settlement-control.daily";
        public const string EvolutionScheduleId = "settlement-evolution.daily";
        public const string RecoveryScheduleId = "settlement-recovery.daily";
        public const string AbandonmentScheduleId = "settlement-abandonment.daily";
        public const string MapActorsScheduleId = "settlement.map-actors.daily";
        public const string CampsScheduleId = "settlement.camps.daily";

        public static void Register(SimulationScheduler scheduler, SettlementSimulationSystem system)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (system == null) throw new ArgumentNullException(nameof(system));

            scheduler.Register(ControlScheduleId, SimulationCadence.Daily, 16, system.ControlDailyTick);
            scheduler.Register(MapActorsScheduleId, SimulationCadence.Daily, 17, system.MapActorsDailyTick);
            scheduler.Register(CampsScheduleId, SimulationCadence.Daily, 18, system.CampsDailyTick);
            scheduler.Register(EvolutionScheduleId, SimulationCadence.Daily, 19, system.EvolutionDailyTick);
            scheduler.Register(RecoveryScheduleId, SimulationCadence.Daily, 20, system.RecoveryDailyTick);
            scheduler.Register(AbandonmentScheduleId, SimulationCadence.Daily, 21, system.AbandonmentDailyTick);
        }
    }
}
