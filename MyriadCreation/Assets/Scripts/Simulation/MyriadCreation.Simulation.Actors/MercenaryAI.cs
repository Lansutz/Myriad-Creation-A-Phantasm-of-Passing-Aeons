using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Simulation.Actors
{
public class MercenaryAI : IMapActorAI
{
	private int _wanderCooldown;

	private int _employerRealmId = -1;

	public void Think(GameWorld world, MapActor actor)
	{
		_wanderCooldown--;
		if (_wanderCooldown > 0 && actor.targetTile >= 0)
		{
			return;
		}
		_wanderCooldown = Random.Range(20, 50);
		if (_employerRealmId >= 0)
		{
			return;
		}
		int num = actor.currentTile % world.mapWidth;
		int num2 = actor.currentTile / world.mapWidth;
		int num3 = -1;
		float num4 = 0f;
		for (int i = -15; i <= 15; i++)
		{
			for (int j = -15; j <= 15; j++)
			{
				int num5 = num + j;
				int num6 = num2 + i;
				if (num5 < 0 || num5 >= world.mapWidth || num6 < 0 || num6 >= world.mapHeight)
				{
					continue;
				}
				int num7 = num6 * world.mapWidth + num5;
				if (num7 >= 0 && num7 < world.tiles.Length)
				{
					TileData tileData = world.tiles[num7];
					if (tileData.exists && tileData.isLand && tileData.development > num4 && tileData.ownerRealmId >= 0)
					{
						num4 = tileData.development;
						num3 = num7;
					}
				}
			}
		}
		if (num3 >= 0)
		{
			actor.SetTarget(num3);
		}
	}

	public void Act(GameWorld world, MapActor actor)
	{
		if (world.tiles[actor.currentTile].development > 0.3f)
		{
			actor.supplies = Mathf.Min(200f, actor.supplies + 2f);
		}
	}

	public void SetEmployer(int realmId)
	{
		_employerRealmId = realmId;
	}
}
}
