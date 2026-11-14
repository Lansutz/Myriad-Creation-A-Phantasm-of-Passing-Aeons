using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
public class Caravan
{
	public int caravanId;

	public int fromRegionId;

	public int toRegionId;

	public int currentNodeIndex;

	[NonSerialized]
	public Dictionary<int, float> cargo = new Dictionary<int, float>();

	public float capacity = 500f;

	public float speed = 1f;

	public bool isMoving = true;

	/// <summary>本次运输方式（陆运/河运/海运）。</summary>
	public MyriadCreation.World.Hydrology.TransportMode transportMode;

	public float moveProgress;

	public float GetCargoWeight(Dictionary<int, GoodsDef> goodsDefs)
	{
		float num = 0f;
		foreach (KeyValuePair<int, float> item in cargo)
		{
			if (goodsDefs.TryGetValue(item.Key, out var value))
			{
				num += item.Value * value.weight;
			}
		}
		return num;
	}

	public bool MoveTick(TradeRoute route, TileData[] tiles)
	{
		if (!isMoving || route.isBlocked)
		{
			return false;
		}
		moveProgress += speed * route.currentEfficiency;
		if (moveProgress >= 1f)
		{
			moveProgress = 0f;
			currentNodeIndex++;
			if (currentNodeIndex >= route.nodeTileIndices.Count)
			{
				isMoving = false;
				return true;
			}
		}
		return false;
	}
}
}
