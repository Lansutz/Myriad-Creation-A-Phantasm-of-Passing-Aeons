using System.Collections.Generic;
using System.Text;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Genetics;
using MyriadCreation.Content.Modding;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public class CharacterManager
{
	private readonly Dictionary<int, CharacterData> _characters = new Dictionary<int, CharacterData>();

	private readonly Dictionary<int, FamilyNode> _families = new Dictionary<int, FamilyNode>();

	private readonly List<CharacterBond> _bonds = new List<CharacterBond>();

	private int _nextCharacterId = 1;

	private int _nextFamilyId = 1;

	private int _nextBondId = 1;

	public Dictionary<int, RaceData> Races { get; set; }

	public InnovationTree Innovations { get; set; }

	public EconomyManager Economy { get; set; }

	public TileData[] Tiles { get; set; }

	public Dictionary<int, RealmData> Realms { get; set; }

	public void InitPrivateFaith(CharacterData c)
	{
		if ((c == null || c.privateFaithId == -1) && c != null)
		{
			c.privateFaithId = c.faithId;
		}
	}

	public bool AddPersonalTenet(CharacterData c, string optionId)
	{
		if (c == null || string.IsNullOrEmpty(optionId))
		{
			return false;
		}
		if (c.personalTenets.Contains(optionId))
		{
			return false;
		}
		c.personalTenets.Add(optionId);
		return true;
	}

	public bool RemovePersonalTenet(CharacterData c, string optionId)
	{
		return c?.personalTenets.Remove(optionId) ?? false;
	}

	public bool HasFaithDivergence(CharacterData c)
	{
		if (c == null)
		{
			return false;
		}
		if (c.privateFaithId != c.faithId)
		{
			return true;
		}
		return c.personalTenets.Count > 0;
	}

	public void UpdateSecretBelief(CharacterData c)
	{
		if (c != null)
		{
			c.isSecretBeliever = HasFaithDivergence(c);
		}
	}

	private static IReadOnlyList<string> GetTraitIds(CharacterData c)
	{
		List<string> list = new List<string>();
		if (c?.traits != null)
		{
			foreach (PersonalityTrait trait in c.traits)
			{
				if (trait != null)
				{
					list.Add(trait.traitId);
				}
			}
		}
		return list;
	}

	public void UpdatePiety(CharacterData c, FaithSystem faith)
	{
		if (c != null)
		{
			float num = 0f;
			if (faith != null)
			{
				num += (float)faith.GetVirtueScore(GetTraitIds(c)) * 0.01f;
				num -= (float)faith.GetSinScore(GetTraitIds(c)) * 0.02f;
			}
			if (c.isSecretBeliever)
			{
				num -= 0.01f;
			}
			c.spiritualFulfillment = Mathf.Clamp(c.spiritualFulfillment + num, 0f, 100f);
		}
	}

	public CharacterData Procreate(int fatherId, int motherId, int birthYear)
	{
		if (fatherId < 0 || motherId < 0)
		{
			return null;
		}
		CharacterData character = GetCharacter(fatherId);
		CharacterData character2 = GetCharacter(motherId);
		if (character == null || character2 == null)
		{
			return null;
		}
		if (!character.isAlive || !character2.isAlive)
		{
			return null;
		}
		if (character.isMale == character2.isMale)
		{
			return null;
		}
		if (character.characterId == character2.characterId)
		{
			return null;
		}
		if (character.age < 16 || character2.age < 16)
		{
			return null;
		}
		if (IsDirectLineage(character, character2))
		{
			return null;
		}
		int raceId = character.raceId;
		int cultureId = character.cultureId;
		int faithId = character.faithId;
		RaceData raceData = ResolveRace(character.raceId);
		RaceData raceData2 = ResolveRace(character2.raceId);
		RaceData expressionRace = raceData ?? raceData2;
		if (raceData != null && raceData2 != null && raceData.raceId != raceData2.raceId)
		{
			expressionRace = AverageRaceBaselines(raceData, raceData2);
		}
		float num = DnaSystem.CalculateInbreeding(character, character2, _characters);
		GenomeInstance genome = character.GetGenome(raceData);
		GenomeInstance genome2 = character2.GetGenome(raceData2);
		bool flag = Random.value < 0.5f;
		GenomeInstance genomeInstance = DnaSystem.InheritGenome(genome, genome2, raceData ?? raceData2, flag, num);
		string firstName = GenerateName(cultureId, (!flag) ? 1 : 0);
		CharacterData characterData = CreateCharacter(firstName, character.lastName, 0, flag, cultureId, raceId, faithId, CharacterRole.Commoner, null, fatherId, motherId, expressionRace, null, genomeInstance);
		if (character.familyId >= 0)
		{
			characterData.familyId = character.familyId;
			if (_families.TryGetValue(character.familyId, out var value))
			{
				value.AddMember(characterData.characterId);
			}
		}
		if (num > 0.05f)
		{
			Debug.Log((object)$"[Character] {character.fullName} × {character2.fullName} 产子 {characterData.fullName}（近亲系数 {num:F3}）");
		}
		return characterData;
	}

	public int CountChildren(int characterId)
	{
		int num = 0;
		foreach (CharacterData value in _characters.Values)
		{
			if (value.fatherId == characterId || value.motherId == characterId)
			{
				num++;
			}
		}
		return num;
	}

	public bool Marry(int aId, int bId, bool allowSameSex = false)
	{
		CharacterData character = GetCharacter(aId);
		CharacterData character2 = GetCharacter(bId);
		if (character == null || character2 == null)
		{
			return false;
		}
		if (!character.isAlive || !character2.isAlive)
		{
			return false;
		}
		if (!allowSameSex && character.isMale == character2.isMale)
		{
			return false;
		}
		if (character.characterId == character2.characterId)
		{
			return false;
		}
		if (character.age < 16 || character2.age < 16)
		{
			return false;
		}
		if (IsDirectLineage(character, character2))
		{
			return false;
		}
		if (character.spouseId >= 0 || character2.spouseId >= 0)
		{
			return false;
		}
		character.spouseId = character2.characterId;
		character2.spouseId = character.characterId;
		LinkSpouses(character, character2);
		return true;
	}

	private void LinkSpouses(CharacterData a, CharacterData b)
	{
		a.ModifyRelation(b.characterId, 20f, "marriage");
		b.ModifyRelation(a.characterId, 20f, "marriage");
		SetRelationType(a, b.characterId, RelationshipType.Spouse);
		SetRelationType(b, a.characterId, RelationshipType.Spouse);
		CreateBond(a.characterId, b.characterId, BondType.Romance);
	}

	private static void SetRelationType(CharacterData owner, int otherId, RelationshipType type)
	{
		if (owner.relations.TryGetValue(otherId, out CharacterRelation relation))
		{
			relation.type = type;
			owner.relations[otherId] = relation;
		}
	}

	/// <summary>性格轴 → 签名特质（教育/事件系统尚未落地，先用性格阈值作为生产端）。</summary>
	private void GrantSignatureTraits(CharacterData c)
	{
		GrantTraitByAxis(c, PersonalityDimension.Boldness, "brave", "craven");
		GrantTraitByAxis(c, PersonalityDimension.Compassion, "compassionate", "callous");
		GrantTraitByAxis(c, PersonalityDimension.Greed, "greedy", "generous");
		GrantTraitByAxis(c, PersonalityDimension.Honor, "honest", "deceitful");
		GrantTraitByAxis(c, PersonalityDimension.Rationality, "calm", "arbitrary");
		GrantTraitByAxis(c, PersonalityDimension.Vengefulness, "vengeful", "forgiving");
		GrantTraitByAxis(c, PersonalityDimension.Piety, "zealous", "cynical");
		GrantTraitByAxis(c, PersonalityDimension.Sociability, "gregarious", "shy");
	}

	private static void GrantTraitByAxis(CharacterData c, PersonalityDimension dim, string highBase, string lowBase)
	{
		float value = c.GetPersonalityValue(dim);
		if (Mathf.Abs(value) < 40f)
		{
			return;
		}
		string baseId = value > 0f ? highBase : lowBase;
		int tier = Mathf.Abs(value) >= 80f ? 3 : (Mathf.Abs(value) >= 60f ? 2 : 1);
		PersonalityTrait trait = MyriadCreation.Simulation.Definitions.PersonalityTraitDatabase.Get(baseId + "_" + tier);
		if (trait != null)
		{
			c.AddTrait(trait);
		}
	}

	/// <summary>特质授予生产端：先查特质量表，再去重授予。</summary>
	public bool AddTrait(int characterId, string traitId)
	{
		CharacterData character = GetCharacter(characterId);
		if (character == null || string.IsNullOrEmpty(traitId) || character.HasTrait(traitId))
		{
			return false;
		}
		PersonalityTrait trait = MyriadCreation.Simulation.Definitions.PersonalityTraitDatabase.Get(traitId);
		if (trait == null)
		{
			return false;
		}
		character.AddTrait(trait);
		return true;
	}

	public CharacterData GetSpouse(int characterId)
	{
		CharacterData character = GetCharacter(characterId);
		if (character == null || character.spouseId < 0)
		{
			return null;
		}
		return GetCharacter(character.spouseId);
	}

	public List<CharacterData> GetChildren(int characterId)
	{
		List<CharacterData> list = new List<CharacterData>();
		foreach (CharacterData value in _characters.Values)
		{
			if (value.fatherId == characterId || value.motherId == characterId)
			{
				list.Add(value);
			}
		}
		return list;
	}

	public List<CharacterData> GetSiblings(int characterId)
	{
		CharacterData character = GetCharacter(characterId);
		List<CharacterData> list = new List<CharacterData>();
		if (character == null)
		{
			return list;
		}
		foreach (CharacterData value in _characters.Values)
		{
			if (value.characterId != characterId && ((character.fatherId >= 0 && value.fatherId == character.fatherId) || (character.motherId >= 0 && value.motherId == character.motherId)))
			{
				list.Add(value);
			}
		}
		return list;
	}

	public List<CharacterData> GetAncestors(int characterId, int maxDepth = 4)
	{
		List<CharacterData> list = new List<CharacterData>();
		CharacterData characterData = GetCharacter(characterId);
		int num = 0;
		while (characterData != null && num < maxDepth)
		{
			CharacterData characterData2 = ((characterData.fatherId >= 0) ? GetCharacter(characterData.fatherId) : null);
			if (characterData2 == null && characterData.motherId >= 0)
			{
				characterData2 = GetCharacter(characterData.motherId);
			}
			if (characterData2 == null)
			{
				break;
			}
			list.Add(characterData2);
			characterData = characterData2;
			num++;
		}
		return list;
	}

	public List<CharacterData> GetGrandchildren(int characterId)
	{
		List<CharacterData> list = new List<CharacterData>();
		foreach (CharacterData child in GetChildren(characterId))
		{
			list.AddRange(GetChildren(child.characterId));
		}
		return list;
	}

	public string BuildFamilyTreeText(int characterId)
	{
		StringBuilder stringBuilder = new StringBuilder();
		CharacterData character = GetCharacter(characterId);
		if (character == null)
		{
			return "（无角色）";
		}
		stringBuilder.AppendLine(string.Format("◆ {0} {1}（{2}岁，{3}）", character.firstName, character.lastName, character.age, character.isMale ? "男" : "女"));
		CharacterData spouse = GetSpouse(characterId);
		stringBuilder.AppendLine((spouse != null) ? $"  配偶：{spouse.firstName} {spouse.lastName}（{spouse.age}岁）" : "  配偶：无");
		List<CharacterData> ancestors = GetAncestors(characterId);
		if (ancestors.Count > 0)
		{
			stringBuilder.AppendLine("  祖辈：");
			foreach (CharacterData item in ancestors)
			{
				stringBuilder.AppendLine($"    - {item.firstName} {item.lastName}（{item.age}岁）");
			}
		}
		if (character.fatherId >= 0 || character.motherId >= 0)
		{
			stringBuilder.AppendLine("  父母：");
			if (character.fatherId >= 0)
			{
				CharacterData character2 = GetCharacter(character.fatherId);
				if (character2 != null)
				{
					stringBuilder.AppendLine($"    父 - {character2.firstName} {character2.lastName}（{character2.age}岁）");
				}
			}
			if (character.motherId >= 0)
			{
				CharacterData character3 = GetCharacter(character.motherId);
				if (character3 != null)
				{
					stringBuilder.AppendLine($"    母 - {character3.firstName} {character3.lastName}（{character3.age}岁）");
				}
			}
		}
		List<CharacterData> children = GetChildren(characterId);
		if (children.Count > 0)
		{
			stringBuilder.AppendLine($"  子女（{children.Count}）：");
			foreach (CharacterData item2 in children)
			{
				stringBuilder.AppendLine(string.Format("    - {0} {1}（{2}岁，{3}）", item2.firstName, item2.lastName, item2.age, item2.isMale ? "男" : "女"));
			}
		}
		List<CharacterData> grandchildren = GetGrandchildren(characterId);
		if (grandchildren.Count > 0)
		{
			stringBuilder.AppendLine($"  孙辈（{grandchildren.Count}）：");
			foreach (CharacterData item3 in grandchildren)
			{
				stringBuilder.AppendLine($"    - {item3.firstName} {item3.lastName}（{item3.age}岁）");
			}
		}
		return stringBuilder.ToString();
	}

	private void AutoProcreate(int currentYear)
	{
		List<CharacterData> list = new List<CharacterData>();
		List<CharacterData> list2 = new List<CharacterData>();
		foreach (CharacterData value in _characters.Values)
		{
			if (value.isAlive && value.age >= 16 && value.age <= 45)
			{
				if (value.isMale)
				{
					list.Add(value);
				}
				else
				{
					list2.Add(value);
				}
			}
		}
		if (list.Count == 0 || list2.Count == 0)
		{
			return;
		}
		foreach (CharacterData item in list)
		{
			if (item.spouseId < 0)
			{
				continue;
			}
			CharacterData character = GetCharacter(item.spouseId);
			if (character != null && character.isAlive && character.age >= 16 && character.age <= 45 && CountChildren(item.characterId) < 8 && CountChildren(character.characterId) < 8 && !IsDirectLineage(item, character) && Random.value < 0.0007f)
			{
				CharacterData characterData = Procreate(item.characterId, character.characterId, currentYear);
				if (characterData != null)
				{
					Debug.Log((object)("[Character] 已婚生育：" + item.firstName + "×" + character.firstName + " → " + characterData.firstName + " " + characterData.lastName));
				}
			}
		}
		// P1.6：先收集候选，再统一成婚/生育，避免在枚举人口列表时改关系/家族集合。
		List<(int maleId, int femaleId)> marriages = new List<(int, int)>();
		foreach (CharacterData item2 in list)
		{
			if (item2.spouseId >= 0 || CountChildren(item2.characterId) >= 8)
			{
				continue;
			}
			foreach (CharacterData item3 in list2)
			{
				if (item3.realmId == item2.realmId && CountChildren(item3.characterId) < 8 && !IsDirectLineage(item2, item3) && Random.value < 0.0004f)
				{
					marriages.Add((item2.characterId, item3.characterId));
					break;
				}
			}
		}
		foreach ((int maleId, int femaleId) in marriages)
		{
			if (Marry(maleId, femaleId))
			{
				Procreate(maleId, femaleId, currentYear);
			}
		}
	}

	public FamilyNode CreateFamily(string familyName, int founderId, int foundingYear, int realmId = -1)
	{
		FamilyNode familyNode = new FamilyNode
		{
			familyId = _nextFamilyId++,
			familyName = familyName,
			founderCharacterId = founderId,
			foundingYear = foundingYear,
			holderRealmId = realmId,
			Innovations = Innovations
		};
		familyNode.AddMember(founderId);
		Heraldry.Ensure(familyNode);
		_families[familyNode.familyId] = familyNode;
		if (_characters.TryGetValue(founderId, out var value))
		{
			value.familyId = familyNode.familyId;
		}
		return familyNode;
	}

	public CharacterBond CreateBond(int charAId, int charBId, BondType type)
	{
		CharacterBond characterBond = new CharacterBond
		{
			bondId = _nextBondId++,
			characterAId = charAId,
			characterBId = charBId,
			type = type,
			establishedDay = 0
		};
		_bonds.Add(characterBond);
		return characterBond;
	}

	public IReadOnlyList<CharacterBond> GetAllBonds()
	{
		return _bonds;
	}

	/// <summary>导出全部纽带（列表副本；元素仍是运行时对象，供存档 DTO 转换使用）。</summary>
	public List<CharacterBond> CaptureBonds()
	{
		return new List<CharacterBond>(_bonds);
	}

	/// <summary>
	/// 读档重建纽带：清空现有列表后写入；bondId 无效或两端角色不存在时跳过并告警（不抛异常）。
	/// 自增 id 推进到 max(bondId)+1，避免读档后新建纽带与已恢复实体撞 id。
	/// </summary>
	public void RestoreBonds(List<CharacterBond> bonds)
	{
		_bonds.Clear();
		int maxId = 0;
		if (bonds != null)
		{
			foreach (CharacterBond bond in bonds)
			{
				if (bond == null || bond.bondId <= 0) continue;
				if (GetCharacter(bond.characterAId) == null || GetCharacter(bond.characterBId) == null)
				{
					Debug.LogWarning($"[CharacterManager] 读档跳过纽带 {bond.bondId}：角色 {bond.characterAId}/{bond.characterBId} 不存在");
					continue;
				}
				_bonds.Add(bond);
				if (bond.bondId > maxId) maxId = bond.bondId;
			}
		}
		_nextBondId = maxId + 1;
	}

	public IEnumerable<CharacterData> EnumerateCharacters()
	{
		return _characters.Values;
	}

	private void DailyDiet()
	{
		if (Economy == null || Tiles == null || Realms == null)
		{
			return;
		}
		foreach (CharacterData value2 in _characters.Values)
		{
			if (!value2.isAlive || value2.realmId < 0 || !Realms.TryGetValue(value2.realmId, out var value) || value.coreTiles.Count == 0)
			{
				continue;
			}
			int num = -1;
			using (HashSet<int>.Enumerator enumerator2 = value.coreTiles.GetEnumerator())
			{
				if (enumerator2.MoveNext())
				{
					num = enumerator2.Current;
				}
			}
			if (num < 0 || num >= Tiles.Length)
			{
				continue;
			}
			int regionId = Tiles[num].regionId;
			TradeCenter tradeCenter = Economy.GetTradeCenter(regionId);
			if (tradeCenter != null && tradeCenter.RemoveGoods(0, 1f))
			{
				float num2;
				switch (value2.role)
				{
				case CharacterRole.Noble:
				case CharacterRole.Ruler:
					num2 = 0.04f;
					break;
				case CharacterRole.Commoner:
				case CharacterRole.Military:
					num2 = 0.015f;
					break;
				default:
					num2 = 0.025f;
					break;
				}
				float num3 = num2;
				value2.obesity = Mathf.Clamp(value2.obesity + num3, 0f, 100f);
			}
			else
			{
				value2.obesity = Mathf.Max(0f, value2.obesity - 0.03f);
				value2.stress = Mathf.Min(100f, value2.stress + 2f);
			}
		}
	}

	private void CheckMentalDisorders()
	{
		foreach (CharacterData value in _characters.Values)
		{
			if (!value.isAlive)
			{
				continue;
			}
			if (string.IsNullOrEmpty(value.mentalDisorderId))
			{
				if (value.highStressDays >= 90)
				{
					value.mentalDisorderId = ((Random.value < 0.6f) ? "depression" : "anxiety");
					value.highStressDays = 0;
					Debug.Log((object)("[Mental] " + value.fullName + " 罹患" + MentalHealthSystem.GetDisorderName(value) + "（长期高压）"));
				}
				else if (value.dread > 80f && Random.value < 0.002f)
				{
					value.mentalDisorderId = "paranoia";
					Debug.Log((object)("[Mental] " + value.fullName + " 罹患偏执（深度恐惧）"));
				}
				else if (value.age >= 70 && value.scholarship < 45f)
				{
					float num = 0.0005f * (float)(value.age - 70 + 1) / 10f;
					if (Random.value < num)
					{
						value.mentalDisorderId = "dementia";
						Debug.Log((object)("[Mental] " + value.fullName + " 罹患失智（年迈心智衰退）"));
					}
				}
				continue;
			}
			MentalDisorderDef def = MentalHealthSystem.GetDef(value.mentalDisorderId);
			if (def == null || !def.reversible)
			{
				continue;
			}
			if (value.stress < 30f)
			{
				value.lowStressRecoveryDays++;
				if (value.lowStressRecoveryDays >= 120)
				{
					Debug.Log((object)("[Mental] " + value.fullName + " 从" + def.GetName() + "中康复"));
					value.mentalDisorderId = "";
					value.lowStressRecoveryDays = 0;
				}
			}
			else
			{
				value.lowStressRecoveryDays = Mathf.Max(0, value.lowStressRecoveryDays - 1);
			}
		}
	}

	private void PersonalityOpinionDrift()
	{
		// 只遍历真实存在的关系边，避免 O(人数²) 每日全配对扫描（一年长跑会从几 ms 漂到上百 ms）。
		foreach (CharacterData character in _characters.Values)
		{
			if (!character.isAlive || character.relations == null || character.relations.Count == 0)
			{
				continue;
			}
			// 快照关系边：同一 tick 内婚姻/生育可能新增关系，不能边枚举边改。
			foreach (KeyValuePair<int, CharacterRelation> relation in new List<KeyValuePair<int, CharacterRelation>>(character.relations))
			{
				CharacterData other = GetCharacter(relation.Key);
				if (other == null || !other.isAlive)
				{
					continue;
				}
				float personalityAffinity = character.GetPersonalityAffinity(other);
				if (Mathf.Abs(personalityAffinity) < 0.5f)
				{
					continue;
				}
				CharacterRelation value = relation.Value;
				value.opinion = Mathf.Clamp(value.opinion + personalityAffinity * 0.002f, -200f, 200f);
				character.relations[relation.Key] = value;
			}
		}
	}

	public void AddStress(int characterId, float amount)
	{
		CharacterData character = GetCharacter(characterId);
		if (character != null)
		{
			character.stress = Mathf.Clamp(character.stress + amount, 0f, 100f);
		}
	}

	public void AddDread(int characterId, float amount)
	{
		CharacterData character = GetCharacter(characterId);
		if (character != null)
		{
			character.dread = Mathf.Clamp(character.dread + amount, 0f, 100f);
		}
	}

	public void ModifyPersonality(int characterId, PersonalityDimension dimension, float delta)
	{
		GetCharacter(characterId)?.AddPersonality(dimension, delta);
	}

	public void ModifyPersonality(int characterId, string dimension, float delta)
	{
		if (PersonalityDimensions.TryParse(dimension, out var dim))
		{
			ModifyPersonality(characterId, dim, delta);
		}
	}

	public bool CureMentalDisorder(int characterId)
	{
		CharacterData character = GetCharacter(characterId);
		if (character == null || string.IsNullOrEmpty(character.mentalDisorderId))
		{
			return false;
		}
		MentalDisorderDef def = MentalHealthSystem.GetDef(character.mentalDisorderId);
		if (def != null && !def.reversible)
		{
			return false;
		}
		Debug.Log((object)("[Mental] " + character.fullName + " 经治疗摆脱" + (def?.GetName() ?? "病痛")));
		character.mentalDisorderId = "";
		character.highStressDays = 0;
		character.lowStressRecoveryDays = 0;
		return true;
	}

	private void ApplyTalentDefectEffect(CharacterData c, DnaExpression expr)
	{
		TalentDefectDef talentDefectDef = DnaSystem.FindDef(expr.talentId);
		if (talentDefectDef != null)
		{
			ApplyDef(c, talentDefectDef);
		}
		talentDefectDef = DnaSystem.FindDef(expr.defectId);
		if (talentDefectDef != null)
		{
			ApplyDef(c, talentDefectDef);
		}
	}

	private RaceData ResolveRace(int raceId)
	{
		if (Races != null && Races.TryGetValue(raceId, out var value))
		{
			return value;
		}
		return null;
	}

	public CharacterData CreateCharacter(string firstName, string lastName, int age, bool isMale, int cultureId, int raceId, int faithId, CharacterRole role, DnaData dna = null, int fatherId = -1, int motherId = -1, RaceData expressionRace = null, CharacterTemplateDef template = null, GenomeInstance genomeOverride = null)
	{
		GenomeInstance childGenome = genomeOverride;
		RaceData raceDataForGenome = ResolveRace(raceId);
		GenomeDef genomeDefForChild = GeneticsCatalog.ResolveGenome(raceDataForGenome?.GetGenomeId());
		if (dna != null)
		{
			// 旧档/旧调用方传入的 DnaData：一次性转成 GenomeInstance，之后不再以 dna 为源。
			childGenome = dna.ToGenomeInstance(raceDataForGenome, isMale);
		}
		if (childGenome == null && (fatherId >= 0 || motherId >= 0))
		{
			CharacterData characterData = ((fatherId >= 0) ? GetCharacter(fatherId) : null);
			CharacterData characterData2 = ((motherId >= 0) ? GetCharacter(motherId) : null);
			if (characterData != null || characterData2 != null)
			{
				GenomeInstance fatherGenome = characterData?.GetGenome(ResolveRace(characterData.raceId));
				GenomeInstance motherGenome = characterData2?.GetGenome(ResolveRace(characterData2.raceId));
				if (genomeDefForChild != null && (fatherGenome != null || motherGenome != null))
				{
					var geneticsRng = new System.Random(_nextCharacterId * 397 + age * 17 + (isMale ? 1 : 0));
					childGenome = GeneticsEngine.Reproduce(
						genomeDefForChild, fatherGenome, motherGenome,
						isMale ? GeneticSex.Male : GeneticSex.Female,
						geneticsRng);
				}
			}
		}
		if (childGenome == null)
		{
			if (genomeDefForChild != null)
			{
				childGenome = GeneticsEngine.CreateFounder(
					genomeDefForChild,
					isMale ? GeneticSex.Male : GeneticSex.Female,
					new System.Random(_nextCharacterId * 397 + age * 17));
			}
			else
			{
				// 内容缺基因组：保留旧 DnaData 生成，避免无基因人物；这是兼容路径，不是主路径。
				dna = DnaSystem.GenerateRandom(raceDataForGenome);
				childGenome = dna.ToGenomeInstance(raceDataForGenome, isMale);
			}
		}
		// 运行态不再保留 DnaData：表型/魅力/疾病均由 genome 推导。
		dna = null;
		CharacterData characterData3 = new CharacterData
		{
			characterId = _nextCharacterId++,
			firstName = firstName,
			lastName = lastName,
			age = age,
			isMale = isMale,
			birthDay = Random.Range(1, 365),
			birthYear = 0,
			cultureId = cultureId,
			raceId = raceId,
			faithId = faithId,
			role = role,
			aiCategory = CharacterAISelector.CategoryForRole(role),
			fatherId = fatherId,
			motherId = motherId,
			dna = null,
			genome = childGenome
		};
		RaceData raceData = expressionRace ?? ResolveRace(raceId);
		DnaExpression expr = (characterData3.dnaExpression = DnaSystem.ComputeExpression(childGenome, raceData));
		characterData3.bodyParts = BodyPartSystem.InitializeBodyParts();
		characterData3.expectedLifespanYears = Mathf.Clamp((raceData?.lifespanBaseYears ?? 75f) + expr.longevityOffsetYears, 20f, 150f);
		characterData3.individualResistance = Mathf.Clamp((raceData?.resistanceBaseline ?? 50f) + expr.resistanceOffset, 0f, 100f);
		characterData3.basicAbilities = BasicAbilities.Random(50f, 12f);
		characterData3.basicAbilities.strength = Mathf.Clamp(characterData3.basicAbilities.strength + expr.martialOffset, 5f, 95f);
		characterData3.basicAbilities.coordination = Mathf.Clamp(characterData3.basicAbilities.coordination + expr.martialOffset, 5f, 95f);
		characterData3.basicAbilities.endurance = Mathf.Clamp(characterData3.basicAbilities.endurance + expr.martialOffset, 5f, 95f);
		characterData3.basicAbilities.memory = Mathf.Clamp(characterData3.basicAbilities.memory + expr.intelligenceOffset, 5f, 95f);
		characterData3.basicAbilities.logic = Mathf.Clamp(characterData3.basicAbilities.logic + expr.intelligenceOffset, 5f, 95f);
		characterData3.basicAbilities.inspiration = Mathf.Clamp(characterData3.basicAbilities.inspiration + expr.intelligenceOffset, 5f, 95f);
		// M3：专精 = f(基本能力, 训练)；暂无教育系统，先给统一训练基线。
		float trainingBaseline = 0.6f;
		characterData3.prowess = SpecialtyDerivation.Derive(SpecialtyType.Prowess, characterData3.basicAbilities, trainingBaseline);
		characterData3.social = SpecialtyDerivation.Derive(SpecialtyType.Social, characterData3.basicAbilities, trainingBaseline);
		characterData3.military = SpecialtyDerivation.Derive(SpecialtyType.Military, characterData3.basicAbilities, trainingBaseline);
		characterData3.management = SpecialtyDerivation.Derive(SpecialtyType.Management, characterData3.basicAbilities, trainingBaseline);
		characterData3.conspiracy = SpecialtyDerivation.Derive(SpecialtyType.Conspiracy, characterData3.basicAbilities, trainingBaseline);
		characterData3.scholarship = SpecialtyDerivation.Derive(SpecialtyType.Scholarship, characterData3.basicAbilities, trainingBaseline);
		ApplyTalentDefectEffect(characterData3, expr);
		InitializePersonality(characterData3, fatherId, motherId);
		float num3;
		if (DnaSystem.IsAppearanceHomozygous(childGenome, raceData))
		{
			num3 = 5f;
		}
		else
		{
			num3 = -3f;
		}
		characterData3.charm = Mathf.Clamp(50f + num3 + Random.Range(-10f, 10f), 0f, 100f);
		if (template != null)
		{
			ApplyTemplate(characterData3, template);
		}
		characterData3.SyncClassFromRole();
		_characters[characterData3.characterId] = characterData3;
		GrantSignatureTraits(characterData3);
		return characterData3;
	}

	public void ApplyTemplate(CharacterData c, CharacterTemplateDef template)
	{
		if (c == null || template == null)
		{
			return;
		}
		if (c.age <= 0)
		{
			int num = Mathf.Max(0, template.minAge);
			int num2 = Mathf.Max(0, template.maxAge);
			if (num2 > num && num2 > 0)
			{
				c.age = Random.Range(num, num2 + 1);
			}
			else if (num2 > 0)
			{
				c.age = num2;
			}
			else if (num > 0)
			{
				c.age = num;
			}
		}
		float[] array = new float[6] { c.prowess, c.social, c.military, c.management, c.conspiracy, c.scholarship };
		for (int i = 0; i < 6; i++)
		{
			if (template.statMin != null && i < template.statMin.Length && template.statMin[i] > 0f)
			{
				array[i] = Mathf.Max(array[i], template.statMin[i]);
			}
			if (template.statMax != null && i < template.statMax.Length && template.statMax[i] > 0f)
			{
				array[i] = Mathf.Min(array[i], template.statMax[i]);
			}
		}
		c.prowess = array[0];
		c.social = array[1];
		c.military = array[2];
		c.management = array[3];
		c.conspiracy = array[4];
		c.scholarship = array[5];
		PersonalityDimension[] all = PersonalityDimensions.All;
		foreach (PersonalityDimension dim in all)
		{
			c.AddPersonality(dim, template.GetPersonalityBias(dim));
		}
	}

	private void InitializePersonality(CharacterData c, int fatherId, int motherId)
	{
		CharacterData characterData = ((fatherId >= 0) ? GetCharacter(fatherId) : null);
		CharacterData characterData2 = ((motherId >= 0) ? GetCharacter(motherId) : null);
		float num = ((characterData != null) ? 1f : 0f);
		float num2 = ((characterData2 != null) ? 1f : 0f);
		float num3 = num + num2;
		PersonalityDimension[] all = PersonalityDimensions.All;
		foreach (PersonalityDimension dim in all)
		{
			if (num3 > 0f)
			{
				float num4 = (characterData?.GetPersonalityValue(dim) ?? 0f) * num / num3 + (characterData2?.GetPersonalityValue(dim) ?? 0f) * num2 / num3;
				c.SetPersonalityValue(dim, num4 + Random.Range(-10f, 10f));
			}
			else
			{
				c.SetPersonalityValue(dim, Random.Range(-30f, 30f));
			}
		}
	}

	private static void ApplyDef(CharacterData c, TalentDefectDef def)
	{
		switch (def.stat)
		{
		case "scholarship":
			c.scholarship = Mathf.Clamp(c.scholarship + def.amount, 0f, 100f);
			break;
		case "prowess":
			c.prowess = Mathf.Clamp(c.prowess + def.amount, 0f, 100f);
			break;
		case "lifespan":
			c.expectedLifespanYears = Mathf.Max(20f, c.expectedLifespanYears + def.amount);
			break;
		case "appearance":
		{
			ref string appearanceTag = ref c.dnaExpression.appearanceTag;
			appearanceTag = appearanceTag + "（" + def.name + "）";
			break;
		}
		}
	}

	private static RaceData AverageRaceBaselines(RaceData a, RaceData b)
	{
		RaceData raceData = new RaceData();
		raceData.raceId = -1;
		raceData.raceName = "混血(" + a.raceName + "+" + b.raceName + ")";
		raceData.intelligenceBaseline = (a.intelligenceBaseline + b.intelligenceBaseline) * 0.5f;
		raceData.martialBaseline = (a.martialBaseline + b.martialBaseline) * 0.5f;
		raceData.lifespanBaseYears = (a.lifespanBaseYears + b.lifespanBaseYears) * 0.5f;
		raceData.lifespanRangeYears = (a.lifespanRangeYears + b.lifespanRangeYears) * 0.5f;
		raceData.resistanceBaseline = (a.resistanceBaseline + b.resistanceBaseline) * 0.5f;
		return raceData;
	}

	private static bool IsDirectLineage(CharacterData a, CharacterData b)
	{
		if (a.fatherId == b.characterId || a.motherId == b.characterId)
		{
			return true;
		}
		if (b.fatherId == a.characterId || b.motherId == a.characterId)
		{
			return true;
		}
		if (a.fatherId >= 0 && a.fatherId == b.fatherId && a.motherId >= 0)
		{
			return a.motherId == b.motherId;
		}
		return false;
	}

	private static string GenerateName(int cultureId, int type)
	{
		if (ContentResolvers.Cultures.TryGet(cultureId, out var value))
		{
			return ContentQueries.GetRandomName(value, type);
		}
		return "无名";
	}

	public void CreateInitialRulers(Dictionary<int, RealmData> realms, int currentYear)
	{
		foreach (RealmData value3 in realms.Values)
		{
			int realmId = value3.realmId;
			int raceId = 0;
			string text = GenerateName(realmId, 2);
			bool flag = Random.value < 0.5f;
			ContentResolvers.CharacterTemplates.TryGet("tmpl_ruler", out var value);
			ContentResolvers.CharacterTemplates.TryGet("tmpl_spouse", out var value2);
			CharacterData characterData = CreateCharacter(GenerateName(realmId, (!flag) ? 1 : 0), text, 0, flag, realmId, raceId, 0, CharacterRole.Ruler, null, -1, -1, null, value);
			characterData.realmId = value3.realmId;
			characterData.isPublicFigure = true;
			value3.monarchId = characterData.characterId;
			characterData.aiCategory = CharacterAICategory.Ruler;
			CharacterKnowledgeRegistry.Learn(value3.realmId, characterData, 1, "court");
			CharacterData characterData2 = CreateCharacter(GenerateName(realmId, flag ? 1 : 0), text, 0, !flag, realmId, raceId, 0, CharacterRole.Spouse, null, -1, -1, null, value2);
			characterData2.realmId = value3.realmId;
			characterData2.isPublicFigure = true;
			CharacterKnowledgeRegistry.Learn(value3.realmId, characterData2, 1, "court");
			FamilyNode familyNode = CreateFamily(text, characterData.characterId, currentYear, value3.realmId);
			familyNode.AddMember(characterData2.characterId);
			characterData2.familyId = familyNode.familyId;
		}
	}

	public void DailyTick(int currentDay, int currentYear)
	{
		foreach (CharacterData value in _characters.Values)
		{
			value.DailyTick(currentDay, currentYear);
		}
		foreach (CharacterBond bond in _bonds)
		{
			bond.DailyTick();
		}
		AutoProcreate(currentYear);
		DailyDiet();
		CheckMentalDisorders();
		PersonalityOpinionDrift();
		foreach (CharacterData value2 in _characters.Values)
		{
			if (!value2.isAlive && value2.commandedArmyId >= 0)
			{
				Debug.Log((object)$"[Character] {value2.fullName} 死亡，军队 {value2.commandedArmyId} 失去指挥");
				value2.commandedArmyId = -1;
			}
		}
	}

	public CharacterData GetCharacter(int id)
	{
		if (!_characters.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public FamilyNode GetFamily(int id)
	{
		if (!_families.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public List<CharacterData> GetAliveCharacters()
	{
		List<CharacterData> list = new List<CharacterData>();
		foreach (CharacterData value in _characters.Values)
		{
			if (value.isAlive)
			{
				list.Add(value);
			}
		}
		return list;
	}

	public List<CharacterData> GetCharactersByRealm(int realmId)
	{
		List<CharacterData> list = new List<CharacterData>();
		foreach (CharacterData value in _characters.Values)
		{
			if (value.realmId == realmId && value.isAlive)
			{
				list.Add(value);
			}
		}
		return list;
	}

	public List<CharacterData> GetCharactersByRole(CharacterRole role)
	{
		List<CharacterData> list = new List<CharacterData>();
		foreach (CharacterData value in _characters.Values)
		{
			if (value.role == role && value.isAlive)
			{
				list.Add(value);
			}
		}
		return list;
	}

	public CharacterData FindRulerOfRealm(int realmId)
	{
		foreach (CharacterData value in _characters.Values)
		{
			if (value.isAlive && value.realmId == realmId && value.role == CharacterRole.Ruler)
			{
				return value;
			}
		}
		return null;
	}

	public CharacterData FindBestGeneral(int realmId)
	{
		CharacterData result = null;
		float num = 0f;
		foreach (CharacterData value in _characters.Values)
		{
			if (value.realmId == realmId && value.isAlive)
			{
				float num2 = value.CalculateCommandAbility();
				if (num2 > num)
				{
					num = num2;
					result = value;
				}
			}
		}
		return result;
	}

	public CharacterData FindBestRuler(int realmId)
	{
		CharacterData result = null;
		float num = 0f;
		foreach (CharacterData value in _characters.Values)
		{
			if (value.realmId == realmId && value.isAlive)
			{
				float num2 = value.CalculateRuleAbility();
				if (num2 > num)
				{
					num = num2;
					result = value;
				}
			}
		}
		return result;
	}

	public int GetTotalCharacterCount()
	{
		return _characters.Count;
	}

	public int GetAliveCharacterCount()
	{
		return GetAliveCharacters().Count;
	}

	public int GetTotalFamilyCount()
	{
		return _families.Count;
	}

	public IReadOnlyDictionary<int, CharacterData> GetAllCharacters()
	{
		return _characters;
	}

	public IReadOnlyDictionary<int, FamilyNode> GetAllFamilies()
	{
		return _families;
	}
}
}
