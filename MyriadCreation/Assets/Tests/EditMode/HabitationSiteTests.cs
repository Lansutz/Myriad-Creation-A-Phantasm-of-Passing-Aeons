using System.Collections.Generic;
using NUnit.Framework;

using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Save.Contracts;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.World.Settlement;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Tests.EditMode
{
    // T013 模式转换规则
    public class SiteTransitionTests
    {
        [TestCase(SiteMode.SedentarySettlement, "sedentary_settlement")]
        [TestCase(SiteMode.FortifiedOutpost, "fortified_outpost")]
        [TestCase(SiteMode.TemporaryEncampment, "temporary_encampment")]
        public void SiteModeKeys_UseCanonicalKeys(SiteMode mode, string key)
        {
            Assert.AreEqual(key, SiteModeKeys.ToKey(mode));
            Assert.IsTrue(SiteModeKeys.TryParse(key, out var parsed));
            Assert.AreEqual(mode, parsed);
        }

        [TestCase(SiteMode.TemporaryEncampment, SiteMode.FortifiedOutpost)]
        [TestCase(SiteMode.TemporaryEncampment, SiteMode.SedentarySettlement)]
        [TestCase(SiteMode.FortifiedOutpost, SiteMode.SedentarySettlement)]
        public void TryTransition_AllowsUpwardPathsAndPreservesSiteId(SiteMode source, SiteMode target)
        {
            var site = new HabitationSite(source);
            var siteId = site.siteId;

            Assert.IsTrue(HabitationSiteTransitionSystem.TryTransition(site, target, out var failure));

            Assert.AreEqual(SiteTransitionFailure.None, failure);
            Assert.AreEqual(target, site.siteMode);
            Assert.AreEqual(siteId, site.siteId);
            Assert.AreEqual(source, site.previousSiteMode);
        }

        [TestCase(SiteMode.FortifiedOutpost, SiteMode.TemporaryEncampment)]
        [TestCase(SiteMode.SedentarySettlement, SiteMode.FortifiedOutpost)]
        [TestCase(SiteMode.SedentarySettlement, SiteMode.TemporaryEncampment)]
        public void TryTransition_RejectsDirectDownwardPaths(SiteMode source, SiteMode target)
        {
            var site = new HabitationSite(source);
            var siteId = site.siteId;

            Assert.IsFalse(HabitationSiteTransitionSystem.TryTransition(site, target, out var failure));

            Assert.AreEqual(SiteTransitionFailure.ForbiddenTransition, failure);
            Assert.AreEqual(source, site.siteMode);
            Assert.AreEqual(siteId, site.siteId);
            Assert.IsFalse(site.hasPreviousSiteMode);
        }

        [TestCase(SiteMode.SedentarySettlement, SiteMode.TemporaryEncampment)]
        [TestCase(SiteMode.SedentarySettlement, SiteMode.FortifiedOutpost)]
        [TestCase(SiteMode.FortifiedOutpost, SiteMode.TemporaryEncampment)]
        public void TryDowngradeDueToDestruction_AllowsExplicitDownwardPaths(SiteMode source, SiteMode target)
        {
            var site = new HabitationSite(source);
            var siteId = site.siteId;

            Assert.IsTrue(HabitationSiteTransitionSystem.TryDowngradeDueToDestruction(
                site, target, out var failure));

            Assert.AreEqual(SiteTransitionFailure.None, failure);
            Assert.AreEqual(target, site.siteMode);
            Assert.AreEqual(siteId, site.siteId);
        }

        [Test]
        public void SedentarySettlement_CannotBeDeletedOrDissolved()
        {
            var settlement = new HabitationSite(SiteMode.SedentarySettlement);

            Assert.IsFalse(HabitationSiteTransitionSystem.CanDelete(settlement));
            Assert.IsFalse(HabitationSiteTransitionSystem.CanDissolve(settlement));
        }

        [Test]
        public void OnlyEncampmentsCanBeDissolved()
        {
            Assert.IsTrue(HabitationSiteTransitionSystem.CanDissolve(
                new HabitationSite(SiteMode.TemporaryEncampment)));
            Assert.IsTrue(HabitationSiteTransitionSystem.CanDelete(
                new HabitationSite(SiteMode.TemporaryEncampment)));
            Assert.IsFalse(HabitationSiteTransitionSystem.CanDissolve(
                new HabitationSite(SiteMode.FortifiedOutpost)));
            Assert.IsFalse(HabitationSiteTransitionSystem.CanDelete(
                new HabitationSite(SiteMode.FortifiedOutpost)));
            Assert.IsFalse(HabitationSiteTransitionSystem.CanDissolve(null));
        }
    }

    // T014 设施形制与损毁
    public class FacilityDamageTests
    {
        private static SettlementData CreateSettlementWith(params FacilityType[] types)
        {
            var settlement = new SettlementData
            {
                siteMode = SiteMode.SedentarySettlement
            };
            settlement.facilities = new List<FacilityData>();
            foreach (var type in types)
            {
                settlement.facilities.Add(new FacilityData
                {
                    facilityType = type,
                    constructionState = ConstructionState.Complete,
                    damageState = FacilityDamageState.Intact
                });
            }
            return settlement;
        }

        [Test]
        public void WallShapeBelongsOnlyToFacilityForm()
        {
            var facility = new FacilityData
            {
                facilityType = FacilityType.Wall,
                facilityForm = new FacilityForm { wallShape = "concentric" }
            };

            Assert.AreEqual("concentric", facility.facilityForm.wallShape);
        }

        [Test]
        public void Raid_DamagesFacilitiesWithoutChangingSiteMode()
        {
            var settlement = CreateSettlementWith(FacilityType.Wall, FacilityType.Gate);

            SettlementDestructionSystem.ApplyFacilityDamage(settlement, DestructionSeverity.Raid);

            Assert.AreEqual(SiteMode.SedentarySettlement, settlement.siteMode);
            foreach (var facility in settlement.facilities)
                Assert.AreEqual(FacilityDamageState.Damaged, facility.damageState);
        }

        [Test]
        public void Storm_DisablesWallsAndGatesWithoutChangingSiteMode()
        {
            var settlement = CreateSettlementWith(FacilityType.Wall, FacilityType.Palisade);

            SettlementDestructionSystem.ApplyFacilityDamage(settlement, DestructionSeverity.Storm);

            Assert.AreEqual(SiteMode.SedentarySettlement, settlement.siteMode);
            Assert.AreEqual(FacilityDamageState.Disabled, settlement.facilities[0].damageState);
            Assert.AreEqual(FacilityDamageState.Damaged, settlement.facilities[1].damageState);
        }

        [Test]
        public void Raze_DestroysFacilitiesButDoesNotChangeSiteMode()
        {
            var settlement = CreateSettlementWith(FacilityType.Wall, FacilityType.Gate);

            SettlementDestructionSystem.ApplyFacilityDamage(settlement, DestructionSeverity.Raze);

            Assert.AreEqual(SiteMode.SedentarySettlement, settlement.siteMode);
            foreach (var facility in settlement.facilities)
            {
                Assert.AreEqual(FacilityDamageState.Destroyed, facility.damageState);
                Assert.AreEqual(ConstructionState.Ruined, facility.constructionState);
            }
        }

        [Test]
        public void Repair_RestoresDamagedFacilityWhenProgressCompletes()
        {
            var settlement = CreateSettlementWith(FacilityType.Palisade);
            SettlementDestructionSystem.ApplyFacilityDamage(settlement, DestructionSeverity.Raid);
            var facility = settlement.facilities[0];
            Assert.AreEqual(FacilityDamageState.Damaged, facility.damageState);

            for (var i = 0; i < 200; i++)
                SettlementDestructionSystem.RepairFacilities(settlement);

            Assert.AreEqual(FacilityDamageState.Intact, facility.damageState);
            Assert.AreEqual(0f, facility.repairProgress);
        }
    }

    // T015 存档映射
    public class HabitationSiteSaveMappingTests
    {
        [Test]
        public void SaveData_UsesCanonicalCollectionAndSiteKeys()
        {
            var save = new MapSaveData
            {
                habitation_sites = new[]
                {
                    new HabitationSiteSaveData
                    {
                        site_id = "00000000-0000-0000-0000-000000000042",
                        site_mode = SiteModeKeys.TemporaryEncampment,
                        site_name = "Camp",
                        location = new SiteLocationSaveData { x = 0.25f, y = 0.75f }
                    }
                }
            };

            var json = UnityEngine.JsonUtility.ToJson(save);
            StringAssert.Contains("\"habitation_sites\"", json);
            StringAssert.Contains("\"site_id\"", json);
            StringAssert.Contains("\"site_mode\":\"temporary_encampment\"", json);
            StringAssert.Contains("\"location\"", json);
            StringAssert.DoesNotContain("\"settlements\"", json);
        }

        [Test]
        public void LegacySaveData_GeneratesStableSiteIdAndExplicitMode()
        {
            var legacyData = new HabitationSiteSaveData
            {
                anchor_id = 42,
                legacy_site_category = (int)SettlementCategory.Camp
            };

            legacyData.EnsureCanonicalFields();
            var stableId = legacyData.site_id;
            legacyData.EnsureCanonicalFields();

            Assert.AreEqual("0000002a-0000-0000-0000-000000000000", stableId);
            Assert.AreEqual(stableId, legacyData.site_id);
            Assert.AreEqual(SiteModeKeys.TemporaryEncampment, legacyData.site_mode);
        }

        [Test]
        public void LegacySaveJson_MapsOldCollectionAndCategoryKeys()
        {
            const string legacyJson =
                "{\"settlements\":[{\"anchorId\":42,\"settlementName\":\"Old camp\",\"settlementCategory\":2,\"x\":0.2,\"y\":0.3}]}";

            // JsonUtility 不认 [FormerlySerializedAs]（实测旧键不会映射），
            // 所以旧档必须走读档入口 SaveJsonMigration，而不是裸 FromJson。
            var save = SaveJsonMigration.FromMapSaveJson(legacyJson);
            Assert.IsNotNull(save, "存档解析");
            Assert.IsNotNull(save.habitation_sites);
            Assert.AreEqual(1, save.habitation_sites.Length);

            var site = save.habitation_sites[0];
            site.EnsureCanonicalFields();
            Assert.AreEqual("Old camp", site.site_name);
            Assert.AreEqual(SiteModeKeys.TemporaryEncampment, site.site_mode);
            Assert.AreEqual("0000002a-0000-0000-0000-000000000000", site.site_id);
            Assert.AreEqual(0.2f, site.location.x);
            Assert.AreEqual(0.3f, site.location.y);
        }

        [Test]
        public void FacilitySaveData_RoundTripsRepairProgress()
        {
            var site = new HabitationSite(SiteMode.SedentarySettlement);
            site.facilities.Add(new FacilityData
            {
                facilityType = FacilityType.Wall,
                constructionState = ConstructionState.Complete,
                damageState = FacilityDamageState.Damaged,
                repairProgress = 40f
            });

            var saveData = new HabitationSiteSaveData();
            saveData.CaptureModeData(site);
            Assert.AreEqual(40f, saveData.facilities[0].repair_progress);

            var restored = new HabitationSite(SiteMode.SedentarySettlement);
            saveData.ApplyModeData(restored);
            Assert.AreEqual(40f, restored.facilities[0].repairProgress);
        }
    }
}
