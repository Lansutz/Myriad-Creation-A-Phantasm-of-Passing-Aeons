using System;
using System.Collections.Generic;
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

        /// <summary>
        /// 社会亚阶层（用户定稿：农民/自由民/奴隶三阶层细分——主枚举不动保存档兼容）
        /// 农民四层：自耕农（有地）/佃农（租地）/农奴（人身束缚）/雇农（无地）
        /// 自由民四民：市民（公民权）/商人/工匠/士人（士农工商）
        /// 奴隶四源：家奴/官奴（国有劳役）/债务奴（抵债）/战俘奴
        /// </summary>
        public enum SocialSubclass
        {
            // ===== 农民 Peasant =====
            Freeholder,     // 自耕农：拥有土地的独立农民
            Tenant,         // 佃农：租地耕种（交租）
            Serf,           // 农奴：人身束缚于土地（中世纪欧洲）
            HiredLaborer,   // 雇农：无地雇工（长工/短工）
            // ===== 自由民 MerchantFreeman =====
            Citizen,        // 市民：城邦公民（公民权）
            Merchant,       // 商人：行商坐贾
            Artisan,        // 工匠：手艺人（行会）
            Scholar,        // 士人/文士（士农工商）
            // ===== 奴隶 Slave =====
            DomesticSlave,  // 家奴：家庭侍从
            StateSlave,     // 官奴：国有劳役
            DebtSlave,      // 债务奴：抵债为奴（自卖）
            WarCaptiveSlave // 战俘奴：战败俘虏
        }

        /// <summary>亚阶层 ↔ 主阶层 映射与查询</summary>
        public static class SocialClassHierarchy
        {
            /// <summary>亚阶层所属主阶层</summary>
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

            /// <summary>主阶层全部亚阶层</summary>
            public static List<SocialSubclass> GetSubclasses(SocialClass socialClass)
            {
                var result = new List<SocialSubclass>();
                foreach (SocialSubclass s in Enum.GetValues(typeof(SocialSubclass)))
                {
                    if (GetClass(s) == socialClass)
                        result.Add(s);
                }
                return result;
            }

            /// <summary>主阶层默认亚阶层（细分前的默认值；未细分阶层返回 null）</summary>
            public static SocialSubclass? GetDefaultSubclass(SocialClass socialClass)
            {
                switch (socialClass)
                {
                    case SocialClass.Peasant: return SocialSubclass.Freeholder;
                    case SocialClass.MerchantFreeman: return SocialSubclass.Citizen;
                    case SocialClass.Slave: return SocialSubclass.DomesticSlave;
                    default: return null; // Royalty/NobilityClergy 未细分
                }
            }
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
