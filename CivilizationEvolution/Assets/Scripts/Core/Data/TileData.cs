using System;
using System.Collections.Generic;
using UnityEngine;

using CivilizationEvolution.Core.Enums;
namespace CivilizationEvolution.Core.Data
{
 /// 地块完整数据结构
 /// 所有模拟数据下沉到地块级，行省仅为逻辑分组
    [System.Serializable]
    public struct TileData
    {
 // ===== 基础标识 =====
        public int tileIndex;
        public int regionId;
 /// <summary>省份归属（沃罗诺伊省区——ProvinceGenerator 生成时赋值并重置，-1=未归属）</summary>
        public int provinceId;
        public int ownerRealmId;
        public int occupyingRealmId;

 /// <summary>河流标记（TerrainGenerator 河流追踪后赋值）</summary>
        public bool isRiver;

 /// <summary>地块是否存在（支持任意形状地图，false=虚空/地图外）</summary>
        public bool exists;

 /// <summary>地形锁定（参考Azgaar FMG特征锁定机制：锁定后程序化生成不覆盖此地块的高程/海陆）</summary>
        public bool locked;

 // ===== 地形 =====
        public float elevation01;
        public float slopeDegree;
        public float terrainShade;
        public bool isGate;
        public GameEnums.RoadLevel roadLevel;

 // ===== 通行系统（军事地理）===== /// <summary>是否可通行（根据坡度计算，>45°为不可通行地区）</summary>
        public bool passable;

 /// <summary>基础通行成本（根据地形/坡度/道路计算，1.0=平原正常道路）</summary>
        public float movementCost;

 /// <summary>是否有关隘封锁（关隘可直接阻挡敌对势力通行）</summary>
        public bool hasBarrier;

 /// <summary>关隘所属政权ID（-1=无）</summary>
        public int barrierOwnerRealmId;

 /// <summary>关隘强度（0-10，影响攻破难度和防御加成）</summary>
        public float barrierStrength;

 /// <summary>附近堡垒ID（-1=无，用于堡垒区域影响计算）</summary>
        public int nearbyFortId;

 /// <summary>堡垒影响等级（0=无，1-3=影响强度，决定损耗/补给加成幅度）</summary>
        public int fortInfluenceLevel;

 // ===== 海陆属性 =====
        public bool isLand;
        public bool isCoast;

        /// <summary>是否为海洋（非陆地），Pathfinding 统一用此名</summary>
        public bool isOcean => !isLand;

        /// <summary>是否为海岸地块（isCoast 的语义化别名）</summary>
        public bool isCoastal => isCoast;
        public GameEnums.OceanTier oceanTier;
        public float oceanDepth01;
        public int seaConnectId;
        public float waterAdjacentWeight;

 // ===== 气候 =====
        public float annualTemp;
        public float diurnalTempRange;
        public float annualPrecipMm;
        public float airHumidityPct;
        public float soilHumidityPct;
        public float accumulatedTemp;
        public float frostFreeDays;
        public GameEnums.ClimateZone climateZone;
        public GameEnums.BiomeType biome;

 // ===== 经济 =====
        public float fertility;
        public float development;
        public float stability;
        public float order;
        public int[] buildingLevels;

 // ===== 人口 =====
        public List<PopulationBlock> populationBlocks;

 // ===== 资源点 =====
        public List<TileResource> resources;

 // ===== 营寨（军队/盗匪/蛮族的临时据点） =====
        public int campId;

 // ===== 脏标记 =====
        public bool isTerrainDirty;
        public bool isClimateDirty;
        public bool isEconomyDirty;
    }

 /// 人口块：50自然人为一个块，浮点精细存储

 /// 地块网格坐标辅助
 /// 统一处理坐标转换、邻接计算、环绕（左右连通）
 /// even-r偏移六边形坐标

}
