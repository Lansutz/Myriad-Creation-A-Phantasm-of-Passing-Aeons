using System.Collections.Generic;
using System.Linq;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.World.Settlement;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class BuildingAvailabilitySystem
{
	private static Dictionary<BuildableType, BuildingRequirement> _requirements;

	private static bool _initialized;

	public static void Initialize()
	{
		if (!_initialized)
		{
			_requirements = new Dictionary<BuildableType, BuildingRequirement>();
			RegisterAllRequirements();
			_initialized = true;
		}
	}

	private static void RegisterAllRequirements()
	{
		Register(BuildableType.Barrier, new BuildingRequirement
		{
			name = "关隘",
			requireBottleneck = true,
			requireLand = true,
			minSlope = 5f,
			maxSlope = 40f,
			description = "必须建在狭窄通道（山口/峡谷/海峡），直接阻挡敌对势力通行，需攻破才能通过",
			allowedBiomes = new GameEnums.BiomeType[6]
			{
				GameEnums.BiomeType.FoldMountains,
				GameEnums.BiomeType.KarstMountains,
				GameEnums.BiomeType.LowHills,
				GameEnums.BiomeType.HighMountains,
				GameEnums.BiomeType.CoastalLowland,
				GameEnums.BiomeType.Fjord
			}
		});
		Register(BuildableType.Fortress, new BuildingRequirement
		{
			name = "堡垒",
			requireLand = true,
			description = "统一的堡垒类型；关口/河口/边防/海防/河防等只是用途，不是类型"
		});
		Register(BuildableType.MountainCity, new BuildingRequirement
		{
			name = "山城",
			requireMountain = true,
			minElevation = 0.6f,
			minSlope = 15f,
			description = "山顶/山脊建城，居高临下"
		});
		Register(BuildableType.WaterCity, new BuildingRequirement
		{
			name = "水城",
			requireRiver = true,
			requireCoast = false,
			maxElevation = 0.35f,
			maxSlope = 3f,
			minPrecipitation = 800f,
			description = "水网密集区，运河为街（威尼斯/苏州式）"
		});
		Register(BuildableType.StarCity, new BuildingRequirement
		{
			name = "星城",
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelIII,
			minTechLevel = 4,
			maxSlope = 10f,
			description = "棱堡时代星形防御工事环绕，需火药技术"
		});
		Register(BuildableType.CitadelCity, new BuildingRequirement
		{
			name = "堡城",
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelII,
			description = "核心要塞+外围城区，军事主导"
		});
		Register(BuildableType.PlannedCity, new BuildingRequirement
		{
			name = "规划城",
			requirePlain = true,
			maxElevation = 0.45f,
			maxSlope = 8f,
			minExistingSettlementLevel = SettlementLevel.LevelIII,
			description = "完全人工规划的新城（迁都/殖民），需平坦地形"
		});
		Register(BuildableType.DeepWaterPort, new BuildingRequirement
		{
			name = "深水港",
			requireCoast = true,
			minSlope = 8f,
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelII,
			minDevelopment = 25f,
			description = "可停泊大型船只，需深水海岸（坡度>8°）+Ⅱ级以上聚落"
		});
		Register(BuildableType.RiverPort, new BuildingRequirement
		{
			name = "内河港",
			requireRiver = true,
			maxElevation = 0.5f,
			description = "河流沿岸港口，内陆-沿海转运"
		});
		Register(BuildableType.ImperialPort, new BuildingRequirement
		{
			name = "帝国港",
			requireCoast = true,
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelIV,
			minDevelopment = 70f,
			minSlope = 10f,
			description = "全球贸易中心+主力舰队母港，需Ⅳ级以上都会+深水海岸"
		});
		Register(BuildableType.Ferry, new BuildingRequirement
		{
			name = "渡口",
			requireRiver = true,
			maxSlope = 5f,
			description = "河流浅滩/可涉水渡河点"
		});
		Register(BuildableType.PostStation, new BuildingRequirement
		{
			name = "驿站",
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelI,
			description = "官方信使/军队调动中继站"
		});
		Register(BuildableType.Watchtower, new BuildingRequirement
		{
			name = "烽火台/瞭望塔",
			requireLand = true,
			minElevation = 0.3f,
			description = "军事预警网络，优先高地"
		});
		Register(BuildableType.Granary, new BuildingRequirement
		{
			name = "粮仓",
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelII,
			minPrecipitation = 200f,
			description = "军队后勤补给点，需农业区"
		});
		Register(BuildableType.Market, new BuildingRequirement
		{
			name = "市集",
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelI,
			minDevelopment = 5f,
			description = "商业贸易节点"
		});
		Register(BuildableType.Temple, new BuildingRequirement
		{
			name = "神庙",
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelI,
			description = "宗教祭祀建筑"
		});
		Register(BuildableType.University, new BuildingRequirement
		{
			name = "大学/学府",
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelIII,
			minDevelopment = 40f,
			description = "高等教育/学术中心，需Ⅲ级以上城邑"
		});
		Register(BuildableType.Workshop, new BuildingRequirement
		{
			name = "作坊/工场",
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelII,
			description = "手工业/制造业"
		});
		Register(BuildableType.Mine, new BuildingRequirement
		{
			name = "矿场",
			requireHills = true,
			minElevation = 0.4f,
			minSlope = 10f,
			description = "矿产开采，需丘陵/山地"
		});
		Register(BuildableType.SaltWorks, new BuildingRequirement
		{
			name = "盐场",
			requireCoast = true,
			maxSlope = 3f,
			minTemperature = 10f,
			description = "海盐晒制，需平坦海岸+温暖气候",
			allowedBiomes = new GameEnums.BiomeType[4]
			{
				GameEnums.BiomeType.CoastalLowland,
				GameEnums.BiomeType.CoastalSaltMarsh,
				GameEnums.BiomeType.HotDesert,
				GameEnums.BiomeType.CoastalDesert
			}
		});
		Register(BuildableType.Fishery, new BuildingRequirement
		{
			name = "渔场",
			requireCoast = true,
			description = "渔业基地，需沿海"
		});
		Register(BuildableType.Shipyard, new BuildingRequirement
		{
			name = "造船厂",
			requireCoast = true,
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelII,
			minDevelopment = 30f,
			description = "舰船建造，需沿海港口+Ⅱ级以上聚落"
		});
		Register(BuildableType.Barracks, new BuildingRequirement
		{
			name = "兵营",
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelII,
			description = "军队驻扎/训练"
		});
		Register(BuildableType.Arsenal, new BuildingRequirement
		{
			name = "军械库",
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelIII,
			minDevelopment = 35f,
			description = "武器/盔甲制造与储存"
		});
		Register(BuildableType.Aqueduct, new BuildingRequirement
		{
			name = "引水渠/水道",
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelIII,
			minDevelopment = 45f,
			description = "城市供水系统，需Ⅲ级以上城邑"
		});
		Register(BuildableType.Sewer, new BuildingRequirement
		{
			name = "下水道",
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelIII,
			minDevelopment = 50f,
			description = "城市排污系统"
		});
		Register(BuildableType.Bathhouse, new BuildingRequirement
		{
			name = "公共浴场",
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelII,
			minDevelopment = 25f,
			description = "公共洗浴/社交场所"
		});
		Register(BuildableType.Library, new BuildingRequirement
		{
			name = "图书馆",
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelIII,
			minDevelopment = 40f,
			description = "知识保存/学术研究"
		});
		Register(BuildableType.Observatory, new BuildingRequirement
		{
			name = "观星台/天文台",
			requireExistingSettlement = true,
			minSettlementLevel = SettlementLevel.LevelIII,
			minElevation = 0.4f,
			description = "天文观测/历法制定，优先高地"
		});
		Register(BuildableType.Lighthouse, new BuildingRequirement
		{
			name = "灯塔",
			requireCoast = true,
			minSlope = 5f,
			description = "航海导航，需海岸高地"
		});
	}

	private static void Register(BuildableType type, BuildingRequirement req)
	{
		req.name = type.ToString();
		_requirements[type] = req;
	}

	public static BuildingAvailability CheckAvailability(BuildableType type, TileData tile, SettlementData existingSettlement = null, int techLevel = 0)
	{
		Initialize();
		if (!_requirements.TryGetValue(type, out var value))
		{
			return BuildingAvailability.Available();
		}
		List<string> list = new List<string>();
		if (value.requireLand && !tile.isLand)
		{
			list.Add("需要陆地");
		}
		if (value.requireCoast && !tile.isCoast && tile.oceanTier != GameEnums.OceanTier.Coast)
		{
			list.Add("需要沿海");
		}
		if (value.requireRiver && !tile.isRiver)
		{
			list.Add("需要河流");
		}
		if (value.requireRiverMouth && (!tile.isCoast || !tile.isRiver))
		{
			list.Add("需要河口（河流+海岸）");
		}
		if (value.requireMountain && tile.elevation01 < 0.6f)
		{
			list.Add("需要山地");
		}
		if (value.requireHills && (tile.elevation01 < 0.4f || tile.elevation01 > 0.8f))
		{
			list.Add("需要丘陵");
		}
		if (value.requirePlain && (tile.elevation01 > 0.5f || tile.slopeDegree > 10f))
		{
			list.Add("需要平原");
		}
		if (value.requireDesert && tile.annualPrecipMm > 400f)
		{
			list.Add("需要沙漠/干旱区");
		}
		if (tile.elevation01 < value.minElevation)
		{
			list.Add($"海拔不足（需≥{value.minElevation:0.00}）");
		}
		if (tile.elevation01 > value.maxElevation)
		{
			list.Add($"海拔过高（需≤{value.maxElevation:0.00}）");
		}
		if (tile.slopeDegree < value.minSlope)
		{
			list.Add($"坡度不足（需≥{value.minSlope:0}°）");
		}
		if (tile.slopeDegree > value.maxSlope)
		{
			list.Add($"坡度过大（需≤{value.maxSlope:0}°）");
		}
		if (tile.annualPrecipMm < value.minPrecipitation)
		{
			list.Add($"降水不足（需≥{value.minPrecipitation:0}mm）");
		}
		if (tile.annualPrecipMm > value.maxPrecipitation)
		{
			list.Add($"降水过多（需≤{value.maxPrecipitation:0}mm）");
		}
		if (tile.annualTemp < value.minTemperature)
		{
			list.Add($"温度过低（需≥{value.minTemperature:0}°C）");
		}
		if (tile.annualTemp > value.maxTemperature)
		{
			list.Add($"温度过高（需≤{value.maxTemperature:0}°C）");
		}
		if (value.allowedBiomes != null && value.allowedBiomes.Length != 0 && !Enumerable.Contains(value.allowedBiomes, tile.biome))
		{
			list.Add($"群系不允许（当前{tile.biome}）");
		}
		if (value.forbiddenBiomes != null && Enumerable.Contains(value.forbiddenBiomes, tile.biome))
		{
			list.Add($"群系被禁止（{tile.biome}）");
		}
		if (value.allowedOceanTiers != null && value.allowedOceanTiers.Length != 0 && !Enumerable.Contains(value.allowedOceanTiers, tile.oceanTier))
		{
			list.Add($"海洋等级不允许（当前{tile.oceanTier}）");
		}
		if (value.requireBottleneck && !(tile.slopeDegree > 15f) && !tile.isCoast && (!tile.isRiver || !(tile.slopeDegree > 10f)))
		{
			list.Add("需要瓶颈节点（山口/峡谷/海峡）");
		}
		if (value.requireIsland && !tile.isCoast)
		{
			list.Add("需要岛屿");
		}
		if (value.requireExistingSettlement && existingSettlement == null)
		{
			list.Add("需要已有聚落");
		}
		if (existingSettlement != null && value.minSettlementLevel > existingSettlement.settlementLevel)
		{
			list.Add($"聚落等级不足（需≥{value.minSettlementLevel}）");
		}
		if (existingSettlement != null && value.minExistingSettlementLevel > existingSettlement.settlementLevel)
		{
			list.Add($"已有聚落等级不足（需≥{value.minExistingSettlementLevel}）");
		}
		if (existingSettlement != null && existingSettlement.development < value.minDevelopment)
		{
			list.Add($"发展度不足（需≥{value.minDevelopment:0}）");
		}
		if (techLevel < value.minTechLevel)
		{
			list.Add($"科技等级不足（需≥{value.minTechLevel}）");
		}
		if (list.Count != 0)
		{
			return BuildingAvailability.Unavailable(list);
		}
		return BuildingAvailability.Available();
	}

	public static List<BuildableType> GetAvailableBuildings(TileData tile, SettlementData existingSettlement = null, int techLevel = 0)
	{
		Initialize();
		List<BuildableType> list = new List<BuildableType>();
		foreach (BuildableType key in _requirements.Keys)
		{
			if (CheckAvailability(key, tile, existingSettlement, techLevel).available)
			{
				list.Add(key);
			}
		}
		return list;
	}

	public static string GetRequirementDescription(BuildableType type)
	{
		Initialize();
		if (_requirements.TryGetValue(type, out var value))
		{
			return value.description;
		}
		return "无特殊条件";
	}

	public static IEnumerable<BuildableType> GetAllBuildableTypes()
	{
		Initialize();
		return _requirements.Keys;
	}
}
}
