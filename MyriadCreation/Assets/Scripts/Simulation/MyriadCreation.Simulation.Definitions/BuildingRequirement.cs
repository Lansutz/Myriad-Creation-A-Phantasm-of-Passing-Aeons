using System;
using MyriadCreation.Core.Enums;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Simulation.Definitions
{
[Serializable]
public struct BuildingRequirement
{
	public string name;

	public bool requireLand;

	public bool requireCoast;

	public bool requireRiver;

	public bool requireRiverMouth;

	public bool requireMountain;

	public bool requireHills;

	public bool requirePlain;

	public bool requireDesert;

	public bool requireIsland;

	public bool requireBottleneck;

	public float minElevation;

	public float maxElevation;

	public float minSlope;

	public float maxSlope;

	public float minPrecipitation;

	public float maxPrecipitation;

	public float minTemperature;

	public float maxTemperature;

	public GameEnums.BiomeType[] allowedBiomes;

	public GameEnums.BiomeType[] forbiddenBiomes;

	public GameEnums.OceanTier[] allowedOceanTiers;

	public bool requireExistingSettlement;

	public SettlementLevel minSettlementLevel;

	public SettlementLevel minExistingSettlementLevel;

	public float minDevelopment;

	public int minTechLevel;

	public string[] requiredGovernmentTypes;

	public string description;

	public static BuildingRequirement Default()
	{
		return new BuildingRequirement
		{
			name = "默认",
			requireLand = true,
			requireCoast = false,
			requireRiver = false,
			requireRiverMouth = false,
			requireMountain = false,
			requireHills = false,
			requirePlain = false,
			requireDesert = false,
			requireIsland = false,
			requireBottleneck = false,
			minElevation = 0f,
			maxElevation = 1f,
			minSlope = 0f,
			maxSlope = 90f,
			minPrecipitation = 0f,
			maxPrecipitation = 10000f,
			minTemperature = -50f,
			maxTemperature = 60f,
			allowedBiomes = Array.Empty<GameEnums.BiomeType>(),
			forbiddenBiomes = Array.Empty<GameEnums.BiomeType>(),
			allowedOceanTiers = Array.Empty<GameEnums.OceanTier>(),
			requireExistingSettlement = false,
			minSettlementLevel = SettlementLevel.LevelI,
			minExistingSettlementLevel = SettlementLevel.LevelI,
			minDevelopment = 0f,
			minTechLevel = 0,
			requiredGovernmentTypes = Array.Empty<string>(),
			description = ""
		};
	}
}
}
