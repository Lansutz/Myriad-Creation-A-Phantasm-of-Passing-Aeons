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

	/// <summary>可作口粮（P3.2 食物篮子）；盐、矿石等即使归在 Food 类也不参与充饥。</summary>
	public bool edible;

	/// <summary>每单位提供的口粮当量（1 = 与小麦同口径）。</summary>
	/// <summary>1 = 与小麦同口径；0/缺省按 1 处理（旧内容/测试夹具）。</summary>
	public float nutrition;

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
