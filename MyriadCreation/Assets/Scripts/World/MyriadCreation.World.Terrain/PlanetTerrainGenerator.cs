using System;
using System.Threading.Tasks;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.World.Biome;
using MyriadCreation.World.Climate;
using MyriadCreation.World.Hydrology;
using UnityEngine;

namespace MyriadCreation.World.Terrain
{
public class PlanetTerrainGenerator
{
	private readonly SphericalNoise _noise;

	private readonly int _seed;

	public float TargetLandFraction = 0.3f;

	public float TerrainFrequency = 1.8f;

	public int TerrainOctaves = 6;

	public float WarpStrength = 0.7f;

	public float WarpFrequency = 1.3f;

	public float MountainHeight = 0.35f;

	public int PlateCount = 12;

	public float PlateBoundaryMountainBoost = 0.25f;

	public float AxialTilt = 0.409f;

	public float GlobalTempOffset;

	public float RiverThreshold = 80f;

	/// <summary>河网阈值随地图面积缩放：小图要有河，大图不能被短沟渠淹没。</summary>
	public static float ResolveRiverThreshold(int width, int height, float baseThreshold)
	{
		return Mathf.Clamp((float)(width * height) / 1024f, 3f, 80f);
	}

	public bool UseMultithreading = true;

	public PlanetTerrainGenerator(int seed = 42)
	{
		_seed = seed;
		_noise = new SphericalNoise(seed);
	}

	public void Generate(TileData[] tiles, int width, int height)
	{
		//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fe: Unknown result type (might be due to invalid IL or missing references)
		if (tiles == null || tiles.Length != width * height)
		{
			throw new ArgumentException($"tiles长度({tiles?.Length}) != width*height({width * height})");
		}
		int num = width * height;
		Debug.Log((object)$"[PlanetTerrainGenerator] 专业版生成：{width}x{height}={num}地块，种子={_seed}，板块={PlateCount}");
		float[] sphereX = new float[num];
		float[] sphereY = new float[num];
		float[] sphereZ = new float[num];
		float[] latAbs = new float[num];
		Parallel.For(0, num, (int i) =>
		{
			int x = i % width;
			int num4 = i / width;
			var (num5, num6, num7) = SphericalNoise.GridToSphere(x, num4, width, height);
			sphereX[i] = num5;
			sphereY[i] = num6;
			sphereZ[i] = num7;
			latAbs[i] = Mathf.Abs(90f - (float)num4 / (float)height * 180f);
		});
		float[] rawElevation = new float[num];
		ParallelLoop(0, num, (int i) =>
		{
			float num4 = SphericalNoise.Normalize01(_noise.DomainWarpFbm(sphereX[i], sphereY[i], sphereZ[i], WarpStrength, WarpFrequency, TerrainOctaves));
			float num5 = _noise.RidgedFbm(sphereX[i] * 2.1f + 10f, sphereY[i] * 2.1f, sphereZ[i] * 2.1f + 5f, 4);
			num4 = Mathf.Lerp(num4, num4 + num5 * MountainHeight, 0.4f);
			rawElevation[i] = Mathf.Clamp01(num4);
		});
		float seaLevel = QuantileNormalize(rawElevation, TargetLandFraction);
		Debug.Log((object)$"[PlanetTerrainGenerator] 分位数归一化完成，海平面阈值={seaLevel:F4}，目标陆地比例={TargetLandFraction}");
		SphericalVoronoi sphericalVoronoi = new SphericalVoronoi(_seed + 1000);
		sphericalVoronoi.Generate(PlateCount);
		int[] grid = sphericalVoronoi.AssignToGrid(width, height);
		bool[] plateBoundary = sphericalVoronoi.DetectBoundaries(grid, width, height);
		ParallelLoop(0, num, (int i) =>
		{
			if (plateBoundary[i] && rawElevation[i] > seaLevel)
			{
				rawElevation[i] = Mathf.Clamp01(rawElevation[i] + PlateBoundaryMountainBoost);
			}
		});
		ParallelLoop(0, num, (int i) =>
		{
			ref TileData reference = ref tiles[i];
			reference.tileIndex = i;
			reference.exists = true;
			if (!reference.locked)
			{
				reference.elevation01 = rawElevation[i];
				reference.isLand = rawElevation[i] > seaLevel;
				if (!reference.isLand)
				{
					ref GameEnums.OceanTier oceanTier = ref reference.oceanTier;
					GameEnums.OceanTier oceanTier2 = ((rawElevation[i] < seaLevel * 0.3f) ? GameEnums.OceanTier.DeepSea : ((rawElevation[i] < seaLevel * 0.7f) ? GameEnums.OceanTier.NearSea : GameEnums.OceanTier.Coast));
					oceanTier = oceanTier2;
					reference.oceanDepth01 = (seaLevel - rawElevation[i]) / seaLevel;
				}
				else
				{
					reference.oceanTier = GameEnums.OceanTier.Land;
					reference.oceanDepth01 = 0f;
				}
			}
		});
		ParallelLoop(0, num, (int i) =>
		{
			int num4 = i % width;
			int num5 = i / width;
			ref TileData reference = ref tiles[i];
			float num6 = 0f;
			if (num4 > 0)
			{
				num6 = Mathf.Max(num6, Mathf.Abs(reference.elevation01 - tiles[i - 1].elevation01));
			}
			if (num4 < width - 1)
			{
				num6 = Mathf.Max(num6, Mathf.Abs(reference.elevation01 - tiles[i + 1].elevation01));
			}
			if (num5 > 0)
			{
				num6 = Mathf.Max(num6, Mathf.Abs(reference.elevation01 - tiles[i - width].elevation01));
			}
			if (num5 < height - 1)
			{
				num6 = Mathf.Max(num6, Mathf.Abs(reference.elevation01 - tiles[i + width].elevation01));
			}
			reference.slopeDegree = num6 * 90f;
			reference.isCoast = false;
			if (reference.isLand)
			{
				if (num4 > 0 && !tiles[i - 1].isLand)
				{
					reference.isCoast = true;
				}
				else if (num4 < width - 1 && !tiles[i + 1].isLand)
				{
					reference.isCoast = true;
				}
				else if (num5 > 0 && !tiles[i - width].isLand)
				{
					reference.isCoast = true;
				}
				else if (num5 < height - 1 && !tiles[i + width].isLand)
				{
					reference.isCoast = true;
				}
			}
			float latitude = 90f - (float)num5 / (float)height * 180f;
			float heightAboveSea = LandElevationAboveSea(reference.elevation01, reference.isLand, seaLevel);
			reference.annualTemp = ComputeBaseAnnualTemperature(latitude, heightAboveSea, reference.isLand, reference.isCoast, GlobalTempOffset);
			float latitudeAbs = Mathf.Abs(latitude);
			reference.annualPrecipMm = ComputeBaseAnnualPrecipitationMm(latitudeAbs, reference.slopeDegree, reference.isLand, reference.isCoast);
			reference.airHumidityPct = ComputeBaseAirHumidityPct(reference.annualPrecipMm);
		});
		bool[] array = new bool[num];
		for (int num2 = 0; num2 < num; num2++)
		{
			array[num2] = tiles[num2].isLand;
		}
		HydraulicErosion hydraulicErosion = new HydraulicErosion(width, height, _seed + 2000);
		hydraulicErosion.ParticleCount = Mathf.Min(120000, num / 4);
		hydraulicErosion.Run(rawElevation, array);
		hydraulicErosion.ApplyToTiles(tiles, rawElevation);
		Debug.Log((object)"[PlanetTerrainGenerator] 水力侵蚀完成");
		ParallelLoop(0, num, (int i) =>
		{
			int num4 = i % width;
			int num5 = i / width;
			ref TileData reference = ref tiles[i];
			if (reference.isLand)
			{
				float num6 = 0f;
				if (num4 > 0)
				{
					num6 = Mathf.Max(num6, Mathf.Abs(reference.elevation01 - tiles[i - 1].elevation01));
				}
				if (num4 < width - 1)
				{
					num6 = Mathf.Max(num6, Mathf.Abs(reference.elevation01 - tiles[i + 1].elevation01));
				}
				if (num5 > 0)
				{
					num6 = Mathf.Max(num6, Mathf.Abs(reference.elevation01 - tiles[i - width].elevation01));
				}
				if (num5 < height - 1)
				{
					num6 = Mathf.Max(num6, Mathf.Abs(reference.elevation01 - tiles[i + width].elevation01));
				}
				reference.slopeDegree = num6 * 90f;
				float latitude = 90f - (float)num5 / (float)height * 180f;
				float heightAboveSea = LandElevationAboveSea(reference.elevation01, reference.isLand, seaLevel);
				reference.annualTemp = ComputeBaseAnnualTemperature(latitude, heightAboveSea, reference.isLand, reference.isCoast, GlobalTempOffset);
			}
		});
		float[] array2 = new float[num];
		for (int num3 = 0; num3 < num; num3++)
		{
			array2[num3] = tiles[num3].annualTemp;
		}
		AtmosphericCirculation atmosphericCirculation = new AtmosphericCirculation(width, height);
		atmosphericCirculation.AxialTilt = AxialTilt;
		atmosphericCirculation.Run(rawElevation, array, array2);
		atmosphericCirculation.ApplyToTiles(tiles, array2);
		Debug.Log((object)"[PlanetTerrainGenerator] 大气环流GCM完成");
		OceanCurrentSimulator oceanCurrentSimulator = new OceanCurrentSimulator(width, height);
		oceanCurrentSimulator.Run(array, atmosphericCirculation.WindU, atmosphericCirculation.WindV, array2);
		oceanCurrentSimulator.ApplyCoastalEffects(tiles, array);
		Debug.Log((object)"[PlanetTerrainGenerator] 洋流模拟完成");
		HydrologySystem hydrologySystem = new HydrologySystem(width, height);
		// 河网阈值随地图面积缩放：固定 80 在 48×24 这类小图上要求 13% 陆地汇水，实际一条河都出不来。
		float riverThreshold = ResolveRiverThreshold(width, height, RiverThreshold);
		hydrologySystem.Run(rawElevation, array, riverThreshold);
		hydrologySystem.ApplyToTiles(tiles);
		Debug.Log((object)$"[PlanetTerrainGenerator] 水文模拟完成：河流{hydrologySystem.GetRiverTileCount()}地块，最大等级={hydrologySystem.GetMaxStreamOrder()}，阈值={riverThreshold:F1}");
		TerrainPCA terrainPCA = new TerrainPCA(width, height);
		terrainPCA.Run(rawElevation, array);
		Debug.Log((object)$"[PlanetTerrainGenerator] PCA地形特征完成：大陆主轴=({terrainPCA.ContinentPrincipalAxis.x:F2}, {terrainPCA.ContinentPrincipalAxis.y:F2})");
		ParallelLoop(0, num, (int i) =>
		{
			ref TileData reference = ref tiles[i];
			reference.biome = HoldridgeBiomeClassifier.Classify(reference.annualTemp, reference.annualPrecipMm, reference.elevation01, reference.isLand, reference.isCoast, reference.isRiver, reference.slopeDegree, latAbs[i]);
			reference.climateZone = ClassifyClimateZone(reference.annualTemp, reference.annualPrecipMm);
			reference.fertility = HoldridgeBiomeClassifier.CalculateFertility(reference.annualTemp, reference.annualPrecipMm, reference.elevation01, reference.slopeDegree, reference.biome);
			reference.npp = EcologyModel.ComputeNpp(reference.annualTemp, reference.annualPrecipMm);
			if (reference.exists && reference.isLand)
			{
				reference.vegetation = reference.npp;
				reference.wildlife = reference.npp * 0.4f;
				if (reference.isCoast) reference.fishStock = 0.8f;
			}
		});
		Debug.Log((object)$"[PlanetTerrainGenerator] 生成完成，陆地{CountLand(tiles)}地块，海洋{num - CountLand(tiles)}地块");
	}

	/// <summary>
	/// 主生成基础温度公式（Phase A：高程/海陆/海岸 -> 温度；Phase B：侵蚀后陆地温度）。纬度单位为度。
	/// <paramref name="elevationForTemperature"/> 是**相对海平面**的高程（海岸 0，最高 1），不是全表归一化高程；
	/// 用绝对高程会让陆地整体被海拔罚掉二三十度，把半张图变成冰盖（P7 诊断：均温 -28.7℃）。
	/// 生成路径与 <see cref="RecalculateBaseClimateForTile"/> 共用本函数，避免公式两处维护漂移。
	/// </summary>
	public static float ComputeBaseAnnualTemperature(float latitude, float elevationForTemperature, bool isLand, bool isCoast, float globalTempOffset)
	{
		float baseTemp = -10f + Mathf.Cos(latitude * (MathF.PI / 180f)) * 38f;
		// 0→1 相对高程约 0→7000m；6.5℃/km 的干绝热近似上限约 45℃。
		float elevationTerm = Mathf.Clamp01(elevationForTemperature) * 45f;
		// 生成期：海洋 +3；陆地海岸 0 / 内陆 -2（Phase A 与 Phase B 同一公式）
		float waterOrCoastTerm = (isLand ? (isCoast ? 0f : (-2f)) : 3f);
		return baseTemp - elevationTerm + waterOrCoastTerm + globalTempOffset;
	}

	/// <summary>把全表归一化高程折算成相对海平面的陆地高程（0=海岸线，1=最高点）。</summary>
	public static float LandElevationAboveSea(float elevation01, bool isLand, float seaLevel)
	{
		if (!isLand) return 0f;
		float span = Mathf.Max(0.01f, 1f - seaLevel);
		return Mathf.Clamp01((elevation01 - seaLevel) / span);
	}

	/// <summary>
	/// 主生成基础降水公式（Phase A：纬度带 + 海岸坡度加成 - 内陆惩罚）。纬度绝对值单位为度。
	/// 生成路径与 <see cref="RecalculateBaseClimateForTile"/> 共用本函数。
	/// </summary>
	public static float ComputeBaseAnnualPrecipitationMm(float latitudeAbs, float slopeDegree, bool isLand, bool isCoast)
	{
		float bandPrecip = ((latitudeAbs < 15f) ? 1800f : ((latitudeAbs < 35f) ? 500f : ((!(latitudeAbs < 60f)) ? 250f : 1200f)));
		float coastSlopeBonus = ((slopeDegree > 15f && isCoast) ? 400f : 0f);
		float inlandPenalty = ((isLand && !isCoast) ? (-200f) : 0f);
		return Mathf.Max(0f, bandPrecip + coastSlopeBonus + inlandPenalty);
	}

	/// <summary>
	/// 主生成基础湿度公式（Phase A：降水归一化到 0~1；GCM 之后生成路径会覆盖，但破碎化补算需与主生成基础口径一致）。
	/// </summary>
	public static float ComputeBaseAirHumidityPct(float annualPrecipMm)
	{
		return Mathf.Clamp01(annualPrecipMm / 2000f);
	}

	/// <summary>
	/// 按主生成路径的基础气候公式重算单地块的 annualTemp / annualPrecipMm / airHumidityPct / climateZone，
	/// 并返回用于 Holdridge 分类的 |lat|。
	/// 公式与 Generate() 中的 Phase A（高程/海陆/海岸 -> 基础温度与纬度带降水）和 Phase B（侵蚀后陆地温度）
	/// 共用同一批 helper；供破碎化等初始地形改写后修补派生属性使用（不跑 GCM/洋流，只复原基础气候公式，
	/// 避免公式复制漂移）。climateZone 用主生成同一分类器重算，保证与新 climate 一致。
	/// </summary>
	/// <param name="elevationForTemperature">
	/// 计算温度用的高程：**相对海平面**的陆地高程（海岸 0、最高 1）。破碎化后 elevation01 已统一折算为相对海平面值，
	/// 直接传当前 elevation01 即可；传破碎化前的绝对高程序会整体抬高温度，与主生成口径不一致。
	/// </param>
	public static float RecalculateBaseClimateForTile(TileData[] tiles, int index, int width, int height, float elevationForTemperature, float globalTempOffset)
	{
		ref TileData reference = ref tiles[index];
		int y = index / width;
		float latitude = 90f - (float)y / (float)height * 180f;
		float latitudeAbs = Mathf.Abs(latitude);
		reference.annualTemp = ComputeBaseAnnualTemperature(latitude, elevationForTemperature, reference.isLand, reference.isCoast, globalTempOffset);
		reference.annualPrecipMm = ComputeBaseAnnualPrecipitationMm(latitudeAbs, reference.slopeDegree, reference.isLand, reference.isCoast);
		reference.airHumidityPct = ComputeBaseAirHumidityPct(reference.annualPrecipMm);
		reference.climateZone = ClassifyClimateZone(reference.annualTemp, reference.annualPrecipMm);
		return latitudeAbs;
	}

	private float QuantileNormalize(float[] elevation, float targetLandFraction)
	{
		int num = elevation.Length;
		float[] array = new float[num];
		Array.Copy(elevation, array, num);
		Array.Sort(array);
		int num2 = Mathf.Clamp((int)((float)num * (1f - targetLandFraction)), 0, num - 1);
		float num3 = array[num2];
		float num4 = array[0];
		float num5 = array[num - 1];
		if (num5 - num4 < 0.001f)
		{
			return 0.5f;
		}
		for (int i = 0; i < num; i++)
		{
			float num6 = ((!(elevation[i] <= num3)) ? (0.5f + 0.5f * (elevation[i] - num3) / (num5 - num3)) : (0.5f * (elevation[i] - num4) / (num3 - num4)));
			elevation[i] = Mathf.Clamp01(num6);
		}
		return 0.5f;
	}

	public static GameEnums.ClimateZone ClassifyClimateZone(float temp, float precip)
	{
		if (temp < -5f)
		{
			return GameEnums.ClimateZone.PolarFrigid;
		}
		if (temp < 5f)
		{
			return GameEnums.ClimateZone.Subarctic;
		}
		if (temp < 10f)
		{
			return GameEnums.ClimateZone.TemperateCold;
		}
		if (temp < 15f)
		{
			if (!(precip < 400f))
			{
				return GameEnums.ClimateZone.TemperateMild;
			}
			return GameEnums.ClimateZone.InlandAridTemperate;
		}
		if (temp < 20f)
		{
			if (!(precip < 400f))
			{
				return GameEnums.ClimateZone.TemperateWarm;
			}
			return GameEnums.ClimateZone.InlandAridTemperate;
		}
		if (temp < 24f)
		{
			return GameEnums.ClimateZone.Subtropical;
		}
		return GameEnums.ClimateZone.Tropical;
	}

	private void ParallelLoop(int from, int to, Action<int> body)
	{
		if (UseMultithreading && to - from > 1000)
		{
			Parallel.For(from, to, body);
			return;
		}
		for (int i = from; i < to; i++)
		{
			body(i);
		}
	}

	private static int CountLand(TileData[] tiles)
	{
		int num = 0;
		for (int i = 0; i < tiles.Length; i++)
		{
			if (tiles[i].isLand)
			{
				num++;
			}
		}
		return num;
	}
}
}
