using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Simulation.Actors
{
public class BanditAI : IMapActorAI
{
	private int _wanderCooldown;

	private int _raidCooldown;

	private bool _hasCamp;

	public void Think(GameWorld world, MapActor actor)
	{
		_wanderCooldown--;
		_raidCooldown--;
		if (_raidCooldown <= 0)
		{
			List<MapActor> list = world.MapActors?.GetActorsNearTile(actor.currentTile, 8);
			if (list != null)
			{
				foreach (MapActor item in list)
				{
					if (item.type == MapActorType.Caravan && !item.isHostile)
					{
						actor.SetTarget(item.currentTile);
						_raidCooldown = Random.Range(15, 30);
						return;
					}
				}
			}
			int num = actor.currentTile % world.mapWidth;
			int num2 = actor.currentTile / world.mapWidth;
			float num3 = Mathf.Clamp(actor.ResolveLeader(world)?.boldness ?? 0f, -100f, 100f);
			for (int i = -6; i <= 6; i++)
			{
				for (int j = -6; j <= 6; j++)
				{
					int num4 = num + j;
					int num5 = num2 + i;
					if (num4 < 0 || num4 >= world.mapWidth || num5 < 0 || num5 >= world.mapHeight)
					{
						continue;
					}
					int num6 = num5 * world.mapWidth + num4;
					if (num6 >= 0 && num6 < world.tiles.Length)
					{
						TileData tileData = world.tiles[num6];
						if (tileData.development > 0.15f && tileData.order < 40f + Mathf.Max(0f, num3) * 0.2f)
						{
							actor.SetTarget(num6);
							_raidCooldown = Random.Range(15, 30);
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
		_wanderCooldown = Random.Range(10, 25);
		int num7 = actor.currentTile % world.mapWidth;
		int num8 = actor.currentTile / world.mapWidth;
		for (int k = 0; k < 10; k++)
		{
			int num9 = Random.Range(-10, 11);
			int num10 = Random.Range(-10, 11);
			int num11 = num7 + num9;
			int num12 = num8 + num10;
			if (num11 < 0 || num11 >= world.mapWidth || num12 < 0 || num12 >= world.mapHeight)
			{
				continue;
			}
			int num13 = num12 * world.mapWidth + num11;
			if (num13 >= 0 && num13 < world.tiles.Length)
			{
				TileData tileData2 = world.tiles[num13];
				if (tileData2.exists && tileData2.isLand && tileData2.order < 35f && tileData2.ownerRealmId < 0)
				{
					actor.SetTarget(num13);
					break;
				}
			}
		}
	}

	public void Act(GameWorld world, MapActor actor)
	{
		TileData tileData = world.tiles[actor.currentTile];
		int tilePopulation = MapActorManager.GetTilePopulation(tileData);
		if (tileData.development > 0.1f && tilePopulation > 0 && _raidCooldown <= 0)
		{
			int num = Mathf.Min(tilePopulation / 8, actor.population / 2);
			MapActorManager.AddPopulationToTile(ref world.tiles[actor.currentTile], -num);
			actor.population += num / 2;
			actor.supplies = Mathf.Min(200f, actor.supplies + 40f);
			tileData.order = Mathf.Max(0f, tileData.order - 5f);
			world.tiles[actor.currentTile] = tileData;
			_raidCooldown = Random.Range(20, 40);
			Debug.Log((object)$"[BanditAI] 土匪#{actor.actorId} 劫掠地块{actor.currentTile}，掳走{num / 2}人");
		}
		if (!_hasCamp && tileData.ownerRealmId < 0 && tileData.order < 30f && actor.population > 50)
		{
			_hasCamp = true;
			actor.attributes["hasCamp"] = 1f;
			Debug.Log((object)$"[BanditAI] 土匪#{actor.actorId} 在地块{actor.currentTile}建立营寨");
		}
	}
}
}
