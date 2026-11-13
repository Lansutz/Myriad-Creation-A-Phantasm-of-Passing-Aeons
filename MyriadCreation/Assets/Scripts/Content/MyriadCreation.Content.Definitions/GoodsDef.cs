using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Content.Definitions
{
public struct GoodsDef
{
	public int goodsId;

	public string goodsName;

	public GameEnums.GoodsCategory category;

	public float baseValue;

	public float weight;

	public bool hasShelfLife;

	public float shelfLifeDays;

	public List<GameEnums.BiomeType> originBiomes;

	public int processedFromId;

	public float processingRatio;

	public bool isNaturalResource;

	public ResourceType resourceType;

	public float baseAbundance;

	public ResourceSpawnCondition spawnCondition;

	public int requiredInnovation;

	public bool renewable;

	public bool isLiving;

	public bool isPerishable;

	public float spoilagePerDay;

	public bool canBeSlaughtered;

	public int slaughterGoodsId;

	public List<GoodsPropertyValue> properties;

	public float surfaceDepthShare;

	public float shaftDepthShare;

	public bool TryGetProperty(string propertyId, out float value)
	{
		value = 0f;
		if (properties == null || string.IsNullOrEmpty(propertyId))
		{
			return false;
		}
		foreach (GoodsPropertyValue property in properties)
		{
			if (!(property.id != propertyId))
			{
				value = property.value;
				return true;
			}
		}
		return false;
	}
}
}
