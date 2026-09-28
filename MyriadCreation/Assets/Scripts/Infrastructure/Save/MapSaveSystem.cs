using System;
using System.Collections.Generic;
using System.IO;



using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Rendering;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Modding;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Settlement;
using MyriadCreation.Simulation.Society;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Hydrology;
using MyriadCreation.World.Settlement;
using MyriadCreation.World.Terrain;


using MyriadCreation.World;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Settlement;
using MyriadCreation.World.Anchor;
namespace MyriadCreation.Infrastructure.Save
{
 /// 地图存档数据（可序列化）
 /// 包含地形、省份、子地块的完整快照

 /// 地图保存/加载/导出系统
 /// 支持 JSON 格式存档、PNG 纹理导出、省份重命名
    public class MapSaveSystem
    {
        private readonly GameWorld _world;
        private readonly MapRenderer _renderer;

 // 存档目录
        private static string SaveDirectory => Path.Combine(Application.persistentDataPath, "MapSaves");

        public MapSaveSystem(GameWorld world, MapRenderer renderer)
        {
            _world = world;
            _renderer = renderer;
            if (!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);
        }

 // ===== 保存 ===== /// <summary>保存地图到指定文件名（JSON格式）</summary>
        public string SaveMap(string fileName)
        {
            if (_world == null || _world.tiles == null)
            {
                Debug.LogError("[MapSaveSystem] 世界数据为空，无法保存");
                return null;
            }

            var saveData = new MapSaveData
            {
                mapWidth = _world.mapWidth,
                mapHeight = _world.mapHeight,
                randomSeed = _world.randomSeed,
                saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

 // 序列化地形
            saveData.tiles = new TileSaveData[_world.tiles.Length];
            for (int i = 0; i < _world.tiles.Length; i++)
            {
                ref TileData t = ref _world.tiles[i];
                saveData.tiles[i] = new TileSaveData
                {
                    exists = t.exists,
                    isLand = t.isLand,
                    elevation01 = t.elevation01,
                    slopeDegree = t.slopeDegree,
                    annualTemp = t.annualTemp,
                    annualPrecipMm = t.annualPrecipMm,
                    airHumidityPct = t.airHumidityPct,
                    biome = (int)t.biome,
                    climateZone = (int)t.climateZone,
                    fertility = t.fertility,
                    provinceId = t.provinceId,
                    ownerRealmId = t.ownerRealmId,
                    occupyingRealmId = t.occupyingRealmId,
                    isCoast = t.isCoast,
                    isRiver = t.isRiver,
                    seaConnectId = t.seaConnectId,
                    oceanTier = (int)t.oceanTier,
                    roadLevel = (int)t.roadLevel,
                    development = t.development,
                    stability = t.stability,
                    order = t.order
                };
            }

 // 序列化省份
            if (_world.provinces != null)
            {
                saveData.provinces = new ProvinceSaveData[_world.provinces.Count];
                int idx = 0;
                foreach (var p in _world.provinces.Values)
                {
                    saveData.provinces[idx++] = new ProvinceSaveData
                    {
                        provinceId = p.provinceId,
                        provinceName = p.provinceName,
                        centerTileIndex = p.centerTileIndex,
                        memberTiles = p.memberTiles.ToArray()
                    };
                }
            }

 // 序列化子地块
            int campCount = _world.Camps?.Count ?? 0;
            if ((_world.settlements?.Count ?? 0) + campCount > 0)
            {
                saveData.habitation_sites = new HabitationSiteSaveData[(_world.settlements?.Count ?? 0) + campCount];
                int idx = 0;
                if (_world.settlements != null)
                foreach (var b in _world.settlements.Values)
                {
                    saveData.habitation_sites[idx] = new HabitationSiteSaveData
                    {
                        site_id = b.siteId,
                        site_mode = SiteModeKeys.ToKey(b.siteMode),
                        site_class = b.siteClass,
                        control_faction = b.controlFaction,
                        location = new SiteLocationSaveData { x = b.x, y = b.y },
                        anchor_id = b.anchorId,
                        site_name = b.settlementName,
                        legacy_settlement_form = (int)b.settlementForm,
                        provinceId = b.provinceId,
                        tileIndex = b.tileIndex,
                        x = b.x,
                        y = b.y,
                        population = b.population,
                        development = b.development,
                        wealth = b.wealth,
                        tradePower = b.tradePower,
                        fortification = b.fortification,
                        garrison = b.garrison,
                        isCapital = b.isCapital,
                        isPort = b.isPort,
                        isCoastal = b.isCoastal,
                        hasMarket = b.hasMarket,
                        hasTemple = b.hasTemple,
                        hasUniversity = b.hasUniversity,
                        legacy_site_category = (int)b.settlementCategory,
                        constructionProgress = b.constructionProgress,
                        constructionTier = b.constructionTier,
                        // 经济成分系统
                        primarySector = (int)b.primarySector,
                        economicInitialized = b.economicInitialized,
                        economicSectors = b.economicComposition != null ?
                            System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(b.economicComposition.Keys, k => (int)k)) : null,
                        economicRatios = b.economicComposition != null ?
                            System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(b.economicComposition.Values, v => v)) : null,
                        // 城市区划系统
                        districtsGenerated = b.districtsGenerated,
                        districts = b.districts != null ?
                            System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(b.districts, d => new CityDistrictSaveData
                            {
                                districtType = (int)d.districtType,
                                districtName = d.districtName,
                                areaRatio = d.areaRatio,
                                populationRatio = d.populationRatio,
                                development = d.development,
                                wealth = d.wealth,
                                foundedDate = d.foundedDate
                            })) : null
                    };
                    saveData.habitation_sites[idx].CaptureModeData(b);
                    idx++;
                }

                if (_world.Camps != null)
                foreach (var camp in _world.Camps.AllCamps)
                {
                    var savedCamp = new HabitationSiteSaveData
                    {
                        site_id = camp.siteId,
                        site_mode = SiteModeKeys.ToKey(camp.siteMode),
                        site_class = camp.siteClass,
                        control_faction = camp.controlFaction,
                        location = new SiteLocationSaveData { x = camp.location.x, y = camp.location.y },
                        anchor_id = camp.anchorId,
                        site_name = camp.siteName,
                        provinceId = camp.anchor?.provinceId ?? -1,
                        tileIndex = camp.tileIndex,
                        x = camp.location.x,
                        y = camp.location.y,
                        population = camp.population
                    };
                    savedCamp.CaptureCampData(camp);
                    saveData.habitation_sites[idx++] = savedCamp;
                }
            }

 // 写入JSON
            string json = JsonUtility.ToJson(saveData, true);
            string path = Path.Combine(SaveDirectory, fileName + ".json");
            File.WriteAllText(path, json);

            long sizeKB = new FileInfo(path).Length / 1024;
            Debug.Log($"[MapSaveSystem] 地图已保存: {path} ({sizeKB}KB, {saveData.tiles.Length}地块, {saveData.provinces?.Length ?? 0}省, {saveData.habitation_sites?.Length ?? 0}个栖居点)");
            return path;
        }

 // ===== 加载 ===== /// <summary>从指定文件名加载地图（JSON格式）</summary>
        public bool LoadMap(string fileName)
        {
            string path = Path.Combine(SaveDirectory, fileName + ".json");
            if (!File.Exists(path))
            {
                Debug.LogError($"[MapSaveSystem] 存档不存在: {path}");
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                var saveData = JsonUtility.FromJson<MapSaveData>(json);

                if (saveData.tiles == null || saveData.tiles.Length == 0)
                {
                    Debug.LogError("[MapSaveSystem] 存档地形数据为空");
                    return false;
                }

 // 验证尺寸
                if (saveData.mapWidth * saveData.mapHeight != saveData.tiles.Length)
                {
                    Debug.LogError($"[MapSaveSystem] 尺寸不匹配: {saveData.mapWidth}x{saveData.mapHeight} != {saveData.tiles.Length}");
                    return false;
                }

 // 恢复地形
                _world.mapWidth = saveData.mapWidth;
                _world.mapHeight = saveData.mapHeight;
                _world.randomSeed = saveData.randomSeed;
                _world.tiles = new TileData[saveData.tiles.Length];

                for (int i = 0; i < saveData.tiles.Length; i++)
                {
                    ref TileSaveData s = ref saveData.tiles[i];
                    _world.tiles[i] = new TileData
                    {
                        exists = s.exists,
                        isLand = s.isLand,
                        elevation01 = s.elevation01,
                        slopeDegree = s.slopeDegree,
                        annualTemp = s.annualTemp,
                        annualPrecipMm = s.annualPrecipMm,
                        airHumidityPct = s.airHumidityPct,
                        biome = (GameEnums.BiomeType)s.biome,
                        climateZone = (GameEnums.ClimateZone)s.climateZone,
                        fertility = s.fertility,
                        provinceId = s.provinceId,
                        ownerRealmId = s.ownerRealmId,
                        occupyingRealmId = s.occupyingRealmId,
                        isCoast = s.isCoast,
                        isRiver = s.isRiver,
                        seaConnectId = s.seaConnectId,
                        oceanTier = (GameEnums.OceanTier)s.oceanTier,
                        roadLevel = (GameEnums.RoadLevel)s.roadLevel,
                        development = s.development,
                        stability = s.stability,
                        order = s.order
                    };
                }

 // 恢复省份
                _world.provinces = new Dictionary<int, Province>();
                if (saveData.provinces != null)
                {
                    foreach (var p in saveData.provinces)
                    {
                        var province = new Province
                        {
                            provinceId = p.provinceId,
                            provinceName = p.provinceName,
                            centerTileIndex = p.centerTileIndex,
                            memberTiles = new List<int>(p.memberTiles)
                        };
                        _world.provinces[p.provinceId] = province;
                    }
                }

 // 恢复子地块
 _world.anchors = new Dictionary<int, AnchorData>();
 _world.settlements = new Dictionary<int, SettlementData>();
 _world.Camps?.Clear();
 if (saveData.habitation_sites != null)
 {
     var loadedSiteIds = new HashSet<string>(System.StringComparer.Ordinal);
     foreach (var b in saveData.habitation_sites)
     {
         b.EnsureCanonicalFields();
         if (!loadedSiteIds.Add(b.site_id))
             throw new InvalidOperationException($"Duplicate habitation site UUID '{b.site_id}' in map save.");
         if (!SiteModeKeys.TryParse(b.site_mode, out var loadedMode))
             throw new InvalidOperationException($"Invalid saved site_mode '{b.site_mode}' for site {b.site_id}.");

         if (b.camp_id > 0)
         {
             if (_world.Camps == null)
                 throw new InvalidOperationException("Camp manager is not initialized while loading habitation sites.");
             if (!Enum.TryParse(b.camp_type, false, out CampType campType))
                 throw new InvalidOperationException($"Invalid saved camp type '{b.camp_type}' for site {b.site_id}.");

             AnchorData campAnchor = null;
             if (b.anchor_id >= 0)
             {
                 campAnchor = new AnchorData
                 {
                     anchorId = b.anchor_id,
                     provinceId = b.provinceId,
                     tileIndex = b.camp_tile_index,
                     x = b.location.x,
                     y = b.location.y
                 };
                 _world.anchors[b.anchor_id] = campAnchor;
             }

             var camp = new CampData
             {
                 siteId = b.site_id,
                 siteMode = loadedMode,
                 siteClass = b.site_class ?? string.Empty,
                 controlFaction = b.control_faction ?? string.Empty,
                 location = new SiteLocation { x = b.location.x, y = b.location.y },
                 anchorId = b.anchor_id,
                 anchor = campAnchor,
                 campId = b.camp_id,
                 type = campType,
                 tileIndex = b.camp_tile_index,
                 campName = b.site_name,
                 population = b.population,
                 ownerRealmId = b.owner_realm_id,
                 ownerActorId = b.owner_actor_id,
                 establishedDay = b.established_day,
                 defense = b.camp_defense,
                 supplies = b.camp_supplies,
                 morale = b.camp_morale,
                 permanence = b.camp_permanence
             };
             b.ApplyModeData(camp);
             if ((b.camp_attribute_keys == null) != (b.camp_attribute_values == null)
                 || (b.camp_attribute_keys != null && b.camp_attribute_keys.Length != b.camp_attribute_values.Length))
                 throw new InvalidOperationException($"Camp attributes are inconsistent for site {b.site_id}.");
             if (b.camp_attribute_keys != null)
             {
                 for (int i = 0; i < b.camp_attribute_keys.Length; i++)
                     camp.attributes.Add(b.camp_attribute_keys[i], b.camp_attribute_values[i]);
             }
             _world.Camps.RestoreCamp(camp);
             continue;
         }

         var anchor = new AnchorData
         {
             anchorId = b.anchor_id,
             provinceId = b.provinceId,
             tileIndex = b.tileIndex,
             x = b.location.x,
             y = b.location.y
         };
         var settlement = new SettlementData
         {
             siteId = b.site_id,
             siteMode = loadedMode,
             siteClass = b.site_class ?? string.Empty,
             controlFaction = b.control_faction ?? string.Empty,
             location = new SiteLocation { x = b.location.x, y = b.location.y },
             anchorId = b.anchor_id,
             anchor = anchor,
                            settlementName = b.site_name,
                            settlementForm = (SettlementForm)b.legacy_settlement_form,
                            population = b.population,
                            development = b.development,
                            wealth = b.wealth,
                            tradePower = b.tradePower,
                            fortification = b.fortification,
                            garrison = b.garrison,
                            isCapital = b.isCapital,
                            isPort = b.isPort,
                            isCoastal = b.isCoastal,
                            hasMarket = b.hasMarket,
                            hasTemple = b.hasTemple,
                            hasUniversity = b.hasUniversity,
                            constructionProgress = b.constructionProgress,
                            constructionTier = b.constructionTier,
                            // 经济成分系统
                            primarySector = (EconomicSector)b.primarySector,
                            economicInitialized = b.economicInitialized
                        };
                        b.ApplyModeData(settlement);
                        settlement.SetLegacyCategoryFromSiteMode();

                        // 恢复经济成分字典
                        if (b.economicSectors != null && b.economicRatios != null && b.economicSectors.Length == b.economicRatios.Length)
                        {
                            for (int i = 0; i < b.economicSectors.Length; i++)
                            {
                                settlement.economicComposition[(EconomicSector)b.economicSectors[i]] = b.economicRatios[i];
                            }
                        }

                        // 恢复城市区划
                        settlement.districtsGenerated = b.districtsGenerated;
                        if (b.districts != null)
                        {
                            foreach (var d in b.districts)
                            {
                                settlement.districts.Add(new CityDistrict(
                                    (CityDistrictType)d.districtType,
                                    d.districtName,
                                    d.areaRatio,
                                    d.populationRatio,
                                    d.development,
                                    d.wealth,
                                    d.foundedDate
                                ));
                            }
                        }

                        _world.anchors[b.anchor_id] = anchor;
                        _world.settlements[b.anchor_id] = settlement;
                    }
                }

 // 强制刷新渲染
                if (_renderer != null)
                {
                    _renderer.BindWorld(_world);
                    _renderer.ForceRefresh();
                }

                Debug.Log($"[MapSaveSystem] 地图已加载: {path} ({saveData.tiles.Length}地块, {_world.provinces.Count}省, {_world.settlements.Count}子地块)");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSaveSystem] 加载失败: {e.Message}");
                return false;
            }
        }

 // ===== 省份重命名 ===== /// <summary>重命名指定省份</summary>
        public bool RenameProvince(int provinceId, string newName)
        {
            if (_world == null || _world.provinces == null) return false;
            if (!_world.provinces.TryGetValue(provinceId, out var province))
            {
                Debug.LogWarning($"[MapSaveSystem] 省份不存在: {provinceId}");
                return false;
            }

            string oldName = province.provinceName;
            province.provinceName = newName;
            Debug.Log($"[MapSaveSystem] 省份重命名: #{provinceId} '{oldName}' → '{newName}'");

            if (_renderer != null)
                _renderer.ForceRefresh();

            return true;
        }

 /// <summary>获取所有省份名称列表（用于UI下拉）</summary>
        public List<(int id, string name)> GetProvinceList()
        {
            var list = new List<(int, string)>();
            if (_world?.provinces == null) return list;
            foreach (var p in _world.provinces.Values)
                list.Add((p.provinceId, p.provinceName));
            list.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            return list;
        }

 // ===== 导出PNG ===== /// <summary>导出当前地图纹理为PNG</summary>
 /// <param name="fileName">文件名（不含扩展名）</param>
 /// <param name="mode">显示模式（-1=当前模式）</param>
        public string ExportMapPNG(string fileName, int mode = -1)
        {
            if (_renderer == null)
            {
                Debug.LogError("[MapSaveSystem] MapRenderer为空，无法导出");
                return null;
            }

 // 切换到指定显示模式（如果指定了）
            MapDisplayMode originalMode = _renderer.DisplayMode;
            if (mode >= 0)
                _renderer.SetDisplayMode((MapDisplayMode)mode);

 // 强制刷新一帧确保纹理是最新的
            _renderer.ForceRefresh();

 // 获取纹理
            var texture = _renderer.GetMapTexture();
            if (texture == null)
            {
                Debug.LogError("[MapSaveSystem] 地图纹理为空");
                return null;
            }

 // 编码为PNG
            byte[] pngData = texture.EncodeToPNG();
            string path = Path.Combine(SaveDirectory, fileName + ".png");
            File.WriteAllBytes(path, pngData);

 // 恢复原显示模式
            if (mode >= 0)
                _renderer.SetDisplayMode(originalMode);

            long sizeKB = pngData.Length / 1024;
            Debug.Log($"[MapSaveSystem] 地图已导出: {path} ({texture.width}x{texture.height}, {sizeKB}KB)");
            return path;
        }

 /// <summary>导出所有显示模式的地图PNG（地形/气候/群系/政治/人口/经济）</summary>
        public List<string> ExportAllMapPNGs(string baseName)
        {
            var paths = new List<string>();
            string[] modeNames = { "terrain", "climate", "biome", "political", "population", "economy" };
            for (int i = 0; i < modeNames.Length; i++)
            {
                string path = ExportMapPNG($"{baseName}_{modeNames[i]}", i);
                if (path != null) paths.Add(path);
            }
            return paths;
        }

 // ===== 存档列表 ===== /// <summary>获取所有存档文件名</summary>
        public string[] GetSaveList()
        {
            if (!Directory.Exists(SaveDirectory)) return Array.Empty<string>();
            var files = Directory.GetFiles(SaveDirectory, "*.json");
            for (int i = 0; i < files.Length; i++)
                files[i] = Path.GetFileNameWithoutExtension(files[i]);
            return files;
        }

 /// <summary>删除指定存档</summary>
        public bool DeleteSave(string fileName)
        {
            string path = Path.Combine(SaveDirectory, fileName + ".json");
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[MapSaveSystem] 存档已删除: {path}");
                return true;
            }
            return false;
        }

 /// <summary>打开存档目录</summary>
        public static void OpenSaveDirectory()
        {
            if (!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);
            Application.OpenURL("file:///" + SaveDirectory.Replace("\\", "/"));
        }
    }
}
