using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using Object = UnityEngine.Object;

namespace MyriadCreation.Simulation.Systems
{
public class PathfindingSystem
{
	public class PathfindingParams
	{
		public int armyRealmId = -1;

		public bool isNavy;

		public float maxSlope = 45f;

		public float maxElevation = 5000f;

		public bool canPassEnemyTerritory;

		public bool canPassImpassable;

		public Dictionary<int, float> tileCostOverrides = new Dictionary<int, float>();
	}

	public class PathResult
	{
		public List<int> path = new List<int>();

		public float totalCost;

		public bool success;

		public string failReason = "";
	}

	private GameWorld _world;

	private int _width;

	private int _height;

	private bool _wrapX = true;

	private bool _wrapY;

	private static readonly int[] DX = new int[8] { 1, -1, 0, 0, 1, 1, -1, -1 };

	private static readonly int[] DY = new int[8] { 0, 0, 1, -1, 1, -1, 1, -1 };

	private static readonly float[] DCost = new float[8] { 1f, 1f, 1f, 1f, 1.414f, 1.414f, 1.414f, 1.414f };

	public PathfindingSystem(GameWorld world, int width, int height, bool wrapX = true, bool wrapY = false)
	{
		_world = world;
		_width = width;
		_height = height;
		_wrapX = wrapX;
		_wrapY = wrapY;
	}

	public PathResult FindPath(int startTile, int endTile, PathfindingParams param)
	{
		PathResult pathResult = new PathResult();
		if (startTile == endTile)
		{
			pathResult.path.Add(startTile);
			pathResult.success = true;
			return pathResult;
		}
		PriorityQueue<int, float> priorityQueue = new PriorityQueue<int, float>();
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		Dictionary<int, float> dictionary2 = new Dictionary<int, float>();
		HashSet<int> hashSet = new HashSet<int>();
		dictionary2[startTile] = 0f;
		priorityQueue.Enqueue(startTile, Heuristic(startTile, endTile));
		int num = _width * _height * 2;
		int num2 = 0;
		while (priorityQueue.Count > 0 && num2 < num)
		{
			num2++;
			int num3 = priorityQueue.Dequeue();
			if (num3 == endTile)
			{
				pathResult.path = ReconstructPath(dictionary, num3);
				pathResult.totalCost = dictionary2[num3];
				pathResult.success = true;
				return pathResult;
			}
			hashSet.Add(num3);
			foreach (var (num4, num5) in GetNeighbors(num3, param))
			{
				if (!hashSet.Contains(num4))
				{
					float num6 = dictionary2[num3] + num5;
					if (!dictionary2.ContainsKey(num4) || num6 < dictionary2[num4])
					{
						dictionary[num4] = num3;
						dictionary2[num4] = num6;
						float priority = num6 + Heuristic(num4, endTile);
						priorityQueue.Enqueue(num4, priority);
					}
				}
			}
		}
		pathResult.failReason = ((num2 >= num) ? "超出最大迭代次数" : "无法到达目标");
		return pathResult;
	}

	private List<(int tile, float cost)> GetNeighbors(int tile, PathfindingParams param)
	{
		List<(int, float)> list = new List<(int, float)>();
		int num = tile % _width;
		int num2 = tile / _width;
		for (int i = 0; i < 8; i++)
		{
			int num3 = num + DX[i];
			int num4 = num2 + DY[i];
			if (_wrapX)
			{
				if (num3 < 0)
				{
					num3 += _width;
				}
				if (num3 >= _width)
				{
					num3 -= _width;
				}
			}
			else if (num3 < 0 || num3 >= _width)
			{
				continue;
			}
			if (_wrapY)
			{
				if (num4 < 0)
				{
					num4 += _height;
				}
				if (num4 >= _height)
				{
					num4 -= _height;
				}
			}
			else if (num4 < 0 || num4 >= _height)
			{
				continue;
			}
			int num5 = num4 * _width + num3;
			float num6 = CalculateMoveCost(tile, num5, param, DCost[i]);
			if (!(num6 < 0f))
			{
				list.Add((num5, num6));
			}
		}
		return list;
	}

	private float CalculateMoveCost(int fromTile, int toTile, PathfindingParams param, float baseCost)
	{
		if ((Object)(object)_world == (Object)null)
		{
			return baseCost;
		}
		PathTile tileData = GetTileData(toTile);
		if (tileData == null)
		{
			return 50f;
		}
		if (param.isNavy)
		{
			if (!tileData.isOcean && !tileData.isCoastal)
			{
				return 50f;
			}
			return baseCost * (tileData.isOcean ? 1f : 1.5f);
		}
		if (tileData.isImpassable)
		{
			return 50f;
		}
		if (tileData.slope > param.maxSlope)
		{
			return 50f;
		}
		if (tileData.elevation > param.maxElevation)
		{
			return 50f;
		}
		if (tileData.isOcean)
		{
			return 50f;
		}
		float num = baseCost;
		num *= GetTerrainCostMultiplier(tileData);
		num *= 1f + tileData.slope / 45f;
		if (tileData.elevation > 2000f)
		{
			num *= 1.5f;
		}
		if (param.armyRealmId >= 0)
		{
			int tileOwner = GetTileOwner(toTile);
			if (tileOwner >= 0 && tileOwner != param.armyRealmId)
			{
				RealmData realm = GetRealm(tileOwner);
				if (realm != null)
				{
					switch (realm.movementControl)
					{
					case GameEnums.MovementControlLevel.Strict:
						if (!realm.militaryAccessGranted.Contains(param.armyRealmId))
						{
							return -1f;
						}
						num *= 1.2f;
						break;
					case GameEnums.MovementControlLevel.Limited:
						num *= 1.5f;
						break;
					}
					if (IsAtWar(param.armyRealmId, tileOwner))
					{
						if (!param.canPassEnemyTerritory)
						{
							return -1f;
						}
						num *= 2f;
					}
				}
			}
		}
		num *= GetBuildingCostMultiplier(toTile, param);
		if (param.tileCostOverrides.TryGetValue(toTile, out var value))
		{
			num = value;
		}
		return num;
	}

	private float GetTerrainCostMultiplier(PathTile tile)
	{
		if (tile.isMountain)
		{
			return 3f;
		}
		if (tile.isHills)
		{
			return 1.8f;
		}
		if (tile.isForest)
		{
			return 1.5f;
		}
		if (tile.isSwamp)
		{
			return 2.5f;
		}
		if (tile.isDesert)
		{
			return 1.8f;
		}
		if (tile.isPlains)
		{
			return 1f;
		}
		if (tile.isCoastal)
		{
			return 1.2f;
		}
		return 1.5f;
	}

	private float GetBuildingCostMultiplier(int tile, PathfindingParams param)
	{
		List<BuildingData> buildingsAtTile = GetBuildingsAtTile(tile);
		float num = 1f;
		foreach (BuildingData item in buildingsAtTile)
		{
			if (item.isFortification)
			{
				num = ((item.ownerRealmId == param.armyRealmId) ? (num * 0.8f) : (num * 1.5f));
			}
		}
		return num;
	}

	private float Heuristic(int a, int b)
	{
		int num = a % _width;
		int num2 = a / _width;
		int num3 = b % _width;
		int num4 = b / _width;
		int num5 = Math.Abs(num - num3);
		if (_wrapX)
		{
			num5 = Math.Min(num5, _width - num5);
		}
		int num6 = Math.Abs(num2 - num4);
		if (_wrapY)
		{
			num6 = Math.Min(num6, _height - num6);
		}
		return num5 + num6;
	}

	private List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
	{
		List<int> list = new List<int> { current };
		while (cameFrom.ContainsKey(current))
		{
			current = cameFrom[current];
			list.Insert(0, current);
		}
		return list;
	}

	private PathTile GetTileData(int tileIndex)
	{
		if ((Object)(object)_world == (Object)null || tileIndex < 0 || tileIndex >= _world.tiles.Length)
		{
			return null;
		}
		TileData tileData = _world.tiles[tileIndex];
		return new PathTile
		{
			isOcean = tileData.isOcean,
			isCoastal = tileData.isCoastal,
			isMountain = (tileData.elevation01 >= 0.7f && tileData.slopeDegree >= 30f),
			isHills = (tileData.elevation01 >= 0.4f && tileData.slopeDegree >= 15f),
			isForest = (tileData.biome == GameEnums.BiomeType.DeciduousForest || tileData.biome == GameEnums.BiomeType.EvergreenForest || tileData.biome == GameEnums.BiomeType.TropicalRainforest || tileData.biome == GameEnums.BiomeType.BorealForest),
			isSwamp = (tileData.biome == GameEnums.BiomeType.Swamp || tileData.biome == GameEnums.BiomeType.WetMarshPlain),
			isDesert = (tileData.biome == GameEnums.BiomeType.HotDesert || tileData.biome == GameEnums.BiomeType.InlandDesert || tileData.biome == GameEnums.BiomeType.ColdDesert),
			isPlains = (tileData.biome == GameEnums.BiomeType.TemperateGrassland || tileData.biome == GameEnums.BiomeType.Savanna),
			isImpassable = !tileData.passable,
			slope = tileData.slopeDegree,
			elevation = tileData.elevation01 * 5000f
		};
	}

	private int GetTileOwner(int tileIndex)
	{
		if ((Object)(object)_world == (Object)null || tileIndex < 0 || tileIndex >= _world.tiles.Length)
		{
			return -1;
		}
		return _world.tiles[tileIndex].ownerRealmId;
	}

	private RealmData GetRealm(int realmId)
	{
		if ((Object)(object)_world != (Object)null && realmId >= 0 && realmId < _world.realms.Count)
		{
			return _world.realms[realmId];
		}
		return null;
	}

	private bool IsAtWar(int realmA, int realmB)
	{
		return false;
	}

	private List<BuildingData> GetBuildingsAtTile(int tileIndex)
	{
		return new List<BuildingData>();
	}
}
}
