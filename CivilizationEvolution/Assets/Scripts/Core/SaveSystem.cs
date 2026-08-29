using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Race;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Core
{
    /// <summary>
    /// 存档系统
    /// 负责游戏世界的序列化保存与反序列化加载
    /// </summary>
    public static class SaveSystem
    {
        private static string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");

        /// <summary>保存游戏</summary>
        public static bool SaveGame(GameWorld world, string saveName)
        {
            try
            {
                if (!Directory.Exists(SaveDirectory))
                    Directory.CreateDirectory(SaveDirectory);

                string filePath = Path.Combine(SaveDirectory, $"{saveName}.sav");

                var saveData = new SaveData
                {
                    version = GameConstants.SaveVersion,
                    saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    mapWidth = world.mapWidth,
                    mapHeight = world.mapHeight,
                    currentYear = world.currentYear,
                    currentDay = world.currentDay,
                    currentSeason = world.currentSeason,
                    tiles = world.tiles,
                    races = world.races,
                    cultures = world.cultures,
                    realms = world.realms,
                    tradeCenters = world.tradeCenters,
                    goodsDefs = world.goodsDefs,
                    configJson = JsonUtility.ToJson(world.config)
                };

                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    formatter.Serialize(stream, saveData);
                }

                Debug.Log($"[SaveSystem] 存档成功: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 存档失败: {e.Message}");
                return false;
            }
        }

        /// <summary>加载游戏</summary>
        public static GameWorld LoadGame(string saveName)
        {
            try
            {
                string filePath = Path.Combine(SaveDirectory, $"{saveName}.sav");
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[SaveSystem] 存档不存在: {filePath}");
                    return null;
                }

                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(filePath, FileMode.Open))
                {
                    SaveData saveData = (SaveData)formatter.Deserialize(stream);

                    var go = new GameObject($"GameWorld_{saveName}");
                    var world = go.AddComponent<GameWorld>();

                    world.mapWidth = saveData.mapWidth;
                    world.mapHeight = saveData.mapHeight;
                    world.currentYear = saveData.currentYear;
                    world.currentDay = saveData.currentDay;
                    world.currentSeason = saveData.currentSeason;
                    world.tiles = saveData.tiles;
                    world.races = saveData.races;
                    world.cultures = saveData.cultures;
                    world.realms = saveData.realms;
                    world.tradeCenters = saveData.tradeCenters;
                    world.goodsDefs = saveData.goodsDefs;

                    // ScriptableObject配置：先建运行时实例，再用JSON快照覆盖恢复
                    world.config = WorldConfig.CreateRuntimeInstance();
                    if (!string.IsNullOrEmpty(saveData.configJson))
                        JsonUtility.FromJsonOverwrite(saveData.configJson, world.config);

                    // 重新初始化子系统（引用类型无法序列化，需要重建）
                    world.ReinitializeSubsystems();

                    Debug.Log($"[SaveSystem] 读档成功: {filePath} (版本{saveData.version}, {saveData.saveTime})");
                    return world;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 读档失败: {e.Message}");
                return null;
            }
        }

        /// <summary>删除存档</summary>
        public static bool DeleteSave(string saveName)
        {
            string filePath = Path.Combine(SaveDirectory, $"{saveName}.sav");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }

        /// <summary>列出所有存档</summary>
        public static string[] ListSaves()
        {
            if (!Directory.Exists(SaveDirectory))
                return new string[0];
            string[] files = Directory.GetFiles(SaveDirectory, "*.sav");
            for (int i = 0; i < files.Length; i++)
                files[i] = Path.GetFileNameWithoutExtension(files[i]);
            return files;
        }

        /// <summary>获取存档目录路径</summary>
        public static string GetSaveDirectory() => SaveDirectory;
    }

    /// <summary>
    /// 存档数据容器
    /// 只包含可序列化的数据，引用类型的子系统需要重建
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version;
        public string saveTime;

        // 世界配置
        public int mapWidth;
        public int mapHeight;
        public int currentYear;
        public int currentDay;
        public int currentSeason;

        // 核心数据
        public TileData[] tiles;
        public System.Collections.Generic.Dictionary<int, RaceData> races;
        public System.Collections.Generic.Dictionary<int, CultureData> cultures;
        public System.Collections.Generic.Dictionary<int, RealmData> realms;
        public System.Collections.Generic.Dictionary<int, TradeCenter> tradeCenters;
        public System.Collections.Generic.Dictionary<int, GoodsDef> goodsDefs;
        public string configJson; // WorldConfig(ScriptableObject)的JSON快照
    }
}
