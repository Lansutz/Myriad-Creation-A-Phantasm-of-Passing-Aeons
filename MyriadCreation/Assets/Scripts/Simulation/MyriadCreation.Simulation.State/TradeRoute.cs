using System.Collections.Generic;
using MyriadCreation.Core.Data;
using UnityEngine;

namespace MyriadCreation.Simulation.State
{
public class TradeRoute
{
	public int fromRegionId;

	public int toRegionId;

	public List<int> nodeTileIndices = new List<int>();

	public float baseEfficiency = 1f;

	public float currentEfficiency = 1f;

	public bool isBlocked;

	public void CalculateEfficiency(TileData[] tiles)
	{
		if (isBlocked || nodeTileIndices.Count == 0)
		{
			currentEfficiency = 0f;
			return;
		}
		float num = baseEfficiency;
		float num2 = 0f;
		foreach (int nodeTileIndex in nodeTileIndices)
		{
			num2 += (float)tiles[nodeTileIndex].roadLevel;
		}
		num2 /= (float)nodeTileIndices.Count;
		num *= 0.6f + num2 * 0.15f;
		float num3 = 0f;
		foreach (int nodeTileIndex2 in nodeTileIndices)
		{
			num3 += tiles[nodeTileIndex2].stability;
		}
		num3 /= (float)nodeTileIndices.Count;
		num *= 0.5f + num3 / 200f;
		num *= Mathf.Exp((float)(-nodeTileIndices.Count) * 0.02f);
		currentEfficiency = Mathf.Clamp(num, 0f, 1.5f);
	}
}
}
