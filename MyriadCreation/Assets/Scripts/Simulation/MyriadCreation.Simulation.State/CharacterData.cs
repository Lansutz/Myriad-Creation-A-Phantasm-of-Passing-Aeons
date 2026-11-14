using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Genetics;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using Random = UnityEngine.Random;

namespace MyriadCreation.Simulation.State
{
public class CharacterData
{
	public int characterId;

	public string firstName;

	public string lastName;

	public int age;

	public bool isMale;

	public int birthDay;

	public int birthYear;

	public int deathDay = -1;

	public int deathYear = -1;

	public int currentSettlementAnchorId = -1;

	public int currentTile = -1;

	/// <summary>人物当前所在建筑块 id（空串 = 不在具体建筑内）；锚点仍是聚落级权威位置。</summary>
	public string currentBlockId = "";

	public bool isPublicFigure;

	public CharacterAICategory aiCategory = CharacterAICategory.Worker;

	public IdentityProfile identity = new IdentityProfile();

	public GameEnums.SocialClass socialClass = GameEnums.SocialClass.Peasant;

	public GameEnums.SocialSubclass socialSubclass;

	public int realmId = -1;

	public int familyId = -1;

	public int cultureId;

	public int raceId;

	public int faithId;

	public string epithet = "";

	public List<string> bodyMarks = new List<string>();

	public Dictionary<BodyPartType, BodyPartData> bodyParts;

	public AchievementRecord achievements;

	public int accessionDay = -1;

	public bool epithetEvaluated;

	public string posthumousTitle = "";

	public string legendaryTitle = "";

	public int conquests;

	public int warsWon;

	public int diplomaticVictories;

	public int cultureWorks;

	public int religiousDeeds;

	public int expeditions;

	public int vigilanceActs;

	public float spiritualFulfillment = 50f;

	public int privateFaithId = -1;

	public List<string> personalTenets = new List<string>();

	public bool isSecretBeliever;

	public CharacterRole role;

	public int fatherId = -1;

	public int motherId = -1;

	public int spouseId = -1;

	public DnaData dna;

	public GenomeInstance genome;

	/// <summary>表型缓存（D3）：基因组在角色生命周期内不变，避免每日重复推导；基因组变化时清空。</summary>
	[NonSerialized]
	private PhenotypeResult _phenotypeCache;

	public PhenotypeAppearance appearance;

	public float expectedLifespanYears = 75f;

	public DnaExpression dnaExpression;

	public float individualResistance = 50f;

	public BasicAbilities basicAbilities = new BasicAbilities();

	[Range(0f, 100f)]
	public float prowess = 50f;

	[Range(0f, 100f)]
	public float social = 50f;

	[Range(0f, 100f)]
	public float military = 50f;

	[Range(0f, 100f)]
	public float management = 50f;

	[Range(0f, 100f)]
	public float conspiracy = 50f;

	[Range(0f, 100f)]
	public float scholarship = 50f;

	/// <summary>六类专精的真实实践累计（下标 = SpecialtyType）；技能点由实践阈值产生，不打怪掉经验。</summary>
	public float[] specialtyExperience = new float[6];

	/// <summary>已解锁的专精节点 id（动词/战术）；顺序仅用于展示。</summary>
	public List<string> unlockedPerks = new List<string>();

	/// <summary>专精对应的既有技能值（0–100），用于节点门槛；不另开一套数值。</summary>
	public float GetSpecialtySkill(SpecialtyType specialty)
	{
		switch (specialty)
		{
			case SpecialtyType.Prowess: return prowess;
			case SpecialtyType.Military: return military;
			case SpecialtyType.Management: return management;
			case SpecialtyType.Social: return social;
			case SpecialtyType.Conspiracy: return conspiracy;
			case SpecialtyType.Scholarship: return scholarship;
			default: return 0f;
		}
	}

	public float GetSpecialtyExperience(SpecialtyType specialty)
	{
		int index = (int)specialty;
		if (specialtyExperience == null || index < 0 || index >= specialtyExperience.Length) return 0f;
		return specialtyExperience[index];
	}

	/// <summary>记录一次真实实践；达到阈值后由 SpecialtyPerkSystem 解锁节点。</summary>
	public void AddSpecialtyExperience(SpecialtyType specialty, float amount)
	{
		if (amount <= 0f) return;
		if (specialtyExperience == null || specialtyExperience.Length < 6) specialtyExperience = new float[6];
		int index = (int)specialty;
		if (index < 0 || index >= specialtyExperience.Length) return;
		specialtyExperience[index] += amount;
	}

	public bool HasPerk(string perkId)
	{
		return !string.IsNullOrEmpty(perkId) && unlockedPerks != null && unlockedPerks.Contains(perkId);
	}

	public float prestige;

	public int prestigeCapacityLevel = 1;

	public float notoriety;

	[Range(0f, 100f)]
	public float health = 100f;

	[Range(0f, 100f)]
	public float fertility = 50f;

	[Range(0f, 100f)]
	public float stress;

	[Range(0f, 100f)]
	public float dread;

	[Range(0f, 100f)]
	public float obesity = 20f;

	[Range(0f, 100f)]
	public float charm = 50f;

	[Range(-100f, 100f)]
	public float boldness;

	[Range(-100f, 100f)]
	public float compassion;

	[Range(-100f, 100f)]
	public float greed;

	[Range(-100f, 100f)]
	public float honor;

	[Range(-100f, 100f)]
	public float rationality;

	[Range(-100f, 100f)]
	public float vengefulness;

	[Range(-100f, 100f)]
	public float piety;

	public float sociability;

	public string mentalDisorderId = "";

	public int highStressDays;

	public int lowStressRecoveryDays;

	public List<ActiveCharacterDisease> activeDiseases = new List<ActiveCharacterDisease>();

	public List<ActiveImpairment> impairments = new List<ActiveImpairment>();

	public List<PersonalityTrait> traits = new List<PersonalityTrait>();

	[NonSerialized]
	public Dictionary<int, CharacterRelation> relations = new Dictionary<int, CharacterRelation>();

	[NonSerialized]
	public Dictionary<string, float> skills = new Dictionary<string, float>();

	public float gold;

	public List<int> ownedTitles = new List<int>();

	public int commandedArmyId = -1;

	public string fullName => firstName + " " + lastName;

	public bool isAlive => deathDay < 0;

	public void SetSocialClass(GameEnums.SocialClass cls)
	{
		socialClass = cls;
		GameEnums.SocialSubclass? defaultSubclass = GameEnums.SocialClassHierarchy.GetDefaultSubclass(cls);
		if (defaultSubclass.HasValue)
		{
			socialSubclass = defaultSubclass.Value;
		}
		SyncIdentityFromLegacy();
	}

	public void SyncIdentityFromLegacy()
	{
		identity = LegacyIdentityMapping.FromLegacy(socialClass, socialSubclass);
	}

	public static GameEnums.SocialClass RoleToClass(CharacterRole role)
	{
		switch (role)
		{
		case CharacterRole.Ruler:
		case CharacterRole.Heir:
		case CharacterRole.Spouse:
			return GameEnums.SocialClass.Royalty;
		case CharacterRole.Noble:
		case CharacterRole.Clergy:
		case CharacterRole.Military:
		case CharacterRole.Courtier:
			return GameEnums.SocialClass.NobilityClergy;
		case CharacterRole.Merchant:
		case CharacterRole.Scholar:
			return GameEnums.SocialClass.MerchantFreeman;
		case CharacterRole.Commoner:
			return GameEnums.SocialClass.Peasant;
		default:
			return GameEnums.SocialClass.Peasant;
		}
	}

	public static GameEnums.SocialSubclass? RoleToSubclass(CharacterRole role)
	{
		return role switch
		{
			CharacterRole.Merchant => (GameEnums.SocialSubclass?)GameEnums.SocialSubclass.Merchant, 
			CharacterRole.Scholar => GameEnums.SocialSubclass.Scholar, 
			CharacterRole.Commoner => GameEnums.SocialSubclass.Freeholder, 
			_ => null, 
		};
	}

	public void SyncClassFromRole()
	{
		socialClass = RoleToClass(role);
		socialSubclass = RoleToSubclass(role) ?? GameEnums.SocialClassHierarchy.GetDefaultSubclass(socialClass).GetValueOrDefault();
		SyncIdentityFromLegacy();
	}

	/// <summary>取缓存表型；基因组不变时只算一次（D3 机制查询与每日健康结算共用）。</summary>
	public PhenotypeResult GetCachedPhenotype()
	{
		if (_phenotypeCache == null) _phenotypeCache = PhenotypeMechanics.PhenotypeOf(this);
		return _phenotypeCache;
	}

	public void SetPhenotypeCache(PhenotypeResult phenotype)
	{
		_phenotypeCache = phenotype;
	}

	public GenomeInstance GetGenome(RaceData race = null)
	{
		if (genome != null)
		{
			_phenotypeCache = null;
			return GenomeMigration.Upgrade(genome);
		}
		genome = ((dna != null) ? dna.GetGenome(race, isMale) : GeneticsEngine.CreateFounder(GeneticsCatalog.ResolveGenome(race?.GetGenomeId()), isMale ? GeneticSex.Male : GeneticSex.Female, new System.Random(characterId * 397 + age)));
		return genome;
	}

	public PhenotypeAppearance RefreshAppearance(RaceData race = null)
	{
		GenomeInstance genomeInstance = GetGenome(race);
		GenomeDef genomeDef = GeneticsCatalog.ResolveGenome(genomeInstance.genomeId);
		PhenotypeResult phenotype = GeneticsEngine.ComputePhenotype(genomeDef, genomeInstance);
		appearance = GeneticsAppearance.Build(genomeDef, phenotype);
		return appearance;
	}

	public float CalculateOverallAbility()
	{
		return (prowess + social + military + management + conspiracy + scholarship) / 6f;
	}

	public float CalculateRuleAbility()
	{
		return management * 0.4f + social * 0.3f + conspiracy * 0.2f + scholarship * 0.1f;
	}

	public float CalculateCommandAbility()
	{
		return military * 0.6f + prowess * 0.2f + conspiracy * 0.1f + scholarship * 0.1f;
	}

	public void GetEffectiveStats(out float prowess, out float social, out float military, out float management, out float conspiracy, out float scholarship, out float charm)
	{
		prowess = this.prowess;
		social = this.social;
		military = this.military;
		management = this.management;
		conspiracy = this.conspiracy;
		scholarship = this.scholarship;
		charm = this.charm;
		if (traits != null)
		{
			foreach (PersonalityTrait trait in traits)
			{
				prowess += trait.martialMod;
				social += trait.diplomacyMod;
				military += trait.warfareMod;
				management += trait.stewardshipMod;
				conspiracy += trait.intrigueMod;
				scholarship += trait.learningMod;
				charm += trait.charmMod;
			}
		}
		MentalDisorderDef def = MentalHealthSystem.GetDef(mentalDisorderId);
		if (def != null)
		{
			prowess += def.martialMod;
			social += def.diplomacyMod;
			military += def.warfareMod;
			management += def.stewardshipMod;
			conspiracy += def.intrigueMod;
			scholarship += def.learningMod;
			charm += def.charmMod;
		}
	}

	public void DailyTick(int currentDay, int currentYear, float ambientTempC = 20f)
	{
		if (!isAlive)
		{
			return;
		}
		if (currentDay == birthDay)
		{
			age++;
		}
		float num = ((dna != null) ? (expectedLifespanYears * 0.6f) : 50f);
		float num2 = ((dna != null) ? expectedLifespanYears : 100f);
		float num3 = (((float)age > num) ? (((float)age - num) / Mathf.Max(1f, num2 - num)) : 0f);
		float num4 = ((obesity > 70f) ? 1.5f : 1f);
		health = Mathf.Clamp(health - num3 * 0.01f * num4, 0f, 100f);
		// D3：寒热损耗由表型（耐温/隔热）决定，而不是全物种统一。
		health = Mathf.Clamp(health - PhenotypeMechanics.TemperatureHealthPenalty(GetCachedPhenotype(), ambientTempC), 0f, 100f);
		MentalDisorderDef def = MentalHealthSystem.GetDef(mentalDisorderId);
		float num5 = ((def != null) ? (0.05f * def.stressDecayMult) : 0.05f);
		stress = Mathf.Max(0f, stress - ((stress > 60f) ? 0.01f : num5));
		if (stress > 80f)
		{
			highStressDays++;
		}
		else
		{
			highStressDays = Mathf.Max(0, highStressDays - 2);
		}
		float num6 = ((stress > 60f) ? 0.02f : 0.01f);
		PersonalityDimension[] all = PersonalityDimensions.All;
		foreach (PersonalityDimension dim in all)
		{
			AddPersonality(dim, Random.Range(0f - num6, num6));
		}
		obesity = Mathf.Max(0f, obesity - 0.01f);
		prestige = Mathf.Max(0f, prestige - 0.05f);
		notoriety = Mathf.Max(0f, notoriety - 0.02f);
		if (health <= 0f)
		{
			Die(currentDay, currentYear, "自然死亡");
		}
		foreach (PersonalityTrait trait in traits)
		{
			trait.ApplyDailyEffect(this);
		}
	}

	public int GetPersonalityTier(PersonalityDimension dim)
	{
		float num = Mathf.Abs(GetPersonalityValue(dim));
		if (num < 15f)
		{
			return 0;
		}
		if (num < 35f)
		{
			return 1;
		}
		if (num < 65f)
		{
			return 2;
		}
		return 3;
	}

	public float GetPersonalityValue(PersonalityDimension dim)
	{
		return dim switch
		{
			PersonalityDimension.Boldness => boldness, 
			PersonalityDimension.Compassion => compassion, 
			PersonalityDimension.Greed => greed, 
			PersonalityDimension.Honor => honor, 
			PersonalityDimension.Rationality => rationality, 
			PersonalityDimension.Vengefulness => vengefulness, 
			PersonalityDimension.Piety => piety, 
			PersonalityDimension.Sociability => sociability, 
			_ => 0f, 
		};
	}

	public void SetPersonalityValue(PersonalityDimension dim, float value)
	{
		float num = Mathf.Clamp(value, -100f, 100f);
		switch (dim)
		{
		case PersonalityDimension.Boldness:
			boldness = num;
			break;
		case PersonalityDimension.Compassion:
			compassion = num;
			break;
		case PersonalityDimension.Greed:
			greed = num;
			break;
		case PersonalityDimension.Honor:
			honor = num;
			break;
		case PersonalityDimension.Rationality:
			rationality = num;
			break;
		case PersonalityDimension.Vengefulness:
			vengefulness = num;
			break;
		case PersonalityDimension.Piety:
			piety = num;
			break;
		case PersonalityDimension.Sociability:
			sociability = num;
			break;
		}
	}

	public void AddPersonality(PersonalityDimension dim, float delta)
	{
		SetPersonalityValue(dim, GetPersonalityValue(dim) + delta);
	}

	public int GetPersonalityTier(string dim)
	{
		if (!PersonalityDimensions.TryParse(dim, out var dim2))
		{
			return 0;
		}
		return GetPersonalityTier(dim2);
	}

	public float GetPersonalityValue(string dim)
	{
		if (!PersonalityDimensions.TryParse(dim, out var dim2))
		{
			return 0f;
		}
		return GetPersonalityValue(dim2);
	}

	public float GetPersonalityAffinity(CharacterData other)
	{
		if (other == null)
		{
			return 0f;
		}
		float num = 0f;
		PersonalityDimension[] all = PersonalityDimensions.All;
		foreach (PersonalityDimension dim in all)
		{
			float personalityValue = GetPersonalityValue(dim);
			float personalityValue2 = other.GetPersonalityValue(dim);
			if (!(Mathf.Abs(personalityValue) < 15f) && !(Mathf.Abs(personalityValue2) < 15f))
			{
				bool flag = personalityValue > 0f == personalityValue2 > 0f;
				int num2 = Mathf.Min(GetPersonalityTier(dim), other.GetPersonalityTier(dim));
				if (flag)
				{
					float num3 = num;
					num = num3 + num2 switch
					{
						3 => 6f, 
						2 => 4f, 
						_ => 2f, 
					};
				}
				else
				{
					float num4 = num;
					num = num4 - num2 switch
					{
						3 => 5f, 
						2 => 3f, 
						_ => 1f, 
					};
				}
			}
		}
		return Mathf.Clamp(num, -20f, 20f);
	}

	public string GetPersonalityDescription()
	{
		(PersonalityDimension, float)[] array = new (PersonalityDimension, float)[PersonalityDimensions.All.Length];
		for (int i = 0; i < PersonalityDimensions.All.Length; i++)
		{
			PersonalityDimension personalityDimension = PersonalityDimensions.All[i];
			array[i] = (personalityDimension, GetPersonalityValue(personalityDimension));
		}
		Array.Sort(array, ((PersonalityDimension dim, float value) a, (PersonalityDimension dim, float value) b) => Mathf.Abs(b.value).CompareTo(Mathf.Abs(a.value)));
		(PersonalityDimension, float) tuple = array[0];
		(PersonalityDimension, float) tuple2 = array[1];
		if (Mathf.Abs(tuple.Item2) < 15f)
		{
			return "性情平和中正，既不偏激也不执拗，处世随分安时。";
		}
		string text = DescribeDimension(tuple.Item1, tuple.Item2);
		string text2 = DescribeDimension(tuple2.Item1, tuple2.Item2);
		return "为人" + text + "，行事" + text2 + "。";
	}

	private static string DescribeDimension(PersonalityDimension dim, float value)
	{
		bool flag = value > 0f;
		return dim switch
		{
			PersonalityDimension.Boldness => flag ? "胆气过人，临事敢为，鲜有畏葸" : "性谨慎，谋定后动，不喜冒险", 
			PersonalityDimension.Compassion => flag ? "心肠慈悲，见不得民生疾苦，常施仁政" : "心硬如铁，视百姓如草芥，无情可动", 
			PersonalityDimension.Greed => flag ? "贪得无厌，见利忘义，库藏永不餍足" : "淡泊财货，不慕荣利，清廉自守", 
			PersonalityDimension.Honor => flag ? "重诺守信，把名誉看得比性命更重" : "轻诺寡信，名节于他不过是可售之物", 
			PersonalityDimension.Rationality => flag ? "冷静理性，遇事权衡利害，不感情用事" : "率性而为，凭一时好恶决断，不计后果", 
			PersonalityDimension.Vengefulness => flag ? "睚眦必报，恩怨分明，得罪过他的人他都记着" : "宽宏大量，受了委屈也多半一笑置之", 
			PersonalityDimension.Piety => flag ? "虔诚信奉，常与神职人员来往，礼敬神祇" : "对神明半信半疑，礼数只是做给人看", 
			_ => "性情难测", 
		};
	}

	public bool HasTrait(string traitId)
	{
		foreach (PersonalityTrait trait in traits)
		{
			if (trait.traitId == traitId)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasTraitAnyLevel(string baseTraitId)
	{
		foreach (PersonalityTrait trait in traits)
		{
			if (trait.traitId == baseTraitId || trait.traitId.StartsWith(baseTraitId + "_"))
			{
				return true;
			}
		}
		return false;
	}

	public string GetPersonalityTagsDisplay()
	{
		List<string> list = new List<string>();
		foreach (PersonalityTrait trait in traits)
		{
			if (trait.category == TraitCategory.Personality)
			{
				list.Add(trait.traitName);
			}
		}
		if (list.Count != 0)
		{
			return string.Join("  ", list);
		}
		return "无明显性格";
	}

	public EconomicalArchetype DetermineEconomicalArchetype()
	{
		float personalityValue = GetPersonalityValue(PersonalityDimension.Boldness);
		float personalityValue2 = GetPersonalityValue(PersonalityDimension.Greed);
		float personalityValue3 = GetPersonalityValue(PersonalityDimension.Piety);
		float personalityValue4 = GetPersonalityValue(PersonalityDimension.Compassion);
		float personalityValue5 = GetPersonalityValue(PersonalityDimension.Honor);
		float personalityValue6 = GetPersonalityValue(PersonalityDimension.Rationality);
		float personalityValue7 = GetPersonalityValue(PersonalityDimension.Vengefulness);
		if (HasTrait("conqueror") || HasTrait("greatest_of_khans"))
		{
			return EconomicalArchetype.Conqueror;
		}
		bool flag = HasTraitAnyLevel("wrathful") || HasTraitAnyLevel("ambitious") || HasTraitAnyLevel("vengeful") || HasTraitAnyLevel("zealous") || HasTraitAnyLevel("sadistic");
		bool flag2 = (personalityValue >= 50f && personalityValue2 >= 50f) || (personalityValue >= 25f && personalityValue2 >= 100f) || (personalityValue >= 100f && personalityValue2 >= 25f);
		if (personalityValue > 0f && personalityValue2 >= 0f && (flag | flag2) && personalityValue4 < 75f && !HasTraitAnyLevel("compassionate") && !HasTraitAnyLevel("craven") && !HasTraitAnyLevel("calm"))
		{
			return EconomicalArchetype.Warlike;
		}
		if (personalityValue3 > 0f && (HasTraitAnyLevel("zealous") || HasTraitAnyLevel("forgiving") || HasTraitAnyLevel("diligent") || HasTraitAnyLevel("humble") || HasTraitAnyLevel("patient")) && !HasTraitAnyLevel("cynical") && !HasTraitAnyLevel("greedy"))
		{
			return EconomicalArchetype.PiousBuilder;
		}
		if (personalityValue > 0f && (HasTraitAnyLevel("diligent") || HasTraitAnyLevel("calm") || HasTraitAnyLevel("patient") || HasTraitAnyLevel("generous") || HasTraitAnyLevel("stubborn")) && !HasTraitAnyLevel("greedy") && !HasTraitAnyLevel("impatient"))
		{
			return EconomicalArchetype.EconomicalBoom;
		}
		if (personalityValue3 <= -50f && personalityValue6 >= 40f && personalityValue >= 0f)
		{
			return EconomicalArchetype.GodlessReformer;
		}
		if (personalityValue5 <= -40f && personalityValue7 >= 40f && personalityValue6 >= 0f)
		{
			return EconomicalArchetype.Schemer;
		}
		if (personalityValue6 >= 40f && personalityValue5 >= 40f && personalityValue2 < 40f)
		{
			return EconomicalArchetype.Administrator;
		}
		if (personalityValue4 >= 40f && personalityValue5 >= 40f && personalityValue3 >= 0f)
		{
			return EconomicalArchetype.CulturalPatron;
		}
		if (personalityValue <= 0f && (HasTraitAnyLevel("paranoid") || HasTraitAnyLevel("craven") || (personalityValue <= -25f && (HasTraitAnyLevel("patient") || HasTraitAnyLevel("calm") || HasTraitAnyLevel("content")))))
		{
			return EconomicalArchetype.Cautious;
		}
		if (HasTraitAnyLevel("fickle") || HasTraitAnyLevel("lunatic"))
		{
			return EconomicalArchetype.Unpredictable;
		}
		return EconomicalArchetype.Balanced;
	}

	public string GetEconomicalArchetypeDisplay()
	{
		EconomicalArchetypeInfo economicalArchetypeInfo = EconomicalArchetypes.Get(DetermineEconomicalArchetype());
		return economicalArchetypeInfo.displayName + "（" + economicalArchetypeInfo.description + "）";
	}

	public void Die(int day, int year, string cause)
	{
		deathDay = day;
		deathYear = year;
		Debug.Log((object)$"[Character] {fullName} 死亡：{cause}，享年{age}岁");
	}

	public void AddTrait(PersonalityTrait trait)
	{
		if (!traits.Exists((PersonalityTrait t) => t.traitId == trait.traitId))
		{
			traits.Add(trait);
			trait.OnAcquired(this);
		}
	}

	public void RemoveTrait(string traitId)
	{
		PersonalityTrait personalityTrait = traits.Find((PersonalityTrait t) => t.traitId == traitId);
		if (personalityTrait != null)
		{
			personalityTrait.OnRemoved(this);
			traits.Remove(personalityTrait);
		}
	}

	public void ModifyRelation(int otherId, float opinionDelta, string reason)
	{
		relations ??= new Dictionary<int, CharacterRelation>();
		if (!relations.TryGetValue(otherId, out var value))
		{
			value = new CharacterRelation
			{
				otherCharacterId = otherId
			};
			relations[otherId] = value;
		}
		value.opinion = Mathf.Clamp(value.opinion + opinionDelta, -200f, 200f);
		value.history ??= new List<string>();
		value.history.Add($"{reason}: {opinionDelta}");
		relations[otherId] = value;
	}

	public float GetPrestigeCapacity()
	{
		return prestigeCapacityLevel switch
		{
			2 => 300f, 
			3 => 600f, 
			4 => 1000f, 
			5 => 1500f, 
			_ => 100f, 
		};
	}

	public float GetNotorietyCapacity()
	{
		return float.MaxValue;
	}

	public void ModifyPrestige(float delta)
	{
		prestige = Mathf.Clamp(prestige + delta, 0f, GetPrestigeCapacity());
		if (prestige >= GetPrestigeCapacity() && prestigeCapacityLevel < 5)
		{
			prestigeCapacityLevel++;
			Debug.Log((object)$"[Character] {fullName} 威望容量升至 {prestigeCapacityLevel} 级");
		}
		else if (prestige < GetPrestigeCapacity() * 0.3f && prestigeCapacityLevel > 1)
		{
			prestigeCapacityLevel--;
			Debug.Log((object)$"[Character] {fullName} 威望容量降至 {prestigeCapacityLevel} 级");
		}
	}

	public void ModifyNotoriety(float delta)
	{
		notoriety = Mathf.Max(0f, notoriety + delta);
	}

	public int GetNotorietyTier()
	{
		if (notoriety >= 1000f)
		{
			return 4;
		}
		if (notoriety >= 500f)
		{
			return 3;
		}
		if (notoriety >= 200f)
		{
			return 2;
		}
		if (notoriety >= 50f)
		{
			return 1;
		}
		return 0;
	}

	public string GetNotorietyTierName()
	{
		return GetNotorietyTier() switch
		{
			0 => "无恶名", 
			1 => "小恶", 
			2 => "大恶", 
			3 => "极恶", 
			4 => "遗臭", 
			_ => "未知", 
		};
	}
}
}
