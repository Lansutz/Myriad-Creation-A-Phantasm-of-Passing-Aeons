using UnityEngine;
using System;
using System.Collections.Generic;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.World.Anchor;
using UnityEngine.Serialization;


namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// 迁移期兼容数据类型：继承 HabitationSite，旧系统仍可通过 SettlementData 字段访问既有数据。
    /// Anchor 保留为空间定位引用，不再作为栖居点的业务父概念。
    /// </summary>
    [Serializable]
    public class SettlementData : HabitationSite, ISerializationCallbackReceiver
    {
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
        [SerializeField, FormerlySerializedAs("settlementName")]
        private string _legacySettlementName;

        public string settlementName
        {
            get => siteName;
            set => siteName = value;
        }

        // ===== 经济与人口 =====
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

        // ===== 聚落性质：定居点(Settlement)/据点(Outpost)/营地(Camp)，可变 =====
        [SerializeField, FormerlySerializedAs("settlementCategory")]
        private SettlementCategory _legacySettlementCategory;

        public SettlementCategory settlementCategory
        {
            get => _legacySettlementCategory;
            set
            {
                _legacySettlementCategory = value;
                siteMode = ToSiteMode(value);
                InitializeModeData(siteMode);
            }
        }

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

        // ===== 永久定居点形态（村镇/城市；据点形态由专门字段描述）=====
        [SerializeField, FormerlySerializedAs("settlementForm")]
        private SettlementForm _legacySettlementForm;
        [SerializeField, FormerlySerializedAs("settlementLevel")]
        private SettlementLevel _legacySettlementLevel;

        public SettlementForm settlementForm
        {
            get => _legacySettlementForm;
            set
            {
                _legacySettlementForm = value;
                if (siteMode == SiteMode.SedentarySettlement)
                    sedentarySettlementData.settlementMorphology = value == SettlementForm.Village ? "village" : "city";
            }
        }

        public SettlementLevel settlementLevel
        {
            get => _legacySettlementLevel;
            set
            {
                _legacySettlementLevel = value;
                if (siteMode == SiteMode.SedentarySettlement)
                    sedentarySettlementData.developmentStage = (int)value;
            }
        }
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
        [NonSerialized] public SettlementForm? evolutionTarget;
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
        public SettlementForm preRuinForm;
        public float recoveryProgress;
        public int remnantCultureId = -1;
        public int remnantFaithId = -1;

        public bool IsRuined => ruinLevel >= 3;

        /// <summary>是否为主要聚居点（城市/要塞/大型镇）</summary>
        public bool IsMajorSettlement =>
            settlementForm == SettlementForm.City ||
            settlementCategory == SettlementCategory.Outpost;
        public bool IsDamaged => ruinLevel > 0 && ruinLevel < 3;

        /// <summary>形态显示名称</summary>
        public string SettlementFormName => settlementForm switch
        {
            SettlementForm.Village => "村镇",
            SettlementForm.City => "城",
            _ => "未知"
        };

        public void OnBeforeSerialize()
        {
            _legacySettlementName = siteName;
            if (anchor != null)
            {
                location.x = anchor.x;
                location.y = anchor.y;
            }
            _legacySettlementCategory = ToLegacyCategory(siteMode);
            if (siteMode == SiteMode.SedentarySettlement && sedentarySettlementData != null)
            {
                _legacySettlementLevel = (SettlementLevel)sedentarySettlementData.developmentStage;
                _legacySettlementForm = sedentarySettlementData.settlementMorphology == "village"
                    ? SettlementForm.Village
                    : SettlementForm.City;
            }
            else if (siteMode == SiteMode.FortifiedOutpost && fortifiedOutpostData != null)
            {
                if (Enum.TryParse(fortifiedOutpostData.subtype, out FortSubtype legacySubtype))
                    fortSubtype = legacySubtype;
                if (Enum.TryParse(fortifiedOutpostData.purpose, out SettlementFunction legacyPurpose))
                    primaryFunction = legacyPurpose;
            }
        }

        public void OnAfterDeserialize()
        {
            siteName = _legacySettlementName ?? string.Empty;
            siteMode = ToSiteMode(_legacySettlementCategory);
            InitializeModeData(siteMode);
            if (siteMode == SiteMode.SedentarySettlement)
            {
                sedentarySettlementData.settlementMorphology =
                    _legacySettlementForm == SettlementForm.Village ? "village" : "city";
                sedentarySettlementData.developmentStage = (int)_legacySettlementLevel;
            }
            else if (siteMode == SiteMode.FortifiedOutpost)
            {
                fortifiedOutpostData.subtype = fortSubtype.ToString();
                fortifiedOutpostData.purpose = primaryFunction.ToString();
            }
        }

        public void SetLegacyCategoryFromSiteMode()
        {
            _legacySettlementCategory = ToLegacyCategory(siteMode);
        }

        private static SiteMode ToSiteMode(SettlementCategory category)
        {
            switch (category)
            {
                case SettlementCategory.PermanentSettlement:
                    return SiteMode.SedentarySettlement;
                case SettlementCategory.Outpost:
                    return SiteMode.FortifiedOutpost;
                case SettlementCategory.Camp:
                    return SiteMode.TemporaryEncampment;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown legacy settlement category.");
            }
        }

        private static SettlementCategory ToLegacyCategory(SiteMode mode)
        {
            switch (mode)
            {
                case SiteMode.SedentarySettlement:
                    return SettlementCategory.PermanentSettlement;
                case SiteMode.FortifiedOutpost:
                    return SettlementCategory.Outpost;
                case SiteMode.TemporaryEncampment:
                    return SettlementCategory.Camp;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown site mode.");
            }
        }
    }
}
