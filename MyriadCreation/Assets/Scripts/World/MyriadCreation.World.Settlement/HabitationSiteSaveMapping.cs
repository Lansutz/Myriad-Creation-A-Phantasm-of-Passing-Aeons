using System;
using MyriadCreation.Core.Enums;
using MyriadCreation.Save.Contracts;

namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// `HabitationSite` / `CampData` 与 `HabitationSiteSaveData` 的映射（**状态所有者侧**）。
    ///
    /// 写成**扩展方法**：调用点保持 `saveData.CaptureModeData(site)` 形式不变，
    /// 只需文件顶部 `using MyriadCreation.World.Settlement;`。
    /// DTO 本体在 `MyriadCreation.Save.Contracts`（只依赖 Core）。
    /// </summary>
    public static class HabitationSiteSaveMapping
    {
        public static void EnsureCanonicalFields(this HabitationSiteSaveData data)
        {
            if (data == null) return;
            data.site_id = HabitationSiteMigration.ResolveSiteId(data.site_id, data.anchor_id);
            if (string.IsNullOrEmpty(data.site_mode))
            {
                data.site_mode = data.legacy_site_category switch
                {
                    0 => SiteModeKeys.SedentarySettlement,
                    1 => SiteModeKeys.FortifiedOutpost,
                    2 => SiteModeKeys.TemporaryEncampment,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(data.legacy_site_category),
                        data.legacy_site_category,
                        "Unknown legacy settlement category.")
                };
            }
            data.location ??= new SiteLocationSaveData { x = data.x, y = data.y };
        }

        public static void CaptureModeData(this HabitationSiteSaveData data, HabitationSite site)
        {
            switch (site.siteMode)
            {
                case SiteMode.SedentarySettlement:
                    data.sedentary_settlement_data = new HabitationSiteSaveData.SedentarySettlementSaveData
                    {
                        settlement_morphology = site.sedentarySettlementData?.settlementMorphology ?? string.Empty,
                        development_stage = site.sedentarySettlementData?.developmentStage ?? 0
                    };
                    break;
                case SiteMode.FortifiedOutpost:
                    data.fortified_outpost_data = new HabitationSiteSaveData.FortifiedOutpostSaveData
                    {
                        purpose = site.fortifiedOutpostData?.purpose ?? string.Empty
                    };
                    break;
                case SiteMode.TemporaryEncampment:
                    data.temporary_encampment_data = new HabitationSiteSaveData.TemporaryEncampmentSaveData
                    {
                        encampment_kind = site.temporaryEncampmentData?.encampmentKind ?? string.Empty
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(site.siteMode), site.siteMode, "Unknown site mode.");
            }

            data.facilities = new FacilitySaveData[site.facilities?.Count ?? 0];
            for (int i = 0; i < data.facilities.Length; i++)
            {
                var facility = site.facilities[i];
                data.facilities[i] = new FacilitySaveData
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

        public static void ApplyModeData(this HabitationSiteSaveData data, HabitationSite site)
        {
            site.InitializeModeData(site.siteMode);
            switch (site.siteMode)
            {
                case SiteMode.SedentarySettlement:
                    site.sedentarySettlementData.settlementMorphology =
                        data.sedentary_settlement_data?.settlement_morphology ?? string.Empty;
                    site.sedentarySettlementData.developmentStage =
                        data.sedentary_settlement_data?.development_stage ?? 0;
                    break;
                case SiteMode.FortifiedOutpost:
                    site.fortifiedOutpostData.purpose = data.fortified_outpost_data?.purpose ?? string.Empty;
                    break;
                case SiteMode.TemporaryEncampment:
                    site.temporaryEncampmentData.encampmentKind =
                        data.temporary_encampment_data?.encampment_kind ?? string.Empty;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(site.siteMode), site.siteMode, "Unknown site mode.");
            }

            site.facilities.Clear();
            if (data.facilities == null)
                return;

            for (int i = 0; i < data.facilities.Length; i++)
            {
                var savedFacility = data.facilities[i];
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

        // CaptureCampData 不在 World 侧：它依赖 Simulation.Actors.CampData，
        // 而 World 不得引用 Simulation。营地的映射在 `Simulation.Actors.CampSaveMapping`。
    }
}
