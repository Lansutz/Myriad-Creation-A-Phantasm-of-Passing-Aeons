using UnityEngine;

namespace CivilizationEvolution.Core
{
    /// <summary>
    /// 全局枚举定义
    /// </summary>
    public static class GameEnums
    {
        /// <summary>大气环流模式</summary>
        public enum CirculationMode
        {
            SingleCell,
            DoubleCell,
            TripleCell
        }

        /// <summary>九大温度带</summary>
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

        /// <summary>生物群系</summary>
        public enum BiomeType
        {
            IceSheet,
            Tundra,
            BorealForest,
            TemperateForest,
            TemperateGrassland,
            Desert,
            Steppe,
            Savanna,
            TropicalRainforest,
            TropicalMonsoon,
            Alpine,
            Wetland,
            Volcanic,
            SaltLake
        }

        /// <summary>海洋分级</summary>
        public enum OceanTier
        {
            None,
            Land,
            Coast,
            NearSea,
            DeepSea
        }

        /// <summary>道路等级</summary>
        public enum RoadLevel
        {
            None,
            DirtRoad,
            OfficialRoad,
            ImperialHighway
        }

        /// <summary>社会阶层</summary>
        public enum SocialClass
        {
            Royalty,
            NobilityClergy,
            MerchantFreeman,
            Peasant,
            Slave
        }

        /// <summary>政体类型</summary>
        public enum GovernmentType
        {
            Tribal,
            Chiefdom,
            Feudal,
            Centralized,
            Theocratic,
            Republic,
            NomadicConfederation
        }

        /// <summary>文化阶段</summary>
        public enum CultureStage
        {
            Band,
            Tribe,
            Chiefdom,
            EthnicGroup,
            HighCivilization
        }

        /// <summary>兵种大类</summary>
        public enum UnitCategory
        {
            Infantry,
            Cavalry,
            Navy
        }

        /// <summary>物资类别</summary>
        public enum GoodsCategory
        {
            Food,
            Crop,
            Livestock,
            Wood,
            Stone,
            MetalOre,
            PreciousMetal,
            Gem,
            Equipment,
            Luxury,
            Slave
        }

        /// <summary>货币阶段</summary>
        public enum CurrencyStage
        {
            Barter,
            Bullion,
            MintedCoin,
            PaperMoney
        }

        /// <summary>战斗状态</summary>
        public enum CombatState
        {
            Idle,
            Marching,
            Besieging,
            InCombat,
            Retreating,
            Routed
        }

        /// <summary>地形战术类型</summary>
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
    }
}
