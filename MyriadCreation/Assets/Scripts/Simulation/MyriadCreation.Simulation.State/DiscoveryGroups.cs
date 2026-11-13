namespace MyriadCreation.Simulation.State
{
public static class DiscoveryGroups
{
	public const string Merchant = "merchant";

	public const string Artisan = "artisan";

	public const string Scholar = "scholar";

	public static bool IsValid(string group)
	{
		if (!(group == "merchant") && !(group == "artisan"))
		{
			return group == "scholar";
		}
		return true;
	}
}
}
