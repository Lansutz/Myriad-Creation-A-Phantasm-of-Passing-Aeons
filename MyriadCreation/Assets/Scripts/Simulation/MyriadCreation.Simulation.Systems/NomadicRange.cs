using System;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
[Serializable]
public class NomadicRange
{
	public int centerTile;

	public int summerCampTile = -1;

	public int winterCampTile = -1;

	public float coreRadius = 5f;

	public float outerRadius = 12f;

	public float influence = 50f;

	public bool IsInRange(int tileIndex, int mapWidth, int mapHeight)
	{
		return GetDistanceToCenter(tileIndex, mapWidth, mapHeight) <= outerRadius;
	}

	public bool IsInCore(int tileIndex, int mapWidth, int mapHeight)
	{
		return GetDistanceToCenter(tileIndex, mapWidth, mapHeight) <= coreRadius;
	}

	public float GetInfluenceAt(int tileIndex, int mapWidth, int mapHeight)
	{
		float distanceToCenter = GetDistanceToCenter(tileIndex, mapWidth, mapHeight);
		if (distanceToCenter > outerRadius)
		{
			return 0f;
		}
		if (distanceToCenter <= coreRadius)
		{
			return influence / 100f;
		}
		float num = 1f - (distanceToCenter - coreRadius) / (outerRadius - coreRadius);
		return influence / 100f * num;
	}

	private float GetDistanceToCenter(int tileIndex, int mapWidth, int mapHeight)
	{
		int num = centerTile % mapWidth;
		int num2 = centerTile / mapWidth;
		int num3 = tileIndex % mapWidth;
		int num4 = tileIndex / mapWidth;
		int num5 = Mathf.Abs(num3 - num);
		num5 = Mathf.Min(num5, mapWidth - num5);
		int num6 = Mathf.Abs(num4 - num2);
		return num5 + num6;
	}
}
}
