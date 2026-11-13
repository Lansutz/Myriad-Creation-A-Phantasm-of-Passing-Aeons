using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Simulation.Actors
{
public class PredatorAI : IMapActorAI
{
	private int _wanderCooldown;

	private int _attackCooldown;

	public void Think(GameWorld world, MapActor actor)
	{
		_wanderCooldown--;
		_attackCooldown--;
		if (_attackCooldown <= 0)
		{
			int num = actor.currentTile % world.mapWidth;
			int num2 = actor.currentTile / world.mapWidth;
			for (int i = -5; i <= 5; i++)
			{
				for (int j = -5; j <= 5; j++)
				{
					int num3 = num + j;
					int num4 = num2 + i;
					if (num3 < 0 || num3 >= world.mapWidth || num4 < 0 || num4 >= world.mapHeight)
					{
						continue;
					}
					int num5 = num4 * world.mapWidth + num3;
					if (num5 >= 0 && num5 < world.tiles.Length)
					{
						TileData tileData = world.tiles[num5];
						if (tileData.development > 0.2f && tileData.order < 50f)
						{
							actor.SetTarget(num5);
							_attackCooldown = Random.Range(20, 50);
							return;
						}
					}
				}
			}
		}
		if (_wanderCooldown > 0 && actor.targetTile >= 0)
		{
			return;
		}
		_wanderCooldown = Random.Range(10, 30);
		int num6 = actor.currentTile % world.mapWidth;
		int num7 = actor.currentTile / world.mapWidth;
		for (int k = 0; k < 10; k++)
		{
			int num8 = Random.Range(-8, 9);
			int num9 = Random.Range(-8, 9);
			int num10 = num6 + num8;
			int num11 = num7 + num9;
			if (num10 < 0 || num10 >= world.mapWidth || num11 < 0 || num11 >= world.mapHeight)
			{
				continue;
			}
			int num12 = num11 * world.mapWidth + num10;
			if (num12 >= 0 && num12 < world.tiles.Length)
			{
				TileData tileData2 = world.tiles[num12];
				if (tileData2.exists && tileData2.isLand && tileData2.order < 40f)
				{
					actor.SetTarget(num12);
					break;
				}
			}
		}
	}

	public void Act(GameWorld world, MapActor actor)
	{
		TileData tileData = world.tiles[actor.currentTile];
		int tilePopulation = MapActorManager.GetTilePopulation(tileData);
		if (tileData.development > 0.2f && tilePopulation > 0 && _attackCooldown <= 0)
		{
			int num = Mathf.Min(tilePopulation / 10, actor.population);
			MapActorManager.AddPopulationToTile(ref world.tiles[actor.currentTile], -num);
			tileData.development = Mathf.Max(0f, tileData.development - 0.02f);
			world.tiles[actor.currentTile] = tileData;
			actor.supplies = Mathf.Min(200f, actor.supplies + 30f);
			_attackCooldown = Random.Range(30, 60);
			Debug.Log((object)$"[PredatorAI] 野兽群#{actor.actorId} 袭击地块{actor.currentTile}，伤亡{num}人");
		}
	}
}
}
