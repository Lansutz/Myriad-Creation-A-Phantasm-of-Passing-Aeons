using MyriadCreation.Core.Enums;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
/// <summary>
/// 角色 → 身份旗标（P2.5/M5 人格效力）：与阶层映射分开，避免“贵族也算教士”。
/// 外国人由调用方按“是否本政权”补旗标。
/// </summary>
public static class CharacterIdentity
{
	public static SocialIdentityFlags Of(CharacterData character)
	{
		if (character == null)
		{
			return SocialIdentityFlags.None;
		}
		switch (character.role)
		{
			case CharacterRole.Ruler:
			case CharacterRole.Heir:
			case CharacterRole.Spouse:
				return SocialIdentityFlags.Royalty | SocialIdentityFlags.Noble;
			case CharacterRole.Noble:
			case CharacterRole.Military:
			case CharacterRole.Courtier:
				return SocialIdentityFlags.Noble;
			case CharacterRole.Clergy:
				return SocialIdentityFlags.Clergy;
			case CharacterRole.Merchant:
			case CharacterRole.Scholar:
				return SocialIdentityFlags.Citizen;
			case CharacterRole.Commoner:
				return SocialIdentityFlags.Freedman;
			default:
				return SocialIdentityFlags.Citizen;
		}
	}

	public static SocialIdentityFlags Of(CharacterData character, int realmId)
	{
		SocialIdentityFlags identity = Of(character);
		if (character != null && realmId >= 0 && character.realmId != realmId)
		{
			identity |= SocialIdentityFlags.Foreigner;
		}
		return identity;
	}
}
}
