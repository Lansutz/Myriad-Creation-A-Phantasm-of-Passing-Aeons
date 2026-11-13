using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;
using UnityEngine;
using Random = System.Random;

namespace MyriadCreation.Simulation.Systems
{
public sealed class SandboxSpawnSystem
{
	private readonly GameWorld _world;

	private readonly Random _rng;

	private int _lastPopulationSeedDay = -9999;

	private int _lastCampSeedDay = -9999;

	public int PopulationSeedIntervalDays = 30;

	public int CampSeedIntervalDays = 90;

	public int MaxCamps = 80;

	public SandboxSpawnSystem(GameWorld world, int seed = 20261030)
	{
		_world = world ?? throw new ArgumentNullException("world");
		_rng = new Random(seed);
	}

	public void MonthlyTick()
	{
		SandboxSpawnRates sandboxSpawnRates = _world.SetupOptions?.spawnRates;
		if (sandboxSpawnRates != null && sandboxSpawnRates.enableNaturalSpawning)
		{
			if (sandboxSpawnRates.spreadPopulation && _world.currentDay - _lastPopulationSeedDay >= Math.Max(1, PopulationSeedIntervalDays))
			{
				_lastPopulationSeedDay = _world.currentDay;
				TrySeedPopulation();
			}
			if (sandboxSpawnRates.spawnSettlements && _world.currentDay - _lastCampSeedDay >= Math.Max(1, CampSeedIntervalDays) && _world.Camps != null && _world.Camps.Count < MaxCamps)
			{
				_lastCampSeedDay = _world.currentDay;
				TrySeedCamp();
			}
		}
	}

	/// <summary>
	/// 人口迁移：从人口超过承载力 1.1 倍的源地块，向邻近可承载地块移出一小群。
	/// 没有过载源/合适目标就不新增人口——不再“随机地块凭空造人”（D16）。
	/// 返回是否真的发生迁移；供测试与长跑断言。
	/// </summary>
	public bool TrySeedPopulation()
	{
		int start = _rng.Next(_world.tiles.Length);
		for (int attempt = 0; attempt < _world.tiles.Length; attempt++)
		{
			int sourceIndex = (start + attempt) % _world.tiles.Length;
			if (!IsSuitableLand(sourceIndex)) continue;
			ref TileData source = ref _world.tiles[sourceIndex];
			if (source.populationBlocks == null || source.populationBlocks.Count == 0) continue;
			float population = PopulationLedger.Total(source);
			float capacity = CarryingCapacitySystem.CalculateCarryingCapacity(source, _world.tradeCenters);
			if (population < 10f || capacity <= 0f || population <= capacity * 1.1f) continue;

			int targetIndex = FindMigrationTarget(sourceIndex);
			if (targetIndex < 0) continue;

			// 只迁出“超出承载的那部分”，每次最多 2% 人口；旧口径每次搬走 10% 全城人口，
			// 一个过载城市会在几个月内被搬空（P7 诊断：1104 人的城 50 年后只剩 397）。
			float excess = population - capacity * 1.1f;
			float move = Mathf.Min(population * 0.02f, excess);
			if (move < 1f) continue;
			PopulationBlock template = source.populationBlocks[0];
			float remaining = move;
			for (int i = source.populationBlocks.Count - 1; i >= 0 && remaining > 0f; i--)
			{
				float take = Mathf.Min(source.populationBlocks[i].count, remaining);
				PopulationLedger.RemoveFromIndex(source.populationBlocks, i, take);
				remaining -= take;
				if (source.populationBlocks[i].count <= 0.01f) source.populationBlocks.RemoveAt(i);
			}
			_world.tiles[sourceIndex] = source;

			ref TileData target = ref _world.tiles[targetIndex];
			target.populationBlocks ??= new List<PopulationBlock>();
			PopulationLedger.Add(target.populationBlocks, new PopulationBlock
			{
				raceId = template.raceId,
				cultureId = template.cultureId,
				faithId = template.faithId,
				count = move,
				satisfaction = Mathf.Clamp(template.satisfaction, 35f, 75f),
				socialClass = template.socialClass
			});
			_world.tiles[targetIndex] = target;
			return true;
		}
		return false;
	}

	private int FindMigrationTarget(int sourceIndex)
	{
		int width = Mathf.Max(1, _world.mapWidth);
		int height = Mathf.Max(1, _world.mapHeight);
		int sx = sourceIndex % width;
		int sy = sourceIndex / width;
		for (int dy = -2; dy <= 2; dy++)
		{
			for (int dx = -2; dx <= 2; dx++)
			{
				if (dx == 0 && dy == 0) continue;
				int x = sx + dx;
				int y = sy + dy;
				if (x < 0 || x >= width || y < 0 || y >= height) continue;
				int index = y * width + x;
				if (!IsSuitableLand(index)) continue;
				if (HasSettlementAt(index)) continue;
				TileData candidate = _world.tiles[index];
				float population = PopulationLedger.Total(candidate);
				float capacity = CarryingCapacitySystem.CalculateCarryingCapacity(candidate, _world.tradeCenters);
				if (capacity > population * 1.5f) return index;
			}
		}
		return -1;
	}

	private bool HasSettlementAt(int tileIndex)
	{
		if (_world.settlements == null) return false;
		foreach (SettlementData settlement in _world.settlements.Values)
		{
			if (settlement != null && settlement.tileIndex == tileIndex) return true;
		}
		return false;
	}

	private void TrySeedCamp()
	{
		for (int i = 0; i < 32; i++)
		{
			int num = _rng.Next(_world.tiles.Length);
			if (!IsSuitableLand(num) || _world.Camps.GetCampsAtTile(num).Count > 0)
			{
				continue;
			}
			if (_world.settlements != null)
			{
				bool flag = false;
				foreach (SettlementData value in _world.settlements.Values)
				{
					if (value.tileIndex == num)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					continue;
				}
			}
			if (_world.Camps.EstablishCamp(CampType.Temporary, num, 20 + _rng.Next(0, 60), -1, -1, "自发营地") != null)
			{
				break;
			}
		}
	}

	private bool IsSuitableLand(int tileIndex)
	{
		if (tileIndex < 0 || tileIndex >= _world.tiles.Length)
		{
			return false;
		}
		ref TileData reference = ref _world.tiles[tileIndex];
		if (!reference.exists || !reference.isLand)
		{
			return false;
		}
		if (reference.biome == GameEnums.BiomeType.IceSheet || reference.biome == GameEnums.BiomeType.Tundra || reference.biome == GameEnums.BiomeType.HotDesert || reference.biome == GameEnums.BiomeType.ColdDesert)
		{
			return false;
		}
		if (reference.elevation01 > 0.85f)
		{
			return false;
		}
		if (reference.fertility < 0.15f && reference.waterAdjacentWeight <= 0f)
		{
			return false;
		}
		return true;
	}
}
}
