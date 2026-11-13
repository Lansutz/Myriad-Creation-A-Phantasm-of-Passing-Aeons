using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;

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

	private void TrySeedPopulation()
	{
		for (int i = 0; i < 24; i++)
		{
			int num = _rng.Next(_world.tiles.Length);
			if (!IsSuitableLand(num))
			{
				continue;
			}
			ref TileData reference = ref _world.tiles[num];
			if (reference.populationBlocks == null || reference.populationBlocks.Count <= 0)
			{
				ref List<PopulationBlock> populationBlocks = ref reference.populationBlocks;
				if (populationBlocks == null)
				{
					populationBlocks = new List<PopulationBlock>();
				}
				reference.populationBlocks.Add(new PopulationBlock
				{
					raceId = 0,
					cultureId = 0,
					count = 4f + (float)_rng.Next(0, 12),
					satisfaction = 55f,
					socialClass = GameEnums.SocialClass.Peasant
				});
				break;
			}
		}
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
