using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using UnityEngine;

namespace MyriadCreation.World.Hydrology
{
public static class OceanZoning
{
	public enum OceanZone
	{
		Land,
		Coast,
		Neritic,
		Oceanic,
		DeepSea
	}

	public const int CoastMaxDist = 1;

	public const int NeriticMaxDist = 4;

	public const int OceanicMaxDist = 8;

	public static int[] CalculateDistanceField(TileData[] tiles, int width, int height, bool wrapX)
	{
		int[] array = new int[tiles.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = -1;
		}
		Queue<int> queue = new Queue<int>();
		for (int j = 0; j < tiles.Length; j++)
		{
			if (tiles[j].isLand || !tiles[j].exists)
			{
				continue;
			}
			foreach (int neighbour in TileGrid.GetNeighbours(j, width, height, wrapX))
			{
				if (neighbour >= 0 && tiles[neighbour].isLand)
				{
					array[j] = 0;
					queue.Enqueue(j);
					break;
				}
			}
		}
		while (queue.Count > 0)
		{
			int num = queue.Dequeue();
			int num2 = array[num];
			foreach (int neighbour2 in TileGrid.GetNeighbours(num, width, height, wrapX))
			{
				if (neighbour2 >= 0 && !tiles[neighbour2].isLand && tiles[neighbour2].exists && array[neighbour2] < 0)
				{
					array[neighbour2] = num2 + 1;
					queue.Enqueue(neighbour2);
				}
			}
		}
		return array;
	}

	public static OceanZone GetZone(int distance)
	{
		if (distance < 0)
		{
			return OceanZone.DeepSea;
		}
		if (distance <= 1)
		{
			return OceanZone.Coast;
		}
		if (distance <= 4)
		{
			return OceanZone.Neritic;
		}
		if (distance <= 8)
		{
			return OceanZone.Oceanic;
		}
		return OceanZone.DeepSea;
	}

	public static string GetZoneName(OceanZone zone)
	{
		return zone switch
		{
			OceanZone.Land => "陆地", 
			OceanZone.Coast => "近海", 
			OceanZone.Neritic => "中海", 
			OceanZone.Oceanic => "远海", 
			OceanZone.DeepSea => "深海", 
			_ => zone.ToString(), 
		};
	}

	public static void ApplyToTiles(TileData[] tiles, int width, int height, bool wrapX)
	{
		int[] array = CalculateDistanceField(tiles, width, height, wrapX);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < tiles.Length; i++)
		{
			if (!tiles[i].isLand && tiles[i].exists)
			{
				OceanZone zone = GetZone(array[i]);
				tiles[i].isCoast = zone == OceanZone.Coast;
				int num5 = i;
				_ = ref tiles[num5];
				tiles[num5].oceanTier = zone switch
				{
					OceanZone.Coast => GameEnums.OceanTier.Coast, 
					OceanZone.Neritic => GameEnums.OceanTier.NearSea, 
					OceanZone.Oceanic => GameEnums.OceanTier.MidSea, 
					_ => GameEnums.OceanTier.DeepSea, 
				};
				switch (zone)
				{
				case OceanZone.Coast:
					num++;
					break;
				case OceanZone.Neritic:
					num2++;
					break;
				case OceanZone.Oceanic:
					num3++;
					break;
				case OceanZone.DeepSea:
					num4++;
					break;
				}
			}
		}
		Debug.Log((object)$"[OceanZoning] 海洋分区完成：近海{num} 中海{num2} 远海{num3} 深海{num4}");
	}
}
}
