using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;
using CivilizationEvolution.Politics;
using CivilizationEvolution.UI;
using UnityEngine;

namespace CivilizationEvolution.Core
{
    /// <summary>
    /// 完整游戏存档系统——保存/加载核心游戏状态（地图、时间、政权、编年史）。
    /// 大战略游戏完整存档，不只是地图。
    /// 角色、军队、战争、外交等复杂数据后续逐步扩展。
    /// </summary>
    public class GameSaveSystem
    {
        private readonly GameWorld _world;

        // 存档目录
        private static string SaveDirectory => Path.Combine(Application.persistentDataPath, "GameSaves");

        public GameSaveSystem(GameWorld world)
        {
            _world = world;
            if (!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);
        }

        // ===== 保存 =====

        /// <summary>保存完整游戏状态到指定文件名（JSON格式）</summary>
        public string SaveGame(string fileName, string saveName = null)
        {
            if (_world == null)
            {
                Debug.LogError("[GameSaveSystem] 世界数据为空，无法保存");
                return null;
            }

            try
            {
                var saveData = new GameSaveData
                {
                    saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    saveName = saveName ?? fileName,
                    currentYear = _world.currentYear,
                    currentDay = _world.currentDay,
                    mapWidth = _world.mapWidth,
                    mapHeight = _world.mapHeight,
                    randomSeed = _world.randomSeed,
                    playerRealmId = _world.PlayerRealmId,
                };

                // 1. 保存地图数据
                SaveMapData(saveData);

                // 2. 保存政权基本信息
                SaveRealms(saveData);

                // 3. 保存编年史
                SaveChronicle(saveData);

                // 序列化到JSON
                string json = JsonUtility.ToJson(saveData, true);
                string path = Path.Combine(SaveDirectory, fileName + ".json");
                File.WriteAllText(path, json);

                Debug.Log($"[GameSaveSystem] 游戏已保存: {fileName} ({saveData.realms?.Length ?? 0}政权, {saveData.chronicleEntries?.Length ?? 0}编年史)");
                return path;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameSaveSystem] 保存失败: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        private void SaveMapData(GameSaveData data)
        {
            if (_world.tiles == null) return;

            data.tiles = new TileSaveData[_world.tiles.Length];
            for (int i = 0; i < _world.tiles.Length; i++)
            {
                ref TileData t = ref _world.tiles[i];
                data.tiles[i] = new TileSaveData
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

            // 省份
            if (_world.provinces != null && _world.provinces.Count > 0)
            {
                data.provinces = new ProvinceSaveData[_world.provinces.Count];
                int idx = 0;
                foreach (var p in _world.provinces.Values)
                {
                    data.provinces[idx++] = new ProvinceSaveData
                    {
                        provinceId = p.provinceId,
                        provinceName = p.provinceName,
                        centerTileIndex = p.centerTileIndex,
                        memberTiles = p.memberTiles?.ToArray() ?? new int[0]
                    };
                }
            }

            // 聚落
            if (_world.burgs != null && _world.burgs.Count > 0)
            {
                data.burgs = new BurgSaveData[_world.burgs.Count];
                int idx = 0;
                foreach (var b in _world.burgs.Values)
                {
                    data.burgs[idx++] = new BurgSaveData
                    {
                        burgId = b.burgId,
                        burgName = b.burgName,
                        type = (int)b.type,
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
                        buildLevel = b.buildLevel,
                    };
                }
            }
        }

        private void SaveRealms(GameSaveData data)
        {
            if (_world.realms == null || _world.realms.Count == 0) return;

            var list = new List<RealmBasicSaveData>();
            foreach (var r in _world.realms.Values)
            {
                var save = new RealmBasicSaveData
                {
                    realmId = r.realmId,
                    realmName = r.realmName,
                    monarchId = r.monarchId,
                    treasury = r.treasury,
                    prestige = r.prestige,
                    stability = r.stability,
                    centralization = r.centralization,
                    suzerainId = r.suzerainId,
                    vassalIds = r.vassalIds?.ToArray() ?? new int[0],
                    primaryCultureId = r.primaryCultureId,
                    stateReligionId = r.stateReligionId,
                };

                // 控制的地块
                var controlled = new List<int>();
                if (_world.tiles != null)
                {
                    for (int i = 0; i < _world.tiles.Length; i++)
                        if (_world.tiles[i].ownerRealmId == r.realmId)
                            controlled.Add(i);
                }
                save.controlledTiles = controlled.ToArray();

                list.Add(save);
            }
            data.realms = list.ToArray();
        }

        private void SaveChronicle(GameSaveData data)
        {
            var chronicle = _world.GetChronicle();
            if (chronicle == null) return;

            var entries = chronicle.GetEntries();
            if (entries == null || entries.Count == 0) return;

            var list = new List<ChronicleEntrySaveData>();
            foreach (var e in entries)
            {
                var save = new ChronicleEntrySaveData
                {
                    entryId = e.entryId,
                    tick = e.tick,
                    year = e.year,
                    eventType = e.eventType,
                    description = e.description,
                    major = e.major,
                    participants = e.participants?.ToArray() ?? new int[0],
                };
                list.Add(save);
            }
            data.chronicleEntries = list.ToArray();
        }

        // ===== 加载 =====

        /// <summary>从指定文件名加载完整游戏状态（JSON格式）</summary>
        public bool LoadGame(string fileName)
        {
            string path = Path.Combine(SaveDirectory, fileName + ".json");
            if (!File.Exists(path))
            {
                Debug.LogError($"[GameSaveSystem] 存档不存在: {path}");
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                var saveData = JsonUtility.FromJson<GameSaveData>(json);

                if (saveData == null)
                {
                    Debug.LogError("[GameSaveSystem] 存档数据解析失败");
                    return false;
                }

                // 1. 恢复基础数据
                _world.mapWidth = saveData.mapWidth;
                _world.mapHeight = saveData.mapHeight;
                _world.randomSeed = saveData.randomSeed;
                _world.currentYear = saveData.currentYear;
                _world.currentDay = saveData.currentDay;
                _world.PlayerRealmId = saveData.playerRealmId;

                // 2. 恢复地图数据
                LoadMapData(saveData);

                // 3. 恢复政权基本信息
                LoadRealms(saveData);

                // 4. 恢复编年史
                LoadChronicle(saveData);

                Debug.Log($"[GameSaveSystem] 游戏已加载: {fileName} ({saveData.realms?.Length ?? 0}政权, {saveData.chronicleEntries?.Length ?? 0}编年史)");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameSaveSystem] 加载失败: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        private void LoadMapData(GameSaveData data)
        {
            if (data.tiles == null || data.tiles.Length == 0) return;

            // 验证尺寸
            if (data.mapWidth * data.mapHeight != data.tiles.Length)
            {
                Debug.LogError($"[GameSaveSystem] 地图尺寸不匹配: {data.mapWidth}x{data.mapHeight} != {data.tiles.Length}");
                return;
            }

            _world.tiles = new TileData[data.tiles.Length];
            for (int i = 0; i < data.tiles.Length; i++)
            {
                ref TileSaveData s = ref data.tiles[i];
                _world.tiles[i] = new TileData
                {
                    tileIndex = i,
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
                    order = s.order,
                    passable = true,
                    movementCost = 1f,
                    populationBlocks = new List<PopulationBlock>(),
                    buildingLevels = new int[0],
                };
            }

            // 恢复省份
            _world.provinces = new Dictionary<int, Province>();
            if (data.provinces != null)
            {
                foreach (var p in data.provinces)
                {
                    var province = new Province
                    {
                        provinceId = p.provinceId,
                        provinceName = p.provinceName,
                        centerTileIndex = p.centerTileIndex,
                        memberTiles = new List<int>(p.memberTiles ?? new int[0])
                    };
                    _world.provinces[p.provinceId] = province;
                }
            }

            // 恢复聚落
            _world.burgs = new Dictionary<int, BurgData>();
            if (data.burgs != null)
            {
                foreach (var b in data.burgs)
                {
                    var burg = new BurgData
                    {
                        burgId = b.burgId,
                        burgName = b.burgName,
                        type = (BurgType)b.type,
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
                        buildLevel = b.buildLevel,
                    };
                    _world.burgs[b.burgId] = burg;
                }
            }
        }

        private void LoadRealms(GameSaveData data)
        {
            if (data.realms == null || data.realms.Length == 0) return;

            _world.realms = new Dictionary<int, RealmData>();
            foreach (var r in data.realms)
            {
                var realm = new RealmData
                {
                    realmId = r.realmId,
                    realmName = r.realmName,
                    monarchId = r.monarchId,
                    treasury = r.treasury,
                    prestige = r.prestige,
                    stability = r.stability,
                    centralization = r.centralization,
                    suzerainId = r.suzerainId,
                    vassalIds = new List<int>(r.vassalIds ?? new int[0]),
                    primaryCultureId = r.primaryCultureId,
                    stateReligionId = r.stateReligionId,
                };
                _world.realms[r.realmId] = realm;
            }
        }

        private void LoadChronicle(GameSaveData data)
        {
            var chronicle = _world.GetChronicle();
            if (chronicle == null || data.chronicleEntries == null) return;

            // 编年史恢复（通过 Add 方法重新添加）
            foreach (var e in data.chronicleEntries)
            {
                chronicle.CurrentTick = e.tick;
                chronicle.CurrentYear = e.year;
                chronicle.Add(e.eventType, e.description, e.major, e.participants ?? new int[0]);
            }
        }

        // ===== 工具方法 =====

        /// <summary>获取所有存档文件名</summary>
        public static string[] GetAllSaves()
        {
            if (!Directory.Exists(SaveDirectory))
                return new string[0];

            var files = Directory.GetFiles(SaveDirectory, "*.json");
            var names = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                names[i] = Path.GetFileNameWithoutExtension(files[i]);
            return names;
        }

        /// <summary>删除指定存档</summary>
        public static bool DeleteSave(string fileName)
        {
            string path = Path.Combine(SaveDirectory, fileName + ".json");
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            return false;
        }
    }
}
