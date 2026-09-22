using System;
using System.Collections.Generic;
using CivilizationEvolution.Core.Simulation;
using CivilizationEvolution.World.Climate;
using CivilizationEvolution.World.Generation;

namespace CivilizationEvolution.Simulation.WorldState
{
    /// <summary>
    /// Owns runtime map dirty state and incremental terrain/climate recalculation.
    /// GameWorld exposes compatibility entry points, but the scheduler executes this system directly.
    /// </summary>
    public sealed class WorldMapSimulationSystem
    {
        private readonly SeaLandGenerator _seaLandGenerator;
        private readonly PlanetClimateSimulator _climateSimulator;
        private readonly HashSet<int> _terrainDirtyTiles = new HashSet<int>();
        private readonly HashSet<int> _climateDirtyTiles = new HashSet<int>();
        private bool _configDirty;

        public WorldMapSimulationSystem(
            SeaLandGenerator seaLandGenerator,
            PlanetClimateSimulator climateSimulator)
        {
            _seaLandGenerator = seaLandGenerator ?? throw new ArgumentNullException(nameof(seaLandGenerator));
            _climateSimulator = climateSimulator ?? throw new ArgumentNullException(nameof(climateSimulator));
        }

        public bool IsDirty => _configDirty || _terrainDirtyTiles.Count > 0 || _climateDirtyTiles.Count > 0;

        public void MarkAllTerrainDirty(int tileCount)
        {
            for (int i = 0; i < tileCount; i++)
                _terrainDirtyTiles.Add(i);
        }

        public void MarkClimateDirty(int tileIndex)
        {
            if (tileIndex >= 0) _climateDirtyTiles.Add(tileIndex);
        }

        public void MarkTileDirty(int tileIndex)
        {
            if (tileIndex < 0) return;
            _terrainDirtyTiles.Add(tileIndex);
            foreach (int neighbour in _seaLandGenerator.GetNeighbourIndices(tileIndex))
                _terrainDirtyTiles.Add(neighbour);
        }

        public void MarkConfigDirty()
        {
            _configDirty = true;
        }

        public void RecalculateAll()
        {
            _seaLandGenerator.RecalculateAll();
            _climateSimulator.RecalculateAll();
            _terrainDirtyTiles.Clear();
            _climateDirtyTiles.Clear();
            _configDirty = false;
        }

        public void RecalculateDirty()
        {
            if (_configDirty)
            {
                RecalculateAll();
                return;
            }

            if (_terrainDirtyTiles.Count > 0)
            {
                _seaLandGenerator.RecalculateDirty(_terrainDirtyTiles);
                foreach (int idx in _terrainDirtyTiles)
                    _climateDirtyTiles.Add(idx);
                _terrainDirtyTiles.Clear();
            }

            if (_climateDirtyTiles.Count > 0)
            {
                _climateSimulator.RecalculateDirty(_climateDirtyTiles);
                _climateDirtyTiles.Clear();
            }
        }
    }

    public static class WorldMapSchedule
    {
        public const string ScheduleId = "world.map.recalculate-dirty";
        public const string DirtyKey = "world.terrain";

        public static void Register(ISimulationScheduler scheduler, WorldMapSimulationSystem system)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (system == null) throw new ArgumentNullException(nameof(system));

            scheduler.RegisterDirty(
                ScheduleId,
                SimulationCadence.Daily,
                10,
                DirtyKey,
                _ => system.RecalculateDirty());
        }
    }
}
