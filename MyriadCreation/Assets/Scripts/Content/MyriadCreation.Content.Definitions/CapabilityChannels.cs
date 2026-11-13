namespace MyriadCreation.Content.Definitions
{
public static class CapabilityChannels
{
	public const string HeatMaxTemperature = "heat.maxTemperature";

	public static float GetBaseValue(string channel)
	{
		_ = channel == "heat.maxTemperature";
		return 0f;
	}
}
}
