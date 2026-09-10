using System.Collections.Generic;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Climate
{
    /// <summary>
    /// 群系属性表（参考 Azgaar FMG 的 biomesData.cost / habitability 设计）。
    /// 每个群系有基础移动成本和可居住性，在群系划分时应用到地块。
    /// 移动成本：1.0=平原正常，越高越难通行；海洋对陆地单位不可通行。
    /// 可居住性：0-100，影响人口承载、聚落生成、文化扩张。
    /// </summary>
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
            var t = new Dictionary<GameEnums.BiomeType, Attributes>();

            // ===== A系：低水沃野（农耕定居基座）=====
            t[GameEnums.BiomeType.AlluvialPlain] = new Attributes { MovementCost = 1.0f, Habitability = 95f };
            t[GameEnums.BiomeType.GreatRiverPlain] = new Attributes { MovementCost = 1.0f, Habitability = 92f };
            t[GameEnums.BiomeType.Delta] = new Attributes { MovementCost = 1.2f, Habitability = 88f };
            t[GameEnums.BiomeType.Interfluvial] = new Attributes { MovementCost = 1.1f, Habitability = 85f };
            t[GameEnums.BiomeType.WetMarshPlain] = new Attributes { MovementCost = 2.0f, Habitability = 55f };
            t[GameEnums.BiomeType.Swamp] = new Attributes { MovementCost = 3.0f, Habitability = 35f };
            t[GameEnums.BiomeType.SedimentaryBasin] = new Attributes { MovementCost = 1.1f, Habitability = 82f };
            t[GameEnums.BiomeType.PiedmontBasin] = new Attributes { MovementCost = 1.3f, Habitability = 78f };
            t[GameEnums.BiomeType.EnclosedBasin] = new Attributes { MovementCost = 1.2f, Habitability = 75f };
            t[GameEnums.BiomeType.InlandAridBasin] = new Attributes { MovementCost = 1.8f, Habitability = 40f };
            t[GameEnums.BiomeType.VolcanicAshPlain] = new Attributes { MovementCost = 1.2f, Habitability = 70f };
            t[GameEnums.BiomeType.PluvialFan] = new Attributes { MovementCost = 1.3f, Habitability = 68f };

            // ===== B系：高地硬骨（屏障、割据与海陆）=====
            t[GameEnums.BiomeType.LoessPlateau] = new Attributes { MovementCost = 1.6f, Habitability = 65f };
            t[GameEnums.BiomeType.LoessKarst] = new Attributes { MovementCost = 2.0f, Habitability = 50f };
            t[GameEnums.BiomeType.FoldMountains] = new Attributes { MovementCost = 4.0f, Habitability = 25f };
            t[GameEnums.BiomeType.LowHills] = new Attributes { MovementCost = 1.8f, Habitability = 60f };
            t[GameEnums.BiomeType.HighMountains] = new Attributes { MovementCost = 6.0f, Habitability = 15f };
            t[GameEnums.BiomeType.BrokenPlateau] = new Attributes { MovementCost = 2.5f, Habitability = 45f };
            t[GameEnums.BiomeType.CoastalLowland] = new Attributes { MovementCost = 1.1f, Habitability = 85f };
            t[GameEnums.BiomeType.Fjord] = new Attributes { MovementCost = 2.5f, Habitability = 40f };
            t[GameEnums.BiomeType.KarstMountains] = new Attributes { MovementCost = 3.5f, Habitability = 30f };
            t[GameEnums.BiomeType.ErodedBadlands] = new Attributes { MovementCost = 3.0f, Habitability = 25f };
            t[GameEnums.BiomeType.VolcanicIslands] = new Attributes { MovementCost = 2.0f, Habitability = 55f };
            t[GameEnums.BiomeType.ContinentalIslands] = new Attributes { MovementCost = 1.5f, Habitability = 70f };
            t[GameEnums.BiomeType.CoralAtoll] = new Attributes { MovementCost = 1.3f, Habitability = 50f };
            t[GameEnums.BiomeType.ContinentalIslet] = new Attributes { MovementCost = 1.4f, Habitability = 60f };
            t[GameEnums.BiomeType.PlateauMarsh] = new Attributes { MovementCost = 2.8f, Habitability = 35f };
            t[GameEnums.BiomeType.CoastalSaltMarsh] = new Attributes { MovementCost = 2.2f, Habitability = 40f };
            t[GameEnums.BiomeType.ImpactCraterAtoll] = new Attributes { MovementCost = 1.8f, Habitability = 45f };
            t[GameEnums.BiomeType.VolcanicIslandArc] = new Attributes { MovementCost = 2.2f, Habitability = 50f };

            // ===== C系：极端覆盖与过渡 =====
            t[GameEnums.BiomeType.IceSheet] = new Attributes { MovementCost = 8.0f, Habitability = 0f };
            t[GameEnums.BiomeType.MountainGlacier] = new Attributes { MovementCost = 10f, Habitability = 0f };
            t[GameEnums.BiomeType.Tundra] = new Attributes { MovementCost = 2.5f, Habitability = 20f };
            t[GameEnums.BiomeType.BorealForest] = new Attributes { MovementCost = 2.0f, Habitability = 45f };
            t[GameEnums.BiomeType.DeciduousForest] = new Attributes { MovementCost = 1.8f, Habitability = 70f };
            t[GameEnums.BiomeType.EvergreenForest] = new Attributes { MovementCost = 1.9f, Habitability = 68f };
            t[GameEnums.BiomeType.MonsoonForest] = new Attributes { MovementCost = 2.0f, Habitability = 65f };
            t[GameEnums.BiomeType.Savanna] = new Attributes { MovementCost = 1.4f, Habitability = 55f };
            t[GameEnums.BiomeType.TropicalRainforest] = new Attributes { MovementCost = 2.5f, Habitability = 50f };
            t[GameEnums.BiomeType.TropicalMonsoon] = new Attributes { MovementCost = 2.2f, Habitability = 58f };
            t[GameEnums.BiomeType.Mangrove] = new Attributes { MovementCost = 3.5f, Habitability = 30f };
            t[GameEnums.BiomeType.HotDesert] = new Attributes { MovementCost = 2.5f, Habitability = 15f };
            t[GameEnums.BiomeType.InlandDesert] = new Attributes { MovementCost = 2.8f, Habitability = 12f };
            t[GameEnums.BiomeType.ColdDesert] = new Attributes { MovementCost = 3.0f, Habitability = 10f };
            t[GameEnums.BiomeType.CoastalDesert] = new Attributes { MovementCost = 2.2f, Habitability = 25f };
            t[GameEnums.BiomeType.SemiAridShrubland] = new Attributes { MovementCost = 1.8f, Habitability = 45f };
            t[GameEnums.BiomeType.SaltDesert] = new Attributes { MovementCost = 3.0f, Habitability = 8f };
            t[GameEnums.BiomeType.DesertOasis] = new Attributes { MovementCost = 1.2f, Habitability = 75f };
            t[GameEnums.BiomeType.EndorheicLake] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.RiverSourceMarsh] = new Attributes { MovementCost = 2.5f, Habitability = 40f };
            t[GameEnums.BiomeType.AlpineMeadow] = new Attributes { MovementCost = 2.8f, Habitability = 35f };
            t[GameEnums.BiomeType.TemperateGrassland] = new Attributes { MovementCost = 1.2f, Habitability = 75f };
            t[GameEnums.BiomeType.GravelGobi] = new Attributes { MovementCost = 2.5f, Habitability = 15f };
            t[GameEnums.BiomeType.Yardang] = new Attributes { MovementCost = 3.0f, Habitability = 10f };
            t[GameEnums.BiomeType.LandBridgeIsthmus] = new Attributes { MovementCost = 1.3f, Habitability = 60f };

            // ===== D系：海洋群系（陆地单位不可通行，可居住性0）=====
            t[GameEnums.BiomeType.CoralReef] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.KelpForest] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.SeagrassMeadow] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.HydrothermalVent] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.AbyssalPlain] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.OceanicTrench] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.ContinentalShelf] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.ContinentalSlope] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.MidOceanRidge] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.SeaMount] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.Estuary] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.Lagoon] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.TidalFlat] = new Attributes { MovementCost = 2.0f, Habitability = 35f };
            t[GameEnums.BiomeType.UpwellingZone] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.PolarSea] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.SeaIce] = new Attributes { MovementCost = 5.0f, Habitability = 5f };

            // ===== 补充群系 =====
            t[GameEnums.BiomeType.CloudForest] = new Attributes { MovementCost = 2.2f, Habitability = 55f };
            t[GameEnums.BiomeType.TropicalSeasonalForest] = new Attributes { MovementCost = 1.9f, Habitability = 62f };
            t[GameEnums.BiomeType.TemperateMixedForest] = new Attributes { MovementCost = 1.7f, Habitability = 72f };
            t[GameEnums.BiomeType.MediterraneanScrub] = new Attributes { MovementCost = 1.6f, Habitability = 65f };
            t[GameEnums.BiomeType.Parkland] = new Attributes { MovementCost = 1.3f, Habitability = 68f };
            t[GameEnums.BiomeType.TundraWetland] = new Attributes { MovementCost = 3.0f, Habitability = 20f };
            t[GameEnums.BiomeType.HotSpringOasis] = new Attributes { MovementCost = 1.3f, Habitability = 60f };
            t[GameEnums.BiomeType.PermafrostPlateau] = new Attributes { MovementCost = 3.0f, Habitability = 15f };
            t[GameEnums.BiomeType.Thermokarst] = new Attributes { MovementCost = 3.5f, Habitability = 18f };
            t[GameEnums.BiomeType.BadlandsDesert] = new Attributes { MovementCost = 3.2f, Habitability = 12f };
            t[GameEnums.BiomeType.ErgSea] = new Attributes { MovementCost = 3.5f, Habitability = 8f };
            t[GameEnums.BiomeType.CalderaLake] = new Attributes { MovementCost = 999f, Habitability = 0f };
            t[GameEnums.BiomeType.GeyserField] = new Attributes { MovementCost = 2.5f, Habitability = 25f };
            t[GameEnums.BiomeType.TowerKarst] = new Attributes { MovementCost = 3.0f, Habitability = 30f };
            t[GameEnums.BiomeType.GlacialValley] = new Attributes { MovementCost = 3.5f, Habitability = 25f };
            t[GameEnums.BiomeType.RiftValley] = new Attributes { MovementCost = 2.0f, Habitability = 45f };

            // 自定义群系默认值
            t[GameEnums.BiomeType.CustomBiome1] = new Attributes { MovementCost = 1.5f, Habitability = 50f };
            t[GameEnums.BiomeType.CustomBiome2] = new Attributes { MovementCost = 1.5f, Habitability = 50f };
            t[GameEnums.BiomeType.CustomBiome3] = new Attributes { MovementCost = 1.5f, Habitability = 50f };
            t[GameEnums.BiomeType.CustomBiome4] = new Attributes { MovementCost = 1.5f, Habitability = 50f };
            t[GameEnums.BiomeType.CustomBiome5] = new Attributes { MovementCost = 1.5f, Habitability = 50f };

            return t;
        }

        /// <summary>获取群系基础移动成本（1.0=平原，999=不可通行）。优先读 JSON 配置，回退硬编码默认值。</summary>
        public static float GetMovementCost(GameEnums.BiomeType biome)
        {
            if (ContentRegistry.IsInitialized && ContentRegistry.TryGetBiome((int)biome, out var def))
                return def.movementCost;
            return _table.TryGetValue(biome, out var a) ? a.MovementCost : 1.5f;
        }

        /// <summary>获取群系可居住性（0-100）。优先读 JSON 配置，回退硬编码默认值。</summary>
        public static float GetHabitability(GameEnums.BiomeType biome)
        {
            if (ContentRegistry.IsInitialized && ContentRegistry.TryGetBiome((int)biome, out var def))
                return def.habitability;
            return _table.TryGetValue(biome, out var a) ? a.Habitability : 50f;
        }

        /// <summary>获取群系全部属性。优先读 JSON 配置，回退硬编码默认值。</summary>
        public static Attributes GetAttributes(GameEnums.BiomeType biome)
        {
            if (ContentRegistry.IsInitialized && ContentRegistry.TryGetBiome((int)biome, out var def))
                return new Attributes { MovementCost = def.movementCost, Habitability = def.habitability };
            return _table.TryGetValue(biome, out var a) ? a : new Attributes { MovementCost = 1.5f, Habitability = 50f };
        }
    }
}
