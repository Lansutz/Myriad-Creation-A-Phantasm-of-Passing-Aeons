using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.Definitions
{
/// <summary>头衔目录：运行时统一从 TitleManager 读取（单一真源）。</summary>
public static class TitleCatalog
{
	public static TitleDefinition Get(string titleId)
	{
		return TitleManager.Get(titleId);
	}

	public static List<TitleDefinition> ByCategory(TitleDerivedCategory category)
	{
		List<TitleDefinition> list = new List<TitleDefinition>();
		foreach (TitleDefinition item in TitleManager.All)
		{
			if (item.category == category)
			{
				list.Add(item);
			}
		}
		list.Sort((TitleDefinition a, TitleDefinition b) => b.rank.CompareTo(a.rank));
		return list;
	}

	public static TitleDefinition Highest(TitleDerivedCategory category)
	{
		TitleDefinition result = null;
		foreach (TitleDefinition item in TitleManager.All)
		{
			if (item.category == category && (result == null || item.rank > result.rank))
			{
				result = item;
			}
		}
		return result;
	}

	public static TitleDefinition PickByWeight(List<TitleDefinition> candidates, Random rng = null)
	{
		if (candidates == null || candidates.Count == 0)
		{
			return null;
		}
		// 确定性优先：未传 rng 时用固定种子，避免 Environment.TickCount 式的不可复现。
		// TODO(M1)：调用方应传世界 seed 派生的 rng（同一世界的头衔抽取可复现但有变化）。
		rng = rng ?? new Random(0);
		float num = 0f;
		foreach (TitleDefinition candidate in candidates)
		{
			num += candidate.weight;
		}
		float num2 = (float)(rng.NextDouble() * (double)num);
		foreach (TitleDefinition candidate2 in candidates)
		{
			num2 -= candidate2.weight;
			if (num2 <= 0f)
			{
				return candidate2;
			}
		}
		return candidates[candidates.Count - 1];
	}

	public static bool IsHigher(TitleDefinition a, TitleDefinition b)
	{
		if (a != null && b != null)
		{
			return a.rank > b.rank;
		}
		return false;
	}
}
}
