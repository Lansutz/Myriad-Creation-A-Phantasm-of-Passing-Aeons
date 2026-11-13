using System;
using System.Collections.Generic;

namespace MyriadCreation.Core.Enums
{
public static class GameEnums
{
	public enum CirculationMode
	{
		SingleCell,
		DoubleCell,
		TripleCell
	}

	public enum ClimateZone
	{
		PolarFrigid,
		Subarctic,
		TemperateCold,
		TemperateMild,
		TemperateWarm,
		Subtropical,
		Tropical,
		HighlandAlpine,
		InlandAridTemperate
	}

	public enum BiomeType
	{
		AlluvialPlain,
		GreatRiverPlain,
		Delta,
		Interfluvial,
		WetMarshPlain,
		Swamp,
		SedimentaryBasin,
		PiedmontBasin,
		EnclosedBasin,
		InlandAridBasin,
		VolcanicAshPlain,
		PluvialFan,
		LoessPlateau,
		LoessKarst,
		FoldMountains,
		LowHills,
		HighMountains,
		BrokenPlateau,
		CoastalLowland,
		Fjord,
		KarstMountains,
		ErodedBadlands,
		VolcanicIslands,
		ContinentalIslands,
		CoralAtoll,
		ContinentalIslet,
		PlateauMarsh,
		CoastalSaltMarsh,
		ImpactCraterAtoll,
		VolcanicIslandArc,
		IceSheet,
		MountainGlacier,
		Tundra,
		BorealForest,
		DeciduousForest,
		EvergreenForest,
		MonsoonForest,
		Savanna,
		TropicalRainforest,
		TropicalMonsoon,
		Mangrove,
		HotDesert,
		InlandDesert,
		ColdDesert,
		CoastalDesert,
		SemiAridShrubland,
		SaltDesert,
		DesertOasis,
		EndorheicLake,
		RiverSourceMarsh,
		AlpineMeadow,
		TemperateGrassland,
		GravelGobi,
		Yardang,
		LandBridgeIsthmus,
		CoralReef,
		KelpForest,
		SeagrassMeadow,
		HydrothermalVent,
		AbyssalPlain,
		OceanicTrench,
		ContinentalShelf,
		ContinentalSlope,
		MidOceanRidge,
		SeaMount,
		Estuary,
		Lagoon,
		TidalFlat,
		UpwellingZone,
		PolarSea,
		SeaIce,
		CloudForest,
		TropicalSeasonalForest,
		TemperateMixedForest,
		MediterraneanScrub,
		Parkland,
		TundraWetland,
		HotSpringOasis,
		PermafrostPlateau,
		Thermokarst,
		BadlandsDesert,
		ErgSea,
		CalderaLake,
		GeyserField,
		TowerKarst,
		GlacialValley,
		RiftValley,
		CustomBiome1,
		CustomBiome2,
		CustomBiome3,
		CustomBiome4,
		CustomBiome5
	}

	public enum OceanTier
	{
		None,
		Land,
		Coast,
		NearSea,
		MidSea,
		FarSea,
		DeepSea
	}

	public enum RoadLevel
	{
		None,
		DirtRoad,
		OfficialRoad,
		ImperialHighway
	}

	public enum MovementControlLevel
	{
		None,
		Loose,
		Limited,
		Strict
	}

	public enum ConflictLevel
	{
		Peace,
		Tension,
		Hostility,
		LimitedWar,
		TotalWar
	}

	public enum RaidType
	{
		BorderSkirmish,
		VillageRaid,
		TownAttack,
		SupplyRaiding,
		SlaveRaiding,
		Massacre
	}

	public enum CasusBelliType
	{
		None,
		RaidReprisal,
		BorderIncident,
		TerritorialDispute,
		ReligiousConflict,
		AllianceObligation,
		HegemonyExpansion,
		DynasticClaim,
		TradeDispute,
		IndependenceWar,
		Reconquest,
		Crusade,
		ImperialConquest,
		CivilWar,
		Intervention
	}

	public enum WarGoalType
	{
		None,
		ConquerTerritory,
		ConquerRegion,
		Vassalization,
		PersonalUnion,
		Indemnity,
		ReleaseVassal,
		ConvertReligion,
		EnforceTradeRights,
		Disarmament,
		Humiliation,
		Annihilation,
		BorderAdjustment,
		Independence,
		InstallRuler
	}

	public enum SocialClass
	{
		Royalty,
		NobilityClergy,
		MerchantFreeman,
		Peasant,
		Slave
	}

	public enum SocialSubclass
	{
		Freeholder,
		Tenant,
		Serf,
		HiredLaborer,
		Citizen,
		Merchant,
		Artisan,
		Scholar,
		DomesticSlave,
		StateSlave,
		DebtSlave,
		WarCaptiveSlave
	}

	public static class SocialClassHierarchy
	{
		public static SocialClass GetClass(SocialSubclass subclass)
		{
			switch (subclass)
			{
			case SocialSubclass.Freeholder:
			case SocialSubclass.Tenant:
			case SocialSubclass.Serf:
			case SocialSubclass.HiredLaborer:
				return SocialClass.Peasant;
			case SocialSubclass.Citizen:
			case SocialSubclass.Merchant:
			case SocialSubclass.Artisan:
			case SocialSubclass.Scholar:
				return SocialClass.MerchantFreeman;
			default:
				return SocialClass.Slave;
			}
		}

		public static List<SocialSubclass> GetSubclasses(SocialClass socialClass)
		{
			List<SocialSubclass> list = new List<SocialSubclass>();
			foreach (SocialSubclass value in Enum.GetValues(typeof(SocialSubclass)))
			{
				if (GetClass(value) == socialClass)
				{
					list.Add(value);
				}
			}
			return list;
		}

		public static SocialSubclass? GetDefaultSubclass(SocialClass socialClass)
		{
			return socialClass switch
			{
				SocialClass.Peasant => (SocialSubclass?)SocialSubclass.Freeholder, 
				SocialClass.MerchantFreeman => SocialSubclass.Citizen, 
				SocialClass.Slave => SocialSubclass.DomesticSlave, 
				_ => null, 
			};
		}
	}

	public enum CultureStage
	{
		Band,
		Tribe,
		Chiefdom,
		EthnicGroup,
		HighCivilization
	}

	/// <summary>
	/// 兵种军种大类（步兵/骑兵/海军，用于 UnitDef 等队伍编制定义）。
	/// 与 Simulation.Units.UnitCategory（Military/Trade/Migration/Bandit/Wilderness，运行时单位类别）不是同一概念，
	/// 故命名为 UnitArmType 以避免同名混淆（枚举值顺序不变，存档/资产数值语义不变）。
	/// </summary>
	public enum UnitArmType
	{
		Infantry,
		Cavalry,
		Navy
	}

	public enum GoodsCategory
	{
		Food,
		Crop,
		Livestock,
		Wood,
		Stone,
		MetalOre,
		Metal,
		Gem,
		Equipment,
		Luxury,
		Slave,
		Cloth,
		Leather,
		Fiber,
		Hide,
		Fur,
		Clay,
		Ceramic,
		Glass
	}

	public enum CurrencyStage
	{
		Barter,
		Bullion,
		MintedCoin,
		PaperMoney
	}

	public enum CombatState
	{
		Idle,
		Marching,
		Besieging,
		InCombat,
		Retreating,
		Routed,
		Dead
	}

	public enum TerrainTacticType
	{
		Plain,
		Forest,
		Mountain,
		River,
		Wetland,
		Desert,
		Fortress,
		Amphibious
	}

	public enum SovereigntyStatus
	{
		Core,
		VassalTribute,
		Foreign
	}
}
}
