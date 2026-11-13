using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Simulation.Actors
{
public class RefugeeAI : IMapActorAI
{
	private int _retargetCooldown;

	private const int RetargetInterval = 30;

	public void Think(GameWorld world, MapActor actor)
	{
		_retargetCooldown--;
		if (_retargetCooldown > 0 && actor.targetTile >= 0)
		{
			return;
		}
		_retargetCooldown = 30;
		int num = -1;
		float num2 = float.MinValue;
		int num3 = 20;
		int num4 = actor.currentTile % world.mapWidth;
		int num5 = actor.currentTile / world.mapWidth;
		float num6 = Mathf.Clamp(actor.ResolveLeader(world)?.compassion ?? 0f, -100f, 100f);
		float num7 = 0.5f + Mathf.Max(0f, num6) / 200f;
		float num8 = 30f - Mathf.Max(0f, num6) * 0.1f;
		for (int i = -num3; i <= num3; i++)
		{
			for (int j = -num3; j <= num3; j++)
			{
				int num9 = num4 + j;
				int num10 = num5 + i;
				if (num9 < 0 || num9 >= world.mapWidth || num10 < 0 || num10 >= world.mapHeight)
				{
					continue;
				}
				int num11 = num10 * world.mapWidth + num9;
				if (num11 < 0 || num11 >= world.tiles.Length)
				{
					continue;
				}
				TileData tileData = world.tiles[num11];
				if (tileData.exists && tileData.isLand)
				{
					float num12 = Mathf.Abs(j) + Mathf.Abs(i);
					float num13 = tileData.order * num7 + tileData.development * num8 - num12 * 2f;
					if (tileData.ownerRealmId >= 0)
					{
						num13 += 10f;
					}
					if (num13 > num2)
					{
						num2 = num13;
						num = num11;
					}
				}
			}
		}
		if (num >= 0)
		{
			actor.SetTarget(num);
		}
	}

	public void Act(GameWorld world, MapActor actor)
	{
		if (actor.currentTile == actor.targetTile)
		{
			TileData tileData = world.tiles[actor.currentTile];
			if (tileData.order > 50f && tileData.development > 0.3f)
			{
				MapActorManager.AddPopulationToTile(ref world.tiles[actor.currentTile], actor.population);
				Debug.Log((object)$"[RefugeeAI] 流民#{actor.actorId} 在地块{actor.currentTile}被接纳，{actor.population}人融入当地");
				actor.population = 0;
			}
			else
			{
				_retargetCooldown = 0;
			}
		}
	}
}
}
