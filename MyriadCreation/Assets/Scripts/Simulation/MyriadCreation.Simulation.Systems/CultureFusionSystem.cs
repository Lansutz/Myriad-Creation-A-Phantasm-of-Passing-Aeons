using System;
using System.Collections.Generic;
using MyriadCreation.Content.Modding;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.CultureHost;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>
/// P2.1 文化融合：当一个文化在政权内已是少数、且只在别处为主文化时，
/// 被主文化吸收——主文化吸收其可兼容的传统，人口块并入主文化，少数文化保留实体供人物引用但不再传播。
/// 与「同化」的区别：同化只换个人口块的文化，融合会改写主文化的传统集合。
/// </summary>
public static class CultureFusionSystem
{
	/// <summary>少数文化在政权内的人口占比上限；超过它说明仍是重要文化，不融合。</summary>
	public const float MinorityShareCap = 0.15f;

	/// <summary>融合所需的最低平均整合度（复用传播进度，避免另造一套“融合点”）。</summary>
	public const float FusionPenetrationThreshold = 60f;

	/// <summary>一次融合最多吸收的传统数，避免主文化瞬间变成拼盘。</summary>
	public const int MaxTraditionsTransferred = 2;

	/// <summary>月结；返回本刻完成的融合对数。onFused(少数文化, 主文化) 供世界层留痕。</summary>
	public static int MonthlyTick(
		Dictionary<int, RealmData> realms,
		Dictionary<int, CultureData> cultures,
		TileData[] tiles,
		Action<int, int> onFused = null)
	{
		if (realms == null || cultures == null || tiles == null)
		{
			return 0;
		}
		int fused = 0;
		foreach (RealmData realm in realms.Values)
		{
			if (realm == null)
			{
				continue;
			}
			int primaryId = realm.primaryCultureId;
			if (primaryId < 0 || !cultures.TryGetValue(primaryId, out CultureData primary) || primary == null || primary.mergedIntoCultureId >= 0)
			{
				continue;
			}

			Dictionary<int, float> counts = new Dictionary<int, float>();
			Dictionary<int, float> penetrationWeighted = new Dictionary<int, float>();
			float total = 0f;
			for (int t = 0; t < tiles.Length; t++)
			{
				ref TileData tile = ref tiles[t];
				if (!tile.exists || tile.ownerRealmId != realm.realmId || tile.populationBlocks == null)
				{
					continue;
				}
				List<PopulationBlock> blocks = tile.populationBlocks;
				for (int b = 0; b < blocks.Count; b++)
				{
					PopulationBlock block = blocks[b];
					if (block.count <= 0f)
					{
						continue;
					}
					counts.TryGetValue(block.cultureId, out float current);
					counts[block.cultureId] = current + block.count;
					penetrationWeighted.TryGetValue(block.cultureId, out float weighted);
					penetrationWeighted[block.cultureId] = weighted + block.culturePenetration * block.count;
					total += block.count;
				}
			}
			if (total <= 0f)
			{
				continue;
			}

			List<int> minorityIds = new List<int>(counts.Keys);
			for (int i = 0; i < minorityIds.Count; i++)
			{
				int minorityId = minorityIds[i];
				if (minorityId == primaryId || !cultures.TryGetValue(minorityId, out CultureData minority) || minority == null)
				{
					continue;
				}
				if (minority.mergedIntoCultureId >= 0)
				{
					continue;
				}
				if (counts[minorityId] / total > MinorityShareCap)
				{
					continue;
				}
				if (IsPrimaryInAnyRealm(realms, minorityId))
				{
					continue;
				}
				float averagePenetration = penetrationWeighted[minorityId] / Math.Max(0.001f, counts[minorityId]);
				if (averagePenetration < FusionPenetrationThreshold)
				{
					continue;
				}
				if (!CultureTransition.CanCultureMerge(minority, primary))
				{
					continue;
				}
				TransferTraditions(primary, minority);
				ConvertRealmPopulation(tiles, realm.realmId, minorityId, primaryId);
				minority.mergedIntoCultureId = primaryId;
				fused++;
				onFused?.Invoke(minorityId, primaryId);
			}
		}
		return fused;
	}

	private static bool IsPrimaryInAnyRealm(Dictionary<int, RealmData> realms, int cultureId)
	{
		foreach (RealmData realm in realms.Values)
		{
			if (realm != null && realm.primaryCultureId == cultureId)
			{
				return true;
			}
		}
		return false;
	}

	private static void TransferTraditions(CultureData primary, CultureData minority)
	{
		List<TraditionDef> candidates =
			CultureTransitionHost.ListTraditionCandidates(primary, minority);
		int transferred = 0;
		for (int i = 0; i < candidates.Count && transferred < MaxTraditionsTransferred; i++)
		{
			var tradition = candidates[i];
			if (tradition == null || string.IsNullOrEmpty(tradition.traditionId))
			{
				continue;
			}
			primary.traditionIds.Add(tradition.traditionId);
			transferred++;
		}
	}

	private static void ConvertRealmPopulation(TileData[] tiles, int realmId, int minorityId, int primaryId)
	{
		for (int t = 0; t < tiles.Length; t++)
		{
			ref TileData tile = ref tiles[t];
			if (!tile.exists || tile.ownerRealmId != realmId || tile.populationBlocks == null)
			{
				continue;
			}
			List<PopulationBlock> blocks = tile.populationBlocks;
			for (int b = blocks.Count - 1; b >= 0; b--)
			{
				if (blocks[b].cultureId == minorityId)
				{
					CultureTransmissionSystem.ConvertBlock(blocks, b, primaryId);
				}
			}
		}
	}
}
}
