using UnityEngine;
using System;
using System.Collections.Generic;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.World.Anchor;


namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// 聚居点（Settlement）——通过锚点在游戏世界中实体显现的东西。
    /// 锚点只管空间位置，这里管所有游戏内容：
    /// 聚落性质（定居点/据点/营地，可变）、人口、经济、军事、区划、政治角色、废墟状态。
    /// 一个锚点对应一个聚居点实例，通过 anchorId 关联。
    /// </summary>
    [Serializable]
    public class SettlementData
    {
        /// <summary>关联的锚点ID</summary>
        public int anchorId;

        /// <summary>关联的锚点实例（运行时由 GameWorld 注入）</summary>
        [NonSerialized] public AnchorData anchor;

        // ===== 空间字段（转发到 AnchorData，引用方逐步迁移到直接用 anchor）=====
        public int provinceId => anchor != null ? anchor.provinceId : 0;
        public int tileIndex => anchor != null ? anchor.tileIndex : -1;
        public float x => anchor != null ? anchor.x : 0.5f;
        public float y => anchor != null ? anchor.y : 0.5f;
        public int controllerAnchorId => anchor != null ? anchor.controllerAnchorId : -1;
        public List<int> controlledAnchorIds => anchor != null ? anchor.controlledAnchorIds : new List<int>();
        public Dictionary<int, float> controlProgress => anchor != null ? anchor.controlProgress : new Dictionary<int, float>();
        public int influenceRadius => anchor != null ? anchor.influenceRadius : 1;
        public bool isControlStable => anchor != null && anchor.isControlStable;
        public int bottleneckTileIndex => anchor != null ? anchor.bottleneckTileIndex : -1;

        /// <summary>聚居点名称</summary>
        public string settlementName;

        // ===== 经济与人口 =====
        public float population;
        public float development;    // 发展度（0~100）
        public float wealth;
        public float tradePower;

        // ===== 军事 =====
        public float fortification;   // 防御等级（0~10）
        public int garrison;          // 驻军人数

        // ===== 状态标记 =====
        public bool isCapital;        // 是否政权首都/王庭/王帐
        public bool isPort;
        public bool isCoastal;
        public bool hasMarket;
        public bool hasTemple;
        public bool hasUniversity;

        // ===== 聚落性质：定居点(Burg)/据点(Outpost)/营地(Camp)，可变 =====
        public SettlementCategory settlementCategory;

        // ===== 建设度（所有类型通用，理解为经验值）=====
        public float constructionProgress;  // 0~100
        public int constructionTier;        // 1~5

        // ===== 城市区划（大型聚居点才有）=====
        public List<CityDistrict> districts = new List<CityDistrict>();
        public bool districtsGenerated = false;

        // ===== 经济成分 =====
        public Dictionary<EconomicSector, float> economicComposition = new Dictionary<EconomicSector, float>();
        public EconomicSector primarySector = EconomicSector.None;
        public bool economicInitialized = false;

        // ===== 聚落形态（定居点细分：村镇/城/堡）=====
        public SettlementType settlementType;
        public SettlementLevel settlementLevel;
        public SettlementFunction primaryFunction;
        public SettlementFunction secondaryFunctions;

        // ===== 城市/据点细分 =====
        public CityFocus cityFocus;
        public CityForm cityForm;
        public FortSubtype fortSubtype;
        public PortTier portTier;
        public BottleneckType bottleneckType;
        public UpgradePath upgradePath;
        public EvolutionStage evolutionStage;
        public WallLevel wallLevel;

        // ===== 形态演化 =====
        public float settlementEvolution;
        public float settlementStability;
        [NonSerialized] public SettlementType? evolutionTarget;
        public int ticksSinceLastTransition;
        public int foundingTick;

        /// <summary>母锚点ID（从哪个聚落分化而来，-1=无）</summary>
        public int parentAnchorId = -1;

        // ===== 驻军运行时数据 =====
        [NonSerialized] public float garrisonQuality = 0f;
        [NonSerialized] public int garrisonQuantity = 0;

        // ===== 废墟/摧毁系统 =====
        public int ruinLevel;             // 0=正常，1=轻度劫掠，2=中度破城，3=重度夷平
        public int ruinedDay = -1;
        public SettlementLevel preRuinLevel;
        public SettlementType preRuinType;
        public float recoveryProgress;
        public int remnantCultureId = -1;
        public int remnantFaithId = -1;

        public bool IsRuined => ruinLevel >= 3;

        /// <summary>是否为主要聚居点（城市/要塞/大型镇）</summary>
        public bool IsMajorSettlement =>
            settlementType == SettlementType.City ||
            settlementCategory == SettlementCategory.Outpost;
        public bool IsDamaged => ruinLevel > 0 && ruinLevel < 3;

        /// <summary>形态显示名称</summary>
        public string SettlementTypeName => settlementCategory switch
        {
            SettlementCategory.Outpost => "堡",
            SettlementCategory.Camp => "营",
            SettlementCategory.Burg when settlementType == SettlementType.City => "城",
            SettlementCategory.Burg => "村镇",
            _ => "未知"
        };
    }
}
