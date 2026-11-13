using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class LandAbandonmentSystem
{
	public struct AbandonmentRecord
	{
		public int tileIndex;

		public int previousRealmId;

		public AbandonmentType type;

		public int day;

		public int populationLost;
	}

	public static AbandonmentRecord? AbandonTile(GameWorld world, int tileIndex, AbandonmentType type, int realmId)
	{
		if ((Object)(object)world == (Object)null || tileIndex < 0 || tileIndex >= world.tiles.Length)
		{
			return null;
		}
		TileData tileData = world.tiles[tileIndex];
		if (!tileData.exists)
		{
			return null;
		}
		int tilePopulation = MapActorManager.GetTilePopulation(tileData);
		AbandonmentRecord value = new AbandonmentRecord
		{
			tileIndex = tileIndex,
			previousRealmId = tileData.ownerRealmId,
			type = type,
			day = world.currentDay,
			populationLost = tilePopulation
		};
		tileData.ownerRealmId = -1;
		tileData.occupyingRealmId = -1;
		tileData.order = Mathf.Max(0f, tileData.order - type switch
		{
			AbandonmentType.Voluntary => 20f, 
			AbandonmentType.Forced => 30f, 
			AbandonmentType.Natural => 40f, 
			_ => 10f, 
		});
		float num = ((type == AbandonmentType.Nomadic) ? 0.05f : 0.15f);
		tileData.development *= 1f - num;
		HandlePopulationOnAbandon(world, tileIndex, type, tilePopulation);
		if (type == AbandonmentType.Nomadic && tileData.campId >= 0)
		{
			world.Camps?.AbandonCamp(tileData.campId);
			tileData.campId = -1;
		}
		world.tiles[tileIndex] = tileData;
		Debug.Log((object)$"[Abandonment] 地块{tileIndex}被{realmId}弃置（{type}），流失人口{tilePopulation}");
		return value;
	}

	public static List<AbandonmentRecord> AbandonTiles(GameWorld world, IEnumerable<int> tileIndices, AbandonmentType type, int realmId)
	{
		List<AbandonmentRecord> list = new List<AbandonmentRecord>();
		foreach (int tileIndex in tileIndices)
		{
			AbandonmentRecord? abandonmentRecord = AbandonTile(world, tileIndex, type, realmId);
			if (abandonmentRecord.HasValue)
			{
				list.Add(abandonmentRecord.Value);
			}
		}
		return list;
	}

	private static void HandlePopulationOnAbandon(GameWorld world, int tileIndex, AbandonmentType type, int pop)
	{
		if (pop > 0 && world.MapActors != null)
		{
			switch (type)
			{
			case AbandonmentType.Voluntary:
				MigratePopulation(world, tileIndex, (float)pop * 0.7f);
				SpawnRefugees(world, tileIndex, Mathf.RoundToInt((float)pop * 0.2f));
				break;
			case AbandonmentType.Forced:
				SpawnRefugees(world, tileIndex, Mathf.RoundToInt((float)pop * 0.5f));
				break;
			case AbandonmentType.Natural:
				SpawnRefugees(world, tileIndex, Mathf.RoundToInt((float)pop * 0.8f));
				break;
			case AbandonmentType.Nomadic:
				MapActorManager.AddPopulationToTile(ref world.tiles[tileIndex], -pop);
				break;
			}
		}
	}

	private static void MigratePopulation(GameWorld world, int fromTile, float amount)
	{
		int num = Mathf.RoundToInt(amount);
		if (num <= 0)
		{
			return;
		}
		_ = world.tiles[fromTile];
		int mapWidth = world.mapWidth;
		int num2 = fromTile % mapWidth;
		int num3 = fromTile / mapWidth;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (j == 0 && i == 0)
				{
					continue;
				}
				int num4 = num2 + j;
				int num5 = num3 + i;
				if (num4 < 0 || num4 >= mapWidth || num5 < 0 || num5 >= world.mapHeight)
				{
					continue;
				}
				int num6 = num5 * mapWidth + num4;
				if (num6 >= 0 && num6 < world.tiles.Length)
				{
					TileData tileData = world.tiles[num6];
					if (tileData.exists && tileData.isLand)
					{
						MapActorManager.AddPopulationToTile(ref world.tiles[num6], num / 4);
					}
				}
			}
		}
		MapActorManager.AddPopulationToTile(ref world.tiles[fromTile], -num);
	}

	private static void SpawnRefugees(GameWorld world, int tileIndex, int count)
	{
		if (count >= 5)
		{
			MapActor mapActor = world.MapActors.SpawnActor(MapActorType.Refugee, tileIndex, count, new RefugeeAI());
			mapActor.actorName = "弃地流民#" + mapActor.actorId;
			mapActor.morale = 25f;
			MapActorManager.AddPopulationToTile(ref world.tiles[tileIndex], -count);
		}
	}

	public static bool ResettleTile(GameWorld world, int tileIndex, int newRealmId, int settlerCount)
	{
		if ((Object)(object)world == (Object)null || tileIndex < 0 || tileIndex >= world.tiles.Length)
		{
			return false;
		}
		TileData tileData = world.tiles[tileIndex];
		if (!tileData.exists || !tileData.isLand)
		{
			return false;
		}
		if (tileData.ownerRealmId >= 0 && tileData.ownerRealmId != newRealmId)
		{
			return false;
		}
		tileData.ownerRealmId = newRealmId;
		tileData.order = Mathf.Max(tileData.order, 20f);
		MapActorManager.AddPopulationToTile(ref world.tiles[tileIndex], settlerCount);
		world.tiles[tileIndex] = tileData;
		Debug.Log((object)$"[Abandonment] 政权{newRealmId}重新定居地块{tileIndex}，迁入{settlerCount}人");
		return true;
	}

	public static void DailyCheckBanditSpawn(GameWorld world)
	{
		if ((Object)(object)world == (Object)null || world.MapActors == null || Random.value > 0.02f)
		{
			return;
		}
		for (int i = 0; i < 5; i++)
		{
			int num = Random.Range(0, world.tiles.Length);
			TileData tileData = world.tiles[num];
			if (tileData.exists && tileData.isLand && tileData.ownerRealmId < 0 && !(tileData.order > 25f) && !(tileData.development < 0.05f))
			{
				MapActor mapActor = world.MapActors.SpawnActor(MapActorType.Bandit, num, Random.Range(15, 80), new BanditAI());
				mapActor.actorName = "土匪#" + mapActor.actorId;
				mapActor.isHostile = true;
				break;
			}
		}
	}
}
}
