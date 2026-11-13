using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Simulation.Actors
{
public class MapActorManager
{
	private readonly GameWorld _world;

	private readonly List<MapActor> _actors = new List<MapActor>();

	private readonly Dictionary<int, MapActor> _actorById = new Dictionary<int, MapActor>();

	private int _nextActorId = 1;

	public float RefugeeSpawnChance = 0.1f;

	public float NomadSpawnChance = 0.05f;

	public float CaravanSpawnChance = 0.2f;

	public float WildlifeSpawnChance = 0.04f;

	public float BanditSpawnChance = 0.04f;

	public int MaxActors = 500;

	public bool NaturalSpawning = true;

	public float SpawnRateMultiplier = 1f;

	public IReadOnlyList<MapActor> AllActors => _actors;

	public int Count => _actors.Count;

	public MapActorManager(GameWorld world)
	{
		_world = world;
	}

	public Dictionary<MapActorType, int> CountByType()
	{
		Dictionary<MapActorType, int> dictionary = new Dictionary<MapActorType, int>();
		foreach (MapActor actor in _actors)
		{
			if (!dictionary.ContainsKey(actor.type))
			{
				dictionary[actor.type] = 0;
			}
			dictionary[actor.type]++;
		}
		return dictionary;
	}

	public MapActor GetActor(int actorId)
	{
		_actorById.TryGetValue(actorId, out var value);
		return value;
	}

	public List<MapActor> GetActorsAtTile(int tileIndex)
	{
		List<MapActor> list = new List<MapActor>();
		foreach (MapActor actor in _actors)
		{
			if (actor.currentTile == tileIndex)
			{
				list.Add(actor);
			}
		}
		return list;
	}

	public List<MapActor> GetActorsNearTile(int tileIndex, int radius)
	{
		List<MapActor> list = new List<MapActor>();
		int mapWidth = _world.mapWidth;
		_ = _world.mapHeight;
		int num = tileIndex % mapWidth;
		int num2 = tileIndex / mapWidth;
		foreach (MapActor actor in _actors)
		{
			int num3 = actor.currentTile % mapWidth;
			int num4 = actor.currentTile / mapWidth;
			if (Mathf.Abs(num3 - num) + Mathf.Abs(num4 - num2) <= radius)
			{
				list.Add(actor);
			}
		}
		return list;
	}

	public void Tick(float deltaDays)
	{
		if (_actors.Count < MaxActors)
		{
			TrySpawnActors();
		}
		for (int num = _actors.Count - 1; num >= 0; num--)
		{
			MapActor mapActor = _actors[num];
			mapActor.Tick(_world, deltaDays);
			if (!mapActor.IsAlive)
			{
				_actors.RemoveAt(num);
				_actorById.Remove(mapActor.actorId);
				MapUnitRegistry.RemoveBySource(UnitSourceKind.MapActor, mapActor.actorId);
			}
		}
	}

	private void TrySpawnActors()
	{
		if (NaturalSpawning)
		{
			float num = Mathf.Clamp(SpawnRateMultiplier, 0f, 3f);
			if (Random.value < RefugeeSpawnChance * num)
			{
				TrySpawnRefugee();
			}
			if (Random.value < NomadSpawnChance * num)
			{
				TrySpawnNomad();
			}
			if (Random.value < CaravanSpawnChance * num)
			{
				TrySpawnCaravan();
			}
			if (Random.value < WildlifeSpawnChance * num)
			{
				TrySpawnWildlife();
			}
			if (Random.value < BanditSpawnChance * num)
			{
				TrySpawnBandit();
			}
		}
	}

	private void TrySpawnRefugee()
	{
		for (int i = 0; i < 20; i++)
		{
			int num = Random.Range(0, _world.tiles.Length);
			TileData tileData = _world.tiles[num];
			if (tileData.exists && tileData.isLand && (!(tileData.order > 30f) || !(tileData.development > 0.2f)))
			{
				MapActor mapActor = CreateActor(MapActorType.Refugee, num, Random.Range(50, 500), UnitSubcategory.Refugee);
				mapActor.actorName = "流民队伍#" + mapActor.actorId;
				mapActor.AI = new RefugeeAI();
				mapActor.morale = 40f;
				break;
			}
		}
	}

	private void TrySpawnNomad()
	{
		for (int i = 0; i < 20; i++)
		{
			int num = Random.Range(0, _world.tiles.Length);
			TileData tileData = _world.tiles[num];
			if (tileData.exists && tileData.isLand && (tileData.biome == GameEnums.BiomeType.Savanna || tileData.biome == GameEnums.BiomeType.TemperateGrassland || tileData.biome == GameEnums.BiomeType.TemperateGrassland))
			{
				MapActor mapActor = CreateActor(MapActorType.Nomad, num, Random.Range(100, 1000), UnitSubcategory.Nomad);
				mapActor.actorName = "游牧部落#" + mapActor.actorId;
				mapActor.AI = new NomadAI();
				mapActor.moveSpeed = 1.5f;
				break;
			}
		}
	}

	private void TrySpawnCaravan()
	{
		int num = -1;
		int num2 = -1;
		for (int i = 0; i < 30; i++)
		{
			int num3 = Random.Range(0, _world.tiles.Length);
			TileData tileData = _world.tiles[num3];
			if (tileData.exists && tileData.isLand && !(tileData.development < 0.2f))
			{
				if (num >= 0)
				{
					num2 = num3;
					break;
				}
				num = num3;
			}
		}
		if (num >= 0 && num2 >= 0 && num != num2)
		{
			MapActor mapActor = CreateActor(MapActorType.Caravan, num, Random.Range(10, 50), UnitSubcategory.TradeCaravan);
			mapActor.actorName = "商队#" + mapActor.actorId;
			mapActor.AI = new CaravanAI(num, num2);
			mapActor.moveSpeed = 1.2f;
			mapActor.supplies = 200f;
		}
	}

	private void TrySpawnWildlife()
	{
		for (int i = 0; i < 20; i++)
		{
			int num = Random.Range(0, _world.tiles.Length);
			TileData tileData = _world.tiles[num];
			if (!tileData.exists || !tileData.isLand)
			{
				continue;
			}
			if (Random.value < 0.55f)
			{
				if (tileData.biome == GameEnums.BiomeType.DeciduousForest || tileData.biome == GameEnums.BiomeType.EvergreenForest || tileData.biome == GameEnums.BiomeType.TropicalRainforest || !(tileData.elevation01 < 0.5f))
				{
					MapActor mapActor = CreateActor(MapActorType.Wildlife, num, Random.Range(5, 50), UnitSubcategory.Predator);
					mapActor.actorName = "掠食兽群#" + mapActor.actorId;
					mapActor.AI = new PredatorAI();
					mapActor.isHostile = true;
					break;
				}
			}
			else if (!(tileData.fertility < 40f))
			{
				bool flag = Random.value < 0.55f;
				MapActor mapActor2 = CreateActor(MapActorType.Wildlife, num, Random.Range(100, 1000), flag ? UnitSubcategory.LocustSwarm : UnitSubcategory.RodentPlague);
				mapActor2.actorName = (flag ? ("蝗群#" + mapActor2.actorId) : ("鼠患#" + mapActor2.actorId));
				mapActor2.AI = new SwarmAI(Random.Range(60, 180));
				mapActor2.moveSpeed = 2f;
				break;
			}
		}
	}

	private void TrySpawnBandit()
	{
		for (int i = 0; i < 20; i++)
		{
			int num = Random.Range(0, _world.tiles.Length);
			TileData tileData = _world.tiles[num];
			if (tileData.exists && tileData.isLand && !(tileData.order > 35f) && (tileData.ownerRealmId < 0 || !(tileData.order > 20f)))
			{
				MapActor mapActor = CreateActor(MapActorType.Bandit, num, Random.Range(20, 200), UnitSubcategory.Bandit);
				mapActor.actorName = "土匪#" + mapActor.actorId;
				mapActor.AI = new BanditAI();
				mapActor.isHostile = true;
				mapActor.moveSpeed = 1.3f;
				break;
			}
		}
	}

	private MapActor CreateActor(MapActorType type, int tileIndex, int population, UnitSubcategory subcategory = UnitSubcategory.None)
	{
		MapActor mapActor = new MapActor
		{
			actorId = _nextActorId++,
			type = type,
			category = MapUnit.CategoryForActor(type),
			subcategory = subcategory,
			currentTile = tileIndex,
			population = population,
			transport = new TransportUnit
			{
				type = MapUnit.MapActorTypeToUnit(type),
				subcategory = subcategory
			}
		};
		_actors.Add(mapActor);
		_actorById[mapActor.actorId] = mapActor;
		MapUnitRegistry.RegisterActor(mapActor);
		return mapActor;
	}

	public MapActor SpawnActor(MapActorType type, int tileIndex, int population, IMapActorAI ai = null, UnitSubcategory subcategory = UnitSubcategory.None)
	{
		MapActor mapActor = CreateActor(type, tileIndex, population, subcategory);
		mapActor.AI = ai ?? UnitAIFactory.Create(subcategory, mapActor.targetTile);
		return mapActor;
	}

	public void RemoveActor(int actorId)
	{
		if (_actorById.TryGetValue(actorId, out var value))
		{
			_actors.Remove(value);
			_actorById.Remove(actorId);
		}
	}

	public static int GetTilePopulation(TileData tile)
	{
		if (tile.populationBlocks == null)
		{
			return 0;
		}
		int num = 0;
		foreach (PopulationBlock populationBlock in tile.populationBlocks)
		{
			num += Mathf.RoundToInt(populationBlock.count);
		}
		return num;
	}

	public static void AddPopulationToTile(ref TileData tile, int amount)
	{
		if (amount == 0)
		{
			return;
		}
		if (tile.populationBlocks == null)
		{
			tile.populationBlocks = new List<PopulationBlock>();
		}
		if (amount > 0)
		{
			if (tile.populationBlocks.Count > 0)
			{
				PopulationBlock value = tile.populationBlocks[0];
				value.count += amount;
				tile.populationBlocks[0] = value;
			}
			else
			{
				tile.populationBlocks.Add(new PopulationBlock
				{
					count = amount
				});
			}
			return;
		}
		int num = -amount;
		int num2 = tile.populationBlocks.Count - 1;
		while (num2 >= 0 && num > 0)
		{
			PopulationBlock value2 = tile.populationBlocks[num2];
			if (value2.count <= (float)num)
			{
				num -= Mathf.RoundToInt(value2.count);
				tile.populationBlocks.RemoveAt(num2);
			}
			else
			{
				value2.count -= num;
				tile.populationBlocks[num2] = value2;
				num = 0;
			}
			num2--;
		}
	}

	public void Clear()
	{
		_actors.Clear();
		_actorById.Clear();
		_nextActorId = 1;
	}
}
}
