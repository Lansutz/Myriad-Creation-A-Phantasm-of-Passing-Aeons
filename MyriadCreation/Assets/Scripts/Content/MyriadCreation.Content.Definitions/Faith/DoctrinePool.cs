using System.Collections.Generic;
using UnityEngine;

namespace MyriadCreation.Content.Definitions
{
public static class DoctrinePool
{
	private static Dictionary<string, DoctrineOptionDef> _options = new Dictionary<string, DoctrineOptionDef>();

	private static Dictionary<string, List<DoctrineOptionDef>> _byPillar = new Dictionary<string, List<DoctrineOptionDef>>();

	private static Dictionary<string, List<DoctrineOptionDef>> _byGroup = new Dictionary<string, List<DoctrineOptionDef>>();

	public static void Load(List<DoctrineOptionDef> options)
	{
		_options.Clear();
		_byPillar.Clear();
		_byGroup.Clear();
		if (options == null)
		{
			return;
		}
		foreach (DoctrineOptionDef option in options)
		{
			if (option != null && !string.IsNullOrEmpty(option.optionId))
			{
				option.groupId = string.IsNullOrEmpty(option.groupId) ? DoctrineSchema.GroupOf(option.optionId) : option.groupId;
				option.kind = DoctrineSchema.KindOf(option.groupId);
				if (!_byGroup.TryGetValue(option.groupId, out var group))
				{
					group = (_byGroup[option.groupId] = new List<DoctrineOptionDef>());
				}
				option.level = group.Count;
				group.Add(option);
				if (!DoctrineSchema.ValidateOption(option, out string error))
				{
					Debug.LogError($"[Doctrine] {option.optionId}: {error}");
				}
				_options[option.optionId] = option;
				if (!_byPillar.TryGetValue(option.pillar, out var value))
				{
					value = (_byPillar[option.pillar] = new List<DoctrineOptionDef>());
				}
				value.Add(option);
			}
		}
	}

	public static DoctrineOptionDef Get(string optionId)
	{
		if (!_options.TryGetValue(optionId, out var value))
		{
			return null;
		}
		return value;
	}

	public static List<DoctrineOptionDef> GetOptions(string pillar, int religionId)
	{
		List<DoctrineOptionDef> list = new List<DoctrineOptionDef>();
		if (_byPillar.TryGetValue(pillar, out var value))
		{
			foreach (DoctrineOptionDef item in value)
			{
				if (item.exclusiveReligionIds.Count == 0 || item.exclusiveReligionIds.Contains(religionId))
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	public static List<DoctrineOptionDef> GetAllOptions()
	{
		return new List<DoctrineOptionDef>(_options.Values);
	}

	public static IReadOnlyList<string> GetGroupIds()
	{
		return new List<string>(_byGroup.Keys);
	}

	public static List<DoctrineOptionDef> GetGroupOptions(string groupId)
	{
		return _byGroup.TryGetValue(groupId, out var value) ? value : new List<DoctrineOptionDef>();
	}
}
}
