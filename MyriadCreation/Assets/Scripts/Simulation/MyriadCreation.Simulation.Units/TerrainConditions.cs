using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Units
{
public struct TerrainConditions
{
	public float TemperatureC;

	public float HumidityPct;

	public float SoilHumidityPct;

	public float PrecipitationMm;

	public float Elevation01;

	public float SlopeDegrees;

	public bool IsLand;

	public bool IsRiver;

	public bool IsCoast;

	public float OceanDepth01;

	public float WaterAdjacentWeight;

	public int RiverStreamOrder;

	public float RiverFlowAccumulation;

	public float RiverWidthMeters;

	public float RiverDepthMeters;

	public float RiverCurrentMps;

	public GameEnums.OceanTier OceanTier;

	public bool IsDeepRiver
	{
		get
		{
			if (IsRiver)
			{
				if (!(RiverWidthMeters > 60f) && !(RiverDepthMeters > 1.2f))
				{
					if (RiverWidthMeters <= 0f)
					{
						if (!(WaterAdjacentWeight > 0.45f))
						{
							return PrecipitationMm > 1200f;
						}
						return true;
					}
					return false;
				}
				return true;
			}
			return false;
		}
	}

	public bool RequiresBoat
	{
		get
		{
			if (!IsLand && OceanTier != GameEnums.OceanTier.Coast)
			{
				return OceanTier != GameEnums.OceanTier.Land;
			}
			return false;
		}
	}

	public bool IsNavigableRiver => IsRiver && MyriadCreation.World.Hydrology.RiverNavigability.IsNavigable(
		RiverStreamOrder, RiverWidthMeters, RiverDepthMeters, Elevation01, PrecipitationMm);

	public static TerrainConditions FromTile(TileData tile)
	{
		return new TerrainConditions
		{
			TemperatureC = tile.annualTemp,
			HumidityPct = tile.airHumidityPct,
			SoilHumidityPct = tile.soilHumidityPct,
			PrecipitationMm = tile.annualPrecipMm,
			Elevation01 = tile.elevation01,
			SlopeDegrees = tile.slopeDegree,
			IsLand = tile.isLand,
			IsRiver = tile.isRiver,
			IsCoast = tile.isCoast,
			OceanDepth01 = tile.oceanDepth01,
			WaterAdjacentWeight = tile.waterAdjacentWeight,
			RiverStreamOrder = tile.riverStreamOrder,
			RiverFlowAccumulation = tile.riverFlowAccumulation,
			RiverWidthMeters = tile.riverWidthMeters,
			RiverDepthMeters = tile.riverDepthMeters,
			RiverCurrentMps = tile.riverCurrentMps,
			OceanTier = tile.oceanTier
		};
	}

	public float CrossingMultiplier(bool hasBoat)
	{
		if (!IsLand)
		{
			switch (OceanTier)
			{
			case GameEnums.OceanTier.Coast:
				if (!hasBoat)
				{
					return 0.35f;
				}
				return 0.7f;
			case GameEnums.OceanTier.NearSea:
				if (!hasBoat)
				{
					return 0.15f;
				}
				return 0.6f;
			case GameEnums.OceanTier.MidSea:
				if (!hasBoat)
				{
					return 0.05f;
				}
				return 0.45f;
			default:
				if (!hasBoat)
				{
					return 0.05f;
				}
				return 0.3f;
			}
		}
		if (!IsRiver)
		{
			return 1f;
		}
		if (hasBoat && IsNavigableRiver)
		{
			return 0.9f;
		}
		if (hasBoat)
		{
			if (!IsDeepRiver)
			{
				return 0.9f;
			}
			return 0.8f;
		}
		if (IsDeepRiver)
		{
			if (!(RiverDepthMeters > 2f))
			{
				return 0.35f;
			}
			return 0.2f;
		}
		return 0.7f;
	}

	public float GroundBearing()
	{
		if (!IsLand)
		{
			return 0f;
		}
		float num = Mathf.Clamp01(SoilHumidityPct / 100f);
		float num2 = Mathf.Clamp01(SlopeDegrees / 45f);
		return Mathf.Clamp01(1f - num * 0.7f - num2 * 0.3f);
	}
}
}
