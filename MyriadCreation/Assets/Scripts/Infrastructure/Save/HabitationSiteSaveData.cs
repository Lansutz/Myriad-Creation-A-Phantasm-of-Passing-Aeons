using System;
using UnityEngine.Serialization;

using MyriadCreation.World.Settlement;

namespace MyriadCreation.Infrastructure.Save
{
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

        [FormerlySerializedAs("anchorId")]
        public int anchor_id;
        [FormerlySerializedAs("settlementName")]
        public string site_name;
        [FormerlySerializedAs("settlementForm")]
        [FormerlySerializedAs("type")]
        public int legacy_settlement_form;
        [FormerlySerializedAs("settlementCategory")]
        public int legacy_site_category;

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

        // ===== 经济成分系统 =====
        public int[] economicSectors;
        public float[] economicRatios;
        public int primarySector;
        public bool economicInitialized;

        // ===== 城市区划系统 =====
        public CityDistrictSaveData[] districts;
        public bool districtsGenerated;

        public void EnsureCanonicalFields()
        {
            site_id = HabitationSiteMigration.ResolveSiteId(site_id, anchor_id);
            if (string.IsNullOrEmpty(site_mode))
            {
                site_mode = legacy_site_category switch
                {
                    0 => SiteModeKeys.SedentarySettlement,
                    1 => SiteModeKeys.FortifiedOutpost,
                    2 => SiteModeKeys.TemporaryEncampment,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(legacy_site_category),
                        legacy_site_category,
                        "Unknown legacy settlement category.")
                };
            }
            location ??= new SiteLocationSaveData { x = x, y = y };
        }

        public void CaptureModeData(HabitationSite site)
        {
            switch (site.siteMode)
            {
                case SiteMode.SedentarySettlement:
                    sedentary_settlement_data = new SedentarySettlementSaveData
                    {
                        settlement_morphology = site.sedentarySettlementData?.settlementMorphology ?? string.Empty,
                        development_stage = site.sedentarySettlementData?.developmentStage ?? 0
                    };
                    break;
                case SiteMode.FortifiedOutpost:
                    fortified_outpost_data = new FortifiedOutpostSaveData
                    {
                        subtype = site.fortifiedOutpostData?.subtype ?? string.Empty,
                        purpose = site.fortifiedOutpostData?.purpose ?? string.Empty
                    };
                    break;
                case SiteMode.TemporaryEncampment:
                    temporary_encampment_data = new TemporaryEncampmentSaveData
                    {
                        encampment_kind = site.temporaryEncampmentData?.encampmentKind ?? string.Empty
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(site.siteMode), site.siteMode, "Unknown site mode.");
            }

            facilities = new FacilitySaveData[site.facilities?.Count ?? 0];
            for (int i = 0; i < facilities.Length; i++)
            {
                var facility = site.facilities[i];
                facilities[i] = new FacilitySaveData
                {
                    facility_id = facility.facilityId,
                    facility_type = FacilityKeys.ToFacilityTypeKey(facility.facilityType),
                    construction_state = FacilityKeys.ToConstructionStateKey(facility.constructionState),
                    damage_state = FacilityKeys.ToDamageStateKey(facility.damageState),
                    material = facility.material,
                    repair_progress = facility.repairProgress,
                    facility_form = new FacilityFormSaveData
                    {
                        wall_shape = facility.facilityForm?.wallShape
                    }
                };
            }
        }

        public void ApplyModeData(HabitationSite site)
        {
            site.InitializeModeData(site.siteMode);
            switch (site.siteMode)
            {
                case SiteMode.SedentarySettlement:
                    site.sedentarySettlementData.settlementMorphology =
                        sedentary_settlement_data?.settlement_morphology ?? string.Empty;
                    site.sedentarySettlementData.developmentStage =
                        sedentary_settlement_data?.development_stage ?? 0;
                    break;
                case SiteMode.FortifiedOutpost:
                    site.fortifiedOutpostData.subtype = fortified_outpost_data?.subtype ?? string.Empty;
                    site.fortifiedOutpostData.purpose = fortified_outpost_data?.purpose ?? string.Empty;
                    break;
                case SiteMode.TemporaryEncampment:
                    site.temporaryEncampmentData.encampmentKind =
                        temporary_encampment_data?.encampment_kind ?? string.Empty;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(site.siteMode), site.siteMode, "Unknown site mode.");
            }

            site.facilities.Clear();
            if (facilities == null)
                return;

            for (int i = 0; i < facilities.Length; i++)
            {
                var savedFacility = facilities[i];
                if (savedFacility == null
                    || !FacilityKeys.TryParseFacilityType(savedFacility.facility_type, out var facilityType)
                    || !FacilityKeys.TryParseConstructionState(savedFacility.construction_state, out var constructionState)
                    || !FacilityKeys.TryParseDamageState(savedFacility.damage_state, out var damageState))
                {
                    throw new FormatException($"Invalid facility data at index {i} for site {site.siteId}.");
                }

                site.facilities.Add(new FacilityData
                {
                    facilityId = savedFacility.facility_id,
                    facilityType = facilityType,
                    constructionState = constructionState,
                    damageState = damageState,
                    material = savedFacility.material ?? string.Empty,
                    repairProgress = savedFacility.repair_progress,
                    facilityForm = new FacilityForm
                    {
                        wallShape = savedFacility.facility_form?.wall_shape ?? string.Empty
                    }
                });
            }
        }

        public void CaptureCampData(MyriadCreation.Simulation.Actors.CampData camp)
        {
            CaptureModeData(camp);
            camp_id = camp.campId;
            camp_type = camp.type.ToString();
            camp_tile_index = camp.tileIndex;
            owner_realm_id = camp.ownerRealmId;
            owner_actor_id = camp.ownerActorId;
            established_day = camp.establishedDay;
            camp_defense = camp.defense;
            camp_supplies = camp.supplies;
            camp_morale = camp.morale;
            camp_permanence = camp.permanence;
            if (camp.attributes == null)
            {
                camp_attribute_keys = null;
                camp_attribute_values = null;
                return;
            }

            var attributes = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, float>>(camp.attributes);
            camp_attribute_keys = new string[attributes.Count];
            camp_attribute_values = new float[attributes.Count];
            for (int i = 0; i < attributes.Count; i++)
            {
                camp_attribute_keys[i] = attributes[i].Key;
                camp_attribute_values[i] = attributes[i].Value;
            }
        }

        [Serializable]
        public class SedentarySettlementSaveData
        {
            public string settlement_morphology;
            public int development_stage;
        }

        [Serializable]
        public class FortifiedOutpostSaveData
        {
            public string subtype;
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

    /// <summary>
    /// 城市区划保存数据
    /// </summary>
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
