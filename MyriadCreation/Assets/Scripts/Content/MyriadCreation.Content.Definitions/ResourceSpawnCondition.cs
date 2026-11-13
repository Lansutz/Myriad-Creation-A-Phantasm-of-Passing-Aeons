using System;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public struct ResourceSpawnCondition
{
	public float minElevation;

	public float maxElevation;

	public float minPrecipitation;

	public float maxPrecipitation;

	public float minTemperature;

	public float maxTemperature;

	public float maxSlope;

	public bool requiresCoast;

	public bool requiresRiver;

	public static ResourceSpawnCondition Unrestricted => new ResourceSpawnCondition
	{
		minElevation = 0f,
		maxElevation = 1f,
		minPrecipitation = 0f,
		maxPrecipitation = 9999f,
		minTemperature = -999f,
		maxTemperature = 999f,
		maxSlope = 90f,
		requiresCoast = false,
		requiresRiver = false
	};
}
}
