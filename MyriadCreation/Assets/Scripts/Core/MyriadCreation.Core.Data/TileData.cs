using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Core.Data
{
[Serializable]
public struct TileData
{
	public int tileIndex;

	public int regionId;

	public int provinceId;

	public int ownerRealmId;

	public int occupyingRealmId;

	public bool isRiver;

	public int riverStreamOrder;

	public float riverFlowAccumulation;

	public float riverWidthMeters;

	public float riverDepthMeters;

	public float riverCurrentMps;

	public bool exists;

	public bool locked;

	public float elevation01;

	public float slopeDegree;

	public float terrainShade;

	public bool isGate;

	public GameEnums.RoadLevel roadLevel;

	public bool passable;

	public float movementCost;

	public bool hasBarrier;

	public int barrierOwnerRealmId;

	public float barrierStrength;

	public int nearbyFortId;

	public int fortInfluenceLevel;

	public bool isLand;

	public bool isCoast;

	public GameEnums.OceanTier oceanTier;

	public float oceanDepth01;

	public int seaConnectId;

	public float waterAdjacentWeight;

	public float annualTemp;

	public float diurnalTempRange;

	public float annualPrecipMm;

	public float airHumidityPct;

	public float soilHumidityPct;

	public float accumulatedTemp;

	public float frostFreeDays;

	public GameEnums.ClimateZone climateZone;

	public GameEnums.BiomeType biome;

	public float fertility;

	/// <summary>Miami 模型净初级生产力 0–1（借鉴 Earth Simulator）；0 = 旧档未计算/极端不毛。</summary>
	public float npp;

	/// <summary>植被覆盖 0–1（生态演替状态）。0 = 旧档/未初始化，按 <see cref="npp"/> 惰性补齐。</summary>
	public float vegetation;

	/// <summary>野生草食动物密度 0–1（聚合口径；随植被与人口压力变化）。</summary>
	public float wildlife;

	/// <summary>沿海鱼群 0–1（P4.3；只在 isCoast 地块有效，捕捞与恢复同源）。</summary>
	public float fishStock;

	/// <summary>放牧压力 0–1（P4.3；随人口变化，压低植被目标）。</summary>
	public float livestock;

	public float development;

	public float stability;

	public float order;

	public int[] buildingLevels;

	public List<PopulationBlock> populationBlocks;

	public List<TileResource> resources;

	public int campId;

	public bool isTerrainDirty;

	public bool isClimateDirty;

	public bool isEconomyDirty;

	public bool isOcean => !isLand;

	public bool isCoastal => isCoast;
}
}
