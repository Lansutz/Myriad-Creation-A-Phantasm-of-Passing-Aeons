using System;
using System.Collections.Generic;
using System.IO;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;
using CivilizationEvolution.Render;
using UnityEngine;

namespace CivilizationEvolution.UI
{
    /// <summary>
    /// 地图存档数据（可序列化）
    /// 包含地形、省份、子地块的完整快照
    /// </summary>
    [Serializable]
    public class MapSaveData
    {
        public int version = 1;
        public int mapWidth;
        public int mapHeight;
        public int randomSeed;
        public long saveTimestamp;

        // 地形数据（紧凑存储）
        public TileSaveData[] tiles;

        // 省份数据
        public ProvinceSaveData[] provinces;

        // 子地块数据
        public BurgSaveData[] burgs;
    }

    [Serializable]
    public struct TileSaveData
    {
        public bool exists;
        public bool isLand;
        public float elevation01;
        public float slopeDegree;
        public float annualTemp;
        public float annualPrecipMm;
        public float airHumidityPct;
        public int biome;
        public int climateZone;
        public float fertility;
        public int provinceId;
        public int ownerRealmId;
        public int occupyingRealmId;
        public bool isCoast;
        public bool isRiver;
        public int seaConnectId;
        public int oceanTier;
        public int roadLevel;
        public float development;
        public float stability;
        public float order;
    }

    [Serializable]
    public class ProvinceSaveData
    {
        public int provinceId;
        public string provinceName;
        public int centerTileIndex;
        public int[] memberTiles;
    }

    [Serializable]
    public class BurgSaveData
    {
        public int burgId;
        public string burgName;
        public int type;
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
        public int buildLevel;
    }

    /// <summary>
    /// 地图保存/加载/导出系统
    /// 支持 JSON 格式存档、PNG 纹理导出、省份重命名
    /// </summary>
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

        // ===== 保存 =====
        /// <summary>保存地图到指定文件名（JSON格式）</summary>
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
            if (_world.burgs != null)
            {
                saveData.burgs = new BurgSaveData[_world.burgs.Count];
                int idx = 0;
                foreach (var b in _world.burgs.Values)
                {
                    saveData.burgs[idx++] = new BurgSaveData
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
                        buildLevel = b.buildLevel
                    };
                }
            }

            // 写入JSON
            string json = JsonUtility.ToJson(saveData, true);
            string path = Path.Combine(SaveDirectory, fileName + ".json");
            File.WriteAllText(path, json);

            long sizeKB = new FileInfo(path).Length / 1024;
            Debug.Log($"[MapSaveSystem] 地图已保存: {path} ({sizeKB}KB, {saveData.tiles.Length}地块, {saveData.provinces?.Length ?? 0}省, {saveData.burgs?.Length ?? 0}子地块)");
            return path;
        }

        // ===== 加载 =====
        /// <summary>从指定文件名加载地图（JSON格式）</summary>
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
                _world.burgs = new Dictionary<int, BurgData>();
                if (saveData.burgs != null)
                {
                    foreach (var b in saveData.burgs)
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
                            buildLevel = b.buildLevel
                        };
                        _world.burgs[b.burgId] = burg;
                    }
                }

                // 强制刷新渲染
                if (_renderer != null)
                {
                    _renderer.BindWorld(_world);
                    _renderer.ForceRefresh();
                }

                Debug.Log($"[MapSaveSystem] 地图已加载: {path} ({saveData.tiles.Length}地块, {_world.provinces.Count}省, {_world.burgs.Count}子地块)");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSaveSystem] 加载失败: {e.Message}");
                return false;
            }
        }

        // ===== 省份重命名 =====
        /// <summary>重命名指定省份</summary>
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

        // ===== 导出PNG =====
        /// <summary>导出当前地图纹理为PNG</summary>
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

        // ===== 存档列表 =====
        /// <summary>获取所有存档文件名</summary>
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
