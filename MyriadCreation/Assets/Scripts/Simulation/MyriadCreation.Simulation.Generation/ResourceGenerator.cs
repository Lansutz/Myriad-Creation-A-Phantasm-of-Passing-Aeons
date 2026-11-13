using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Generation
{
public static class ResourceGenerator
{
	public static void GenerateAll(GameWorld world)
	{
		if ((Object)(object)world == (Object)null || world.tiles == null)
		{
			return;
		}
		List<GoodsDef> naturalGoods = GetNaturalGoods(world);
		if (naturalGoods.Count == 0)
		{
			Debug.LogWarning((object)"[ResourceGenerator] 没有自然资源类物资定义（GoodsDef.isNaturalResource=true）");
			return;
		}
		int num = 0;
		for (int i = 0; i < world.tiles.Length; i++)
		{
			ref TileData reference = ref world.tiles[i];
			if (!reference.exists || !reference.isLand)
			{
				continue;
			}
			if (reference.resources == null)
			{
				reference.resources = new List<TileResource>();
			}
			else
			{
				reference.resources.Clear();
			}
			foreach (GoodsDef item in naturalGoods)
			{
				if (TrySpawnResource(world, in reference, item, out var resource))
				{
					reference.resources.Add(resource);
					num++;
				}
			}
		}
		Debug.Log((object)$"[ResourceGenerator] 自然资源点生成完成：共 {num} 个资源点，涉及 {naturalGoods.Count} 种自然资源");
	}

	private static bool TrySpawnResource(GameWorld world, in TileData tile, GoodsDef goods, out TileResource resource)
	{
		resource = default;
		if (goods.originBiomes == null || goods.originBiomes.Count == 0)
		{
			return false;
		}
		if (!goods.originBiomes.Contains(tile.biome))
		{
			return false;
		}
		if (!CheckSpawnCondition(in tile, goods.spawnCondition))
		{
			return false;
		}
		float num = CalculateGeographicQuality(in tile, goods);
		float num2 = goods.baseAbundance * num;
		if (Random.value > num2)
		{
			return false;
		}
		float abundance = Mathf.Clamp01(goods.baseAbundance * num * Random.Range(0.7f, 1.3f));
		resource = TileResource.Create(goods.goodsId, abundance, RollDepth(in goods, Random.value));
		return true;
	}

	public static ResourceAccessDepth RollDepth(in GoodsDef goods, float roll)
	{
		if (goods.resourceType != ResourceType.Mineral && goods.resourceType != ResourceType.Special)
		{
			return ResourceAccessDepth.Surface;
		}
		float num = Mathf.Clamp01(goods.surfaceDepthShare);
		float num2 = Mathf.Clamp01(goods.shaftDepthShare);
		if (roll < num)
		{
			return ResourceAccessDepth.Surface;
		}
		if (roll < num + num2)
		{
			return ResourceAccessDepth.Shaft;
		}
		return ResourceAccessDepth.Deep;
	}

	private static bool CheckSpawnCondition(in TileData tile, ResourceSpawnCondition cond)
	{
		if (tile.elevation01 < cond.minElevation)
		{
			return false;
		}
		if (tile.elevation01 > cond.maxElevation)
		{
			return false;
		}
		if (tile.annualPrecipMm < cond.minPrecipitation)
		{
			return false;
		}
		if (tile.annualPrecipMm > cond.maxPrecipitation)
		{
			return false;
		}
		if (tile.annualTemp < cond.minTemperature)
		{
			return false;
		}
		if (tile.annualTemp > cond.maxTemperature)
		{
			return false;
		}
		if (tile.slopeDegree > cond.maxSlope)
		{
			return false;
		}
		if (cond.requiresCoast && !tile.isCoast)
		{
			return false;
		}
		if (cond.requiresRiver && !tile.isRiver)
		{
			return false;
		}
		return true;
	}

	private static float CalculateGeographicQuality(in TileData tile, GoodsDef goods)
	{
		float num = 1f;
		if (goods.resourceType == ResourceType.WildPlant)
		{
			num *= 0.5f + tile.fertility * 0.5f;
		}
		if (goods.resourceType == ResourceType.Mineral)
		{
			num *= 0.6f + Mathf.Clamp01(tile.slopeDegree / 45f) * 0.4f;
		}
		if (goods.resourceType == ResourceType.Aquatic)
		{
			num = ((!tile.isCoast && !tile.isRiver) ? (num * 0.5f) : (num * 1.2f));
		}
		return Mathf.Clamp01(num);
	}

	private static List<GoodsDef> GetNaturalGoods(GameWorld world)
	{
		List<GoodsDef> list = new List<GoodsDef>();
		if ((Object)(object)world == (Object)null || world.goodsDefs == null)
		{
			return list;
		}
		foreach (KeyValuePair<int, GoodsDef> goodsDef in world.goodsDefs)
		{
			if (goodsDef.Value.isNaturalResource)
			{
				list.Add(goodsDef.Value);
			}
		}
		return list;
	}
}
}
