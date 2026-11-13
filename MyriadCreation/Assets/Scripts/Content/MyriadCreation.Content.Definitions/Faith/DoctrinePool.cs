using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
public static class DoctrinePool
{
	private static Dictionary<string, DoctrineOptionDef> _options = new Dictionary<string, DoctrineOptionDef>();

	private static Dictionary<string, List<DoctrineOptionDef>> _byPillar = new Dictionary<string, List<DoctrineOptionDef>>();

	public static void Load(List<DoctrineOptionDef> options)
	{
		_options.Clear();
		_byPillar.Clear();
		if (options == null)
		{
			return;
		}
		foreach (DoctrineOptionDef option in options)
		{
			if (option != null && !string.IsNullOrEmpty(option.optionId))
			{
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
}
}
