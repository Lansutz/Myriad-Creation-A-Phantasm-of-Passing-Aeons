using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
public enum OrthodoxyVerdict
{
	/// <summary>该 religion 未声明正统边界，不能据此判异端。</summary>
	Unspecified,
	InBounds,
	OutOfBounds
}

/// <summary>
/// 正统边界判定（P2.2）：religion 声明「必备教义」与「排除教义」，
/// 候选 faith 的选中教义必须包含全部必备项、且不得包含任何排除项。
/// 未声明边界的 religion 返回 Unspecified——不自动判异端（failMode=block）。
/// </summary>
public static class OrthodoxyEvaluator
{
	public static OrthodoxyVerdict Evaluate(
		ReligionDef orthodox,
		IReadOnlyCollection<string> doctrineIds,
		ICollection<string> violations = null)
	{
		if (orthodox == null)
		{
			return OrthodoxyVerdict.Unspecified;
		}
		bool hasTenets = orthodox.orthodoxyTenets != null && orthodox.orthodoxyTenets.Count > 0;
		bool hasExclusions = orthodox.orthodoxyExclusions != null && orthodox.orthodoxyExclusions.Count > 0;
		if (!hasTenets && !hasExclusions)
		{
			return OrthodoxyVerdict.Unspecified;
		}

		bool outOfBounds = false;
		if (hasTenets)
		{
			for (int i = 0; i < orthodox.orthodoxyTenets.Count; i++)
			{
				string tenet = orthodox.orthodoxyTenets[i];
				if (!Contains(doctrineIds, tenet))
				{
					outOfBounds = true;
					violations?.Add("missing tenet: " + tenet);
				}
			}
		}
		if (hasExclusions)
		{
			for (int i = 0; i < orthodox.orthodoxyExclusions.Count; i++)
			{
				string excluded = orthodox.orthodoxyExclusions[i];
				if (Contains(doctrineIds, excluded))
				{
					outOfBounds = true;
					violations?.Add("excluded doctrine: " + excluded);
				}
			}
		}
		return outOfBounds ? OrthodoxyVerdict.OutOfBounds : OrthodoxyVerdict.InBounds;
	}

	private static bool Contains(IReadOnlyCollection<string> ids, string target)
	{
		if (ids == null || string.IsNullOrEmpty(target))
		{
			return false;
		}
		foreach (string id in ids)
		{
			if (id == target)
			{
				return true;
			}
		}
		return false;
	}
}
}
