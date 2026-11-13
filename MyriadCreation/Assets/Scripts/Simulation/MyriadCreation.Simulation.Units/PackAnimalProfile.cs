using System;

namespace MyriadCreation.Simulation.Units
{
[Serializable]
public struct PackAnimalProfile
{
	public float capacity;

	public float speedBonus;

	public int draftStrength;

	public float minTempC;

	public float maxTempC;

	public float maxHumidityPct;

	public float maxSoilHumidityPct;

	public float maxSlopeDegrees;

	public float maxElevation01;
}
}
