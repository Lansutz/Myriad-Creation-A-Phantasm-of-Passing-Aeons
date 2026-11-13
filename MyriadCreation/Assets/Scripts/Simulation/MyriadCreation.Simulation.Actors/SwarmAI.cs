using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Simulation.Actors
{
public class SwarmAI : IMapActorAI
{
	private int _lifespanDays;

	private int _spreadCooldown;

	/// <summary>上一次造成损害的地块；蝗群只在“落到新地块”时啃食一次，而不是每天原地扣一遍。</summary>
	private int _lastDamagedTile = -1;

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
					if (tileData.exists && tileData.isLand && tileData.fertility > 0.3f)
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
		// 蝗群每天都会调用 Act：只在到达新地块时造成一次损害，否则一个蝗群 120 天就能把沿途肥力清零。
		if (actor.currentTile == _lastDamagedTile) return;
		_lastDamagedTile = actor.currentTile;
		TileData tileData = world.tiles[actor.currentTile];
		// 蝗群啃的是植被与当季作物，不是土壤本身；肥力只轻微受损，休耕可恢复。
		tileData.vegetation = Mathf.Max(0f, tileData.vegetation - 0.25f);
		tileData.fertility = Mathf.Max(0f, tileData.fertility - 0.01f);
		tileData.development = Mathf.Max(0f, tileData.development - 0.005f);
		world.tiles[actor.currentTile] = tileData;
	}
}
}
