using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.State;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>
/// P2.1 文化分裂：一个有故土的文化，在另一个政权里长期抗拒同化且人口占比足够时，
/// 就地分出一个子文化（继承语言/族群/传统，独立演化），该政权内的该文化人口改挂子文化。
/// 与「同化」「融合」互斥：分裂是抗拒整合的结果，不是被吸收。
/// </summary>
public static class CultureSplitSystem
{
	/// <summary>在宿主政权内的人口占比下限；太小说明只是侨民，不足以成支。</summary>
	public const float MinorityShareFloor = 0.25f;

	/// <summary>平均整合度上限；已经融入的文化不会再分裂。</summary>
	public const float PenetrationCeiling = 25f;

	/// <summary>同一文化两次分裂的最小间隔（天）。</summary>
	public const int SplitCooldownDays = 3650;

	/// <summary>一个文化最多派生几个子文化，防止无限增殖。</summary>
	public const int MaxChildren = 4;

	/// <summary>月结；返回本刻新建的子文化 id（没有分裂返回 -1）。onSplit(母文化, 子文化, 宿主政权)。</summary>
	public static int MonthlyTick(
		Dictionary<int, RealmData> realms,
		Dictionary<int, CultureData> cultures,
		TileData[] tiles,
		int currentDay,
		Action<int, int, int> onSplit = null)
	{
		if (realms == null || cultures == null || tiles == null)
		{
			return -1;
		}

		Dictionary<int, float> populationByCulture = new Dictionary<int, float>();
		Dictionary<int, float> penetrationByCulture = new Dictionary<int, float>();
		Dictionary<int, Dictionary<int, float>> byRealm = new Dictionary<int, Dictionary<int, float>>();
		for (int t = 0; t < tiles.Length; t++)
		{
			ref TileData tile = ref tiles[t];
			if (!tile.exists || tile.ownerRealmId < 0 || tile.populationBlocks == null)
			{
				continue;
			}
			if (!byRealm.TryGetValue(tile.ownerRealmId, out Dictionary<int, float> realmCounts))
			{
				realmCounts = new Dictionary<int, float>();
				byRealm[tile.ownerRealmId] = realmCounts;
			}
			for (int b = 0; b < tile.populationBlocks.Count; b++)
			{
				PopulationBlock block = tile.populationBlocks[b];
				if (block.count <= 0f)
				{
					continue;
				}
				realmCounts.TryGetValue(block.cultureId, out float realmCurrent);
				realmCounts[block.cultureId] = realmCurrent + block.count;
				populationByCulture.TryGetValue(block.cultureId, out float totalCurrent);
				populationByCulture[block.cultureId] = totalCurrent + block.count;
				penetrationByCulture.TryGetValue(block.cultureId, out float weighted);
				penetrationByCulture[block.cultureId] = weighted + block.culturePenetration * block.count;
			}
		}

		foreach (KeyValuePair<int, RealmData> pair in realms)
		{
			RealmData realm = pair.Value;
			if (realm == null || !byRealm.TryGetValue(realm.realmId, out Dictionary<int, float> counts) || counts.Count == 0)
			{
				continue;
			}
			float total = 0f;
			foreach (float value in counts.Values)
			{
				total += value;
			}
			if (total <= 0f)
			{
				continue;
			}

			foreach (KeyValuePair<int, float> entry in counts)
			{
				int cultureId = entry.Key;
				if (cultureId == realm.primaryCultureId || entry.Value / total < MinorityShareFloor)
				{
					continue;
				}
				if (!cultures.TryGetValue(cultureId, out CultureData culture) || culture == null)
				{
					continue;
				}
				if (!culture.allowsBranching || culture.mergedIntoCultureId >= 0)
				{
					continue;
				}
				if (culture.childCultureIds != null && culture.childCultureIds.Count >= MaxChildren)
				{
					continue;
				}
				if (culture.lastSplitDay >= 0 && currentDay - culture.lastSplitDay < SplitCooldownDays)
				{
					continue;
				}
				if (!HasHomeland(realms, cultureId, realm.realmId))
				{
					continue;
				}
				float averagePenetration = penetrationByCulture[cultureId] / Mathf.Max(0.001f, populationByCulture[cultureId]);
				if (averagePenetration >= PenetrationCeiling)
				{
					continue;
				}

				int childId = NextCultureId(cultures);
				CultureData child = CloneAsChild(culture, childId, realm);
				cultures[childId] = child;
				culture.childCultureIds ??= new List<int>();
				culture.childCultureIds.Add(childId);
				culture.lastSplitDay = currentDay;
				ConvertRealmPopulation(tiles, realm.realmId, cultureId, childId);
				onSplit?.Invoke(cultureId, childId, realm.realmId);
				return childId;
			}
		}
		return -1;
	}

	/// <summary>母文化必须在别的政权里是主文化；没有故土的侨民文化不分裂。</summary>
	private static bool HasHomeland(Dictionary<int, RealmData> realms, int cultureId, int excludeRealmId)
	{
		foreach (RealmData realm in realms.Values)
		{
			if (realm != null && realm.realmId != excludeRealmId && realm.primaryCultureId == cultureId)
			{
				return true;
			}
		}
		return false;
	}

	private static int NextCultureId(Dictionary<int, CultureData> cultures)
	{
		int max = 0;
		foreach (int id in cultures.Keys)
		{
			if (id > max)
			{
				max = id;
			}
		}
		return max + 1;
	}

	private static CultureData CloneAsChild(CultureData parent, int childId, RealmData hostRealm)
	{
		string homeland = string.IsNullOrEmpty(hostRealm.realmName) ? "边地" : hostRealm.realmName;
		float hue, saturation, value;
		Color.RGBToHSV(parent.color, out hue, out saturation, out value);
		return new CultureData
		{
			cultureId = childId,
			cultureName = parent.cultureName + "·" + homeland + "支",
			cultureColorHex = parent.cultureColorHex,
			color = Color.HSVToRGB(Mathf.Repeat(hue + 0.06f, 1f), Mathf.Clamp01(saturation * 0.9f + 0.05f), Mathf.Clamp01(value)),
			stage = parent.stage,
			languageId = parent.languageId,
			languageFamilyId = parent.languageFamilyId,
			ethnicGroupId = parent.ethnicGroupId,
			heritageId = parent.heritageId,
			ethosId = parent.ethosId,
			faithId = parent.faithId,
			traditionIds = new List<string>(parent.traditionIds ?? new List<string>()),
			maturity = parent.maturity,
			spreadPower = parent.spreadPower,
			parentCultureId = parent.cultureId,
			allowsBranching = false
		};
	}

	private static void ConvertRealmPopulation(TileData[] tiles, int realmId, int cultureId, int childId)
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
				if (blocks[b].cultureId == cultureId)
				{
					CultureTransmissionSystem.ConvertBlock(blocks, b, childId);
				}
			}
		}
	}
}
}
