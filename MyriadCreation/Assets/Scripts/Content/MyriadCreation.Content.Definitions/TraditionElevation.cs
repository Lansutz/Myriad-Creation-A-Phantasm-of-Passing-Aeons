namespace MyriadCreation.Content.Definitions
{
public static class TraditionElevation
{
	public static int Rank(CharacterRole role)
	{
		return role switch
		{
			CharacterRole.Commoner => 0, 
			CharacterRole.Spouse => 1, 
			CharacterRole.Heir => 1, 
			CharacterRole.Courtier => 1, 
			CharacterRole.Merchant => 2, 
			CharacterRole.Military => 2, 
			CharacterRole.Clergy => 2, 
			CharacterRole.Scholar => 2, 
			CharacterRole.Noble => 3, 
			CharacterRole.Ruler => 4, 
			_ => 0, 
		};
	}
}
}
