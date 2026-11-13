using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>
/// 农业产出（P3.0）：由“耕作者 × 肥力 × NPP × 基础设施”推导，不读抽象繁荣度。
/// 产出进入地区 TradeCenter 的小麦（goodsId=0），与既有消费/饥荒链衔接。
/// </summary>
public static class AgricultureYield
{
	public const int FoodGoodsId = 0;

	/// <summary>每名耕作者每日的食物产出基数；与 `PersonFoodPerDay=0.01` 对齐，留少量余量。</summary>
	public const float OutputPerFarmerPerDay = 0.03f;

	public static float Compute(TileData tile)
	{
		if (!tile.exists || !tile.isLand || tile.populationBlocks == null)
		{
			return 0f;
		}
		float farmers = 0f;
		for (int i = 0; i < tile.populationBlocks.Count; i++)
		{
			PopulationBlock block = tile.populationBlocks[i];
			if (block.socialClass == GameEnums.SocialClass.Peasant && block.count > 0f)
			{
				farmers += block.count;
			}
		}
		if (farmers <= 0f)
		{
			return 0f;
		}
		float fertility = Mathf.Clamp01(tile.fertility);
		if (fertility <= 0.02f)
		{
			return 0f;
		}
		float npp = tile.npp > 0f ? Mathf.Clamp01(tile.npp) : 0.5f;
		float development = Mathf.Clamp01(tile.development);
		return farmers * OutputPerFarmerPerDay
			* (0.2f + 0.8f * fertility)
			* (0.5f + 0.5f * npp)
			* (0.7f + 0.3f * development);
	}
}
}
