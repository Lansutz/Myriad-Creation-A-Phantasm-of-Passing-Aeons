using System.Collections.Generic;
using System.Linq;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Laws
{
public static class CrimeRegistry
{
	private static readonly Dictionary<CrimeType, CrimeDefinition> _registry;

	public static IReadOnlyDictionary<CrimeType, CrimeDefinition> All => _registry;

	static CrimeRegistry()
	{
		_registry = new Dictionary<CrimeType, CrimeDefinition>();
		RegisterDefaults();
	}

	private static void RegisterDefaults()
	{
		Add(new CrimeDefinition
		{
			type = CrimeType.Murder,
			name = "谋杀",
			baseSeverity = 100f,
			defaultPunishment = PunishmentType.Death,
			behaviors = new List<CrimeBehavior> { CrimeBehavior.Homicide },
			targetClasses = new List<CrimeTargetClass>
			{
				CrimeTargetClass.NaturalPerson,
				CrimeTargetClass.Official,
				CrimeTargetClass.Merchant,
				CrimeTargetClass.FamilyMember
			},
			contexts = new List<string>(),
			citedDomainIds = new List<int> { 3 },
			citedFamilyIds = new List<int> { 6 },
			citedGroupIds = new List<int>()
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.Treason,
			name = "叛国",
			baseSeverity = 100f,
			defaultPunishment = PunishmentType.Death,
			behaviors = new List<CrimeBehavior>
			{
				CrimeBehavior.Treason,
				CrimeBehavior.CollusionWithEnemy,
				CrimeBehavior.Conspiracy
			},
			targetClasses = new List<CrimeTargetClass>
			{
				CrimeTargetClass.Sovereign,
				CrimeTargetClass.Realm
			},
			contexts = new List<string>(),
			citedDomainIds = new List<int> { 0 },
			citedFamilyIds = new List<int> { 0 },
			citedGroupIds = new List<int> { 0 }
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.Theft,
			name = "盗窃",
			baseSeverity = 30f,
			defaultPunishment = PunishmentType.Fine,
			behaviors = new List<CrimeBehavior>
			{
				CrimeBehavior.Theft,
				CrimeBehavior.Embezzlement
			},
			targetClasses = new List<CrimeTargetClass> { CrimeTargetClass.PropertyOwner },
			contexts = new List<string> { "night_burglary" },
			citedDomainIds = new List<int> { 3 },
			citedFamilyIds = new List<int> { 7 },
			citedGroupIds = new List<int>()
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.Assault,
			name = "伤害",
			baseSeverity = 40f,
			defaultPunishment = PunishmentType.Fine,
			behaviors = new List<CrimeBehavior> { CrimeBehavior.Battery },
			targetClasses = new List<CrimeTargetClass> { CrimeTargetClass.NaturalPerson },
			contexts = new List<string>(),
			citedDomainIds = new List<int> { 3 },
			citedFamilyIds = new List<int> { 6 },
			citedGroupIds = new List<int>()
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.Blasphemy,
			name = "亵渎",
			baseSeverity = 60f,
			defaultPunishment = PunishmentType.Exile,
			behaviors = new List<CrimeBehavior> { CrimeBehavior.Blasphemy },
			targetClasses = new List<CrimeTargetClass> { CrimeTargetClass.FaithObject },
			contexts = new List<string>(),
			citedDomainIds = new List<int> { 4 },
			citedFamilyIds = new List<int> { 9 },
			citedGroupIds = new List<int> { 28 }
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.Heresy,
			name = "异端",
			baseSeverity = 80f,
			defaultPunishment = PunishmentType.Death,
			behaviors = new List<CrimeBehavior> { CrimeBehavior.Heresy },
			targetClasses = new List<CrimeTargetClass> { CrimeTargetClass.CoReligionist },
			contexts = new List<string>(),
			citedDomainIds = new List<int> { 4 },
			citedFamilyIds = new List<int> { 9 },
			citedGroupIds = new List<int> { 28 }
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.SexualMisconduct,
			name = "性伦理罪",
			baseSeverity = 40f,
			defaultPunishment = PunishmentType.Fine,
			behaviors = new List<CrimeBehavior> { CrimeBehavior.SexualMisconduct },
			targetClasses = new List<CrimeTargetClass> { CrimeTargetClass.CoReligionist },
			contexts = new List<string>(),
			citedDomainIds = new List<int> { 4 },
			citedFamilyIds = new List<int> { 9 },
			citedGroupIds = new List<int>()
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.Witchcraft,
			name = "巫术",
			baseSeverity = 50f,
			defaultPunishment = PunishmentType.Exile,
			behaviors = new List<CrimeBehavior> { CrimeBehavior.Witchcraft },
			targetClasses = new List<CrimeTargetClass> { CrimeTargetClass.CoReligionist },
			contexts = new List<string>(),
			citedDomainIds = new List<int> { 4 },
			citedFamilyIds = new List<int> { 9 },
			citedGroupIds = new List<int>()
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.TaxEvasion,
			name = "逃税",
			baseSeverity = 25f,
			defaultPunishment = PunishmentType.Fine,
			behaviors = new List<CrimeBehavior> { CrimeBehavior.TaxEvasion },
			targetClasses = new List<CrimeTargetClass> { CrimeTargetClass.RealmTreasury },
			contexts = new List<string>(),
			citedDomainIds = new List<int> { 3 },
			citedFamilyIds = new List<int> { 7 },
			citedGroupIds = new List<int> { 24 }
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.Desertion,
			name = "逃兵",
			baseSeverity = 70f,
			defaultPunishment = PunishmentType.Death,
			behaviors = new List<CrimeBehavior> { CrimeBehavior.Deserter },
			targetClasses = new List<CrimeTargetClass> { CrimeTargetClass.MilitaryPersonnel },
			contexts = new List<string> { "wartime", "duty_post" },
			citedDomainIds = new List<int> { 1 },
			citedFamilyIds = new List<int> { 4 },
			citedGroupIds = new List<int> { 12 }
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.Fraud,
			name = "欺诈",
			baseSeverity = 35f,
			defaultPunishment = PunishmentType.Fine,
			behaviors = new List<CrimeBehavior> { CrimeBehavior.Fraud },
			targetClasses = new List<CrimeTargetClass> { CrimeTargetClass.Counterparty },
			contexts = new List<string>(),
			citedDomainIds = new List<int> { 3 },
			citedFamilyIds = new List<int> { 7 },
			citedGroupIds = new List<int>()
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.Arson,
			name = "纵火",
			baseSeverity = 55f,
			defaultPunishment = PunishmentType.Fine,
			behaviors = new List<CrimeBehavior> { CrimeBehavior.Arson },
			targetClasses = new List<CrimeTargetClass>
			{
				CrimeTargetClass.Dwelling,
				CrimeTargetClass.Warehouse,
				CrimeTargetClass.OfficialBuilding
			},
			contexts = new List<string>(),
			citedDomainIds = new List<int> { 3 },
			citedFamilyIds = new List<int> { 6 },
			citedGroupIds = new List<int>()
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.Rape,
			name = "性侵",
			baseSeverity = 80f,
			defaultPunishment = PunishmentType.Corporal,
			behaviors = new List<CrimeBehavior> { CrimeBehavior.Rape },
			targetClasses = new List<CrimeTargetClass>
			{
				CrimeTargetClass.NaturalPerson,
				CrimeTargetClass.IdentityOrder
			},
			contexts = new List<string>(),
			citedDomainIds = new List<int> { 3 },
			citedFamilyIds = new List<int> { 6 },
			citedGroupIds = new List<int>()
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.Kidnapping,
			name = "绑架",
			baseSeverity = 60f,
			defaultPunishment = PunishmentType.Exile,
			behaviors = new List<CrimeBehavior> { CrimeBehavior.Kidnap },
			targetClasses = new List<CrimeTargetClass>
			{
				CrimeTargetClass.NaturalPerson,
				CrimeTargetClass.PropertyOwner
			},
			contexts = new List<string>(),
			citedDomainIds = new List<int> { 3 },
			citedFamilyIds = new List<int> { 6 },
			citedGroupIds = new List<int>()
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.Smuggling,
			name = "走私",
			baseSeverity = 25f,
			defaultPunishment = PunishmentType.Fine,
			behaviors = new List<CrimeBehavior> { CrimeBehavior.Contraband },
			targetClasses = new List<CrimeTargetClass>
			{
				CrimeTargetClass.Frontier,
				CrimeTargetClass.Merchant
			},
			contexts = new List<string>(),
			citedDomainIds = new List<int> { 3 },
			citedFamilyIds = new List<int> { 8, 7 },
			citedGroupIds = new List<int> { 27, 23 }
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.Piracy,
			name = "海盗",
			baseSeverity = 70f,
			defaultPunishment = PunishmentType.Death,
			behaviors = new List<CrimeBehavior> { CrimeBehavior.Piracy },
			targetClasses = new List<CrimeTargetClass>
			{
				CrimeTargetClass.MerchantShip,
				CrimeTargetClass.SeaLane
			},
			contexts = new List<string>(),
			citedDomainIds = new List<int> { 3 },
			citedFamilyIds = new List<int> { 7 },
			citedGroupIds = new List<int>()
		});
		Add(new CrimeDefinition
		{
			type = CrimeType.Rebellion,
			name = "叛乱",
			baseSeverity = 100f,
			defaultPunishment = PunishmentType.Death,
			behaviors = new List<CrimeBehavior>
			{
				CrimeBehavior.Sedition,
				CrimeBehavior.Conspiracy
			},
			targetClasses = new List<CrimeTargetClass> { CrimeTargetClass.Realm },
			contexts = new List<string>(),
			citedDomainIds = new List<int> { 0 },
			citedFamilyIds = new List<int> { 0 },
			citedGroupIds = new List<int>()
		});
	}

	public static void Add(CrimeDefinition definition)
	{
		_registry[definition.type] = definition;
	}

	public static bool TryGet(CrimeType type, out CrimeDefinition definition)
	{
		return _registry.TryGetValue(type, out definition);
	}

	public static List<LawConstraints.LawDomain> GetCitedDomains(CrimeDefinition def)
	{
		return (from int v in def.citedDomainIds ?? new List<int>()
			select (LawConstraints.LawDomain)v).ToList();
	}

	public static List<LawConstraints.LawFamily> GetCitedFamilies(CrimeDefinition def)
	{
		return (from int v in def.citedFamilyIds ?? new List<int>()
			select (LawConstraints.LawFamily)v).ToList();
	}

	public static List<LawConstraints.LawGroup> GetCitedGroups(CrimeDefinition def)
	{
		return (from int v in def.citedGroupIds ?? new List<int>()
			select (LawConstraints.LawGroup)v).ToList();
	}

	public static bool IsCitedByDomain(CrimeType type, LawConstraints.LawDomain domain)
	{
		if (!TryGet(type, out var definition))
		{
			return false;
		}
		if (definition.citedDomainIds != null)
		{
			return definition.citedDomainIds.Contains((int)domain);
		}
		return false;
	}

	public static List<CrimeType> MatchByBehavior(CrimeBehavior behavior, CrimeTargetClass? target = null, string context = null)
	{
		List<KeyValuePair<CrimeType, int>> list = new List<KeyValuePair<CrimeType, int>>();
		foreach (KeyValuePair<CrimeType, CrimeDefinition> item in _registry)
		{
			CrimeDefinition value = item.Value;
			if (value.behaviors != null && value.behaviors.Contains(behavior))
			{
				int num = 1;
				if (target.HasValue && value.targetClasses != null && value.targetClasses.Contains(target.Value))
				{
					num += 2;
				}
				if (!string.IsNullOrEmpty(context) && value.contexts != null && value.contexts.Contains(context))
				{
					num++;
				}
				list.Add(new KeyValuePair<CrimeType, int>(item.Key, num));
			}
		}
		return (from p in list
			orderby p.Value descending
			select p.Key).ToList();
	}
}
}
