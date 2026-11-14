using System;

namespace MyriadCreation.Save.Contracts
{
    /// <summary>
    /// 栖居点存档 DTO（**纯字段**；含三种 site mode 的分支数据与设施/区块/市场引用）。
    ///
    /// 映射（`EnsureCanonicalFields` / `CaptureModeData` / `ApplyModeData` / `CaptureCampData`）
    /// 在 `MyriadCreation.World.Settlement.HabitationSiteSaveMapping`（扩展方法，调用点不变）。
    /// </summary>
    [Serializable]
    public class HabitationSiteSaveData
    {
        public string site_id;
        public string site_mode;
        public string site_class;
        public string control_faction;
        public SiteLocationSaveData location;
        public SedentarySettlementSaveData sedentary_settlement_data;
        public FortifiedOutpostSaveData fortified_outpost_data;
        public TemporaryEncampmentSaveData temporary_encampment_data;
        public FacilitySaveData[] facilities;
        public int camp_id;
        public string camp_type;
        public int camp_tile_index;
        public int owner_realm_id;
        public int owner_actor_id;
        public int established_day;
        public float camp_defense;
        public float camp_supplies;
        public float camp_morale;
        public float camp_permanence;
        public string[] camp_attribute_keys;
        public float[] camp_attribute_values;

        // ===== 兼容旧档字段 =====
        public int anchor_id;
        public string site_name;
        public int legacy_settlement_form;
        public int legacy_settlement_level = -1;
        public int legacy_site_category;

        // ===== 环境衰退（P10.2）=====
        public int environmentalPressureDays;
        public string lastDeclineCause = string.Empty;

        // ===== 聚落基础字段 =====
        public int provinceId;
        public int tileIndex;
        public float x;
        public float y;
        public float population;
        public float development;
        public float wealth;
        public float tradePower;
        public float fortification;
        public int garrison;
        public bool isCapital;
        public bool isPort;
        public bool isCoastal;
        public bool hasMarket;
        public bool hasTemple;
        public bool hasUniversity;
        public float constructionProgress;
        public int constructionTier;
        /// <summary>聚落废弃压力累计天数（P1.1）；旧档缺字段为 0。</summary>
        public int abandonmentPressureDays;
        public int occupationPressureDays;

        // ===== 经济 =====
        public int[] economicSectors;
        public float[] economicRatios;
        public int primarySector;
        public bool economicInitialized;

        // ===== 城市区划 =====
        public CityDistrictSaveData[] districts;
        public bool districtsGenerated;

        // ===== 区块树与市场 =====
        public SettlementBlockSaveData[] blocks;
        public SettlementMarketSaveData market;

        [Serializable]
        public class SedentarySettlementSaveData
        {
            public string settlement_morphology;
            public int development_stage;
        }

        [Serializable]
        public class FortifiedOutpostSaveData
        {
            public string purpose;
        }

        [Serializable]
        public class TemporaryEncampmentSaveData
        {
            public string encampment_kind;
        }
    }

    [Serializable]
    public class FacilitySaveData
    {
        public string facility_id;
        public string facility_type;
        public string construction_state;
        public string damage_state;
        public string material;
        public float repair_progress;
        public FacilityFormSaveData facility_form;
    }

    [Serializable]
    public class FacilityFormSaveData
    {
        public string wall_shape;
    }

    [Serializable]
    public class SiteLocationSaveData
    {
        public float x;
        public float y;
    }

    /// <summary>城市区划保存数据</summary>
    [Serializable]
    public class CityDistrictSaveData
    {
        public int districtType;
        public string districtName;
        public float areaRatio;
        public float populationRatio;
        public float development;
        public float wealth;
        public float foundedDate;
    }
}
