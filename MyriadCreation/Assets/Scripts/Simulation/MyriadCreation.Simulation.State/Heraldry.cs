using System;

namespace MyriadCreation.Simulation.State
{
public enum TinctureClass
{
	Metal,
	Colour,
	Stain
}

/// <summary>
/// 家族纹章（借鉴 Azgaar/Armoria 的“division + ordinary + tincture”模型，MIT）：
/// 由 familyId/名称/建国年确定性生成，只存符号组合，不依赖美术资源。
/// 颜色遵循“金属不压金属、颜色不压颜色”的纹章规则。
/// </summary>
public static class Heraldry
{
	private const string MetalOr = "#d4af37";
	private const string MetalArgent = "#e8e8e8";

	private static readonly string[] Divisions =
	{
		"perPale", "perFess", "perBend", "perChevron", "perCross", "perSaltire", "perPile", "gyronny"
	};

	private static readonly string[] DivisionWeights =
	{
		"perPale", "perPale", "perFess", "perFess", "perBend", "perChevron", "perCross",
		"perCross", "perSaltire", "perPile", "gyronny"
	};

	private static readonly string[] Ordinaries =
	{
		"bend", "chevron", "cross", "saltire", "fess", "pale", "chief", "pall", "bordure", "canton"
	};

	private static readonly string[] Shields =
	{
		"heater", "spanish", "kite", "round", "oval", "diamond", "gonfalon"
	};

	private static readonly string[] Metals = { MetalOr, MetalArgent };

	private static readonly string[] Colours =
	{
		"#b23a3a", "#2f5fa8", "#2f7d4f", "#6b3fa0", "#1b1b1b", "#7a4a26", "#2f7a7a", "#8a2f5a"
	};

	private static readonly string[] Stains = { "#6b2f3a", "#5a2f6b", "#6b4a2f" };

	public static void Ensure(FamilyNode family)
	{
		if (family == null || (!string.IsNullOrEmpty(family.coaPattern) && !string.IsNullOrEmpty(family.coaColors)))
		{
			return;
		}
		Generate(family.familyId, family.familyName, family.foundingYear, out string pattern, out string colors);
		family.coaPattern = pattern;
		family.coaColors = colors;
	}

	public static void Generate(int familyId, string name, int foundingYear, out string pattern, out string colors)
	{
		unchecked
		{
			int hash = familyId * 73856093 ^ foundingYear * 19349663 ^ (name ?? "").GetHashCode();
			int positive = hash & 0x7fffffff;

			string division = DivisionWeights[positive % DivisionWeights.Length];
			string ordinary = Ordinaries[(positive / 11) % Ordinaries.Length];
			string shield = Shields[(positive / 23) % Shields.Length];
			pattern = division + "|" + ordinary + "|" + shield;

			// 底场与主纹章必须一金属一颜色（rule of tincture）。
			bool fieldIsMetal = ((positive / 37) & 1) == 0;
			string field = fieldIsMetal
				? Metals[(positive / 3) % Metals.Length]
				: Colours[(positive / 5) % Colours.Length];
			string charge = fieldIsMetal
				? Colours[(positive / 7) % Colours.Length]
				: Metals[(positive / 13) % Metals.Length];
			string detail = Stains[(positive / 29) % Stains.Length];
			colors = field + "|" + charge + "|" + detail;
		}
	}

	public static TinctureClass Classify(string hex)
	{
		if (string.IsNullOrEmpty(hex))
		{
			return TinctureClass.Colour;
		}
		if (string.Equals(hex, MetalOr, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(hex, MetalArgent, StringComparison.OrdinalIgnoreCase))
		{
			return TinctureClass.Metal;
		}
		for (int i = 0; i < Stains.Length; i++)
		{
			if (string.Equals(Stains[i], hex, StringComparison.OrdinalIgnoreCase))
			{
				return TinctureClass.Stain;
			}
		}
		return TinctureClass.Colour;
	}
}
}
