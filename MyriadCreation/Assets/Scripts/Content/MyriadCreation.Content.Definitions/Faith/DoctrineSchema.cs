using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
/// <summary>教义参数声明：键必须在全局注册表里，值必须带来源或 TODO_M05。</summary>
[Serializable]
public sealed class DoctrineParameterDef
{
	public string key;

	public float value;

	/// <summary>来源（法条/文献/内容表）；留空时必须是 TODO_M05。</summary>
	public string source = "";
}

/// <summary>
/// 教义 schema：把平铺的 optionId 归入「组 → 档」。
/// 组与域/序由内容前缀派生（D56 已约定 doctrine_/ethics/ritual/institution/material 前缀），
/// 不在每个消费者里各写一遍映射；参数键走全局注册表，内容不得自造。
/// </summary>
public static class DoctrineSchema
{
	public const string TODO_M05 = "TODO_M05";

	private static readonly Dictionary<string, string> GroupDomain = new Dictionary<string, string>
	{
		["theology"] = "theology",
		["ethics"] = "justice",
		["marriage"] = "marriage",
		["funeral"] = "ritual",
		["clerical_gender"] = "office",
		["clerical_marriage"] = "office",
		["clerical_appointment"] = "office",
		["ritual_devotion"] = "ritual",
		["sacred_material"] = "economy"
	};

	private static readonly Dictionary<string, string> GroupKind = new Dictionary<string, string>
	{
		["theology"] = "category",
		["ethics"] = "category",
		["marriage"] = "sequence",
		["funeral"] = "category",
		["clerical_gender"] = "sequence",
		["clerical_marriage"] = "sequence",
		["clerical_appointment"] = "category",
		["ritual_devotion"] = "category",
		["sacred_material"] = "category"
	};

	private static readonly Dictionary<string, string> ParameterSpecs = new Dictionary<string, string>
	{
		// key → unit（值为空表示布尔/开关型参数）
		["marriage.consanguinity_degree"] = "degree",
		["marriage.spouse_count"] = "persons"
	};

	/// <summary>optionId → 组 id；未知前缀归入 theology（内容可显式声明 groupId 覆盖）。</summary>
	public static string GroupOf(string optionId)
	{
		if (string.IsNullOrEmpty(optionId))
		{
			return "theology";
		}
		if (optionId.StartsWith("doctrine_homosexuality_", StringComparison.Ordinal)
			|| optionId.StartsWith("doctrine_adultery_", StringComparison.Ordinal)
			|| optionId.StartsWith("doctrine_witchcraft_", StringComparison.Ordinal))
		{
			return "ethics";
		}
		if (optionId.StartsWith("doctrine_divorce_", StringComparison.Ordinal))
		{
			return "marriage";
		}
		if (optionId.StartsWith("doctrine_funeral_", StringComparison.Ordinal))
		{
			return "funeral";
		}
		if (optionId.StartsWith("doctrine_clerical_gender_", StringComparison.Ordinal))
		{
			return "clerical_gender";
		}
		if (optionId.StartsWith("doctrine_clerical_marriage_", StringComparison.Ordinal))
		{
			return "clerical_marriage";
		}
		if (optionId.StartsWith("doctrine_clerical_appointment_", StringComparison.Ordinal))
		{
			return "clerical_appointment";
		}
		if (optionId == "doctrine_ritual_feast"
			|| optionId == "doctrine_inner_journey"
			|| optionId == "doctrine_bhakti"
			|| optionId == "doctrine_esotericism"
			|| optionId == "doctrine_pilgrimage")
		{
			return "ritual_devotion";
		}
		if (optionId == "doctrine_holy_sites"
			|| optionId == "doctrine_temple_building"
			|| optionId == "doctrine_relics")
		{
			return "sacred_material";
		}
		return "theology";
	}

	public static bool IsKnownGroup(string groupId)
	{
		return !string.IsNullOrEmpty(groupId) && GroupDomain.ContainsKey(groupId);
	}

	public static string DomainOf(string groupId)
	{
		return groupId != null && GroupDomain.TryGetValue(groupId, out string domain) ? domain : "";
	}

	public static string KindOf(string groupId)
	{
		return groupId != null && GroupKind.TryGetValue(groupId, out string kind) ? kind : "";
	}

	public static bool IsParameterRegistered(string key)
	{
		return !string.IsNullOrEmpty(key) && ParameterSpecs.ContainsKey(key);
	}

	/// <summary>校验一条内容选项；返回 false 时 error 给出可定位原因（加载期记录，不静默吞掉）。</summary>
	public static bool ValidateOption(DoctrineOptionDef option, out string error)
	{
		error = "";
		if (option == null || string.IsNullOrEmpty(option.optionId))
		{
			error = "optionId is required.";
			return false;
		}
		string groupId = string.IsNullOrEmpty(option.groupId) ? GroupOf(option.optionId) : option.groupId;
		if (!IsKnownGroup(groupId))
		{
			error = $"unknown doctrine group '{groupId}'.";
			return false;
		}
		if (option.parameters != null)
		{
			for (int i = 0; i < option.parameters.Count; i++)
			{
				DoctrineParameterDef parameter = option.parameters[i];
				if (parameter == null || string.IsNullOrEmpty(parameter.key))
				{
					error = "parameter key is required.";
					return false;
				}
				if (!IsParameterRegistered(parameter.key))
				{
					error = $"unregistered doctrine parameter '{parameter.key}'.";
					return false;
				}
				if (string.IsNullOrWhiteSpace(parameter.source))
				{
					error = $"doctrine parameter '{parameter.key}' has no source; use '{TODO_M05}'.";
					return false;
				}
			}
		}
		return true;
	}
}
}
