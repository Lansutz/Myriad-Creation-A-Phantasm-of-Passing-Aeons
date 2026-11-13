using System;
using System.Collections.Generic;
using System.IO;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Enums;
using MyriadCreation.World.Settlement;
using UnityEngine;

namespace MyriadCreation.Simulation.Definitions
{
[Serializable]
public sealed class BuildableRequirementDef
{
	public int buildableType;
	public string name = "";
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
	public float maxElevation = 1f;
	public float minSlope;
	public float maxSlope = 90f;
	public float minPrecipitation;
	public float maxPrecipitation = 10000f;
	public float minTemperature = -50f;
	public float maxTemperature = 60f;
	public int[] allowedBiomes = Array.Empty<int>();
	public int[] forbiddenBiomes = Array.Empty<int>();
	public int[] allowedOceanTiers = Array.Empty<int>();
	public bool requireExistingSettlement;
	public int minSettlementLevel;
	public int minExistingSettlementLevel;
	public float minDevelopment;
	public int minTechLevel;
	public string[] requiredGovernmentTypes = Array.Empty<string>();
	public string description = "";
}

[Serializable]
internal sealed class BuildableRequirementListWrapper
{
	public List<BuildableRequirementDef> buildableTypes = new List<BuildableRequirementDef>();
}

/// <summary>
/// 聚落形态建造门槛外提（P2.4）：从 StreamingAssets/Base/Buildable/BuildableTypes.json 懒加载；
/// 缺失/非法时返回空表，由 BuildingAvailabilitySystem 回退到代码表。
/// </summary>
public static class BuildableRequirementLoader
{
	private static bool _attempted;

	private static Dictionary<BuildableType, BuildingRequirement> _requirements;

	public static bool HasContent => _requirements != null && _requirements.Count > 0;

	public static IReadOnlyDictionary<BuildableType, BuildingRequirement> Requirements => _requirements;

	public static bool EnsureLoaded()
	{
		if (!_attempted)
		{
			LoadFromFile(ResolveDefaultPath());
		}
		return HasContent;
	}

	public static string ResolveDefaultPath()
	{
		string root = Application.streamingAssetsPath;
		return string.IsNullOrEmpty(root) ? "" : Path.Combine(root, "Base/Buildable/BuildableTypes.json");
	}

	public static bool LoadFromFile(string path)
	{
		_attempted = true;
		_requirements = null;
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return false;
		}
		try
		{
			BuildableRequirementListWrapper wrapper =
				JsonUtility.FromJson<BuildableRequirementListWrapper>(File.ReadAllText(path));
			if (wrapper?.buildableTypes == null || wrapper.buildableTypes.Count == 0)
			{
				return false;
			}
			Dictionary<BuildableType, BuildingRequirement> map = new Dictionary<BuildableType, BuildingRequirement>();
			for (int i = 0; i < wrapper.buildableTypes.Count; i++)
			{
				BuildableRequirementDef def = wrapper.buildableTypes[i];
				if (def == null || !Enum.IsDefined(typeof(BuildableType), def.buildableType))
				{
					continue;
				}
				map[(BuildableType)def.buildableType] = new BuildingRequirement
				{
					name = def.name ?? "",
					requireLand = def.requireLand,
					requireCoast = def.requireCoast,
					requireRiver = def.requireRiver,
					requireRiverMouth = def.requireRiverMouth,
					requireMountain = def.requireMountain,
					requireHills = def.requireHills,
					requirePlain = def.requirePlain,
					requireDesert = def.requireDesert,
					requireIsland = def.requireIsland,
					requireBottleneck = def.requireBottleneck,
					minElevation = def.minElevation,
					maxElevation = def.maxElevation,
					minSlope = def.minSlope,
					maxSlope = def.maxSlope,
					minPrecipitation = def.minPrecipitation,
					maxPrecipitation = def.maxPrecipitation,
					minTemperature = def.minTemperature,
					maxTemperature = def.maxTemperature,
					allowedBiomes = ToBiomes(def.allowedBiomes),
					forbiddenBiomes = ToBiomes(def.forbiddenBiomes),
					allowedOceanTiers = ToOceanTiers(def.allowedOceanTiers),
					requireExistingSettlement = def.requireExistingSettlement,
					minSettlementLevel = (SettlementLevel)ClampEnum(def.minSettlementLevel, typeof(SettlementLevel)),
					minExistingSettlementLevel = (SettlementLevel)ClampEnum(def.minExistingSettlementLevel, typeof(SettlementLevel)),
					minDevelopment = def.minDevelopment,
					minTechLevel = def.minTechLevel,
					requiredGovernmentTypes = def.requiredGovernmentTypes ?? Array.Empty<string>(),
					description = def.description ?? ""
				};
			}
			_requirements = map;
			return HasContent;
		}
		catch (Exception ex)
		{
			Debug.LogError("[BuildableRequirementLoader] 读取 BuildableTypes.json 失败：" + ex.Message);
			return false;
		}
	}

	private static GameEnums.BiomeType[] ToBiomes(int[] values)
	{
		if (values == null || values.Length == 0)
		{
			return Array.Empty<GameEnums.BiomeType>();
		}
		List<GameEnums.BiomeType> list = new List<GameEnums.BiomeType>(values.Length);
		for (int i = 0; i < values.Length; i++)
		{
			if (Enum.IsDefined(typeof(GameEnums.BiomeType), values[i]))
			{
				list.Add((GameEnums.BiomeType)values[i]);
			}
		}
		return list.ToArray();
	}

	private static GameEnums.OceanTier[] ToOceanTiers(int[] values)
	{
		if (values == null || values.Length == 0)
		{
			return Array.Empty<GameEnums.OceanTier>();
		}
		List<GameEnums.OceanTier> list = new List<GameEnums.OceanTier>(values.Length);
		for (int i = 0; i < values.Length; i++)
		{
			if (Enum.IsDefined(typeof(GameEnums.OceanTier), values[i]))
			{
				list.Add((GameEnums.OceanTier)values[i]);
			}
		}
		return list.ToArray();
	}

	private static int ClampEnum(int value, Type enumType)
	{
		return Enum.IsDefined(enumType, value) ? value : 0;
	}
}
}
