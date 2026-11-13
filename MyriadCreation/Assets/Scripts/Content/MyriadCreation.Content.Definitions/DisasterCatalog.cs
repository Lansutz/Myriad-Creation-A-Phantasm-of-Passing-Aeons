using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
/// <summary>灾害内容表行（JSON 形态）。</summary>
[Serializable]
public sealed class DisasterContentDef
{
	/// <summary>DisasterType 的 int 值。</summary>
	public int type;

	public string name = "";

	/// <summary>DisasterCategory 的 int 值。</summary>
	public int category;

	public float baseFrequency;

	public int minDuration;

	public int maxDuration;

	public float baseSeverity;
}

/// <summary>灾害外提表（P2.4）：内容优先，未加载时代码兜底。</summary>
public static class DisasterCatalog
{
	private static Dictionary<DisasterType, DisasterDef> _defs;

	public static bool IsLoaded => _defs != null && _defs.Count > 0;

	public static IReadOnlyDictionary<DisasterType, DisasterDef> All => _defs;

	public static void LoadFromContent(IEnumerable<DisasterContentDef> defs)
	{
		_defs ??= new Dictionary<DisasterType, DisasterDef>();
		if (defs == null)
		{
			return;
		}
		foreach (DisasterContentDef def in defs)
		{
			if (def == null || !Enum.IsDefined(typeof(DisasterType), def.type))
			{
				continue;
			}
			DisasterType type = (DisasterType)def.type;
			int minDuration = Math.Max(1, def.minDuration);
			int maxDuration = Math.Max(minDuration, def.maxDuration);
			_defs[type] = new DisasterDef
			{
				type = type,
				name = def.name ?? "",
				category = (DisasterCategory)ClampCategory(def.category),
				baseFrequency = def.baseFrequency,
				minDuration = minDuration,
				maxDuration = maxDuration,
				baseSeverity = def.baseSeverity
			};
		}
	}

	public static void ResetContent()
	{
		_defs = null;
	}

	private static int ClampCategory(int value)
	{
		return value < 0 || value > (int)DisasterCategory.Anthropogenic ? (int)DisasterCategory.Meteorological : value;
	}
}
}
