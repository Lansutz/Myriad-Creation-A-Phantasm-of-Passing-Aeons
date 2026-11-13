using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
public static class CharacterAISelector
{
	public static CharacterAICategory CategoryForRole(CharacterRole role)
	{
		switch (role)
		{
		case CharacterRole.Ruler:
		case CharacterRole.Heir:
		case CharacterRole.Spouse:
			return CharacterAICategory.Ruler;
		case CharacterRole.Noble:
		case CharacterRole.Courtier:
			return CharacterAICategory.Official;
		case CharacterRole.Merchant:
			return CharacterAICategory.Merchant;
		case CharacterRole.Military:
			return CharacterAICategory.Soldier;
		case CharacterRole.Scholar:
			return CharacterAICategory.Scholar;
		case CharacterRole.Clergy:
			return CharacterAICategory.Priest;
		case CharacterRole.Courier:
			return CharacterAICategory.Courier;
		default:
			return CharacterAICategory.Worker;
		}
	}
}
}
