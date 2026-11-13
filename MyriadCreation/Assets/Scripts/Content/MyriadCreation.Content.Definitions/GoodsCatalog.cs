using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Content.Definitions
{
/// <summary>商品内容表行（JSON 形态）；属性（properties）仍由 GoodsPropertyCatalog 合并。</summary>
[Serializable]
public sealed class GoodsContentDef
{
	public int goodsId;

	public string goodsName = "";

	/// <summary>GameEnums.GoodsCategory 的 int 值。</summary>
	public int category;

	public float baseValue;

	public float weight;

	public bool hasShelfLife;

	public float shelfLifeDays;

	public int processedFromId = -1;

	public float processingRatio = 1f;

	public bool isPerishable;

	public float spoilagePerDay;

	public bool isLiving;

	public bool canBeSlaughtered;

	public int slaughterGoodsId = -1;

	public bool isNaturalResource;

	/// <summary>ResourceType 的 int 值（0=WildAnimal…6=Special）。</summary>
	public int resourceType;

	public float baseAbundance;

	public int requiredInnovation = -1;

	public float surfaceDepthShare = 1f;

	public float shaftDepthShare;

	public bool renewable;

	public List<int> originBiomes = new List<int>();
}

/// <summary>商品外提表（P2.4）：内容优先，未加载时代码兜底。</summary>
public static class GoodsCatalog
{
	private static Dictionary<int, GoodsDef> _defs;

	public static bool IsLoaded => _defs != null && _defs.Count > 0;

	public static IReadOnlyDictionary<int, GoodsDef> All => _defs;

	public static void LoadFromContent(IEnumerable<GoodsContentDef> defs)
	{
		_defs ??= new Dictionary<int, GoodsDef>();
		if (defs == null)
		{
			return;
		}
		foreach (GoodsContentDef def in defs)
		{
			if (def == null || def.goodsId < 0 || string.IsNullOrEmpty(def.goodsName))
			{
				continue;
			}
			List<GameEnums.BiomeType> biomes = new List<GameEnums.BiomeType>();
			if (def.originBiomes != null && def.originBiomes.Count > 0)
			{
				for (int i = 0; i < def.originBiomes.Count; i++)
				{
					if (Enum.IsDefined(typeof(GameEnums.BiomeType), def.originBiomes[i]))
					{
						biomes.Add((GameEnums.BiomeType)def.originBiomes[i]);
					}
				}
			}
			else if (def.isNaturalResource)
			{
				biomes = DefaultNaturalBiomes((ResourceType)ClampResourceType(def.resourceType));
			}
			_defs[def.goodsId] = new GoodsDef
			{
				goodsId = def.goodsId,
				goodsName = def.goodsName,
				category = (GameEnums.GoodsCategory)ClampCategory(def.category),
				baseValue = def.baseValue,
				weight = def.weight,
				hasShelfLife = def.hasShelfLife,
				shelfLifeDays = def.shelfLifeDays,
				originBiomes = biomes,
				processedFromId = def.processedFromId,
				processingRatio = def.processingRatio,
				isNaturalResource = def.isNaturalResource,
				resourceType = (ResourceType)ClampResourceType(def.resourceType),
				baseAbundance = def.baseAbundance,
				spawnCondition = ResourceSpawnCondition.Unrestricted,
				requiredInnovation = def.requiredInnovation,
				renewable = def.renewable,
				isLiving = def.isLiving,
				isPerishable = def.isPerishable,
				spoilagePerDay = def.spoilagePerDay,
				canBeSlaughtered = def.canBeSlaughtered,
				slaughterGoodsId = def.slaughterGoodsId,
				properties = new List<GoodsPropertyValue>(),
				surfaceDepthShare = def.surfaceDepthShare,
				shaftDepthShare = def.shaftDepthShare
			};
		}
	}

	public static void ResetContent()
	{
		_defs = null;
	}

	private static List<GameEnums.BiomeType> DefaultNaturalBiomes(ResourceType type)
	{
		List<GameEnums.BiomeType> biomes = new List<GameEnums.BiomeType>();
		foreach (GameEnums.BiomeType biome in Enum.GetValues(typeof(GameEnums.BiomeType)))
		{
			if (biome == GameEnums.BiomeType.IceSheet || biome == GameEnums.BiomeType.MountainGlacier)
			{
				continue;
			}
			biomes.Add(biome);
		}
		return biomes;
	}

	private static int ClampCategory(int value)
	{
		return value < 0 || value > (int)GameEnums.GoodsCategory.Glass ? (int)GameEnums.GoodsCategory.Food : value;
	}

	private static int ClampResourceType(int value)
	{
		return value < 0 || value > (int)ResourceType.Special ? (int)ResourceType.WildAnimal : value;
	}
}
}
