using System.Collections.Generic;
using System.Linq;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Content.Modding;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.Systems
{
public static class TitleManager
{
	private static readonly Dictionary<string, TitleDefinition> _registry = new Dictionary<string, TitleDefinition>();

	public static IEnumerable<TitleDefinition> All => _registry.Values;

	public static void Clear()
	{
		_registry.Clear();
	}

	/// <summary>把内容层 Titles.json（Content.Modding.TitleDef）映射进运行时唯一真源 TitleManager。</summary>
	public static void LoadFromContent()
	{
		Clear();
		foreach (TitleDef def in ContentResolvers.Titles.All)
		{
			if (def == null || string.IsNullOrEmpty(def.titleId))
			{
				continue;
			}
			Register(new TitleDefinition
			{
				titleId = def.titleId,
				category = def.category,
				successionMode = def.successionMode,
				ownLawPreset = def.ownLawPreset ?? "",
				rank = def.rank,
				weight = def.weight,
				semantic = def.semantic ?? ""
			});
		}
	}

	public static void Register(TitleDefinition definition)
	{
		if (definition != null && !string.IsNullOrEmpty(definition.titleId))
		{
			_registry[definition.titleId] = definition;
		}
	}

	public static bool TryGet(string titleId, out TitleDefinition definition)
	{
		if (string.IsNullOrEmpty(titleId))
		{
			definition = null;
			return false;
		}
		return _registry.TryGetValue(titleId, out definition);
	}

	public static TitleDefinition Get(string titleId)
	{
		if (!TryGet(titleId, out var definition))
		{
			return null;
		}
		return definition;
	}

	public static IEnumerable<TitleDefinition> ByCategory(TitleDerivedCategory category)
	{
		return _registry.Values.Where((TitleDefinition t) => t.category == category);
	}

	public static IEnumerable<TitleDefinition> ByTier(int tier)
	{
		return _registry.Values.Where((TitleDefinition t) => t.tier == tier);
	}

	public static IEnumerable<TitleDefinition> ChildrenOf(string superiorTitle)
	{
		return _registry.Values.Where((TitleDefinition t) => t.superiorTitle == superiorTitle);
	}

	public static IEnumerable<TitleDefinition> Roots()
	{
		return _registry.Values.Where((TitleDefinition t) => string.IsNullOrEmpty(t.superiorTitle));
	}

	public static List<string> GetChain(string titleId)
	{
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>();
		string text = titleId;
		while (!string.IsNullOrEmpty(text) && !hashSet.Contains(text))
		{
			hashSet.Add(text);
			list.Add(text);
			TitleDefinition titleDefinition = Get(text);
			if (titleDefinition == null)
			{
				break;
			}
			text = titleDefinition.superiorTitle;
		}
		return list;
	}

	public static Dictionary<int, int> CountByTier()
	{
		return (from t in _registry.Values
			group t by t.tier).ToDictionary((IGrouping<int, TitleDefinition> g) => g.Key, (IGrouping<int, TitleDefinition> g) => g.Count());
	}

	public static List<TitleCandidateContext> FilterEligible(TitleDefinition definition, IEnumerable<TitleCandidateContext> candidates)
	{
		List<TitleCandidateContext> list = new List<TitleCandidateContext>();
		if (definition?.eligibility == null || candidates == null)
		{
			return list;
		}
		foreach (TitleCandidateContext candidate in candidates)
		{
			if (definition.eligibility.IsHardPass(candidate))
			{
				list.Add(candidate);
			}
		}
		return list;
	}

	public static string ResolveAppellation(string titleId, TitleAppellationContext context)
	{
		return Get(titleId)?.appellationId;
	}
}
}
