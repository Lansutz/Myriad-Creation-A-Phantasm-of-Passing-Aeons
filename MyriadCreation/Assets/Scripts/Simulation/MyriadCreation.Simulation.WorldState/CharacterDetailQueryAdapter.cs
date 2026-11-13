using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using UnityEngine;

namespace MyriadCreation.Simulation.WorldState
{
/// <summary>
/// UI 4e：ICharacterDetailQuery 的 Simulation host 适配器。
/// 转调 CharacterManager/CharacterData + RealmData officeHolders/家族关系；null/未知 id 返回 null。
/// </summary>
public sealed class CharacterDetailQueryAdapter : ICharacterDetailQuery
{
	private readonly GameWorld _world;

	public CharacterDetailQueryAdapter(GameWorld world)
	{
		_world = world;
	}

	public CharacterDetailDto GetCharacterDetail(int characterId)
	{
		CharacterManager manager = _world?.GetCharacterManager();
		CharacterData data = manager?.GetCharacter(characterId);
		if (data == null)
		{
			return null;
		}

		RealmData realm = null;
		if (_world?.realms != null)
		{
			_world.realms.TryGetValue(data.realmId, out realm);
		}
		FamilyNode family = null;
		if (manager != null && data.familyId >= 0)
		{
			family = manager.GetFamily(data.familyId);
		}
		int officeTitleId = FindOfficeTitleId(realm, characterId);
		string officeName = "";
		if (officeTitleId >= 0)
		{
			CultureData culture = null;
			if (_world?.cultures != null && realm != null)
			{
				_world.cultures.TryGetValue(realm.primaryCultureId, out culture);
			}
			officeName = OfficeTitleCatalog.GetTitle(culture, (OfficialOffice)officeTitleId, "Kingdom");
		}

		CharacterDetailDto dto = new CharacterDetailDto
		{
			characterId = data.characterId,
			name = data.fullName ?? "",
			age = data.age,
			isMale = data.isMale,
			isAlive = data.isAlive,
			realmId = data.realmId,
			realmName = realm?.realmName ?? "",
			roleId = (int)data.role,
			roleName = RoleName(data.role),
			officeTitleId = officeTitleId,
			officeName = officeName,
			cultureId = data.cultureId,
			cultureName = _world?.CultureQuery?.GetCultureName(data.cultureId) ?? "",
			faithId = data.faithId,
			faithName = _world?.FaithQuery?.GetFaithName(data.faithId) ?? "",
			personality = data.GetPersonalityDescription() ?? "",
			dynastyName = family?.familyName ?? "",
			familyName = family != null && !string.IsNullOrEmpty(family.familyName)
				? family.familyName
				: (data.lastName ?? ""),
			familyId = data.familyId,
			isRuler = data.role == CharacterRole.Ruler,
			isHeir = data.role == CharacterRole.Heir,
			prowess = data.prowess,
			social = data.social,
			military = data.military,
			management = data.management,
			conspiracy = data.conspiracy,
			scholarship = data.scholarship,
			prestige = data.prestige,
			health = data.health,
			stress = data.stress,
			piety = data.spiritualFulfillment,
			father = ToFamilyMember(manager, data.fatherId, "父亲"),
			mother = ToFamilyMember(manager, data.motherId, "母亲"),
			spouse = ToFamilyMember(manager, data.spouseId, "配偶")
		};

		List<FamilyMemberDto> children = new List<FamilyMemberDto>();
		List<CharacterData> childData = manager?.GetChildren(characterId);
		if (childData != null)
		{
			foreach (CharacterData child in childData)
			{
				if (child != null)
				{
					children.Add(ToFamilyMember(manager, child.characterId, "子女"));
				}
			}
		}
		dto.children = children;

		List<string> traits = new List<string>();
		if (data.traits != null)
		{
			foreach (PersonalityTrait trait in data.traits)
			{
				if (trait == null)
				{
					continue;
				}
				string label = string.IsNullOrEmpty(trait.traitName) ? trait.traitId : trait.traitName;
				if (!string.IsNullOrEmpty(label))
				{
					traits.Add(label);
				}
			}
		}
		dto.traits = traits;

		List<FamilyMemberDto> siblings = new List<FamilyMemberDto>();
		List<CharacterData> siblingData = manager?.GetSiblings(characterId);
		if (siblingData != null)
		{
			foreach (CharacterData sibling in siblingData)
			{
				if (sibling != null)
				{
					siblings.Add(ToFamilyMember(manager, sibling.characterId, "兄弟姐妹"));
				}
			}
		}
		dto.siblings = siblings;
		return dto;
	}

	private static int FindOfficeTitleId(RealmData realm, int characterId)
	{
		if (realm?.officeHolders == null)
		{
			return -1;
		}
		int best = -1;
		foreach (var pair in realm.officeHolders)
		{
			if (pair.Value == characterId && (best < 0 || pair.Key < best))
			{
				best = pair.Key;
			}
		}
		return best;
	}

	private static FamilyMemberDto ToFamilyMember(CharacterManager manager, int characterId, string relation)
	{
		if (characterId < 0)
		{
			return null;
		}
		CharacterData person = manager?.GetCharacter(characterId);
		if (person == null)
		{
			return null;
		}
		return new FamilyMemberDto
		{
			characterId = person.characterId,
			name = person.fullName ?? "",
			age = person.age,
			isAlive = person.isAlive,
			relation = relation
		};
	}

	private static string RoleName(CharacterRole role)
	{
		switch (role)
		{
		case CharacterRole.Noble: return "贵族";
		case CharacterRole.Clergy: return "神职";
		case CharacterRole.Merchant: return "商人";
		case CharacterRole.Military: return "军人";
		case CharacterRole.Scholar: return "学者";
		case CharacterRole.Ruler: return "统治者";
		case CharacterRole.Heir: return "继承人";
		case CharacterRole.Spouse: return "配偶";
		case CharacterRole.Courtier: return "廷臣";
		case CharacterRole.Courier: return "使者";
		default: return "平民";
		}
	}
}
}
