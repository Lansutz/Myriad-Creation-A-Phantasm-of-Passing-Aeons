using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Simulation.LawHost
{
public static class DefaultLawCatalog
{
	/// <summary>
	/// 法律组定义（唯一内容来源：StreamingAssets/Base/Laws/Laws.json，经 LawContentLoader 懒加载）。
	/// 文件缺失/损坏时返回 <see cref="CreateDefaultGroups"/> 的**最小回退集**（仅 taxation/military 核心法），
	/// 保证法律系统仍可启动；完整 9 组/32 法目录不再硬编码在生产代码里（JSON 是唯一权威来源）。
	/// 返回深拷贝，调用方可安全修改。
	/// </summary>
	public static List<LawGroupDef> CreateGroups()
	{
		if (LawContentLoader.EnsureLoaded() && LawContentLoader.HasContent)
		{
			return CloneGroups(LawContentLoader.Groups);
		}
		return CreateDefaultGroups();
	}

	/// <summary>
	/// JSON 缺失/损坏时的最小回退法律组：只保留 taxation（3 条）与 military（2 条）核心法律，
	/// 覆盖开局/政变映射最常引用的 lawId（tax_light/tax_standard/tax_heavy/militia/standing_army）。
	/// 这不是完整目录：完整 9 组/32 法请修复 StreamingAssets/Base/Laws/Laws.json。
	/// </summary>
	private static Dictionary<string, LawDef> _lawIndex;

	/// <summary>按 lawId 查定义（缓存）；找不到返回 null。用于人格效力/豁免判定。</summary>
	public static LawDef FindLaw(string lawId)
	{
		if (string.IsNullOrEmpty(lawId))
		{
			return null;
		}
		if (_lawIndex == null)
		{
			_lawIndex = new Dictionary<string, LawDef>();
			List<LawGroupDef> groups = CreateGroups();
			for (int g = 0; g < groups.Count; g++)
			{
				LawGroupDef group = groups[g];
				if (group?.laws == null) continue;
				for (int i = 0; i < group.laws.Count; i++)
				{
					LawDef law = group.laws[i];
					if (law != null && !string.IsNullOrEmpty(law.lawId))
					{
						_lawIndex[law.lawId] = law;
					}
				}
			}
		}
		return _lawIndex.TryGetValue(lawId, out LawDef value) ? value : null;
	}

	public static List<LawGroupDef> CreateDefaultGroups()
	{
		return new List<LawGroupDef>
		{
			new LawGroupDef
			{
				groupId = "taxation",
				name = "赋税",
				order = 10,
				laws = new List<LawDef>
				{
					Law("tax_light", "taxation", "轻赋", 1, 50f, 10f, 0.9f, 2f, 1f),
					Law("tax_standard", "taxation", "常赋", 2, 80f, 20f, 1.1f),
					Law("tax_heavy", "taxation", "重赋", 3, 120f, 30f, 1.4f, -4f, -2f, initiallyKnown: false, enactmentDays: 60)
				}
			},
			new LawGroupDef
			{
				groupId = "military",
				name = "军制",
				order = 20,
				laws = new List<LawDef>
				{
					Law("militia", "military", "民兵", 1, 60f, 15f, 1f, 1f, 0f, 1.2f, authority: ProposalAuthority.CentralInstitution),
					Law("standing_army", "military", "常备军", 2, 150f, 35f, 1f, -2f, 0f, 1.5f, initiallyKnown: false, enactmentDays: 60, authority: ProposalAuthority.CentralInstitution)
				}
			}
		};
	}

	/// <summary>
	/// 法令定义（唯一内容来源：StreamingAssets/Base/Laws/Laws.json，经 LawContentLoader 懒加载）。
	/// 缺失/损坏时返回 <see cref="CreateDefaultDecrees"/> 的最小回退（1 条核心法令）。
	/// </summary>
	public static List<DecreeDef> CreateDecrees()
	{
		if (LawContentLoader.EnsureLoaded() && LawContentLoader.HasContent)
		{
			return CloneDecrees(LawContentLoader.Decrees);
		}
		return CreateDefaultDecrees();
	}

	/// <summary>JSON 缺失/损坏时的最小回退法令：只保留一条无前置要求的核心法令，避免开局无法颁布任何法令。</summary>
	public static List<DecreeDef> CreateDefaultDecrees()
	{
		return new List<DecreeDef>
		{
			new DecreeDef
			{
				decreeId = "decree_tax_relief",
				name = "蠲免赋税",
				description = "临时减轻赋税，安抚民心。",
				costTreasury = 200f,
				costAuthority = 10f,
				durationDays = 180,
				cooldownDays = 540,
				requirements = new LawRequirement
				{
					minLegitimacy = 40f
				},
				effects = new LawEffect
				{
					taxMultiplier = 0.7f,
					unrestPerYear = -3f,
					stabilityPerYear = 2f
				}
			}
		};
	}

	public static Dictionary<string, LawDef> IndexLaws(IEnumerable<LawGroupDef> groups)
	{
		Dictionary<string, LawDef> dictionary = new Dictionary<string, LawDef>();
		if (groups == null)
		{
			return dictionary;
		}
		foreach (LawGroupDef group in groups)
		{
			if (group?.laws == null)
			{
				continue;
			}
			foreach (LawDef law in group.laws)
			{
				if (law != null && !string.IsNullOrEmpty(law.lawId))
				{
					dictionary[law.lawId] = law;
				}
			}
		}
		return dictionary;
	}

	public static Dictionary<string, DecreeDef> IndexDecrees(IEnumerable<DecreeDef> decrees)
	{
		Dictionary<string, DecreeDef> dictionary = new Dictionary<string, DecreeDef>();
		if (decrees == null)
		{
			return dictionary;
		}
		foreach (DecreeDef decree in decrees)
		{
			if (decree != null && !string.IsNullOrEmpty(decree.decreeId))
			{
				dictionary[decree.decreeId] = decree;
			}
		}
		return dictionary;
	}

	private static LawDef Law(string id, string groupId, string name, int tier, float costTreasury, float costPrestige, float tax = 1f, float stability = 0f, float legitimacy = 0f, float manpower = 1f, string description = "", bool initiallyKnown = true, int enactmentDays = 30, ProposalAuthority authority = ProposalAuthority.HeadOfState, CentralInstitution requiredInstitution = CentralInstitution.None)
	{
		return new LawDef
		{
			lawId = id,
			groupId = groupId,
			name = name,
			description = description,
			tier = tier,
			enactmentCostTreasury = costTreasury,
			enactmentCostPrestige = costPrestige,
			isInitiallyKnown = initiallyKnown,
			enactmentDays = enactmentDays,
			proposalAuthority = authority,
			requiredInstitution = requiredInstitution,
			effects = new LawEffect
			{
				taxMultiplier = tax,
				stabilityPerYear = stability,
				legitimacyPerYear = legitimacy,
				manpowerMultiplier = manpower
			}
		};
	}
	// ===== 已加载 JSON 定义的深拷贝（避免调用方修改共享缓存） =====

	public static List<LawGroupDef> CloneGroups(IReadOnlyList<LawGroupDef> source)
	{
		List<LawGroupDef> list = new List<LawGroupDef>();
		if (source == null)
		{
			return list;
		}
		foreach (LawGroupDef group in source)
		{
			if (group == null)
			{
				continue;
			}
			LawGroupDef copy = new LawGroupDef
			{
				groupId = group.groupId,
				name = group.name,
				order = group.order,
				description = group.description,
				fallbackLawId = group.fallbackLawId
			};
			if (group.laws != null)
			{
				foreach (LawDef law in group.laws)
				{
					if (law != null)
					{
						copy.laws.Add(CloneLaw(law));
					}
				}
			}
			list.Add(copy);
		}
		return list;
	}

	public static List<DecreeDef> CloneDecrees(IReadOnlyList<DecreeDef> source)
	{
		List<DecreeDef> list = new List<DecreeDef>();
		if (source == null)
		{
			return list;
		}
		foreach (DecreeDef decree in source)
		{
			if (decree != null)
			{
				list.Add(CloneDecree(decree));
			}
		}
		return list;
	}

	private static LawDef CloneLaw(LawDef source)
	{
		LawDef copy = new LawDef
		{
			lawId = source.lawId,
			groupId = source.groupId,
			name = source.name,
			description = source.description,
			tier = source.tier,
			requirements = CloneRequirements(source.requirements),
			effects = CloneEffect(source.effects),
			enactmentCostTreasury = source.enactmentCostTreasury,
			enactmentCostPrestige = source.enactmentCostPrestige,
			upkeepPerYear = source.upkeepPerYear,
			isInitiallyKnown = source.isInitiallyKnown,
			isFallback = source.isFallback,
			normKind = source.normKind,
			scope = source.scope,
			precedence = source.precedence,
			issuerCharacterId = source.issuerCharacterId,
			enactmentDays = source.enactmentDays,
			proposalAuthority = source.proposalAuthority,
			requiredInstitution = source.requiredInstitution,
			appliesTo = source.appliesTo,
			exemptIdentities = source.exemptIdentities
		};
		if (source.overrides != null)
		{
			copy.overrides.AddRange(source.overrides);
		}
		if (source.excludesLawIds != null)
		{
			copy.excludesLawIds.AddRange(source.excludesLawIds);
		}
		return copy;
	}

	private static DecreeDef CloneDecree(DecreeDef source)
	{
		DecreeDef copy = new DecreeDef
		{
			decreeId = source.decreeId,
			name = source.name,
			description = source.description,
			requirements = CloneRequirements(source.requirements),
			effects = CloneEffect(source.effects),
			costTreasury = source.costTreasury,
			costAuthority = source.costAuthority,
			durationDays = source.durationDays,
			cooldownDays = source.cooldownDays
		};
		if (source.excludesDecreeIds != null)
		{
			copy.excludesDecreeIds.AddRange(source.excludesDecreeIds);
		}
		return copy;
	}

	private static LawRequirement CloneRequirements(LawRequirement source)
	{
		LawRequirement lawRequirement = new LawRequirement();
		if (source == null)
		{
			return lawRequirement;
		}
		lawRequirement.minTreasury = source.minTreasury;
		lawRequirement.minLegitimacy = source.minLegitimacy;
		lawRequirement.minCentralization = source.minCentralization;
		if (source.requiredLawIds != null)
		{
			lawRequirement.requiredLawIds.AddRange(source.requiredLawIds);
		}
		if (source.requiredInnovationIds != null)
		{
			lawRequirement.requiredInnovationIds.AddRange(source.requiredInnovationIds);
		}
		if (source.requiredDecreeIds != null)
		{
			lawRequirement.requiredDecreeIds.AddRange(source.requiredDecreeIds);
		}
		lawRequirement.requiredPolityForm = source.requiredPolityForm;
		return lawRequirement;
	}

	private static LawEffect CloneEffect(LawEffect source)
	{
		LawEffect lawEffect = new LawEffect();
		if (source == null)
		{
			return lawEffect;
		}
		lawEffect.taxMultiplier = source.taxMultiplier;
		lawEffect.stabilityPerYear = source.stabilityPerYear;
		lawEffect.legitimacyPerYear = source.legitimacyPerYear;
		lawEffect.manpowerMultiplier = source.manpowerMultiplier;
		lawEffect.unrestPerYear = source.unrestPerYear;
		return lawEffect;
	}

}
}
