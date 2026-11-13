using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.State;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>
/// P2.1 文化传播/同化：人口块在所在政权主文化的影响下逐步改换文化。
/// 阻力直接取 <see cref="CultureTransition.GetGroupResistance"/>（与群体融合同一公式），
/// 不另造"同化点"；<see cref="PopulationBlock.culturePenetration"/> 就是进度，
/// 达到 100 才换文化，并在换文化时与同属性人口块合并（不产生碎片人口）。
/// </summary>
public static class CultureTransmissionSystem
{
	public const float PenetrationThreshold = 100f;

	/// <summary>每月进度 = (阈值 - 阻力) × 该系数；阻力越接近阈值，同化越慢。</summary>
	public const float PenetrationGainScale = 12f;

	/// <summary>同化阻力上限：比群体合并更严，完全异质的文化不会仅因同属一个政权就消失。</summary>
	public const float AssimilationResistanceCeiling = 0.45f;

	/// <summary>阻力超过阈值后每月回落的进度（接触减少，不等于反向同化）。</summary>
	public const float PenetrationDecay = 3f;

	/// <summary>月结一次；返回本刻完成文化转换的人口块数（测试与长跑可观测）。</summary>
	public static int MonthlyTick(
		IReadOnlyDictionary<int, RealmData> realms,
		Dictionary<int, CultureData> cultures,
		TileData[] tiles,
		IContentLookup content = null)
	{
		if (realms == null || cultures == null || tiles == null)
		{
			return 0;
		}
		int converted = 0;
		for (int t = 0; t < tiles.Length; t++)
		{
			ref TileData tile = ref tiles[t];
			if (!tile.exists || tile.populationBlocks == null || tile.populationBlocks.Count == 0)
			{
				continue;
			}
			if (tile.ownerRealmId < 0 || !realms.TryGetValue(tile.ownerRealmId, out RealmData realm) || realm == null)
			{
				continue;
			}
			int targetCultureId = realm.primaryCultureId;
			if (targetCultureId < 0 || !cultures.TryGetValue(targetCultureId, out CultureData target) || target == null)
			{
				continue;
			}
			List<PopulationBlock> blocks = tile.populationBlocks;
			for (int i = blocks.Count - 1; i >= 0; i--)
			{
				PopulationBlock block = blocks[i];
				if (block.cultureId == targetCultureId)
				{
					continue;
				}
				if (!cultures.TryGetValue(block.cultureId, out CultureData source) || source == null)
				{
					continue;
				}
				float resistance = CultureTransition.GetGroupResistance(source, target, content);
				if (resistance >= AssimilationResistanceCeiling)
				{
					if (block.culturePenetration > 0f)
					{
						block.culturePenetration = Mathf.Max(0f, block.culturePenetration - PenetrationDecay);
						blocks[i] = block;
					}
					continue;
				}
				block.culturePenetration += (AssimilationResistanceCeiling - resistance) * PenetrationGainScale;
				if (block.culturePenetration >= PenetrationThreshold)
				{
					ConvertBlock(blocks, i, targetCultureId);
					converted++;
				}
				else
				{
					blocks[i] = block;
				}
			}
		}
		return converted;
	}

	/// <summary>换文化：优先并入同属性人口块（种族/信仰/阶级/职业一致），否则原位改写。</summary>
	private static void ConvertBlock(List<PopulationBlock> blocks, int index, int targetCultureId)
	{
		PopulationBlock converted = blocks[index];
		converted.cultureId = targetCultureId;
		converted.culturePenetration = PenetrationThreshold;
		for (int j = 0; j < blocks.Count; j++)
		{
			if (j == index)
			{
				continue;
			}
			PopulationBlock other = blocks[j];
			if (other.cultureId != targetCultureId || !SamePopulation(other, converted))
			{
				continue;
			}
			other.count += converted.count;
			blocks[j] = other;
			blocks.RemoveAt(index);
			return;
		}
		blocks[index] = converted;
	}

	private static bool SamePopulation(PopulationBlock a, PopulationBlock b)
	{
		return a.raceId == b.raceId
			&& a.faithId == b.faithId
			&& a.socialClass == b.socialClass
			&& a.profession == b.profession
			&& a.economicClass == b.economicClass
			&& a.stratum == b.stratum
			&& a.occupations == b.occupations;
	}
}
}
