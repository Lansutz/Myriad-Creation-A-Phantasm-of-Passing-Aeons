using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.World.Biome
{
public static class BiomeAttributes
{
	public struct Attributes
	{
		public float MovementCost;

		public float Habitability;
	}

	private static readonly Dictionary<GameEnums.BiomeType, Attributes> _table = BuildTable();

	private static Dictionary<GameEnums.BiomeType, Attributes> BuildTable()
	{
		return new Dictionary<GameEnums.BiomeType, Attributes>
		{
			[GameEnums.BiomeType.AlluvialPlain] = new Attributes
			{
				MovementCost = 1f,
				Habitability = 95f
			},
			[GameEnums.BiomeType.GreatRiverPlain] = new Attributes
			{
				MovementCost = 1f,
				Habitability = 92f
			},
			[GameEnums.BiomeType.Delta] = new Attributes
			{
				MovementCost = 1.2f,
				Habitability = 88f
			},
			[GameEnums.BiomeType.Interfluvial] = new Attributes
			{
				MovementCost = 1.1f,
				Habitability = 85f
			},
			[GameEnums.BiomeType.WetMarshPlain] = new Attributes
			{
				MovementCost = 2f,
				Habitability = 55f
			},
			[GameEnums.BiomeType.Swamp] = new Attributes
			{
				MovementCost = 3f,
				Habitability = 35f
			},
			[GameEnums.BiomeType.SedimentaryBasin] = new Attributes
			{
				MovementCost = 1.1f,
				Habitability = 82f
			},
			[GameEnums.BiomeType.PiedmontBasin] = new Attributes
			{
				MovementCost = 1.3f,
				Habitability = 78f
			},
			[GameEnums.BiomeType.EnclosedBasin] = new Attributes
			{
				MovementCost = 1.2f,
				Habitability = 75f
			},
			[GameEnums.BiomeType.InlandAridBasin] = new Attributes
			{
				MovementCost = 1.8f,
				Habitability = 40f
			},
			[GameEnums.BiomeType.VolcanicAshPlain] = new Attributes
			{
				MovementCost = 1.2f,
				Habitability = 70f
			},
			[GameEnums.BiomeType.PluvialFan] = new Attributes
			{
				MovementCost = 1.3f,
				Habitability = 68f
			},
			[GameEnums.BiomeType.LoessPlateau] = new Attributes
			{
				MovementCost = 1.6f,
				Habitability = 65f
			},
			[GameEnums.BiomeType.LoessKarst] = new Attributes
			{
				MovementCost = 2f,
				Habitability = 50f
			},
			[GameEnums.BiomeType.FoldMountains] = new Attributes
			{
				MovementCost = 4f,
				Habitability = 25f
			},
			[GameEnums.BiomeType.LowHills] = new Attributes
			{
				MovementCost = 1.8f,
				Habitability = 60f
			},
			[GameEnums.BiomeType.HighMountains] = new Attributes
			{
				MovementCost = 6f,
				Habitability = 15f
			},
			[GameEnums.BiomeType.BrokenPlateau] = new Attributes
			{
				MovementCost = 2.5f,
				Habitability = 45f
			},
			[GameEnums.BiomeType.CoastalLowland] = new Attributes
			{
				MovementCost = 1.1f,
				Habitability = 85f
			},
			[GameEnums.BiomeType.Fjord] = new Attributes
			{
				MovementCost = 2.5f,
				Habitability = 40f
			},
			[GameEnums.BiomeType.KarstMountains] = new Attributes
			{
				MovementCost = 3.5f,
				Habitability = 30f
			},
			[GameEnums.BiomeType.ErodedBadlands] = new Attributes
			{
				MovementCost = 3f,
				Habitability = 25f
			},
			[GameEnums.BiomeType.VolcanicIslands] = new Attributes
			{
				MovementCost = 2f,
				Habitability = 55f
			},
			[GameEnums.BiomeType.ContinentalIslands] = new Attributes
			{
				MovementCost = 1.5f,
				Habitability = 70f
			},
			[GameEnums.BiomeType.CoralAtoll] = new Attributes
			{
				MovementCost = 1.3f,
				Habitability = 50f
			},
			[GameEnums.BiomeType.ContinentalIslet] = new Attributes
			{
				MovementCost = 1.4f,
				Habitability = 60f
			},
			[GameEnums.BiomeType.PlateauMarsh] = new Attributes
			{
				MovementCost = 2.8f,
				Habitability = 35f
			},
			[GameEnums.BiomeType.CoastalSaltMarsh] = new Attributes
			{
				MovementCost = 2.2f,
				Habitability = 40f
			},
			[GameEnums.BiomeType.ImpactCraterAtoll] = new Attributes
			{
				MovementCost = 1.8f,
				Habitability = 45f
			},
			[GameEnums.BiomeType.VolcanicIslandArc] = new Attributes
			{
				MovementCost = 2.2f,
				Habitability = 50f
			},
			[GameEnums.BiomeType.IceSheet] = new Attributes
			{
				MovementCost = 8f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.MountainGlacier] = new Attributes
			{
				MovementCost = 10f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.Tundra] = new Attributes
			{
				MovementCost = 2.5f,
				Habitability = 20f
			},
			[GameEnums.BiomeType.BorealForest] = new Attributes
			{
				MovementCost = 2f,
				Habitability = 45f
			},
			[GameEnums.BiomeType.DeciduousForest] = new Attributes
			{
				MovementCost = 1.8f,
				Habitability = 70f
			},
			[GameEnums.BiomeType.EvergreenForest] = new Attributes
			{
				MovementCost = 1.9f,
				Habitability = 68f
			},
			[GameEnums.BiomeType.MonsoonForest] = new Attributes
			{
				MovementCost = 2f,
				Habitability = 65f
			},
			[GameEnums.BiomeType.Savanna] = new Attributes
			{
				MovementCost = 1.4f,
				Habitability = 55f
			},
			[GameEnums.BiomeType.TropicalRainforest] = new Attributes
			{
				MovementCost = 2.5f,
				Habitability = 50f
			},
			[GameEnums.BiomeType.TropicalMonsoon] = new Attributes
			{
				MovementCost = 2.2f,
				Habitability = 58f
			},
			[GameEnums.BiomeType.Mangrove] = new Attributes
			{
				MovementCost = 3.5f,
				Habitability = 30f
			},
			[GameEnums.BiomeType.HotDesert] = new Attributes
			{
				MovementCost = 2.5f,
				Habitability = 15f
			},
			[GameEnums.BiomeType.InlandDesert] = new Attributes
			{
				MovementCost = 2.8f,
				Habitability = 12f
			},
			[GameEnums.BiomeType.ColdDesert] = new Attributes
			{
				MovementCost = 3f,
				Habitability = 10f
			},
			[GameEnums.BiomeType.CoastalDesert] = new Attributes
			{
				MovementCost = 2.2f,
				Habitability = 25f
			},
			[GameEnums.BiomeType.SemiAridShrubland] = new Attributes
			{
				MovementCost = 1.8f,
				Habitability = 45f
			},
			[GameEnums.BiomeType.SaltDesert] = new Attributes
			{
				MovementCost = 3f,
				Habitability = 8f
			},
			[GameEnums.BiomeType.DesertOasis] = new Attributes
			{
				MovementCost = 1.2f,
				Habitability = 75f
			},
			[GameEnums.BiomeType.EndorheicLake] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.RiverSourceMarsh] = new Attributes
			{
				MovementCost = 2.5f,
				Habitability = 40f
			},
			[GameEnums.BiomeType.AlpineMeadow] = new Attributes
			{
				MovementCost = 2.8f,
				Habitability = 35f
			},
			[GameEnums.BiomeType.TemperateGrassland] = new Attributes
			{
				MovementCost = 1.2f,
				Habitability = 75f
			},
			[GameEnums.BiomeType.GravelGobi] = new Attributes
			{
				MovementCost = 2.5f,
				Habitability = 15f
			},
			[GameEnums.BiomeType.Yardang] = new Attributes
			{
				MovementCost = 3f,
				Habitability = 10f
			},
			[GameEnums.BiomeType.LandBridgeIsthmus] = new Attributes
			{
				MovementCost = 1.3f,
				Habitability = 60f
			},
			[GameEnums.BiomeType.CoralReef] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.KelpForest] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.SeagrassMeadow] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.HydrothermalVent] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.AbyssalPlain] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.OceanicTrench] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.ContinentalShelf] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.ContinentalSlope] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.MidOceanRidge] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.SeaMount] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.Estuary] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.Lagoon] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.TidalFlat] = new Attributes
			{
				MovementCost = 2f,
				Habitability = 35f
			},
			[GameEnums.BiomeType.UpwellingZone] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.PolarSea] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.SeaIce] = new Attributes
			{
				MovementCost = 5f,
				Habitability = 5f
			},
			[GameEnums.BiomeType.CloudForest] = new Attributes
			{
				MovementCost = 2.2f,
				Habitability = 55f
			},
			[GameEnums.BiomeType.TropicalSeasonalForest] = new Attributes
			{
				MovementCost = 1.9f,
				Habitability = 62f
			},
			[GameEnums.BiomeType.TemperateMixedForest] = new Attributes
			{
				MovementCost = 1.7f,
				Habitability = 72f
			},
			[GameEnums.BiomeType.MediterraneanScrub] = new Attributes
			{
				MovementCost = 1.6f,
				Habitability = 65f
			},
			[GameEnums.BiomeType.Parkland] = new Attributes
			{
				MovementCost = 1.3f,
				Habitability = 68f
			},
			[GameEnums.BiomeType.TundraWetland] = new Attributes
			{
				MovementCost = 3f,
				Habitability = 20f
			},
			[GameEnums.BiomeType.HotSpringOasis] = new Attributes
			{
				MovementCost = 1.3f,
				Habitability = 60f
			},
			[GameEnums.BiomeType.PermafrostPlateau] = new Attributes
			{
				MovementCost = 3f,
				Habitability = 15f
			},
			[GameEnums.BiomeType.Thermokarst] = new Attributes
			{
				MovementCost = 3.5f,
				Habitability = 18f
			},
			[GameEnums.BiomeType.BadlandsDesert] = new Attributes
			{
				MovementCost = 3.2f,
				Habitability = 12f
			},
			[GameEnums.BiomeType.ErgSea] = new Attributes
			{
				MovementCost = 3.5f,
				Habitability = 8f
			},
			[GameEnums.BiomeType.CalderaLake] = new Attributes
			{
				MovementCost = 50f,
				Habitability = 0f
			},
			[GameEnums.BiomeType.GeyserField] = new Attributes
			{
				MovementCost = 2.5f,
				Habitability = 25f
			},
			[GameEnums.BiomeType.TowerKarst] = new Attributes
			{
				MovementCost = 3f,
				Habitability = 30f
			},
			[GameEnums.BiomeType.GlacialValley] = new Attributes
			{
				MovementCost = 3.5f,
				Habitability = 25f
			},
			[GameEnums.BiomeType.RiftValley] = new Attributes
			{
				MovementCost = 2f,
				Habitability = 45f
			},
			[GameEnums.BiomeType.CustomBiome1] = new Attributes
			{
				MovementCost = 1.5f,
				Habitability = 50f
			},
			[GameEnums.BiomeType.CustomBiome2] = new Attributes
			{
				MovementCost = 1.5f,
				Habitability = 50f
			},
			[GameEnums.BiomeType.CustomBiome3] = new Attributes
			{
				MovementCost = 1.5f,
				Habitability = 50f
			},
			[GameEnums.BiomeType.CustomBiome4] = new Attributes
			{
				MovementCost = 1.5f,
				Habitability = 50f
			},
			[GameEnums.BiomeType.CustomBiome5] = new Attributes
			{
				MovementCost = 1.5f,
				Habitability = 50f
			}
		};
	}

	public static float GetMovementCost(GameEnums.BiomeType biome)
	{
		if (BiomeRegistry.TryGet((int)biome, out var def))
		{
			return def.movementCost;
		}
		if (!_table.TryGetValue(biome, out var value))
		{
			return 1.5f;
		}
		return value.MovementCost;
	}

	public static float GetHabitability(GameEnums.BiomeType biome)
	{
		if (BiomeRegistry.TryGet((int)biome, out var def))
		{
			return def.habitability;
		}
		if (!_table.TryGetValue(biome, out var value))
		{
			return 50f;
		}
		return value.Habitability;
	}

	public static Attributes GetAttributes(GameEnums.BiomeType biome)
	{
		if (BiomeRegistry.TryGet((int)biome, out var def))
		{
			return new Attributes
			{
				MovementCost = def.movementCost,
				Habitability = def.habitability
			};
		}
		if (!_table.TryGetValue(biome, out var value))
		{
			return new Attributes
			{
				MovementCost = 1.5f,
				Habitability = 50f
			};
		}
		return value;
	}
}
}
