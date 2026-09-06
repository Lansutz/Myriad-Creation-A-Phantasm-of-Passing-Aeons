using System;
using System.Collections.Generic;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    // ============================================================
    // 建筑可用性条件系统
    // 特定建筑受地形/水文/群系限制，不符合条件的不在UI显示
    // ============================================================

    /// <summary>
    /// 可修建建筑类型（包括堡垒亚型、特殊城形态、港口设施等）
    /// 用于UI过滤和AI建造决策
    /// </summary>
    public enum BuildableType
    {
        // ===== 堡垒亚型（18种）=====
        Barrier,            // 关隘（建在狭窄通道，直接阻挡敌对势力通行）
        PassFort,           // 关口堡垒（建在关隘附近，区域控制，不直接阻挡通行）
        HighlandKeep,       // 高地堡
        ManorFort,          // 坞堡庄园
        PlainGarrison,      // 平原屯堡
        EstuaryFort,        // 河口堡
        StarFort,           // 棱堡
        HillFort,           // 垒寨
        BorderFort,         // 边堡
        CoastalFort,        // 海岸堡垒
        RiverFort,          // 河防堡垒
        MountainFortress,   // 山地要塞
        IslandFort,         // 岛屿要塞
        SiegeCastle,        // 攻城城堡
        RoyalCastle,        // 王城/宫堡
        AbbeyFort,          // 修道院堡垒
        TradingFort,        // 商站堡垒

        // ===== 特殊城形态（需要特定条件）=====
        MountainCity,       // 山城
        WaterCity,          // 水城
        StarCity,           // 星城（棱堡时代）
        CitadelCity,        // 堡城
        PlannedCity,        // 规划城

        // ===== 港口设施 =====
        DeepWaterPort,      // 深水港
        RiverPort,          // 内河港
        ImperialPort,       // 帝国港

        // ===== 特殊设施 =====
        Ferry,              // 渡口
        PostStation,        // 驿站
        Watchtower,         // 烽火台/瞭望塔
        Granary,            // 粮仓
        Market,             // 市集
        Temple,             // 神庙
        University,         // 大学/学府
        Workshop,           // 作坊/工场
        Mine,               // 矿场
        SaltWorks,          // 盐场
        Fishery,            // 渔场
        Shipyard,           // 造船厂
        Barracks,           // 兵营
        Arsenal,            // 军械库
        Aqueduct,           // 引水渠/水道
        Sewer,              // 下水道
        Bathhouse,          // 公共浴场
        Library,            // 图书馆
        Observatory,        // 观星台/天文台
        Lighthouse          // 灯塔
    }

    /// <summary>
    /// 修建条件（地形/水文/群系/海拔/坡度/资源/位置）
    /// 所有条件为AND关系，全部满足才可修建
    /// </summary>
    [Serializable]
    public struct BuildingRequirement
    {
        /// <summary>条件名称（用于UI提示）</summary>
        public string name;

        /// <summary>是否必须是陆地</summary>
        public bool requireLand;

        /// <summary>是否必须沿海</summary>
        public bool requireCoast;

        /// <summary>是否必须有河流</summary>
        public bool requireRiver;

        /// <summary>是否必须是河口（河流+海岸）</summary>
        public bool requireRiverMouth;

        /// <summary>是否必须是山地（海拔>阈值）</summary>
        public bool requireMountain;

        /// <summary>是否必须是丘陵（海拔在阈值之间）</summary>
        public bool requireHills;

        /// <summary>是否必须是平原（海拔<阈值且坡度<阈值）</summary>
        public bool requirePlain;

        /// <summary>是否必须是沙漠/干旱区</summary>
        public bool requireDesert;

        /// <summary>是否必须是岛屿（四周环海）</summary>
        public bool requireIsland;

        /// <summary>是否必须是瓶颈节点（关口/海峡/峡谷）</summary>
        public bool requireBottleneck;

        /// <summary>最小海拔（0-1）</summary>
        public float minElevation;

        /// <summary>最大海拔（0-1）</summary>
        public float maxElevation;

        /// <summary>最小坡度（度）</summary>
        public float minSlope;

        /// <summary>最大坡度（度）</summary>
        public float maxSlope;

        /// <summary>最小年降水（mm）</summary>
        public float minPrecipitation;

        /// <summary>最大年降水（mm）</summary>
        public float maxPrecipitation;

        /// <summary>最小年均温（°C）</summary>
        public float minTemperature;

        /// <summary>最大年均温（°C）</summary>
        public float maxTemperature;

        /// <summary>允许的群系类型（空=全部允许）</summary>
        public GameEnums.BiomeType[] allowedBiomes;

        /// <summary>禁止的群系类型</summary>
        public GameEnums.BiomeType[] forbiddenBiomes;

        /// <summary>允许的海洋等级（用于海洋建筑）</summary>
        public GameEnums.OceanTier[] allowedOceanTiers;

        /// <summary>是否需要已有聚落（升级类建筑）</summary>
        public bool requireExistingSettlement;

        /// <summary>需要的最低聚落等级</summary>
        public SettlementLevel minSettlementLevel;

        /// <summary>需要的最低已有聚落等级（用于升级类建筑，与requireExistingSettlement配合）</summary>
        public SettlementLevel minExistingSettlementLevel;

        /// <summary>需要的最低发展度</summary>
        public float minDevelopment;

        /// <summary>需要的政权科技等级（0-5，对应文明引擎T参数）</summary>
        public int minTechLevel;

        /// <summary>需要的政权类型（空=全部允许）</summary>
        public string[] requiredGovernmentTypes;

        /// <summary>条件描述（用于UI tooltip）</summary>
        public string description;

        /// <summary>创建一个全允许的默认条件</summary>
        public static BuildingRequirement Default()
        {
            return new BuildingRequirement
            {
                name = "默认",
                requireLand = true,
                requireCoast = false,
                requireRiver = false,
                requireRiverMouth = false,
                requireMountain = false,
                requireHills = false,
                requirePlain = false,
                requireDesert = false,
                requireIsland = false,
                requireBottleneck = false,
                minElevation = 0f,
                maxElevation = 1f,
                minSlope = 0f,
                maxSlope = 90f,
                minPrecipitation = 0f,
                maxPrecipitation = 10000f,
                minTemperature = -50f,
                maxTemperature = 60f,
                allowedBiomes = Array.Empty<GameEnums.BiomeType>(),
                forbiddenBiomes = Array.Empty<GameEnums.BiomeType>(),
                allowedOceanTiers = Array.Empty<GameEnums.OceanTier>(),
                requireExistingSettlement = false,
                minSettlementLevel = SettlementLevel.LevelI,
                minExistingSettlementLevel = SettlementLevel.LevelI,
                minDevelopment = 0f,
                minTechLevel = 0,
                requiredGovernmentTypes = Array.Empty<string>(),
                description = ""
            };
        }
    }

    /// <summary>
    /// 建筑可用性检查结果
    /// </summary>
    public struct BuildingAvailability
    {
        public bool available;
        public List<string> failedConditions;
        public string summary;

        public static BuildingAvailability Available()
        {
            return new BuildingAvailability { available = true, failedConditions = new List<string>(), summary = "可修建" };
        }

        public static BuildingAvailability Unavailable(List<string> failed)
        {
            return new BuildingAvailability
            {
                available = false,
                failedConditions = failed,
                summary = $"不可修建（{failed.Count}项条件未满足）"
            };
        }
    }
}
