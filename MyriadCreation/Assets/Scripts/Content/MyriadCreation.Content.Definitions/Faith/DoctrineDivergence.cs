using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
/// <summary>
/// 教义分歧度（0–100）：按「组」比较，而不是按 option 数量抡权重。
/// `sequence` 组取组内档位距离，`category` 组取集合距离；两边都缺该组不计入分母。
/// 权重目前统一（TODO_M05），等内容给出按域权重后再替换。
/// </summary>
public static class DoctrineDivergence
{
	public static float Calculate(
		IReadOnlyCollection<string> a,
		IReadOnlyCollection<string> b,
		IReadOnlyDictionary<string, DoctrineOptionDef> options)
	{
		if (a == null || b == null || options == null)
		{
			return 0f;
		}
		Dictionary<string, List<DoctrineOptionDef>> groupsA = BuildGroups(a, options);
		Dictionary<string, List<DoctrineOptionDef>> groupsB = BuildGroups(b, options);
		HashSet<string> groupIds = new HashSet<string>(groupsA.Keys);
		groupIds.UnionWith(groupsB.Keys);

		float total = 0f;
		int counted = 0;
		foreach (string groupId in groupIds)
		{
			groupsA.TryGetValue(groupId, out List<DoctrineOptionDef> left);
			groupsB.TryGetValue(groupId, out List<DoctrineOptionDef> right);
			if ((left == null || left.Count == 0) && (right == null || right.Count == 0))
			{
				continue;
			}
			string kind = left != null && left.Count > 0 ? left[0].kind : right[0].kind;
			float difference = kind == "sequence"
				? SequenceDistance(left, right, MaxLevelInGroup(groupId, options))
				: CategoryDistance(left, right);
			total += difference;
			counted++;
		}
		return counted == 0 ? 0f : total / counted * 100f;
	}

	private static Dictionary<string, List<DoctrineOptionDef>> BuildGroups(
		IReadOnlyCollection<string> optionIds,
		IReadOnlyDictionary<string, DoctrineOptionDef> options)
	{
		Dictionary<string, List<DoctrineOptionDef>> groups = new Dictionary<string, List<DoctrineOptionDef>>();
		foreach (string optionId in optionIds)
		{
			if (string.IsNullOrEmpty(optionId) || !options.TryGetValue(optionId, out DoctrineOptionDef option) || option == null)
			{
				continue;
			}
			string groupId = string.IsNullOrEmpty(option.groupId) ? DoctrineSchema.GroupOf(optionId) : option.groupId;
			if (!groups.TryGetValue(groupId, out List<DoctrineOptionDef> list))
			{
				list = new List<DoctrineOptionDef>();
				groups[groupId] = list;
			}
			list.Add(option);
		}
		return groups;
	}

	private static float SequenceDistance(List<DoctrineOptionDef> left, List<DoctrineOptionDef> right, int maxLevel)
	{
		if (left == null || left.Count == 0 || right == null || right.Count == 0)
		{
			return 1f;
		}
		if (maxLevel <= 0)
		{
			return AverageLevel(left) == AverageLevel(right) ? 0f : 1f;
		}
		return Math.Min(1f, Math.Abs(AverageLevel(left) - AverageLevel(right)) / maxLevel);
	}

	private static float CategoryDistance(List<DoctrineOptionDef> left, List<DoctrineOptionDef> right)
	{
		HashSet<string> leftIds = new HashSet<string>();
		HashSet<string> rightIds = new HashSet<string>();
		if (left != null)
		{
			for (int i = 0; i < left.Count; i++) leftIds.Add(left[i].optionId);
		}
		if (right != null)
		{
			for (int i = 0; i < right.Count; i++) rightIds.Add(right[i].optionId);
		}
		HashSet<string> union = new HashSet<string>(leftIds);
		union.UnionWith(rightIds);
		if (union.Count == 0)
		{
			return 0f;
		}
		int intersection = 0;
		foreach (string id in leftIds)
		{
			if (rightIds.Contains(id)) intersection++;
		}
		return 1f - (float)intersection / union.Count;
	}

	private static int MaxLevelInGroup(string groupId, IReadOnlyDictionary<string, DoctrineOptionDef> options)
	{
		int maxLevel = 0;
		foreach (DoctrineOptionDef option in options.Values)
		{
			if (option == null) continue;
			string optionGroup = string.IsNullOrEmpty(option.groupId) ? DoctrineSchema.GroupOf(option.optionId) : option.groupId;
			if (optionGroup == groupId && option.level > maxLevel) maxLevel = option.level;
		}
		return maxLevel;
	}

	private static float AverageLevel(List<DoctrineOptionDef> options)
	{
		float sum = 0f;
		for (int i = 0; i < options.Count; i++)
		{
			sum += options[i].level;
		}
		return sum / Math.Max(1, options.Count);
	}
}
}
