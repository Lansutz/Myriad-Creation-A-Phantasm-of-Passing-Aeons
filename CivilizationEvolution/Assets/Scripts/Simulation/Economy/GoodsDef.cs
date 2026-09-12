using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Economy
{
    public struct GoodsDef
    {
        public int goodsId;
        public string goodsName;
        public GameEnums.GoodsCategory category;
        public float baseValue;
        public float weight;
        public bool hasShelfLife;
        public float shelfLifeDays;
        public List<GameEnums.BiomeType> originBiomes;
        public int processedFromId;
        public float processingRatio;

        // ===== 自然资源属性（物产=革新前置条件+地图资源点生成） =====
        /// <summary>是否为自然资源（true=地图上可生成资源点，false=纯加工品）</summary>
        public bool isNaturalResource;

        /// <summary>资源类型（决定生成规则和开发方式）</summary>
        public ResourceType resourceType;

        /// <summary>基础丰度（0-1，影响生成概率和产出上限）</summary>
        public float baseAbundance;

        /// <summary>生成条件（地理阈值，满足才可能在地块上生成）</summary>
        public ResourceSpawnCondition spawnCondition;

        /// <summary>开发该资源所需的前置革新ID（-1=无需技术，直接可采集）</summary>
        public int requiredInnovation;

        /// <summary>是否为可再生资源（动物/植物/森林=true，矿物=false）</summary>
        public bool renewable;
    }
}
