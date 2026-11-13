using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Simulation.Units
{
public static class UnitPathfinder
{
	private static readonly ConditionalWeakTable<GameWorld, PathfindingSystem> Cache = new ConditionalWeakTable<GameWorld, PathfindingSystem>();

	public static PathfindingSystem Get(GameWorld world)
	{
		if ((Object)(object)world == (Object)null)
		{
			return null;
		}
		if (Cache.TryGetValue(world, out var value) && value != null)
		{
			return value;
		}
		value = new PathfindingSystem(world, world.mapWidth, world.mapHeight);
		Cache.Add(world, value);
		return value;
	}

	public static List<int> FindPath(GameWorld world, int start, int end)
	{
		List<int> list = new List<int>();
		if ((Object)(object)world == (Object)null || world.tiles == null)
		{
			return list;
		}
		if (start < 0 || end < 0 || start >= world.tiles.Length || end >= world.tiles.Length)
		{
			return list;
		}
		if (start == end)
		{
			return list;
		}
		PathfindingSystem pathfindingSystem = Get(world);
		if (pathfindingSystem == null)
		{
			return list;
		}
		PathfindingSystem.PathResult pathResult = pathfindingSystem.FindPath(start, end, new PathfindingSystem.PathfindingParams
		{
			armyRealmId = -1,
			isNavy = false,
			maxSlope = 60f,
			maxElevation = 6000f,
			canPassEnemyTerritory = true,
			canPassImpassable = false
		});
		if (pathResult == null || !pathResult.success || pathResult.path == null)
		{
			return list;
		}
		for (int i = 0; i < pathResult.path.Count; i++)
		{
			if (pathResult.path[i] != start)
			{
				list.Add(pathResult.path[i]);
			}
		}
		return list;
	}
}
}
