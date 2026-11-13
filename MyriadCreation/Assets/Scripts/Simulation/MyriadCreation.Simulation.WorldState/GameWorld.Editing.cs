using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Core.Events;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Simulation.AI;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.Diplomacy;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Innovation;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.Planning;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Settlement;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.Warfare;
using MyriadCreation.World;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Biome;
using MyriadCreation.World.Climate;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;
using MyriadCreation.World.Terrain;
using UnityEngine;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.Culture;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace MyriadCreation.Simulation.WorldState
{
public partial class GameWorld : MonoBehaviour
{

	public void RecalculateDirty()
	{
		_worldMapSimulation?.RecalculateDirty();
	}

	public void PaintTerrain(int tileIndex, float newElevation)
	{
		if (tileIndex >= 0 && tileIndex < tiles.Length)
		{
			tiles[tileIndex].elevation01 = newElevation;
			_worldMapSimulation?.MarkTileDirty(tileIndex);
			SimulationScheduler.MarkDirty("world.terrain");
		}
	}

	public void UpdateConfig(Action<WorldConfig> configUpdater)
	{
		configUpdater?.Invoke(config);
		_worldMapSimulation?.MarkConfigDirty();
		SimulationScheduler.MarkDirty("world.terrain");
	}

	public bool CreateTile(int tileIndex)
	{
		if (tileIndex < 0 || tileIndex >= tiles.Length)
		{
			return false;
		}
		if (tiles[tileIndex].exists)
		{
			return false;
		}
		tiles[tileIndex].exists = true;
		tiles[tileIndex].ownerRealmId = -1;
		tiles[tileIndex].occupyingRealmId = -1;
		tiles[tileIndex].stability = 50f;
		tiles[tileIndex].order = 50f;
		tiles[tileIndex].development = 0.1f;
		if (tiles[tileIndex].populationBlocks == null)
		{
			tiles[tileIndex].populationBlocks = new List<PopulationBlock>();
		}
		if (tiles[tileIndex].buildingLevels == null)
		{
			tiles[tileIndex].buildingLevels = new int[6];
		}
		_worldMapSimulation?.MarkTileDirty(tileIndex);
		return true;
	}

	public bool RemoveTile(int tileIndex)
	{
		if (tileIndex < 0 || tileIndex >= tiles.Length)
		{
			return false;
		}
		if (!tiles[tileIndex].exists)
		{
			return false;
		}
		tiles[tileIndex].exists = false;
		tiles[tileIndex].ownerRealmId = -1;
		tiles[tileIndex].occupyingRealmId = -1;
		tiles[tileIndex].populationBlocks?.Clear();
		_worldMapSimulation?.MarkTileDirty(tileIndex);
		return true;
	}
}
}
