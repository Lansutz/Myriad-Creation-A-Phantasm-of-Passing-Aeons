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

        /// <summary>通行管制等级（外交联动，严格管制需军事通行权）</summary>
        public enum MovementControlLevel
        {
            None,           // 无管制：军队可自由通过
            Loose,          // 松散管制：军队可通过，但有关税/检查，速度略降
            Limited,        // 有限管制：军队可通过，但需登记，速度明显下降，可能被监视
            Strict          // 严格管制：军队必须请求军事通行权，否则不可通过
        }


        /// <summary>冲突等级（区分敌对状态和战争状态）</summary>
        public enum ConflictLevel
        {
            Peace,          // 和平：正常外交
            Tension,        // 紧张：有摩擦，无直接冲突
            Hostility,      // 敌对：可进行低烈度冲突（劫掠/边境摩擦）
            LimitedWar,     // 有限战争：局部战争，不全面动员
            TotalWar        // 全面战争：正式宣战，全面战争
        }

        /// <summary>劫掠类型（敌对状态下的低烈度行动）</summary>
        public enum RaidType
        {
            BorderSkirmish, // 边境摩擦：小规模冲突，人员伤亡小
            VillageRaid,    // 劫掠村镇：掠夺物资，破坏建筑，可能俘虏人口
            TownAttack,     // 攻击城镇：攻击小型城镇，掠夺更多物资，可能触发宣战
            SupplyRaiding,  // 补给劫掠：攻击商队/补给线，不直接攻击聚落
            SlaveRaiding    // 掠奴：专门掠夺人口为奴
        }

        /// <summary>战争借口类型（Casus Belli——为什么开战）</summary>
        public enum CasusBelliType
        {
            None,                   // 无借口（不宣而战，高惩罚）
            RaidReprisal,           // 劫掠报复（对方劫掠了己方村镇）
            BorderIncident,          // 边境事件（边境摩擦升级）
            TerritorialDispute,      // 领土争端（对争议领土有宣称）
            ReligiousConflict,       // 宗教冲突（异端/异教徒/圣地）
            AllianceObligation,      // 联盟义务（防御同盟被攻击）
            HegemonyExpansion,       // 霸权扩张（实力差距大，主动扩张）
            DynasticClaim,           // 王朝宣称（继承/联姻宣称）
            TradeDispute,             // 贸易争端（商队被劫/贸易壁垒）
            IndependenceWar,          // 独立战争（附庸/被压迫者独立）
            Reconquest,               // 收复失地（曾经拥有的领土）
            Crusade,                  // 圣战（宗教号召的大规模战争）
            ImperialConquest,         // 帝国征服（建立帝国的征服战争）
            CivilWar,                 // 内战（继承争端/叛乱）
            Intervention              // 武装干涉（支持一方势力）
        }

        /// <summary>战争目标类型（War Goal——开战想要达到什么目的）</summary>
        public enum WarGoalType
        {
            None,                     // 无明确目标（纯粹破坏/劫掠）
            ConquerTerritory,         // 夺取领土（指定地块/省份）
            ConquerRegion,            // 夺取地区（整个地区）
            Vassalization,            // 迫使附庸（对方成为附庸）
            PersonalUnion,            // 共主邦联（王朝联合）
            Indemnity,                // 索取赔款（战争赔款）
            ReleaseVassal,            // 释放附庸（迫使对方释放附庸）
            ConvertReligion,          // 迫使改宗（对方改信己方宗教）
            EnforceTradeRights,       // 强制贸易权（获得贸易特权）
            Disarmament,              // 裁军（迫使对方裁减军备）
            Humiliation,              // 羞辱（降低对方威望/稳定度）
            Annihilation,             // 灭国（彻底摧毁对方政权）
            BorderAdjustment,         // 边境调整（小规模领土变更）
            Independence,              // 独立（从宗主国独立）
            InstallRuler               // 扶植统治者（更换对方统治者）
        }

        /// <summary>和平条约条款类型（Peace Treaty Clause——实际得到什么）</summary>
        public enum TreatyClauseType
        {
            TerritoryCession,         // 领土割让
            WarReparations,           // 战争赔款
            Vassalage,                // 附庸关系
            PersonalUnion,            // 共主邦联
            ReleasePrisoners,         // 释放囚犯
            ReligiousFreedom,         // 宗教自由
            TradePrivileges,          // 贸易特权
            Disarmament,              // 裁军条款
            Humiliation,              // 羞辱条款
            Annexation,               // 吞并（整个政权）
            Independence,             // 承认独立
            BorderDemilitarization,   // 边境非军事化
            AllianceObligation,       // 同盟义务（战败方加入战胜方同盟）
            RoyalMarriage,            // 强制联姻
            CulturalAssimilation,     // 文化同化（战败方地区文化转变）
            WarCrimesTrial,           // 战争罪审判
            ResourceConcession,       // 资源特许权（矿山/港口）
            Truce                      // 停战协定（强制休战N年）
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