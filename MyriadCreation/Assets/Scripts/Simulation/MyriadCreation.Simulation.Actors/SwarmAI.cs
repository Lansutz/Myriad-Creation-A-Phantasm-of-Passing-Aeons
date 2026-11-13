using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Simulation.Actors
{
public class SwarmAI : IMapActorAI
{
	private int _lifespanDays;

	private int _spreadCooldown;

	public SwarmAI(int lifespanDays = 120)
	{
		_lifespanDays = lifespanDays;
	}

	public void Think(GameWorld world, MapActor actor)
	{
		_lifespanDays--;
		_spreadCooldown--;
		if (_lifespanDays <= 0)
		{
			actor.population = 0;
		}
		else
		{
			if (_spreadCooldown > 0 && actor.targetTile >= 0)
			{
				return;
			}
			_spreadCooldown = Random.Range(5, 15);
			int num = actor.currentTile % world.mapWidth;
			int num2 = actor.currentTile / world.mapWidth;
			for (int i = 0; i < 8; i++)
			{
				int num3 = Random.Range(-4, 5);
				int num4 = Random.Range(-4, 5);
				int num5 = num + num3;
				int num6 = num2 + num4;
				if (num5 < 0 || num5 >= world.mapWidth || num6 < 0 || num6 >= world.mapHeight)
				{
					continue;
				}
				int num7 = num6 * world.mapWidth + num5;
				if (num7 >= 0 && num7 < world.tiles.Length)
				{
					TileData tileData = world.tiles[num7];
					if (tileData.exists && tileData.isLand && tileData.fertility > 30f)
					{
						actor.SetTarget(num7);
						break;
					}
				}
			}
		}
	}

	public void Act(GameWorld world, MapActor actor)
	{
		TileData tileData = world.tiles[actor.currentTile];
		tileData.fertility = Mathf.Max(0f, tileData.fertility - 2f);
		tileData.development = Mathf.Max(0f, tileData.development - 0.005f);
		world.tiles[actor.currentTile] = tileData;
	}
}
}
