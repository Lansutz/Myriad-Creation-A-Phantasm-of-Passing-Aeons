using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyriadCreation.Content.Definitions
{
public static class ReligionCatalog
{
	private static Dictionary<int, ReligionDef> _religions = new Dictionary<int, ReligionDef>();

	private static Color[] _palette;

	private static int _nextId = 1000;

	public static IReadOnlyDictionary<int, ReligionDef> All => _religions;

	public static void Load(List<ReligionDef> religions)
	{
		_religions.Clear();
		if (religions == null)
		{
			return;
		}
		foreach (ReligionDef religion in religions)
		{
			if (religion != null)
			{
				_religions[religion.religionId] = religion;
			}
		}
	}

	public static ReligionDef Get(int religionId)
	{
		if (!_religions.TryGetValue(religionId, out var value))
		{
			return null;
		}
		return value;
	}

	public static ReligionDef GetRoot(int religionId)
	{
		ReligionDef religionDef = Get(religionId);
		HashSet<int> hashSet = new HashSet<int>();
		while (religionDef != null && !religionDef.IsRoot && hashSet.Add(religionDef.religionId))
		{
			religionDef = Get(religionDef.parentReligionId);
		}
		return religionDef;
	}

	public static ReligionDef CreateTradition(int parentId, string name, string school, bool hasSuccession = false)
	{
		int num = _nextId++;
		ReligionDef religionDef = new ReligionDef
		{
			religionId = num,
			religionName = name,
			parentReligionId = parentId,
			schoolParentId = -1,
			nodeType = ReligionNodeType.Tradition,
			hasSuccession = hasSuccession,
			school = school
		};
		_religions[num] = religionDef;
		return religionDef;
	}

	public static void CreateRite(int religionId, string riteName, string riteFamily = "")
	{
		if (_religions.TryGetValue(religionId, out var value))
		{
			if (!value.rites.Contains(riteName))
			{
				value.rites.Add(riteName);
			}
			if (!string.IsNullOrEmpty(riteFamily))
			{
				value.riteFamily = riteFamily;
			}
		}
	}

	public static ReligionDef CreateSuccession(int parentId, string name, string headName, string rite, string riteFamily)
	{
		int num = _nextId++;
		ReligionDef religionDef = new ReligionDef
		{
			religionId = num,
			religionName = name,
			parentReligionId = parentId,
			schoolParentId = -1,
			nodeType = ReligionNodeType.Succession,
			hasSuccession = true,
			headName = headName,
			communionName = (Get(parentId)?.communionName ?? ""),
			riteFamily = riteFamily
		};
		if (!string.IsNullOrEmpty(rite))
		{
			religionDef.rites.Add(rite);
		}
		_religions[num] = religionDef;
		return religionDef;
	}

	public static ReligionDef CreateReligion(string name, string worldview, string founder, int schoolParentId = -1)
	{
		int num = _nextId++;
		ReligionDef religionDef = new ReligionDef
		{
			religionId = num,
			religionName = name,
			parentReligionId = -1,
			schoolParentId = schoolParentId,
			nodeType = ReligionNodeType.Religion,
			hasSuccession = true,
			worldview = worldview,
			founder = founder
		};
		_religions[num] = religionDef;
		return religionDef;
	}

	public static float GetDivergence(ReligionDef a, ReligionDef b)
	{
		if (a == null || b == null)
		{
			return 0f;
		}
		return GetDivergence(a.selectedDoctrines, b.selectedDoctrines);
	}

	/// <summary>按显式教义集合计算分歧度（运行时覆盖/合一后的口径）。</summary>
	public static float GetDivergence(IReadOnlyCollection<string> a, IReadOnlyCollection<string> b)
	{
		Dictionary<string, DoctrineOptionDef> options = new Dictionary<string, DoctrineOptionDef>();
		List<DoctrineOptionDef> all = DoctrinePool.GetAllOptions();
		for (int i = 0; i < all.Count; i++)
		{
			DoctrineOptionDef option = all[i];
			if (option != null && !string.IsNullOrEmpty(option.optionId))
			{
				options[option.optionId] = option;
			}
		}
		return DoctrineDivergence.Calculate(a, b, options);
	}

	/// <summary>默认民间信仰 id；内容未标时回退为无前置的根宗教，最后回退 -1。</summary>
	public static int GetDefaultFolkReligionId()
	{
		int fallbackFaith = -1;
		int fallbackReligion = -1;
		foreach (var pair in All)
		{
			ReligionDef def = pair.Value;
			if (def == null) continue;
			if (def.isFolkDefault) return pair.Key;
			if (def.nodeType == ReligionNodeType.Succession
				&& (def.requiredInnovations == null || def.requiredInnovations.Count == 0)
				&& fallbackFaith < 0)
			{
				fallbackFaith = pair.Key;
			}
			if (def.nodeType == ReligionNodeType.Religion && fallbackReligion < 0)
			{
				fallbackReligion = pair.Key;
			}
		}
		return fallbackFaith >= 0 ? fallbackFaith : fallbackReligion;
	}

	public static bool IsAvailable(int religionId, Func<int, bool> hasInnovation)
	{
		ReligionDef religionDef = Get(religionId);
		if (religionDef == null)
		{
			return true;
		}
		if (religionDef.requiredInnovations == null || religionDef.requiredInnovations.Count == 0)
		{
			return true;
		}
		if (hasInnovation == null)
		{
			return true;
		}
		foreach (int requiredInnovation in religionDef.requiredInnovations)
		{
			if (hasInnovation(requiredInnovation))
			{
				return true;
			}
		}
		return false;
	}

	public static int ResolveTraditionId(int religionId)
	{
		ReligionDef religionDef = Get(religionId);
		if (religionDef == null)
		{
			return -1;
		}
		if (religionDef.nodeType == ReligionNodeType.Tradition)
		{
			return religionDef.religionId;
		}
		int num = -1;
		foreach (ReligionDef value in _religions.Values)
		{
			if (value != null && value.nodeType == ReligionNodeType.Tradition && DescendsFrom(value, religionDef.religionId))
			{
				if (value.isMainstreamTradition)
				{
					return value.religionId;
				}
				if (num < 0)
				{
					num = value.religionId;
				}
			}
		}
		return num;
	}

	private static bool DescendsFrom(ReligionDef node, int ancestorId)
	{
		HashSet<int> hashSet = new HashSet<int>();
		int parentReligionId = node.parentReligionId;
		while (parentReligionId >= 0 && hashSet.Add(parentReligionId))
		{
			if (parentReligionId == ancestorId)
			{
				return true;
			}
			ReligionDef religionDef = Get(parentReligionId);
			if (religionDef == null)
			{
				return false;
			}
			parentReligionId = religionDef.parentReligionId;
		}
		return false;
	}

	public static Color GetColor(int religionId, ReligionMapLevel level)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		ReligionDef religionDef = Get(religionId);
		if (religionDef == null)
		{
			return Color.gray;
		}
		switch (level)
		{
		case ReligionMapLevel.Religion:
			return GetRoot(religionId)?.color ?? religionDef.color;
		case ReligionMapLevel.Succession:
			return religionDef.color;
		case ReligionMapLevel.Tradition:
		{
			float num = (float)((((!string.IsNullOrEmpty(religionDef.PrimaryRite)) ? religionDef.PrimaryRite : religionDef.school).GetHashCode() & 0x7FFFFFFF) % 360) / 360f;
			float num2;
			float num3;
			float num4;
			Color.RGBToHSV(religionDef.color, out num2, out num3, out num4);
			return Color.HSVToRGB(num, Mathf.Clamp01(num3 * 0.7f), Mathf.Clamp01(num4 * 0.9f));
		}
		default:
			return religionDef.color;
		}
	}

	public static void EnsureColors()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		if (_palette == null)
		{
			_palette = new Color[16];
			for (int i = 0; i < 16; i++)
			{
				_palette[i] = Color.HSVToRGB((float)i * 0.618f % 1f, 0.6f, 0.85f);
			}
		}
		foreach (ReligionDef value in _religions.Values)
		{
			if (value.color == Color.white && !value.IsRoot)
			{
				value.color = _palette[value.religionId % 16];
			}
		}
	}
}
}
