using System;
using MyriadCreation.Core.Enums;
using MyriadCreation.World.Climate;
using MyriadCreation.World.Terrain;
using UnityEngine;
using Random = System.Random;

namespace MyriadCreation.World.Generation
{
[Serializable]
public class MapGenerationConfig
{
	public enum ProvinceQualityTier
	{
		Performance,
		Balanced,
		Quality
	}

	public enum RegionalEdgeMode
	{
		Open,
		SeaWest,
		SeaEast,
		SeaNorth,
		SeaSouth,
		Island
	}

	public enum MapBasemap
	{
		[Tooltip("程序生成：使用球面噪声+板块构造自动生成地形。显示全部海陆参数。")]
		Procedural,
		[Tooltip("内置底图：使用预设地形模板（如地球、盘古大陆等）。")]
		Builtin,
		[Tooltip("导入高度图：以灰度图片亮度作为高程。导入后形态、陆地量、破碎度不再影响海陆骨架。")]
		ImportHeightmap
	}

	/// <summary>
	/// 生成配置用的地图尺寸预设（Medium/Large/Huge）。与 Core.Enums.MapSizePreset（Bootstrap/UI 用的
	/// Large/Huge/Reference/Enormous 尺寸档）不是同一概念，改名避免同名混淆。
	/// </summary>
	public enum GenerationMapSize
	{
		[Tooltip("中型：1024×512=524288地块。默认推荐，细节丰富。")]
		Medium,
		[Tooltip("大型：2048×1024=2097152地块。大战略地图，生成较慢。")]
		Large,
		[Tooltip("巨型：4096×2048=8388608地块。极致细节，需高性能电脑。")]
		Huge
	}

	public enum ContinentShape
	{
		[Tooltip("单陆：一块主大陆，周围海洋。")]
		SingleLandmass,
		[Tooltip("双陆：两块大陆隔海相望。")]
		DualLandmass,
		[Tooltip("环形陆地：大陆环绕中央海洋（地中海式）。")]
		RingLandmass,
		[Tooltip("成片群岛：无大块陆地，大量岛屿散布。")]
		Archipelago
	}

	/// <summary>
	/// 生成配置用的环流模式（4 档：SingleCell/DoubleCell/ThreeCell/EnhancedMonsoon）。
	/// 对外/存档使用 GameEnums.CirculationMode（3 档，无 EnhancedMonsoon），用 ToGameEnums() 映射，避免静默漂移。
	/// </summary>
	public enum GenerationCirculationMode
	{
		[Tooltip("单环流：赤道-极地直接环流。极端气候，赤道极热极地极冷。")]
		SingleCell,
		[Tooltip("双环流：简化的两圈环流。气候对比中等。")]
		DoubleCell,
		[Tooltip("三环流：地球标准（Hadley 0-30度 / Ferrel 30-60度 / Polar 60-90度）。气候带分明，对比最强。")]
		ThreeCell,
		[Tooltip("增强季风：三圈基础上海陆热力差异放大，季节性风向反转强烈。")]
		EnhancedMonsoon
	}

	/// <summary>
	/// 把生成配置的环流模式映射到对外/存档使用的 GameEnums.CirculationMode
	/// （WorldConfig.circulationMode / PlanetClimateSimulator 使用）：
	/// SingleCell/DoubleCell 直映，ThreeCell→TripleCell；EnhancedMonsoon 在 Core 气候模型中没有独立档，
	/// 同样按 TripleCell 处理（额外季风增强由 ApplyToGCM 的 MonsoonStrength×1.5 承担）。
	/// 当前 WorldConfig 与 MapGenerationConfig 之间没有自动适配点（两者各自独立），保留本映射避免两套枚举静默漂移。
	/// </summary>
	public static GameEnums.CirculationMode ToGameEnums(GenerationCirculationMode mode)
	{
		switch (mode)
		{
		case GenerationCirculationMode.SingleCell:
			return GameEnums.CirculationMode.SingleCell;
		case GenerationCirculationMode.DoubleCell:
			return GameEnums.CirculationMode.DoubleCell;
		case GenerationCirculationMode.ThreeCell:
			return GameEnums.CirculationMode.TripleCell;
		case GenerationCirculationMode.EnhancedMonsoon:
			return GameEnums.CirculationMode.TripleCell;
		default:
			return GameEnums.CirculationMode.TripleCell;
		}
	}

	/// <summary>当前配置的环流模式映射到 GameEnums.CirculationMode（见静态版说明）。</summary>
	public GameEnums.CirculationMode ToGameEnums()
	{
		return ToGameEnums(Circulation);
	}

	[Header("【全局】")]
	[Tooltip("地图底图来源。程序生成=噪声自动生成；内置底图=预设模板；导入高度图=以灰度图亮度为高程。")]
	public MapBasemap Basemap;

	[Header("【地形模板】")]
	[Tooltip("地形规模分类。完整世界=全球海陆格局；局部=区域地理特征（含边界条件）。两者生成逻辑和参数体系不同。")]
	public TerrainScale TerrainScale;

	[Tooltip("完整世界地形模板。选择后自动应用参数预设，仍可用滑块微调。")]
	public WorldTemplate WorldTemplate = WorldTemplate.EarthLike;

	[Tooltip("局部地形模板。选择后自动应用参数预设，仍可用滑块微调。")]
	public RegionalTemplate RegionalTemplate = RegionalTemplate.CoastalPlain;

	[Tooltip("随机种子（整数）。相同种子+相同设置=相同地图。-1=随机生成。")]
	public int Seed = -1;

	[Tooltip("使用字符串种子。开启后 SeedString 的哈希值作为种子，支持输入任意文字复现地图。")]
	public bool UseStringSeed;

	[Tooltip("字符串种子。任意文字如 我的世界、Atlantis，哈希为 int 种子。相同字符串=相同地图。")]
	public string SeedString = "";

	[Tooltip("地图内部像素尺寸。更大更细致但生成和运行更耗时。")]
	public GenerationMapSize MapSize;

	[Tooltip("陆地划分出的省份总数。数量越高，省界越密。")]
	[Range(1000f, 12000f)]
	public int ProvinceCount = 6000;

	[Tooltip("省生成质量：性能/均衡/质量。会影响省数与 Lloyd 迭代次数。")]
	public ProvinceQualityTier ProvinceQuality = ProvinceQualityTier.Balanced;

	[Tooltip("Lloyd 松弛迭代次数。越多省界越规整，生成越慢。")]
	[Range(0f, 5f)]
	public int ProvinceLloydIterations = 2;

	[Tooltip("平均每个省份的地块数量。地块是省界划分的最小调整单位，不参与模拟。总地块数=省份数×每省地块数。")]
	[Range(8f, 200f)]
	public int TilesPerProvince = 24;

	[Tooltip("大陆布局形态。单陆=一块主大陆；双陆=两块大陆；环形陆地=大陆环绕中央海；成片群岛=无大块陆地。")]
	public ContinentShape Shape;

	[Tooltip("局部模式边界：开放/西海/东海/北海/南海/岛屿。只在地形规模=局部时生效。")]
	public RegionalEdgeMode RegionalEdge;

	[Tooltip("局部模式边缘过渡宽度（占地图短边比例）。")]
	[Range(0.02f, 0.3f)]
	public float RegionalEdgeFalloff = 0.12f;

	[Header("【海陆】")]
	[Tooltip("外海缓冲。默认开启；在地图四周渐进压低地势，避免大陆贴住画面边缘。")]
	public bool OuterSeaBuffer = true;

	[Tooltip("海平面。设定海陆判定高度；提高会淹没低地、扩大海洋并切断陆桥。")]
	[Range(0f, 1f)]
	public float SeaLevel = 0.05f;

	[Tooltip("陆地量。塑造大陆骨架的整体强度；提高会让更多地形露出海面。")]
	[Range(0f, 1f)]
	public float LandAmount = 0.85f;

	[Tooltip("破碎度。控制大陆与岛群的大尺度破碎程度；提高会形成更多、更小的陆块。")]
	[Range(0f, 1f)]
	public float Fragmentation = 0.06f;

	[Tooltip("海岸破碎度。只增加近海的海湾、半岛和小岛细节；不改变大陆的大致尺度。")]
	[Range(0f, 1f)]
	public float CoastFragmentation = 0.8f;

	[Header("【气候】")]
	[Tooltip("环流模式。决定纬向气候带和海岸冷暖湿干差异；三环流的气候对比最强。")]
	public GenerationCirculationMode Circulation = GenerationCirculationMode.ThreeCell;

	[Tooltip("热赤道。上下平移热带、干湿带与寒带；0表示热赤道位于地图中央。")]
	[Range(-1f, 1f)]
	public float ThermalEquator;

	[Tooltip("北缘纬度。设置地图上边缘对应的纬度，用于截取全球气候带。")]
	[Range(-90f, 90f)]
	public float NorthLatitude = 65f;

	[Tooltip("南缘纬度。设置地图下边缘对应的纬度，用于截取全球气候带。")]
	[Range(-90f, 90f)]
	public float SouthLatitude = -10f;

	[Tooltip("全球温度偏移（度C）。负值=冰期（冰川扩张），正值=间冰期（温暖湿润）。本项目特色参数。")]
	[Range(-8f, 8f)]
	public float GlobalTemperatureOffset;

	[Header("【水文与地貌】")]
	[Tooltip("河网密度。控制形成河流所需的汇水面积阈值。稀疏=大河少，密集=河网密布。")]
	[Range(0f, 1f)]
	public float RiverDensity = 0.5f;

	[Tooltip("水力侵蚀强度。0=无侵蚀（原始地形），1=强侵蚀（深谷峡谷）。粒子基侵蚀模拟。")]
	[Range(0f, 1f)]
	public float ErosionIntensity = 0.3f;

	[Header("【省份划分】")]
	[Tooltip("省份大小差异。控制省份面积的差距；越高越容易同时出现大省和小省。")]
	[Range(0f, 1f)]
	public float ProvinceSizeVariance = 0.9f;

	[Tooltip("省份规整度。整理省份种子的位置；越高越均匀规整，越低越自然不规则。")]
	[Range(0f, 1f)]
	public float ProvinceRegularity = 1f;

	[Header("【高度图】")]
	[Tooltip("导入的灰度图高度文件路径。为空则按上方旋钮程序生成地形。")]
	public string HeightmapPath = "";

	[Tooltip("是否已导入高度图。导入后形态、陆地量与破碎度不再影响海陆骨架。")]
	public bool HasImportedHeightmap;

	[Header("【开局势力】")]
	[Tooltip("初始文明数量。0=无初始文明，玩家从部落开始。")]
	[Range(0f, 50f)]
	public int InitialCivilizations = 10;

	[Tooltip("初始文明发展程度。低=部落起步，高=已有王国。")]
	[Range(0f, 1f)]
	public float InitialCivilizationLevel = 0.3f;

	[Header("【高级 · 地形细节】")]
	[Range(0.5f, 4f)]
	public float TerrainFrequency = 1.8f;

	[Range(3f, 10f)]
	public int TerrainOctaves = 6;

	[Range(0f, 1.5f)]
	public float WarpStrength = 0.7f;

	[Range(0.5f, 3f)]
	public float WarpFrequency = 1.3f;

	[Range(0f, 0.5f)]
	public float PlateBoundaryBoost = 0.25f;

	[Range(6f, 20f)]
	public int PlateCount = 12;

	[Header("【高级 · 气候细节】")]
	[Range(0f, 1f)]
	public float MonsoonStrength = 0.6f;

	[Range(200f, 1500f)]
	public float OrographicPrecipFactor = 800f;

	[Range(0.1f, 0.8f)]
	public float RainShadowFactor = 0.3f;

	[Header("【高级 · 侵蚀细节】")]
	[Range(0.1f, 0.8f)]
	public float ErosionRate = 0.3f;

	[Range(0.1f, 0.8f)]
	public float DepositionRate = 0.3f;

	[Range(0.005f, 0.05f)]
	public float EvaporationRate = 0.02f;

	[Range(0f, 0.5f)]
	public float ErosionInertia = 0.05f;

	public const float PlanetOmega = 7.292E-05f;

	public const float PlanetRadius = 6371000f;

	public const float SolarConstant = 1361f;

	public const bool UseMultithreading = true;

	public const int OceanIterations = 20;

	public const int ErosionMaxLifetime = 64;

	public bool IsLandSkeletonParamsEnabled
	{
		get
		{
			if (!HasImportedHeightmap)
			{
				return Basemap != MapBasemap.ImportHeightmap;
			}
			return false;
		}
	}

	public bool IsProceduralParamsVisible
	{
		get
		{
			if (Basemap == MapBasemap.Procedural)
			{
				return !HasImportedHeightmap;
			}
			return false;
		}
	}

	public bool IsAdvancedTerrainVisible
	{
		get
		{
			if (Basemap == MapBasemap.Procedural)
			{
				return !HasImportedHeightmap;
			}
			return false;
		}
	}

	public void ApplyProvinceQualityPreset()
	{
		switch (ProvinceQuality)
		{
		case ProvinceQualityTier.Performance:
			ProvinceCount = 5000;
			ProvinceLloydIterations = 1;
			break;
		case ProvinceQualityTier.Quality:
			ProvinceCount = 8000;
			ProvinceLloydIterations = 3;
			break;
		default:
			ProvinceCount = 6000;
			ProvinceLloydIterations = 2;
			break;
		}
	}

	public (int width, int height) GetMapDimensions()
	{
		return MapSize switch
		{
			GenerationMapSize.Medium => (width: 1024, height: 512), 
			GenerationMapSize.Large => (width: 2048, height: 1024), 
			GenerationMapSize.Huge => (width: 4096, height: 2048), 
			_ => (width: 1024, height: 512), 
		};
	}

	public int GetTotalTiles()
	{
		var (num, num2) = GetMapDimensions();
		return num * num2;
	}

	public int GetActualSeed()
	{
		if (UseStringSeed && !string.IsNullOrWhiteSpace(SeedString))
		{
			return HashStringToSeed(SeedString);
		}
		if (Seed >= 0)
		{
			return Seed;
		}
		return new Random().Next();
	}

	public static int HashStringToSeed(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return 0;
		}
		uint num = 2166136261u;
		foreach (char c in str)
		{
			num ^= c;
			num *= 16777619;
		}
		return (int)(num & 0x7FFFFFFF);
	}

	public static string RandomizeSeedString()
	{
		string[] array = new string[16]
		{
			"Ancient", "Silent", "Golden", "Frozen", "Crimson", "Emerald", "Storm", "Ash", "Moon", "Sun",
			"Iron", "Bronze", "Mystic", "Sacred", "Lost", "Hidden"
		};
		string[] array2 = new string[16]
		{
			"Ocean", "Mountain", "Empire", "Kingdom", "Realm", "Isle", "Continent", "Desert", "Forest", "River",
			"Valley", "Plains", "Tundra", "Archipelago", "Peninsula", "Highland"
		};
		Random random = new Random();
		return $"{array[random.Next(array.Length)]}{array2[random.Next(array2.Length)]}{random.Next(100, 999)}";
	}

	public void ApplyToGenerator(PlanetTerrainGenerator gen)
	{
		if (IsProceduralParamsVisible)
		{
			gen.TargetLandFraction = Mathf.Lerp(0.15f, 0.6f, LandAmount);
			gen.WarpFrequency = WarpFrequency * (0.5f + Fragmentation * 2f);
			gen.TerrainFrequency = TerrainFrequency * (0.8f + CoastFragmentation * 0.4f);
			gen.PlateCount = PlateCount;
			gen.PlateBoundaryMountainBoost = PlateBoundaryBoost;
			gen.WarpStrength = WarpStrength;
			gen.TerrainOctaves = TerrainOctaves;
		}
		gen.AxialTilt = 0.40910518f;
		gen.GlobalTempOffset = GlobalTemperatureOffset;
		gen.RiverThreshold = Mathf.Lerp(200f, 20f, RiverDensity);
	}

	public void ApplyToGCM(AtmosphericCirculation gcm)
	{
		gcm.MonsoonStrength = MonsoonStrength * ((Circulation == GenerationCirculationMode.EnhancedMonsoon) ? 1.5f : 1f);
		gcm.OrographicPrecipFactor = OrographicPrecipFactor;
		gcm.RainShadowFactor = RainShadowFactor;
		gcm.Season = ThermalEquator * 0.25f;
	}

	public void ApplyToErosion(HydraulicErosion erosion, int totalTiles)
	{
		erosion.ParticleCount = (int)(ErosionIntensity * (float)Mathf.Min(150000, totalTiles / 3));
		erosion.ErosionRate = ErosionRate * (0.5f + ErosionIntensity);
		erosion.DepositionRate = DepositionRate;
		erosion.EvaporationRate = EvaporationRate;
		erosion.Inertia = ErosionInertia;
	}

	public (float north, float south) GetLatitudeRange()
	{
		return (north: NorthLatitude, south: SouthLatitude);
	}

	public static MapGenerationConfig Default()
	{
		return new MapGenerationConfig();
	}

	public static MapGenerationConfig EarthLike()
	{
		return new MapGenerationConfig
		{
			Basemap = MapBasemap.Procedural,
			MapSize = GenerationMapSize.Medium,
			ProvinceCount = 6000,
			TilesPerProvince = 12,
			Shape = ContinentShape.DualLandmass,
			OuterSeaBuffer = true,
			SeaLevel = 0.05f,
			LandAmount = 0.85f,
			Fragmentation = 0.06f,
			CoastFragmentation = 0.8f,
			Circulation = GenerationCirculationMode.ThreeCell,
			ThermalEquator = 0f,
			NorthLatitude = 65f,
			SouthLatitude = -10f,
			GlobalTemperatureOffset = 0f,
			RiverDensity = 0.5f,
			ErosionIntensity = 0.3f,
			ProvinceSizeVariance = 0.9f,
			ProvinceRegularity = 1f,
			InitialCivilizations = 10
		};
	}

	public static MapGenerationConfig IceAge()
	{
		MapGenerationConfig mapGenerationConfig = EarthLike();
		mapGenerationConfig.GlobalTemperatureOffset = -6f;
		mapGenerationConfig.SeaLevel = 0.02f;
		mapGenerationConfig.LandAmount = 0.9f;
		mapGenerationConfig.RiverDensity = 0.3f;
		mapGenerationConfig.Shape = ContinentShape.SingleLandmass;
		return mapGenerationConfig;
	}

	public static MapGenerationConfig Hothouse()
	{
		MapGenerationConfig mapGenerationConfig = EarthLike();
		mapGenerationConfig.GlobalTemperatureOffset = 6f;
		mapGenerationConfig.SeaLevel = 0.1f;
		mapGenerationConfig.LandAmount = 0.75f;
		mapGenerationConfig.RiverDensity = 0.7f;
		mapGenerationConfig.MonsoonStrength = 0.8f;
		return mapGenerationConfig;
	}

	public static MapGenerationConfig ArchipelagoWorld()
	{
		MapGenerationConfig mapGenerationConfig = EarthLike();
		mapGenerationConfig.Shape = ContinentShape.Archipelago;
		mapGenerationConfig.Fragmentation = 0.8f;
		mapGenerationConfig.CoastFragmentation = 0.95f;
		mapGenerationConfig.LandAmount = 0.4f;
		mapGenerationConfig.ProvinceCount = 6000;
		return mapGenerationConfig;
	}
}
}
