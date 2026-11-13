using System.Collections.Generic;

namespace MyriadCreation.World.Terrain
{
public static class TerrainTemplateSystem
{
	private static readonly Dictionary<WorldTemplate, TerrainTemplatePreset> _worldPresets = new Dictionary<WorldTemplate, TerrainTemplatePreset>
	{
		{
			WorldTemplate.Pangaea,
			new TerrainTemplatePreset
			{
				Name = "盘古大陆",
				Description = "以一块超级主大陆为主，周围环绕海洋，辅以零散岛屿和半岛",
				Scale = TerrainScale.World,
				TargetLandFraction = 0.35f,
				TerrainFrequency = 1.2f,
				TerrainOctaves = 5,
				WarpStrength = 0.5f,
				WarpFrequency = 1f,
				MountainHeight = 0.4f,
				PlateCount = 8,
				PlateBoundaryMountainBoost = 0.3f,
				MinContinentCount = 1,
				MaxContinentCount = 1,
				MinContinentSize = 0.5f,
				MaxContinentSize = 0.8f,
				IslandDensity = 0.2f,
				HasSpecialShape = true,
				SpecialShapeType = "SingleContinent"
			}
		},
		{
			WorldTemplate.DualContinents,
			new TerrainTemplatePreset
			{
				Name = "双大陆",
				Description = "以两块主大陆为主隔海相望（大小随机，各有范围），辅以零散岛屿",
				Scale = TerrainScale.World,
				TargetLandFraction = 0.32f,
				TerrainFrequency = 1.5f,
				TerrainOctaves = 6,
				WarpStrength = 0.6f,
				WarpFrequency = 1.2f,
				MountainHeight = 0.38f,
				PlateCount = 10,
				PlateBoundaryMountainBoost = 0.28f,
				MinContinentCount = 2,
				MaxContinentCount = 2,
				MinContinentSize = 0.2f,
				MaxContinentSize = 0.45f,
				IslandDensity = 0.3f,
				HasSpecialShape = true,
				SpecialShapeType = "DualContinent"
			}
		},
		{
			WorldTemplate.MultiContinents,
			new TerrainTemplatePreset
			{
				Name = "多大陆",
				Description = "以3-5块主大陆为主散布全球（大小随机，各有范围），辅以零散岛屿",
				Scale = TerrainScale.World,
				TargetLandFraction = 0.3f,
				TerrainFrequency = 2f,
				TerrainOctaves = 6,
				WarpStrength = 0.8f,
				WarpFrequency = 1.5f,
				MountainHeight = 0.35f,
				PlateCount = 14,
				PlateBoundaryMountainBoost = 0.25f,
				MinContinentCount = 3,
				MaxContinentCount = 5,
				MinContinentSize = 0.1f,
				MaxContinentSize = 0.35f,
				IslandDensity = 0.4f,
				HasSpecialShape = true,
				SpecialShapeType = "MultiContinent"
			}
		},
		{
			WorldTemplate.ArchipelagoWorld,
			new TerrainTemplatePreset
			{
				Name = "群岛世界",
				Description = "以大量岛屿为主，无大块主陆地，岛屿大小随机",
				Scale = TerrainScale.World,
				TargetLandFraction = 0.15f,
				TerrainFrequency = 3.5f,
				TerrainOctaves = 7,
				WarpStrength = 1f,
				WarpFrequency = 2f,
				MountainHeight = 0.2f,
				PlateCount = 20,
				PlateBoundaryMountainBoost = 0.15f,
				MinContinentCount = 0,
				MaxContinentCount = 0,
				MinContinentSize = 0f,
				MaxContinentSize = 0.1f,
				IslandDensity = 0.9f,
				HasSpecialShape = false
			}
		},
		{
			WorldTemplate.RingContinent,
			new TerrainTemplatePreset
			{
				Name = "环形大陆",
				Description = "以环形主大陆为主环绕中央内海，辅以零散岛屿和半岛",
				Scale = TerrainScale.World,
				TargetLandFraction = 0.4f,
				TerrainFrequency = 1f,
				TerrainOctaves = 5,
				WarpStrength = 0.4f,
				WarpFrequency = 0.8f,
				MountainHeight = 0.45f,
				PlateCount = 6,
				PlateBoundaryMountainBoost = 0.35f,
				MinContinentCount = 1,
				MaxContinentCount = 1,
				MinContinentSize = 0.4f,
				MaxContinentSize = 0.6f,
				IslandDensity = 0.25f,
				HasSpecialShape = true,
				SpecialShapeType = "RingContinent"
			}
		},
		{
			WorldTemplate.Mediterranean,
			new TerrainTemplatePreset
			{
				Name = "地中海世界",
				Description = "以中央海为核心，周围多块主陆地环绕（大小随机），辅以零散岛屿",
				Scale = TerrainScale.World,
				TargetLandFraction = 0.38f,
				TerrainFrequency = 1.8f,
				TerrainOctaves = 6,
				WarpStrength = 0.7f,
				WarpFrequency = 1.3f,
				MountainHeight = 0.38f,
				PlateCount = 12,
				PlateBoundaryMountainBoost = 0.28f,
				MinContinentCount = 3,
				MaxContinentCount = 6,
				MinContinentSize = 0.08f,
				MaxContinentSize = 0.3f,
				IslandDensity = 0.45f,
				HasSpecialShape = true,
				SpecialShapeType = "Mediterranean"
			}
		},
		{
			WorldTemplate.EarthLike,
			new TerrainTemplatePreset
			{
				Name = "类地球",
				Description = "以几块主大陆为主类似地球海陆分布（大小随机），辅以零散岛屿",
				Scale = TerrainScale.World,
				TargetLandFraction = 0.29f,
				TerrainFrequency = 1.8f,
				TerrainOctaves = 6,
				WarpStrength = 0.7f,
				WarpFrequency = 1.3f,
				MountainHeight = 0.35f,
				PlateCount = 12,
				PlateBoundaryMountainBoost = 0.25f,
				MinContinentCount = 4,
				MaxContinentCount = 7,
				MinContinentSize = 0.05f,
				MaxContinentSize = 0.3f,
				IslandDensity = 0.35f,
				HasSpecialShape = false
			}
		},
		{
			WorldTemplate.Custom,
			new TerrainTemplatePreset
			{
				Name = "自定义",
				Description = "纯参数，无模板预设",
				Scale = TerrainScale.World,
				HasSpecialShape = false
			}
		}
	};

	private static readonly Dictionary<RegionalTemplate, TerrainTemplatePreset> _regionalPresets = new Dictionary<RegionalTemplate, TerrainTemplatePreset>
	{
		{
			RegionalTemplate.RiverValley,
			new TerrainTemplatePreset
			{
				Name = "河流流域",
				Description = "一条大河+流域平原",
				Scale = TerrainScale.Regional,
				TargetLandFraction = 0.7f,
				TerrainFrequency = 1.5f,
				TerrainOctaves = 5,
				WarpStrength = 0.5f,
				WarpFrequency = 1f,
				MountainHeight = 0.25f,
				PlateCount = 6,
				PlateBoundaryMountainBoost = 0.2f,
				HasSpecialShape = true,
				SpecialShapeType = "RiverValley"
			}
		},
		{
			RegionalTemplate.Peninsula,
			new TerrainTemplatePreset
			{
				Name = "半岛",
				Description = "伸入海洋的半岛",
				Scale = TerrainScale.Regional,
				TargetLandFraction = 0.55f,
				TerrainFrequency = 1.8f,
				TerrainOctaves = 5,
				WarpStrength = 0.6f,
				WarpFrequency = 1.2f,
				MountainHeight = 0.3f,
				PlateCount = 8,
				PlateBoundaryMountainBoost = 0.22f,
				HasSpecialShape = true,
				SpecialShapeType = "Peninsula"
			}
		},
		{
			RegionalTemplate.Archipelago,
			new TerrainTemplatePreset
			{
				Name = "群岛",
				Description = "一片群岛散布",
				Scale = TerrainScale.Regional,
				TargetLandFraction = 0.25f,
				TerrainFrequency = 3f,
				TerrainOctaves = 7,
				WarpStrength = 0.9f,
				WarpFrequency = 1.8f,
				MountainHeight = 0.22f,
				PlateCount = 16,
				PlateBoundaryMountainBoost = 0.18f,
				HasSpecialShape = true,
				SpecialShapeType = "Archipelago"
			}
		},
		{
			RegionalTemplate.InlandSea,
			new TerrainTemplatePreset
			{
				Name = "内陆海",
				Description = "中央海+周围陆地",
				Scale = TerrainScale.Regional,
				TargetLandFraction = 0.6f,
				TerrainFrequency = 1.2f,
				TerrainOctaves = 5,
				WarpStrength = 0.4f,
				WarpFrequency = 0.9f,
				MountainHeight = 0.35f,
				PlateCount = 8,
				PlateBoundaryMountainBoost = 0.25f,
				HasSpecialShape = true,
				SpecialShapeType = "InlandSea"
			}
		},
		{
			RegionalTemplate.MountainRange,
			new TerrainTemplatePreset
			{
				Name = "山脉屏障",
				Description = "一条大山脉+两侧低地",
				Scale = TerrainScale.Regional,
				TargetLandFraction = 0.75f,
				TerrainFrequency = 1f,
				TerrainOctaves = 5,
				WarpStrength = 0.3f,
				WarpFrequency = 0.7f,
				MountainHeight = 0.55f,
				PlateCount = 4,
				PlateBoundaryMountainBoost = 0.4f,
				HasSpecialShape = true,
				SpecialShapeType = "MountainRange"
			}
		},
		{
			RegionalTemplate.Basin,
			new TerrainTemplatePreset
			{
				Name = "盆地",
				Description = "四周高中央低",
				Scale = TerrainScale.Regional,
				TargetLandFraction = 0.8f,
				TerrainFrequency = 1.3f,
				TerrainOctaves = 5,
				WarpStrength = 0.4f,
				WarpFrequency = 0.9f,
				MountainHeight = 0.45f,
				PlateCount = 6,
				PlateBoundaryMountainBoost = 0.3f,
				HasSpecialShape = true,
				SpecialShapeType = "Basin"
			}
		},
		{
			RegionalTemplate.CoastalPlain,
			new TerrainTemplatePreset
			{
				Name = "沿海平原",
				Description = "一侧海一侧缓升陆地",
				Scale = TerrainScale.Regional,
				TargetLandFraction = 0.65f,
				TerrainFrequency = 1f,
				TerrainOctaves = 4,
				WarpStrength = 0.3f,
				WarpFrequency = 0.6f,
				MountainHeight = 0.2f,
				PlateCount = 4,
				PlateBoundaryMountainBoost = 0.15f,
				HasSpecialShape = true,
				SpecialShapeType = "CoastalPlain"
			}
		},
		{
			RegionalTemplate.Fjord,
			new TerrainTemplatePreset
			{
				Name = "峡湾",
				Description = "冰川侵蚀的锯齿海岸",
				Scale = TerrainScale.Regional,
				TargetLandFraction = 0.5f,
				TerrainFrequency = 2.5f,
				TerrainOctaves = 7,
				WarpStrength = 1f,
				WarpFrequency = 2f,
				MountainHeight = 0.4f,
				PlateCount = 10,
				PlateBoundaryMountainBoost = 0.3f,
				HasSpecialShape = true,
				SpecialShapeType = "Fjord"
			}
		},
		{
			RegionalTemplate.Custom,
			new TerrainTemplatePreset
			{
				Name = "自定义",
				Description = "纯参数，无模板预设",
				Scale = TerrainScale.Regional,
				HasSpecialShape = false
			}
		}
	};

	public static TerrainTemplatePreset GetWorldPreset(WorldTemplate template)
	{
		if (!_worldPresets.TryGetValue(template, out var value))
		{
			return _worldPresets[WorldTemplate.Custom];
		}
		return value;
	}

	public static TerrainTemplatePreset GetRegionalPreset(RegionalTemplate template)
	{
		if (!_regionalPresets.TryGetValue(template, out var value))
		{
			return _regionalPresets[RegionalTemplate.Custom];
		}
		return value;
	}

	public static string[] GetWorldTemplateNames()
	{
		string[] array = new string[_worldPresets.Count];
		int num = 0;
		foreach (KeyValuePair<WorldTemplate, TerrainTemplatePreset> worldPreset in _worldPresets)
		{
			array[num++] = worldPreset.Value.Name;
		}
		return array;
	}

	public static string[] GetRegionalTemplateNames()
	{
		string[] array = new string[_regionalPresets.Count];
		int num = 0;
		foreach (KeyValuePair<RegionalTemplate, TerrainTemplatePreset> regionalPreset in _regionalPresets)
		{
			array[num++] = regionalPreset.Value.Name;
		}
		return array;
	}

	public static void ApplyPreset(TerrainTemplatePreset preset, PlanetTerrainGenerator generator)
	{
		if (preset != null && generator != null && !(preset.Name == "自定义"))
		{
			generator.TargetLandFraction = preset.TargetLandFraction;
			generator.TerrainFrequency = preset.TerrainFrequency;
			generator.TerrainOctaves = preset.TerrainOctaves;
			generator.WarpStrength = preset.WarpStrength;
			generator.WarpFrequency = preset.WarpFrequency;
			generator.MountainHeight = preset.MountainHeight;
			generator.PlateCount = preset.PlateCount;
			generator.PlateBoundaryMountainBoost = preset.PlateBoundaryMountainBoost;
		}
	}
}
}
