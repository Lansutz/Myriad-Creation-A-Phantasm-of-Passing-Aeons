using System;
using UnityEngine;

namespace MyriadCreation.Simulation.State
{
/// <summary>
/// 家族纹章（借鉴程序化纹章生成思路）：由 familyId/名称/建国年确定性生成，
/// 只存 pattern + 三色，不依赖美术资源；家族创建时写入，旧档缺失时按需补。
/// </summary>
public static class Heraldry
{
	private static readonly string[] Patterns =
	{
		"bend", "chevron", "cross", "saltire", "paly", "quarterly", "fess", "pall"
	};

	private static readonly string[] Tinctures =
	{
		"#b23a3a", "#2f5fa8", "#2f7d4f", "#6b3fa0", "#c08a2e",
		"#1b1b1b", "#f2f2f2", "#7a4a26", "#2f7a7a", "#8a2f5a"
	};

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
			pattern = Patterns[positive % Patterns.Length];
			string field = Tinctures[(positive / 7) % Tinctures.Length];
			string charge = Tinctures[(positive / 13 + 3) % Tinctures.Length];
			string detail = Tinctures[(positive / 29 + 5) % Tinctures.Length];
			colors = field + "|" + charge + "|" + detail;
		}
	}
}
}
