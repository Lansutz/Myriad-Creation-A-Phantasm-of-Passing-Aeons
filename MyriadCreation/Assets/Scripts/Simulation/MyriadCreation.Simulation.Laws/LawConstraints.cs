using System;
using System.Collections.Generic;
using System.Linq;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Laws
{
public static class LawConstraints
{
	public class LawTemplate
	{
		public string name;

		public string description;

		public string category;

		public Dictionary<LawDimension, int> recommendedPrimary = new Dictionary<LawDimension, int>();

		public Dictionary<string, int> recommendedSubOptions = new Dictionary<string, int>();

		public InheritanceLaw recommendedInheritance;

		public TitleDistribution recommendedTitleDist;

		public DomainDistribution recommendedDomainDist;

		public LawSource? recommendedLegalSource;

		public FaithStatus? recommendedFaith;

		public FaithLawPriority? recommendedFaithLawPriority;

		public HeterodoxPolicy? recommendedHeterodoxPolicy;

		public FaithTitle? recommendedFaithTitle;

		public Dictionary<string, int> titleSeats = new Dictionary<string, int>();

		public Dictionary<string, string> titleVariants = new Dictionary<string, string>();

		public float generationWeight = 1f;
	}

	public enum LawDomain
	{
		Institutional,
		Military,
		Faith,
		Governance,
		LegalTradition
	}

	public enum LawFamily
	{
		SupremePower,
		CentralPower,
		LocalPower,
		RealmStructure,
		Military,
		FaithGovernance,
		Household,
		Economy,
		Frontier,
		LegalSource
	}

	public enum LawGroup
	{
		SupremeSovereigntyGroup,
		SupremeSuccessionGroup,
		SupremeScopeGroup,
		TitleDistributionGroup,
		DomainDistributionGroup,
		CentralExistenceGroup,
		CentralSuccessionGroup,
		CentralInstitutionGroup,
		LocalSuccessionGroup,
		LocalScopeGroup,
		RealmStructureGroup,
		MilitaryRecruitmentGroup,
		MilitaryOrganizationGroup,
		MilitaryManagementGroup,
		EstablishedFaithGroup,
		FaithLawPriorityGroup,
		HeterodoxPolicyGroup,
		FaithTitleGroup,
		HouseholdGroup,
		LandTenureGroup,
		MarriageStatusGroup,
		ClassCitizenshipGroup,
		NameRegistrationGroup,
		MeasuresGroup,
		TradeControlGroup,
		NationalTaxGroup,
		LocalTaxGroup,
		PassageGroup,
		BorderMarketGroup,
		LegalSourceGroup
	}

	public enum LawDimension
	{
		SupremeSovereignty,
		SupremeSuccession,
		SupremeScope,
		CentralExistence,
		CentralSuccession,
		CentralInstitution,
		LocalSuccession,
		LocalScope,
		SpatialStructure
	}

	public enum SupremeSovereignty
	{
		Monarchy,
		Republic
	}

	public enum TitleDistribution
	{
		Exclusive,
		FamilyShared,
		HasTitle,
		NoTitle
	}

	public enum DomainDistribution
	{
		Exclusive,
		Partible,
		Appanage
	}

	public enum CentralExistence
	{
		None,
		Established
	}

	public enum FaithStatus
	{
		None,
		Established
	}

	public enum FaithLawPriority
	{
		ReligiousFirst,
		SecularFirst,
		Parallel
	}

	public enum HeterodoxPolicy
	{
		Tolerant,
		Restricted,
		Prohibited
	}

	public enum FaithTitle
	{
		None,
		Granted
	}

	public class SubOptionGroup
	{
		public string groupName;

		public string parentDimension;

		public int parentValue;

		public List<SubOption> options = new List<SubOption>();

		public bool isActive;

		public bool isOptional;
	}

	public class SubOption
	{
		public string name;

		public int value;

		public string description;
	}

	public static List<SubOptionGroup> GetSubOptionGroups(LawDimension dimension)
	{
		List<SubOptionGroup> list = new List<SubOptionGroup>();
		switch (dimension)
		{
		case LawDimension.SupremeSuccession:
			list.Add(new SubOptionGroup
			{
				groupName = "继承法（四轴）",
				parentDimension = "SupremeSuccession",
				parentValue = 0,
				options = new List<SubOption>
				{
					new SubOption
					{
						name = "范围轴",
						value = 0,
						description = "限于本族 / 血亲不论姓氏"
					},
					new SubOption
					{
						name = "支系轴",
						value = 1,
						description = "长支优先 / 兄终弟及"
					},
					new SubOption
					{
						name = "性别轴",
						value = 2,
						description = "仅男/男优/平等/女优/仅女"
					},
					new SubOption
					{
						name = "长幼轴",
						value = 3,
						description = "年长者先 / 年幼者先"
					}
				}
			});
			list.Add(new SubOptionGroup
			{
				groupName = "选举范围（身份）",
				parentDimension = "SupremeSuccession",
				parentValue = 1,
				options = new List<SubOption>
				{
					new SubOption
					{
						name = "全体公民",
						value = 0,
						description = "所有公民直接投票（雅典式）"
					},
					new SubOption
					{
						name = "公民大会",
						value = 1,
						description = "公民大会代表选举"
					},
					new SubOption
					{
						name = "部落联盟",
						value = 2,
						description = "各部落代表选举"
					},
					new SubOption
					{
						name = "自由民",
						value = 3,
						description = "全体自由民选举"
					}
				}
			});
			list.Add(new SubOptionGroup
			{
				groupName = "性别资格",
				parentDimension = "SupremeSuccession",
				parentValue = 1,
				options = new List<SubOption>
				{
					new SubOption
					{
						name = "仅男性",
						value = 0,
						description = "只有男性可被选举"
					},
					new SubOption
					{
						name = "男性优先",
						value = 1,
						description = "男性优先，女性可被选举"
					},
					new SubOption
					{
						name = "男女平等",
						value = 2,
						description = "男女平等选举权"
					},
					new SubOption
					{
						name = "女性优先",
						value = 3,
						description = "女性优先"
					},
					new SubOption
					{
						name = "仅女性",
						value = 4,
						description = "只有女性可被选举"
					}
				}
			});
			list.Add(new SubOptionGroup
			{
				groupName = "推举主体（身份）",
				parentDimension = "SupremeSuccession",
				parentValue = 2,
				options = new List<SubOption>
				{
					new SubOption
					{
						name = "贵族推举",
						value = 0,
						description = "贵族阶层推举（波兰式）"
					},
					new SubOption
					{
						name = "元老院推举",
						value = 1,
						description = "元老院推举（罗马式）"
					},
					new SubOption
					{
						name = "选帝侯",
						value = 2,
						description = "选帝侯推举（神罗式）"
					},
					new SubOption
					{
						name = "军队推举",
						value = 3,
						description = "军队推举（罗马禁卫军式）"
					},
					new SubOption
					{
						name = "教会推举",
						value = 4,
						description = "教阶推举（教皇选举式）"
					}
				}
			});
			list.Add(new SubOptionGroup
			{
				groupName = "性别资格",
				parentDimension = "SupremeSuccession",
				parentValue = 2,
				options = new List<SubOption>
				{
					new SubOption
					{
						name = "仅男性",
						value = 0,
						description = "只有男性可被推举"
					},
					new SubOption
					{
						name = "男性优先",
						value = 1,
						description = "男性优先，女性可被推举"
					},
					new SubOption
					{
						name = "男女平等",
						value = 2,
						description = "男女平等被推举权"
					},
					new SubOption
					{
						name = "女性优先",
						value = 3,
						description = "女性优先"
					},
					new SubOption
					{
						name = "仅女性",
						value = 4,
						description = "只有女性可被推举"
					}
				}
			});
			break;
		case LawDimension.SupremeScope:
			list.Add(new SubOptionGroup
			{
				groupName = "头衔分配",
				parentDimension = "SupremeScope",
				parentValue = -1,
				options = new List<SubOption>
				{
					new SubOption
					{
						name = "独享（君主制）",
						value = 0,
						description = "一人独占最高头衔——君主制"
					},
					new SubOption
					{
						name = "家族共享（君主制）",
						value = 1,
						description = "法兰克人式，家族共享最高头衔——君主制"
					},
					new SubOption
					{
						name = "有最高头衔（共和制）",
						value = 2,
						description = "有最高头衔如执政官/独裁官——共和制"
					},
					new SubOption
					{
						name = "无最高头衔（共和制）",
						value = 3,
						description = "无最高头衔，中央机构为主导如元老院——共和制"
					}
				}
			});
			list.Add(new SubOptionGroup
			{
				groupName = "领地分配",
				parentDimension = "SupremeScope",
				parentValue = -1,
				options = new List<SubOption>
				{
					new SubOption
					{
						name = "独享",
						value = 0,
						description = "一人独占所有领地"
					},
					new SubOption
					{
						name = "均分",
						value = 1,
						description = "诸子均分领地"
					},
					new SubOption
					{
						name = "采邑",
						value = 2,
						description = "嫡长子继承核心，其余分封采邑"
					}
				}
			});
			break;
		case LawDimension.CentralInstitution:
			list.Add(new SubOptionGroup
			{
				groupName = "议会构成",
				parentDimension = "CentralInstitution",
				parentValue = 2,
				options = new List<SubOption>
				{
					new SubOption
					{
						name = "一院制",
						value = 0,
						description = "单一代表院（雅典公民大会/罗马元老院）"
					},
					new SubOption
					{
						name = "两院制",
						value = 1,
						description = "贵族院+平民院（英国上下院）"
					},
					new SubOption
					{
						name = "等级会议",
						value = 2,
						description = "按等级分庭（法国三级会议/神罗帝国议会）"
					}
				}
			});
			list.Add(new SubOptionGroup
			{
				groupName = "长老构成",
				parentDimension = "CentralInstitution",
				parentValue = 3,
				options = new List<SubOption>
				{
					new SubOption
					{
						name = "氏族长老",
						value = 0,
						description = "各氏族长老组成"
					},
					new SubOption
					{
						name = "贵族长老",
						value = 1,
						description = "贵族阶层长老组成"
					},
					new SubOption
					{
						name = "功勋长老",
						value = 2,
						description = "按功勋选拔的长老"
					}
				}
			});
			list.Add(new SubOptionGroup
			{
				groupName = "官僚体系",
				parentDimension = "CentralInstitution",
				parentValue = 4,
				options = new List<SubOption>
				{
					new SubOption
					{
						name = "三省六部",
						value = 0,
						description = "中式三省六部制"
					},
					new SubOption
					{
						name = "三公九卿",
						value = 1,
						description = "中式三公九卿制"
					},
					new SubOption
					{
						name = "部院制",
						value = 2,
						description = "近代部院制"
					}
				}
			});
			break;
		case LawDimension.LocalSuccession:
			list.Add(new SubOptionGroup
			{
				groupName = "任命主体",
				parentDimension = "LocalSuccession",
				parentValue = 0,
				options = new List<SubOption>
				{
					new SubOption
					{
						name = "中央派任",
						value = 0,
						description = "中央派任流官/总督（郡县/行省）"
					},
					new SubOption
					{
						name = "教区委任",
						value = 1,
						description = "教区委任（教区体系）"
					},
					new SubOption
					{
						name = "军事任免",
						value = 2,
						description = "军事上级任免（军管区）"
					}
				}
			});
			list.Add(new SubOptionGroup
			{
				groupName = "领有身份",
				parentDimension = "LocalSuccession",
				parentValue = 3,
				options = new List<SubOption>
				{
					new SubOption
					{
						name = "世袭封臣",
						value = 0,
						description = "封建契约——异姓功臣"
					},
					new SubOption
					{
						name = "宗室采邑",
						value = 1,
						description = "分封宗亲——西周/阿拔斯"
					},
					new SubOption
					{
						name = "军功领邑",
						value = 2,
						description = "战功封赏"
					}
				}
			});
			list.Add(new SubOptionGroup
			{
				groupName = "选举范围",
				parentDimension = "LocalSuccession",
				parentValue = 1,
				options = new List<SubOption>
				{
					new SubOption
					{
						name = "本地公民",
						value = 0,
						description = "本地公民选举"
					},
					new SubOption
					{
						name = "部落推举",
						value = 1,
						description = "部落长老推举"
					},
					new SubOption
					{
						name = "自治市议会",
						value = 2,
						description = "自治市议会选举"
					}
				}
			});
			list.Add(new SubOptionGroup
			{
				groupName = "考试体系",
				parentDimension = "LocalSuccession",
				parentValue = 2,
				options = new List<SubOption>
				{
					new SubOption
					{
						name = "科举制",
						value = 0,
						description = "中式科举——分科考试选拔文官"
					},
					new SubOption
					{
						name = "文官考试",
						value = 1,
						description = "近代文官考试制度"
					},
					new SubOption
					{
						name = "荐举制",
						value = 2,
						description = "地方荐举+中央考核（察举制）"
					},
					new SubOption
					{
						name = "九品中正",
						value = 3,
						description = "中正官评定品级（魏晋式）"
					}
				}
			});
			list.Add(new SubOptionGroup
			{
				groupName = "特许类型",
				parentDimension = "LocalSuccession",
				parentValue = 4,
				isOptional = true,
				options = new List<SubOption>
				{
					new SubOption
					{
						name = "自由市",
						value = 0,
						description = "完全自治自由市"
					},
					new SubOption
					{
						name = "帝国自由市",
						value = 1,
						description = "直属于最高权力的自由市"
					},
					new SubOption
					{
						name = "特许市镇",
						value = 2,
						description = "有限自治特许市镇"
					}
				}
			});
			break;
		}
		return list;
	}

	public static List<SubOptionGroup> GetActiveSubOptionGroups(LawDimension dimension, LawComposition comp)
	{
		List<SubOptionGroup> subOptionGroups = GetSubOptionGroups(dimension);
		int currentPrimary = GetCurrentPrimary(dimension, comp);
		if (dimension == LawDimension.CentralInstitution && comp.centralExistence != CentralExistence.Established)
		{
			return new List<SubOptionGroup>();
		}
		List<SubOptionGroup> list = new List<SubOptionGroup>();
		foreach (SubOptionGroup item in subOptionGroups)
		{
			if (item.parentValue == -1)
			{
				item.isActive = true;
				list.Add(item);
				continue;
			}
			item.isActive = item.parentValue == currentPrimary;
			if (item.isActive)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static List<LawTemplate> GetTemplates()
	{
		return new List<LawTemplate>
		{
			new LawTemplate
			{
				name = "官僚君主国",
				description = "世袭君主+全能+有常设+中央任命+官僚中枢+中央派任+完全直辖+单一制",
				category = "君主制",
				recommendedPrimary = new Dictionary<LawDimension, int>
				{
					{
						LawDimension.SupremeSuccession,
						0
					},
					{
						LawDimension.SupremeScope,
						0
					},
					{
						LawDimension.CentralExistence,
						1
					},
					{
						LawDimension.CentralSuccession,
						0
					},
					{
						LawDimension.CentralInstitution,
						4
					},
					{
						LawDimension.LocalSuccession,
						0
					},
					{
						LawDimension.LocalScope,
						3
					},
					{
						LawDimension.SpatialStructure,
						0
					}
				},
				recommendedInheritance = InheritanceLaw.ChinesePartible(),
				recommendedTitleDist = TitleDistribution.Exclusive,
				recommendedDomainDist = DomainDistribution.Appanage,
				recommendedLegalSource = LawSource.Statutory,
				recommendedSubOptions = new Dictionary<string, int>
				{
					{ "任命主体", 0 },
					{ "官僚体系", 0 }
				}
			},
			new LawTemplate
			{
				name = "封建君主国",
				description = "世袭君主+惯例约束+有常设+官位世袭+王庭+世袭封臣+全权自治+盟约式",
				category = "君主制",
				recommendedPrimary = new Dictionary<LawDimension, int>
				{
					{
						LawDimension.SupremeSuccession,
						0
					},
					{
						LawDimension.SupremeScope,
						2
					},
					{
						LawDimension.CentralExistence,
						1
					},
					{
						LawDimension.CentralSuccession,
						3
					},
					{
						LawDimension.CentralInstitution,
						1
					},
					{
						LawDimension.LocalSuccession,
						3
					},
					{
						LawDimension.LocalScope,
						0
					},
					{
						LawDimension.SpatialStructure,
						2
					}
				},
				recommendedInheritance = InheritanceLaw.Primogeniture(),
				recommendedTitleDist = TitleDistribution.FamilyShared,
				recommendedDomainDist = DomainDistribution.Partible,
				recommendedLegalSource = LawSource.Customary,
				recommendedSubOptions = new Dictionary<string, int> { { "领有身份", 0 } }
			},
			new LawTemplate
			{
				name = "军政推举式",
				description = "僭主制主导+推举制第1次要+血缘世袭第2次要+有常设+军队推举+议事会+军事委员会",
				category = "君主制",
				recommendedPrimary = new Dictionary<LawDimension, int>
				{
					{
						LawDimension.SupremeSuccession,
						3
					},
					{
						LawDimension.SupremeScope,
						0
					},
					{
						LawDimension.CentralExistence,
						1
					},
					{
						LawDimension.CentralSuccession,
						0
					},
					{
						LawDimension.CentralInstitution,
						6
					},
					{
						LawDimension.LocalSuccession,
						0
					},
					{
						LawDimension.LocalScope,
						1
					},
					{
						LawDimension.SpatialStructure,
						0
					}
				},
				recommendedTitleDist = TitleDistribution.Exclusive,
				recommendedDomainDist = DomainDistribution.Exclusive,
				recommendedLegalSource = LawSource.Common,
				recommendedSubOptions = new Dictionary<string, int> { { "任命主体", 2 } }
			},
			new LawTemplate
			{
				name = "古典民主共和",
				description = "公民大会直接选举+共议制约+有常设+选举+一院议会+地方选举+全权自治+单一制",
				category = "共和制",
				recommendedPrimary = new Dictionary<LawDimension, int>
				{
					{
						LawDimension.SupremeSuccession,
						1
					},
					{
						LawDimension.SupremeScope,
						3
					},
					{
						LawDimension.CentralExistence,
						1
					},
					{
						LawDimension.CentralSuccession,
						1
					},
					{
						LawDimension.CentralInstitution,
						2
					},
					{
						LawDimension.LocalSuccession,
						1
					},
					{
						LawDimension.LocalScope,
						0
					},
					{
						LawDimension.SpatialStructure,
						0
					}
				},
				recommendedTitleDist = TitleDistribution.Exclusive,
				recommendedDomainDist = DomainDistribution.Exclusive,
				recommendedLegalSource = LawSource.Common,
				recommendedSubOptions = new Dictionary<string, int>
				{
					{ "选举范围", 0 },
					{ "议会构成", 0 }
				}
			},
			new LawTemplate
			{
				name = "贵族共和",
				description = "推举君主+法理受限+有常设+恩庇推举+议事会+中央任命+征税司法+联合式",
				category = "共和制",
				recommendedPrimary = new Dictionary<LawDimension, int>
				{
					{
						LawDimension.SupremeSuccession,
						2
					},
					{
						LawDimension.SupremeScope,
						1
					},
					{
						LawDimension.CentralExistence,
						1
					},
					{
						LawDimension.CentralSuccession,
						1
					},
					{
						LawDimension.CentralInstitution,
						2
					},
					{
						LawDimension.LocalSuccession,
						0
					},
					{
						LawDimension.LocalScope,
						1
					},
					{
						LawDimension.SpatialStructure,
						1
					}
				},
				recommendedTitleDist = TitleDistribution.Exclusive,
				recommendedDomainDist = DomainDistribution.Exclusive,
				recommendedLegalSource = LawSource.Common,
				recommendedSubOptions = new Dictionary<string, int>
				{
					{ "推举主体", 1 },
					{ "议会构成", 0 },
					{ "任命主体", 0 }
				}
			},
			new LawTemplate
			{
				name = "部落联盟",
				description = "推举君主+共议制约+无常设中央+部落推举+地方世袭+全权自治+盟约式",
				category = "混合",
				recommendedPrimary = new Dictionary<LawDimension, int>
				{
					{
						LawDimension.SupremeSuccession,
						2
					},
					{
						LawDimension.SupremeScope,
						3
					},
					{
						LawDimension.CentralExistence,
						0
					},
					{
						LawDimension.CentralSuccession,
						1
					},
					{
						LawDimension.CentralInstitution,
						0
					},
					{
						LawDimension.LocalSuccession,
						3
					},
					{
						LawDimension.LocalScope,
						0
					},
					{
						LawDimension.SpatialStructure,
						2
					}
				},
				recommendedTitleDist = TitleDistribution.FamilyShared,
				recommendedDomainDist = DomainDistribution.Partible,
				recommendedLegalSource = LawSource.Customary,
				recommendedSubOptions = new Dictionary<string, int>
				{
					{ "推举主体", 0 },
					{ "领有身份", 1 }
				}
			}
		};
	}

	public static bool AllowsSecondary(LawDimension dimension)
	{
		return dimension == LawDimension.SupremeSuccession;
	}

	public static int GetMaxSecondaryCount(float administrativeCapacity = 0.5f)
	{
		if (administrativeCapacity < 0.3f)
		{
			return 1;
		}
		if (administrativeCapacity < 0.7f)
		{
			return 2;
		}
		return 3;
	}

	public static LawFamily GetFamily(LawGroup group)
	{
		switch (group)
		{
		case LawGroup.SupremeSovereigntyGroup:
		case LawGroup.SupremeSuccessionGroup:
		case LawGroup.SupremeScopeGroup:
		case LawGroup.TitleDistributionGroup:
		case LawGroup.DomainDistributionGroup:
			return LawFamily.SupremePower;
		case LawGroup.CentralExistenceGroup:
		case LawGroup.CentralSuccessionGroup:
		case LawGroup.CentralInstitutionGroup:
			return LawFamily.CentralPower;
		case LawGroup.LocalSuccessionGroup:
		case LawGroup.LocalScopeGroup:
			return LawFamily.LocalPower;
		case LawGroup.RealmStructureGroup:
			return LawFamily.RealmStructure;
		case LawGroup.MilitaryRecruitmentGroup:
		case LawGroup.MilitaryOrganizationGroup:
		case LawGroup.MilitaryManagementGroup:
			return LawFamily.Military;
		case LawGroup.EstablishedFaithGroup:
		case LawGroup.FaithLawPriorityGroup:
		case LawGroup.HeterodoxPolicyGroup:
		case LawGroup.FaithTitleGroup:
			return LawFamily.FaithGovernance;
		case LawGroup.HouseholdGroup:
		case LawGroup.LandTenureGroup:
		case LawGroup.MarriageStatusGroup:
		case LawGroup.ClassCitizenshipGroup:
		case LawGroup.NameRegistrationGroup:
			return LawFamily.Household;
		case LawGroup.MeasuresGroup:
		case LawGroup.TradeControlGroup:
		case LawGroup.NationalTaxGroup:
		case LawGroup.LocalTaxGroup:
			return LawFamily.Economy;
		case LawGroup.PassageGroup:
		case LawGroup.BorderMarketGroup:
			return LawFamily.Frontier;
		case LawGroup.LegalSourceGroup:
			return LawFamily.LegalSource;
		default:
			return LawFamily.SupremePower;
		}
	}

	public static LawDomain GetDomain(LawFamily family)
	{
		switch (family)
		{
		case LawFamily.SupremePower:
		case LawFamily.CentralPower:
		case LawFamily.LocalPower:
		case LawFamily.RealmStructure:
			return LawDomain.Institutional;
		case LawFamily.Military:
			return LawDomain.Military;
		case LawFamily.FaithGovernance:
			return LawDomain.Faith;
		case LawFamily.Household:
		case LawFamily.Economy:
		case LawFamily.Frontier:
			return LawDomain.Governance;
		case LawFamily.LegalSource:
			return LawDomain.LegalTradition;
		default:
			return LawDomain.Institutional;
		}
	}

	public static LawDomain GetDomain(LawGroup group)
	{
		return GetDomain(GetFamily(group));
	}

	public static LawGroup MapDimensionToGroup(LawDimension dim)
	{
		return dim switch
		{
			LawDimension.SupremeSovereignty => LawGroup.SupremeSovereigntyGroup, 
			LawDimension.SupremeSuccession => LawGroup.SupremeSuccessionGroup, 
			LawDimension.SupremeScope => LawGroup.SupremeScopeGroup, 
			LawDimension.CentralExistence => LawGroup.CentralExistenceGroup, 
			LawDimension.CentralSuccession => LawGroup.CentralSuccessionGroup, 
			LawDimension.CentralInstitution => LawGroup.CentralInstitutionGroup, 
			LawDimension.LocalSuccession => LawGroup.LocalSuccessionGroup, 
			LawDimension.LocalScope => LawGroup.LocalScopeGroup, 
			LawDimension.SpatialStructure => LawGroup.RealmStructureGroup, 
			_ => LawGroup.SupremeSovereigntyGroup, 
		};
	}

	public static LawDomain GetDomain(LawDimension dim)
	{
		return GetDomain(MapDimensionToGroup(dim));
	}

	public static string GetDomainName(LawDomain domain)
	{
		return domain switch
		{
			LawDomain.Institutional => "建制", 
			LawDomain.Military => "军事", 
			LawDomain.Faith => "信仰", 
			LawDomain.Governance => "境内治理", 
			LawDomain.LegalTradition => "法源", 
			_ => domain.ToString(), 
		};
	}

	public static string GetFamilyName(LawFamily family)
	{
		return family switch
		{
			LawFamily.SupremePower => "最高权力", 
			LawFamily.CentralPower => "中央权力", 
			LawFamily.LocalPower => "地方权力", 
			LawFamily.RealmStructure => "结构", 
			LawFamily.Military => "军政", 
			LawFamily.FaithGovernance => "信仰治理", 
			LawFamily.Household => "身份", 
			LawFamily.Economy => "财货", 
			LawFamily.Frontier => "边域", 
			LawFamily.LegalSource => "法源", 
			_ => family.ToString(), 
		};
	}

	public static string GetGroupName(LawGroup group)
	{
		return group switch
		{
			LawGroup.SupremeSovereigntyGroup => "最高归属", 
			LawGroup.SupremeSuccessionGroup => "最高交接", 
			LawGroup.SupremeScopeGroup => "最高范围", 
			LawGroup.TitleDistributionGroup => "头衔分配", 
			LawGroup.DomainDistributionGroup => "领地分配", 
			LawGroup.CentralExistenceGroup => "中央有无", 
			LawGroup.CentralSuccessionGroup => "中央交接", 
			LawGroup.CentralInstitutionGroup => "中央机构", 
			LawGroup.LocalSuccessionGroup => "地方交接", 
			LawGroup.LocalScopeGroup => "地方职能", 
			LawGroup.RealmStructureGroup => "央地结构", 
			LawGroup.MilitaryRecruitmentGroup => "兵役", 
			LawGroup.MilitaryOrganizationGroup => "军队组织", 
			LawGroup.MilitaryManagementGroup => "军事管理", 
			LawGroup.EstablishedFaithGroup => "国教地位", 
			LawGroup.FaithLawPriorityGroup => "圣俗优先", 
			LawGroup.HeterodoxPolicyGroup => "异教政策", 
			LawGroup.FaithTitleGroup => "国教头衔", 
			LawGroup.HouseholdGroup => "户籍", 
			LawGroup.LandTenureGroup => "土地所有制", 
			LawGroup.MarriageStatusGroup => "婚姻身份", 
			LawGroup.ClassCitizenshipGroup => "阶层身份与公民权", 
			LawGroup.NameRegistrationGroup => "姓名登记", 
			LawGroup.MeasuresGroup => "货币度量衡", 
			LawGroup.TradeControlGroup => "贸易管制", 
			LawGroup.NationalTaxGroup => "国家税法", 
			LawGroup.LocalTaxGroup => "聚落本地税法", 
			LawGroup.PassageGroup => "关隘通行", 
			LawGroup.BorderMarketGroup => "边市互市", 
			LawGroup.LegalSourceGroup => "法源", 
			_ => group.ToString(), 
		};
	}

	public static bool IsCombinationValid(LawComposition comp)
	{
		if (comp.centralExistence == CentralExistence.None && comp.centralInstitution.primary != 0)
		{
			return false;
		}
		return true;
	}

	private static int GetCurrentPrimary(LawDimension dimension, LawComposition comp)
	{
		return dimension switch
		{
			LawDimension.SupremeSovereignty => (int)comp.supremeSovereignty, 
			LawDimension.SupremeSuccession => comp.supremeSuccession.primary, 
			LawDimension.SupremeScope => comp.supremeScope.primary, 
			LawDimension.CentralExistence => (int)comp.centralExistence, 
			LawDimension.CentralSuccession => comp.centralSuccession.primary, 
			LawDimension.CentralInstitution => comp.centralInstitution.primary, 
			LawDimension.LocalSuccession => comp.localSuccession.primary, 
			LawDimension.LocalScope => comp.localScope.primary, 
			LawDimension.SpatialStructure => comp.spatialStructure.primary, 
			_ => -1, 
		};
	}

	public static string GetDimensionName(LawDimension dim)
	{
		return dim switch
		{
			LawDimension.SupremeSovereignty => "最高权力·归属", 
			LawDimension.SupremeSuccession => "最高权力·交接", 
			LawDimension.SupremeScope => "最高权力·分配", 
			LawDimension.CentralExistence => "中央权力·有无", 
			LawDimension.CentralSuccession => "中央权力·交接", 
			LawDimension.CentralInstitution => "中央权力·机构", 
			LawDimension.LocalSuccession => "地方权力·交接", 
			LawDimension.LocalScope => "地方权力·职能", 
			LawDimension.SpatialStructure => "央地结构", 
			_ => dim.ToString(), 
		};
	}

	public static string GetComponentName(LawDimension dim, int value)
	{
		switch (dim)
		{
		case LawDimension.SupremeSovereignty:
		{
			SupremeSovereignty supremeSovereignty = (SupremeSovereignty)value;
			return supremeSovereignty.ToString();
		}
		case LawDimension.SupremeSuccession:
		{
			SupremeSuccession supremeSuccession = (SupremeSuccession)value;
			return supremeSuccession.ToString();
		}
		case LawDimension.SupremeScope:
		{
			SupremeScope supremeScope = (SupremeScope)value;
			return supremeScope.ToString();
		}
		case LawDimension.CentralExistence:
		{
			CentralExistence centralExistence = (CentralExistence)value;
			return centralExistence.ToString();
		}
		case LawDimension.CentralSuccession:
		{
			CentralSuccession centralSuccession = (CentralSuccession)value;
			return centralSuccession.ToString();
		}
		case LawDimension.CentralInstitution:
		{
			CentralInstitution centralInstitution = (CentralInstitution)value;
			return centralInstitution.ToString();
		}
		case LawDimension.LocalSuccession:
		{
			LocalSuccession localSuccession = (LocalSuccession)value;
			return localSuccession.ToString();
		}
		case LawDimension.LocalScope:
		{
			LocalScope localScope = (LocalScope)value;
			return localScope.ToString();
		}
		case LawDimension.SpatialStructure:
		{
			SpatialStructure spatialStructure = (SpatialStructure)value;
			return spatialStructure.ToString();
		}
		default:
			return value.ToString();
		}
	}

	public static List<int> GetAllOptions(LawDimension dimension)
	{
		return dimension switch
		{
			LawDimension.SupremeSovereignty => Enum.GetValues(typeof(SupremeSovereignty)).Cast<int>().ToList(), 
			LawDimension.SupremeSuccession => Enum.GetValues(typeof(SupremeSuccession)).Cast<int>().ToList(), 
			LawDimension.SupremeScope => Enum.GetValues(typeof(SupremeScope)).Cast<int>().ToList(), 
			LawDimension.CentralExistence => Enum.GetValues(typeof(CentralExistence)).Cast<int>().ToList(), 
			LawDimension.CentralSuccession => Enum.GetValues(typeof(CentralSuccession)).Cast<int>().ToList(), 
			LawDimension.CentralInstitution => Enum.GetValues(typeof(CentralInstitution)).Cast<int>().ToList(), 
			LawDimension.LocalSuccession => Enum.GetValues(typeof(LocalSuccession)).Cast<int>().ToList(), 
			LawDimension.LocalScope => Enum.GetValues(typeof(LocalScope)).Cast<int>().ToList(), 
			LawDimension.SpatialStructure => Enum.GetValues(typeof(SpatialStructure)).Cast<int>().ToList(), 
			_ => new List<int>(), 
		};
	}

	public static List<int> GetAvailableOptions(LawDimension dimension, LawComposition comp, bool isPrimary = true)
	{
		List<int> allOptions = GetAllOptions(dimension);
		List<int> list = new List<int>();
		switch (dimension)
		{
		case LawDimension.SupremeSovereignty:
			return allOptions;
		case LawDimension.SupremeSuccession:
		{
			foreach (int item in allOptions)
			{
				SupremeSuccession supremeSuccession = (SupremeSuccession)item;
				if (comp.supremeSovereignty != SupremeSovereignty.Republic || supremeSuccession != SupremeSuccession.Hereditary)
				{
					list.Add(item);
				}
			}
			return list;
		}
		case LawDimension.CentralSuccession:
		{
			foreach (int item2 in allOptions)
			{
				CentralSuccession centralSuccession = (CentralSuccession)item2;
				if (comp.supremeSovereignty != SupremeSovereignty.Republic || centralSuccession != CentralSuccession.Hereditary)
				{
					list.Add(item2);
				}
			}
			return list;
		}
		case LawDimension.LocalSuccession:
		{
			foreach (int item3 in allOptions)
			{
				LocalSuccession localSuccession = (LocalSuccession)item3;
				if (comp.supremeSovereignty != SupremeSovereignty.Republic || localSuccession != LocalSuccession.Hereditary)
				{
					list.Add(item3);
				}
			}
			return list;
		}
		default:
			return allOptions;
		}
	}

	public static List<TitleDistribution> GetAvailableTitleDistributions(LawComposition comp)
	{
		if (comp.supremeSovereignty == SupremeSovereignty.Monarchy)
		{
			return new List<TitleDistribution>
			{
				TitleDistribution.Exclusive,
				TitleDistribution.FamilyShared
			};
		}
		return new List<TitleDistribution>
		{
			TitleDistribution.HasTitle,
			TitleDistribution.NoTitle
		};
	}

	public static bool IsExistingGroup(LawGroup group)
	{
		if ((uint)group <= 10u)
		{
			return true;
		}
		return false;
	}

	public static bool IsOpenedGroup(LawGroup group)
	{
		bool flag = IsExistingGroup(group);
		if (!flag)
		{
			bool flag2 = (((uint)(group - 13) <= 3u || group == LawGroup.LegalSourceGroup) ? true : false);
			flag = flag2;
		}
		return flag;
	}

	private static LawDimension? GroupToDimension(LawGroup group)
	{
		return group switch
		{
			LawGroup.SupremeSovereigntyGroup => (LawDimension?)LawDimension.SupremeSovereignty, 
			LawGroup.SupremeSuccessionGroup => LawDimension.SupremeSuccession, 
			LawGroup.SupremeScopeGroup => LawDimension.SupremeScope, 
			LawGroup.CentralExistenceGroup => LawDimension.CentralExistence, 
			LawGroup.CentralSuccessionGroup => LawDimension.CentralSuccession, 
			LawGroup.CentralInstitutionGroup => LawDimension.CentralInstitution, 
			LawGroup.LocalSuccessionGroup => LawDimension.LocalSuccession, 
			LawGroup.LocalScopeGroup => LawDimension.LocalScope, 
			LawGroup.RealmStructureGroup => LawDimension.SpatialStructure, 
			_ => null, 
		};
	}

	public static List<int> GetGroupAllOptions(LawGroup group)
	{
		switch (group)
		{
		case LawGroup.TitleDistributionGroup:
			return Enum.GetValues(typeof(TitleDistribution)).Cast<int>().ToList();
		case LawGroup.DomainDistributionGroup:
			return Enum.GetValues(typeof(DomainDistribution)).Cast<int>().ToList();
		case LawGroup.EstablishedFaithGroup:
			return Enum.GetValues(typeof(FaithStatus)).Cast<int>().ToList();
		case LawGroup.FaithLawPriorityGroup:
			return Enum.GetValues(typeof(FaithLawPriority)).Cast<int>().ToList();
		case LawGroup.HeterodoxPolicyGroup:
			return Enum.GetValues(typeof(HeterodoxPolicy)).Cast<int>().ToList();
		case LawGroup.FaithTitleGroup:
			return Enum.GetValues(typeof(FaithTitle)).Cast<int>().ToList();
		case LawGroup.LegalSourceGroup:
			return Enum.GetValues(typeof(LawSource)).Cast<int>().ToList();
		default:
		{
			LawDimension? lawDimension = GroupToDimension(group);
			if (!lawDimension.HasValue)
			{
				return new List<int>();
			}
			return GetAllOptions(lawDimension.Value);
		}
		}
	}

	public static bool IsGroupEnabled(LawComposition comp, LawGroup group)
	{
		if (comp == null)
		{
			return false;
		}
		if ((uint)(group - 14) <= 2u)
		{
			return comp.establishedFaith == FaithStatus.Established;
		}
		return IsOpenedGroup(group);
	}

	public static List<int> GetGroupAvailableOptions(LawGroup group, LawComposition comp, bool isPrimary = true)
	{
		if (!IsGroupEnabled(comp, group))
		{
			return new List<int>();
		}
		bool flag;
		switch (group)
		{
		case LawGroup.TitleDistributionGroup:
			return GetAvailableTitleDistributions(comp).Cast<int>().ToList();
		case LawGroup.EstablishedFaithGroup:
		case LawGroup.FaithLawPriorityGroup:
		case LawGroup.HeterodoxPolicyGroup:
		case LawGroup.FaithTitleGroup:
		case LawGroup.LegalSourceGroup:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return GetGroupAllOptions(group);
		}
		LawDimension? lawDimension = GroupToDimension(group);
		if (!lawDimension.HasValue)
		{
			return new List<int>();
		}
		return GetAvailableOptions(lawDimension.Value, comp, isPrimary);
	}
}
}
