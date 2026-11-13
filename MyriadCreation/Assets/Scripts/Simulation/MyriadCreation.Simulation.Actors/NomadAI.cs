using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Simulation.Actors
{
public class NomadAI : IMapActorAI
{
	private int _wanderCooldown;

	public void Think(GameWorld world, MapActor actor)
	{
		_wanderCooldown--;
		if (_wanderCooldown > 0 && actor.targetTile >= 0)
		{
			return;
		}
		_wanderCooldown = Random.Range(15, 45);
		int num = actor.currentTile % world.mapWidth;
		int num2 = actor.currentTile / world.mapWidth;
		int num3 = Random.Range(5, 15);
		for (int i = 0; i < 10; i++)
		{
			int num4 = Random.Range(-num3, num3 + 1);
			int num5 = Random.Range(-num3, num3 + 1);
			int num6 = num + num4;
			int num7 = num2 + num5;
			if (num6 < 0 || num6 >= world.mapWidth || num7 < 0 || num7 >= world.mapHeight)
			{
				continue;
			}
			int num8 = num7 * world.mapWidth + num6;
			if (num8 >= 0 && num8 < world.tiles.Length)
			{
				TileData tileData = world.tiles[num8];
				if (tileData.exists && tileData.isLand && (tileData.biome == GameEnums.BiomeType.Savanna || tileData.biome == GameEnums.BiomeType.TemperateGrassland || tileData.biome == GameEnums.BiomeType.TemperateGrassland || tileData.fertility > 30f))
				{
					actor.SetTarget(num8);
					break;
				}
			}
		}
	}

	public void Act(GameWorld world, MapActor actor)
	{
		if (world.tiles[actor.currentTile].fertility > 40f)
		{
			actor.supplies = Mathf.Min(200f, actor.supplies + 5f);
			actor.health = Mathf.Min(100f, actor.health + 1f);
		}
	}
}
}
