using CivilizationEvolution.Map;
using System;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Building
{
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
}
