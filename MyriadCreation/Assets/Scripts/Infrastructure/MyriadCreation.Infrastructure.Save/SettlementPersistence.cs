using System;
using System.Collections.Generic;
using System.Linq;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Settlement;
using MyriadCreation.Save.Contracts;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Infrastructure.Save
{
    /// <summary>
    /// 省份 / 聚落 / 营地的入档与还原（SaveVersion 12）。
    ///
    /// 为什么单独一层：SaveSystem v2（JsonUtility + DTO）此前只存地块、政权、角色等，
    /// 读档后 <c>world.provinces</c> / <c>world.anchors</c> / <c>world.settlements</c> / 营地全是空的，
    /// 城墙、建筑、市场库存、城市区划、营地补给都会蒸发。老 GameSaveSystem 有这段逻辑，
    /// 但它是 Obsolete 且未接入游戏循环，所以把这段抽成共享实现给 SaveSystem 用。
    ///
    /// 还原一律**就地清空再填充**现有字典：子系统（SettlementSimulationSystem 等）在
    /// InitializeSubsystems 时已捕获这些字典实例，换新实例会让系统指向旧容器。
    /// </summary>
    public static class SettlementPersistence
    {
        // ===== 省份 =====

        public static List<ProvinceSaveData> CaptureProvinces(GameWorld world)
        {
            if (world?.provinces == null || world.provinces.Count == 0) return new List<ProvinceSaveData>();

            var result = new List<ProvinceSaveData>(world.provinces.Count);
            foreach (var province in world.provinces.Values)
            {
                if (province == null) continue;
                result.Add(new ProvinceSaveData
                {
                    provinceId = province.provinceId,
                    provinceName = province.provinceName,
                    centerTileIndex = province.centerTileIndex,
                    memberTiles = province.memberTiles?.ToArray() ?? new int[0]
                });
            }

            return result;
        }

        public static void RestoreProvinces(GameWorld world, List<ProvinceSaveData> saved)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.provinces ??= new Dictionary<int, Province>();
            world.provinces.Clear();
            if (saved == null) return;

            foreach (var entry in saved)
            {
                if (entry == null) continue;
                world.provinces[entry.provinceId] = new Province
                {
                    provinceId = entry.provinceId,
                    provinceName = entry.provinceName,
                    centerTileIndex = entry.centerTileIndex,
                    memberTiles = new List<int>(entry.memberTiles ?? new int[0])
                };
            }
        }

        // ===== 聚落 + 营地 =====

        public static List<HabitationSiteSaveData> CaptureSites(GameWorld world)
        {
            var result = new List<HabitationSiteSaveData>();
            if (world == null) return result;

            if (world.settlements != null)
            {
                foreach (var settlement in world.settlements.Values)
                {
                    if (settlement == null) continue;
                    // provinceId/tileIndex/x/y 在 SettlementData 上是委托给 anchor 的属性，
                    // 读档前/测试用例里 anchor 可能为空，直接用字段并回退锚点表。
                    var anchor = settlement.anchor
                        ?? (world.anchors != null && world.anchors.TryGetValue(settlement.anchorId, out var knownAnchor) ? knownAnchor : null);
                    var saved = new HabitationSiteSaveData
                    {
                        site_id = settlement.siteId,
                        site_mode = SiteModeKeys.ToKey(settlement.siteMode),
                        site_class = settlement.siteClass,
                        control_faction = settlement.controlFaction,
                        location = new SiteLocationSaveData { x = settlement.location.x, y = settlement.location.y },
                        anchor_id = settlement.anchorId,
                        site_name = settlement.settlementName,
                        legacy_settlement_form = (int)settlement.settlementForm,
                        legacy_site_category = (int)settlement.settlementCategory,
                        provinceId = anchor?.provinceId ?? -1,
                        tileIndex = anchor?.tileIndex ?? -1,
                        x = settlement.location.x,
                        y = settlement.location.y,
                        population = settlement.population,
                        development = settlement.development,
                        wealth = settlement.wealth,
                        tradePower = settlement.tradePower,
                        fortification = settlement.fortification,
                        garrison = settlement.garrison,
                        isCapital = settlement.isCapital,
                        isPort = settlement.isPort,
                        isCoastal = settlement.isCoastal,
                        hasMarket = settlement.hasMarket,
                        hasTemple = settlement.hasTemple,
                        hasUniversity = settlement.hasUniversity,
                        constructionProgress = settlement.constructionProgress,
                        constructionTier = settlement.constructionTier,
                        primarySector = (int)settlement.primarySector,
                        economicInitialized = settlement.economicInitialized,
                        economicSectors = settlement.economicComposition?.Keys.Select(k => (int)k).ToArray(),
                        economicRatios = settlement.economicComposition?.Values.ToArray(),
                        districtsGenerated = settlement.districtsGenerated,
                        districts = settlement.districts?.Select(d => new CityDistrictSaveData
                        {
                            districtType = (int)d.districtType,
                            districtName = d.districtName,
                            areaRatio = d.areaRatio,
                            populationRatio = d.populationRatio,
                            development = d.development,
                            wealth = d.wealth,
                            foundedDate = d.foundedDate
                        }).ToArray(),
                        blocks = SettlementBlockSaveMapping.CaptureAll(settlement.blocks),
                        market = SettlementMarketSaveMapping.Capture(settlement.market)
                    };
                    saved.CaptureModeData(settlement);
                    result.Add(saved);
                }
            }

            var camps = world.Camps;
            if (camps != null)
            {
                foreach (var camp in camps.AllCamps)
                {
                    if (camp == null) continue;
                    var saved = new HabitationSiteSaveData
                    {
                        site_id = camp.siteId,
                        site_mode = SiteModeKeys.ToKey(camp.siteMode),
                        site_class = camp.siteClass,
                        control_faction = camp.controlFaction,
                        location = new SiteLocationSaveData { x = camp.location.x, y = camp.location.y },
                        anchor_id = camp.anchorId,
                        site_name = camp.siteName,
                        provinceId = world.tiles != null && camp.tileIndex >= 0 && camp.tileIndex < world.tiles.Length
                            ? world.tiles[camp.tileIndex].provinceId
                            : -1,
                        tileIndex = camp.tileIndex,
                        x = camp.location.x,
                        y = camp.location.y,
                        population = camp.population
                    };
                    saved.CaptureCampData(camp);
                    result.Add(saved);
                }
            }

            return result;
        }

        public static void RestoreSites(GameWorld world, List<HabitationSiteSaveData> saved, CampManager camps = null)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));

            world.anchors ??= new Dictionary<int, AnchorData>();
            world.settlements ??= new Dictionary<int, SettlementData>();
            world.anchors.Clear();
            world.settlements.Clear();

            var campManager = camps ?? world.Camps;
            campManager?.Clear();

            if (saved == null || saved.Count == 0) return;

            var loadedSiteIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in saved)
            {
                if (entry == null) continue;
                entry.EnsureCanonicalFields();

                if (!loadedSiteIds.Add(entry.site_id))
                    throw new InvalidOperationException($"Duplicate habitation site UUID '{entry.site_id}' in save.");
                if (!SiteModeKeys.TryParse(entry.site_mode, out var loadedMode))
                    throw new InvalidOperationException($"Invalid saved site_mode '{entry.site_mode}' for site {entry.site_id}.");

                if (entry.camp_id > 0)
                {
                    if (campManager == null)
                        throw new InvalidOperationException("Camp manager is not initialized while loading habitation sites.");
                    if (!Enum.TryParse(entry.camp_type, false, out CampType campType))
                        throw new InvalidOperationException($"Invalid saved camp type '{entry.camp_type}' for site {entry.site_id}.");

                    AnchorData campAnchor = null;
                    if (entry.anchor_id >= 0)
                    {
                        campAnchor = new AnchorData
                        {
                            anchorId = entry.anchor_id,
                            provinceId = entry.provinceId,
                            tileIndex = entry.camp_tile_index,
                            x = entry.location.x,
                            y = entry.location.y
                        };
                        world.anchors[entry.anchor_id] = campAnchor;
                    }

                    var camp = new CampData
                    {
                        siteId = entry.site_id,
                        siteMode = loadedMode,
                        siteClass = entry.site_class ?? string.Empty,
                        controlFaction = entry.control_faction ?? string.Empty,
                        location = new SiteLocation { x = entry.location.x, y = entry.location.y },
                        anchorId = entry.anchor_id,
                        anchor = campAnchor,
                        campId = entry.camp_id,
                        type = campType,
                        tileIndex = entry.camp_tile_index,
                        campName = entry.site_name,
                        population = entry.population,
                        ownerRealmId = entry.owner_realm_id,
                        ownerActorId = entry.owner_actor_id,
                        establishedDay = entry.established_day,
                        defense = entry.camp_defense,
                        supplies = entry.camp_supplies,
                        morale = entry.camp_morale,
                        permanence = entry.camp_permanence
                    };
                    entry.ApplyModeData(camp);

                    if ((entry.camp_attribute_keys == null) != (entry.camp_attribute_values == null)
                        || (entry.camp_attribute_keys != null && entry.camp_attribute_keys.Length != entry.camp_attribute_values.Length))
                        throw new InvalidOperationException($"Camp attributes are inconsistent for site {entry.site_id}.");
                    if (entry.camp_attribute_keys != null)
                    {
                        for (int i = 0; i < entry.camp_attribute_keys.Length; i++)
                            camp.attributes[entry.camp_attribute_keys[i]] = entry.camp_attribute_values[i];
                    }

                    campManager.RestoreCamp(camp);
                    continue;
                }

                var anchor = new AnchorData
                {
                    anchorId = entry.anchor_id,
                    provinceId = entry.provinceId,
                    tileIndex = entry.tileIndex,
                    x = entry.location.x,
                    y = entry.location.y
                };
                var settlement = new SettlementData
                {
                    siteId = entry.site_id,
                    siteMode = loadedMode,
                    siteClass = entry.site_class ?? string.Empty,
                    controlFaction = entry.control_faction ?? string.Empty,
                    location = new SiteLocation { x = entry.location.x, y = entry.location.y },
                    anchorId = entry.anchor_id,
                    anchor = anchor,
                    settlementName = entry.site_name,
                    settlementForm = (SettlementForm)entry.legacy_settlement_form,
                    population = entry.population,
                    development = entry.development,
                    wealth = entry.wealth,
                    tradePower = entry.tradePower,
                    fortification = entry.fortification,
                    garrison = entry.garrison,
                    isCapital = entry.isCapital,
                    isPort = entry.isPort,
                    isCoastal = entry.isCoastal,
                    hasMarket = entry.hasMarket,
                    hasTemple = entry.hasTemple,
                    hasUniversity = entry.hasUniversity,
                    constructionProgress = entry.constructionProgress,
                    constructionTier = entry.constructionTier,
                    primarySector = (EconomicSector)entry.primarySector,
                    economicInitialized = entry.economicInitialized
                };
                entry.ApplyModeData(settlement);
                settlement.SetLegacyCategoryFromSiteMode();

                if (entry.economicSectors != null && entry.economicRatios != null
                    && entry.economicSectors.Length == entry.economicRatios.Length)
                {
                    for (int i = 0; i < entry.economicSectors.Length; i++)
                        settlement.economicComposition[(EconomicSector)entry.economicSectors[i]] = entry.economicRatios[i];
                }

                settlement.districtsGenerated = entry.districtsGenerated;
                if (entry.districts != null)
                {
                    foreach (var district in entry.districts)
                    {
                        settlement.districts.Add(new CityDistrict(
                            (CityDistrictType)district.districtType,
                            district.districtName,
                            district.areaRatio,
                            district.populationRatio,
                            district.development,
                            district.wealth,
                            district.foundedDate));
                    }
                }

                SettlementBlockSaveMapping.RestoreAll(settlement.blocks, entry.blocks);
                if (settlement.blocks == null || settlement.blocks.Count == 0)
                    settlement.EnsureBlocksFromLegacy();
                settlement.RecalculateFortificationLevelFromBlocks();
                settlement.market = entry.market != null ? SettlementMarketSaveMapping.ToMarket(entry.market) : null;

                world.anchors[entry.anchor_id] = anchor;
                world.settlements[entry.anchor_id] = settlement;
            }
        }
    }
}
