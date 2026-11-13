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

	/// <summary>
	/// 每名农业劳动者每日的食物产出基数。校准：中等肥力/NPP/开发（三项乘子乘积约 0.34）
	/// 下一名农夫日产 0.02，可养 2 人——自身 + 一个脱产者/工匠；好地可养 3–4 人。
	/// 0.03 的旧校准只能“农夫自养”，社会一旦分化出商人/贵族，全城就净缺粮。
	/// </summary>
	public const float OutputPerFarmerPerDay = 0.06f;

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
			// 奴隶在多数前现代经济里也下地干活；只数 Peasant 会把农业劳动力系统性算少。
			if ((block.socialClass == GameEnums.SocialClass.Peasant || block.socialClass == GameEnums.SocialClass.Slave)
				&& block.count > 0f)
			{
				farmers += block.count;
			}
		}
		return ComputeForLabor(tile, farmers);
	}

	/// <summary>
	/// 给定劳动人口（不要求已写成地块人口块）的农业日产出。
	/// 营地/迁徙队伍等“尚未落入地块人口块”的人群也能按同一劳动—土地关系产粮。
	/// </summary>
	/// <summary>
	/// 农业可用 NPP：河流/海岸提供灌溉与运输，不能按当地降水算 NPP。
	/// 干旱河谷（npp≈0.02）在生成期仍可能被判定为高肥力冲积土；若直接用 NPP 乘，
	/// 会被算成“沃土但几乎不产粮”，长跑里城市粮仓必然被吃空。
	/// </summary>
	public static float EffectiveNpp(TileData tile)
	{
		float npp = tile.npp > 0f ? Mathf.Clamp01(tile.npp) : 0.5f;
		if (tile.isRiver) npp = Mathf.Max(npp, 0.65f);
		else if (tile.isCoast) npp = Mathf.Max(npp, 0.45f);
		return npp;
	}

	public static float ComputeForLabor(TileData tile, float farmers)
	{
		if (!tile.exists || !tile.isLand || farmers <= 0f)
		{
			return 0f;
		}
		float fertility = Mathf.Clamp01(tile.fertility);
		if (fertility <= 0.02f)
		{
			return 0f;
		}
		float npp = EffectiveNpp(tile);
		float development = Mathf.Clamp01(tile.development);
		return farmers * OutputPerFarmerPerDay
			* (0.2f + 0.8f * fertility)
			* (0.5f + 0.5f * npp)
			* (0.7f + 0.3f * development);
	}
}
}
