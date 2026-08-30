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
        /// <summary>
        /// 生物群系（55个，按 civilization-engine-v45 三系分类）
        /// A系：低水沃野（农耕定居基座）12个
        /// B系：高地硬骨（屏障割据与海陆）18个
        /// C系：极端覆盖与过渡（减速通道与资源边界）25个
        /// </summary>
        public enum BiomeType
        {
            // ===== A系：低水沃野（农耕与定居基座）=====
            AlluvialPlain,      // 冲积平原
            GreatRiverPlain,    // 大河平原
            Delta,              // 三角洲
            Interfluvial,       // 河间地
            WetMarshPlain,      // 湿沼平原
            Swamp,              // 沼泽地
            SedimentaryBasin,   // 沉积盆地
            PiedmontBasin,      // 山前拗陷盆地
            EnclosedBasin,      // 环山构造盆地
            InlandAridBasin,    // 内陆干旱盆地
            VolcanicAshPlain,   // 火山灰平原
            PluvialFan,         // 洪积扇

            // ===== B系：高地硬骨（屏障、割据与海洋陆地）=====
            LoessPlateau,       // 黄土高原
            LoessKarst,         // 黄土溶蚀
            FoldMountains,      // 褶皱山地
            LowHills,            // 低山丘陵
            HighMountains,       // 高亢山地
            BrokenPlateau,       // 破碎高原
            CoastalLowland,      // 滨海低地
            Fjord,               // 峡湾
            KarstMountains,      // 岩溶山地
            ErodedBadlands,      // 侵蚀劣地
            VolcanicIslands,     // 火山群岛
            ContinentalIslands,  // 大陆群岛
            CoralAtoll,          // 珊瑚离岛
            ContinentalIslet,    // 大陆离岛
            PlateauMarsh,        // 高原沼泽
            CoastalSaltMarsh,    // 滨海盐沼
            ImpactCraterAtoll,   // 陨坑环岛
            VolcanicIslandArc,   // 火山岛弧

            // ===== C系：极端覆盖与过渡（减速、通道与资源边界）=====
            IceSheet,            // 冰盖
            MountainGlacier,     // 山岳冰川
            Tundra,              // 冻原
            BorealForest,        // 寒带针叶林（泰加林）
            DeciduousForest,     // 落叶阔叶林
            EvergreenForest,     // 常绿阔叶林
            MonsoonForest,       // 季风干湿林
            Savanna,             // 稀树草原
            TropicalRainforest,  // 雨林
            TropicalMonsoon,     // 季雨林
            Mangrove,            // 红树林
            HotDesert,           // 炎热沙漠
            InlandDesert,        // 内陆沙漠
            ColdDesert,          // 寒冷沙漠
            CoastalDesert,       // 滨海沙漠
            SemiAridShrubland,   // 半干旱灌丛
            SaltDesert,          // 盐碱荒漠
            DesertOasis,         // 沙漠绿洲
            EndorheicLake,       // 内流大湖
            RiverSourceMarsh,    // 河源沼泽
            AlpineMeadow,        // 高山草甸
            TemperateGrassland,  // 温带草原
            GravelGobi,          // 砾质戈壁
            Yardang,             // 风蚀城堡
            LandBridgeIsthmus,    // 陆桥地峡

            // ===== D系：海洋群系与特殊生境 =====
            CoralReef,            // 珊瑚礁
            KelpForest,           // 海带森林
            SeagrassMeadow,       // 海草床
            HydrothermalVent,     // 热液喷口
            AbyssalPlain,         // 深海平原
            OceanicTrench,        // 海沟
            ContinentalShelf,     // 大陆架
            ContinentalSlope,     // 大陆坡
            MidOceanRidge,        // 洋中脊
            SeaMount,             // 海山
            Estuary,              // 河口湾
            Lagoon,               // 潟湖
            TidalFlat,            // 潮滩
            UpwellingZone,        // 上升流区
            PolarSea,             // 极地海域
            SeaIce,               // 浮冰区
            CloudForest,          // 云雾林
            TropicalSeasonalForest, // 热带季雨林
            TemperateMixedForest,   // 温带混交林
            MediterraneanScrub,     // 地中海灌丛
            Parkland,               // 疏林草原
            TundraWetland,          // 苔原湿地
            HotSpringOasis,         // 热泉绿洲
            PermafrostPlateau,      // 永久冻土高原
            Thermokarst,            // 热喀斯特
            BadlandsDesert,         // 恶地荒漠
            ErgSea,                 // 沙海
            CalderaLake,            // 破火山口湖
            GeyserField,            // 间歇泉区
            TowerKarst,             // 峰林
            GlacialValley,          // 冰川谷
            RiftValley,             // 裂谷
            CustomBiome1,           // 自定义群系1
            CustomBiome2,           // 自定义群系2
            CustomBiome3,           // 自定义群系3
            CustomBiome4,           // 自定义群系4
            CustomBiome5            // 自定义群系5
        }

        /// <summary>海洋分级</summary>
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

        /// <summary>政体类型已废弃：政体由 GovernmentComposition 七维成分组合表达，
        /// 粗分类（君主/共和）由 SupremeSuccessionLevel 推导，不再使用单标签枚举。</summary>

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
            Routed,
            Dead // 追加于末尾（枚举序安全：旧存档值 0-5 不变）
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
