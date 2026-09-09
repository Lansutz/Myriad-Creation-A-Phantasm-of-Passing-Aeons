using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Race;
using CivilizationEvolution.War;

namespace CivilizationEvolution.Core
{
 /// 存档系统（v2） /// v1 使用 BinaryFormatter，在 Unity 6 中已被禁用（运行时抛 NotSupportedException，存档静默失败）， /// v2 改为 JsonUtility + 可序列化 DTO：Dictionary/HashSet 转 List 包装（JsonUtility 不支持字典）， /// 零第三方依赖；JSON 文本亦为 WebGL 等沙箱平台迁移留有余地。 /// 存档结构变更时递增 GameConstants.SaveVersion。    public static class SaveSystem
    {
        private static string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");

 /// <summary>保存游戏</summary>        public static bool SaveGame(GameWorld world, string saveName)
        {
            try
            {
                if (!Directory.Exists(SaveDirectory))
                    Directory.CreateDirectory(SaveDirectory);

                string filePath = Path.Combine(SaveDirectory, $"{saveName}.sav");

                var saveData = ToSaveData(world);
                File.WriteAllText(filePath, JsonUtility.ToJson(saveData, true));

                Debug.Log($"[SaveSystem] 存档成功: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 存档失败: {e.Message}");
                return false;
            }
        }

 /// <summary>加载游戏</summary>        public static GameWorld LoadGame(string saveName)
        {
            try
            {
                string filePath = Path.Combine(SaveDirectory, $"{saveName}.sav");
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[SaveSystem] 存档不存在: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                var saveData = JsonUtility.FromJson<SaveData>(json);
                if (saveData == null || saveData.tiles == null)
                {
 // 非法 JSON / v1 二进制存档无法解析                    Debug.LogError($"[SaveSystem] 读档失败: {filePath}（存档格式无效或为 v1 二进制存档，请新建游戏）");
                    return null;
                }

                if (saveData.version != GameConstants.SaveVersion)
                {
                    Debug.LogWarning($"[SaveSystem] 存档版本 {saveData.version} ≠ 当前 {GameConstants.SaveVersion}，尝试兼容加载");
                }

                var go = new GameObject($"GameWorld_{saveName}");
                var world = go.AddComponent<GameWorld>();
                ApplyToWorld(saveData, world);

                Debug.Log($"[SaveSystem] 读档成功: {filePath} (版本{saveData.version}, {saveData.saveTime})");
                return world;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 读档失败: {e.Message}");
                return null;
            }
        }

 /// <summary>删除存档</summary>        public static bool DeleteSave(string saveName)
        {
            string filePath = Path.Combine(SaveDirectory, $"{saveName}.sav");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }

 /// <summary>列出所有存档</summary>        public static string[] ListSaves()
        {
            if (!Directory.Exists(SaveDirectory))
                return new string[0];
            string[] files = Directory.GetFiles(SaveDirectory, "*.sav");
            for (int i = 0; i < files.Length; i++)
                files[i] = Path.GetFileNameWithoutExtension(files[i]);
            return files;
        }

 /// <summary>获取存档目录路径</summary>        public static string GetSaveDirectory() => SaveDirectory;

 // ===== 游戏对象 ⇄ DTO 转换 =====
        private static SaveData ToSaveData(GameWorld world)
        {
            var data = new SaveData
            {
                version = GameConstants.SaveVersion,
                saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                mapWidth = world.mapWidth,
                mapHeight = world.mapHeight,
                currentYear = world.currentYear,
                currentDay = world.currentDay,
                currentSeason = world.currentSeason,
                tiles = world.tiles,
                races = new List<RaceDTO>(),
                cultures = new List<CultureData>(),
                realms = new List<RealmDTO>(),
                tradeCenters = new List<TradeCenterDTO>(),
                goodsDefs = new List<GoodsDef>(),
                configJson = JsonUtility.ToJson(world.config)
            };

            foreach (var kv in world.races) data.races.Add(RaceDTO.FromRaceData(kv.Value));
            foreach (var kv in world.cultures) data.cultures.Add(kv.Value);
            foreach (var kv in world.realms) data.realms.Add(RealmDTO.FromRealmData(kv.Value));
            foreach (var kv in world.tradeCenters) data.tradeCenters.Add(TradeCenterDTO.FromTradeCenter(kv.Value));
            foreach (var kv in world.goodsDefs) data.goodsDefs.Add(kv.Value);
            data.wars = world.GetWars() != null ? new List<WarState>(world.GetWars()) : new List<WarState>();
            return data;
        }

        private static void ApplyToWorld(SaveData data, GameWorld world)
        {
            world.mapWidth = data.mapWidth;
            world.mapHeight = data.mapHeight;
            world.currentYear = data.currentYear;
            world.currentDay = data.currentDay;
            world.currentSeason = data.currentSeason;
            world.tiles = data.tiles;

 // 注：AddComponent 时 GameWorld.Awake 已先跑 InitializeWorld（含默认种族/文化/unitDefs）， // 此处用存档数据整体覆盖；unitDefs 不入档，保留默认集。            world.races.Clear();
            if (data.races != null)
                foreach (var dto in data.races) world.races[dto.raceId] = dto.ToRaceData();

            world.cultures.Clear();
            if (data.cultures != null)
                foreach (var c in data.cultures) world.cultures[c.cultureId] = c;

            world.realms.Clear();
            if (data.realms != null)
                foreach (var dto in data.realms) world.realms[dto.realmId] = dto.ToRealmData();

            world.tradeCenters.Clear();
            if (data.tradeCenters != null)
                foreach (var dto in data.tradeCenters) world.tradeCenters[dto.regionId] = dto.ToTradeCenter();

            world.goodsDefs.Clear();
            if (data.goodsDefs != null)
                foreach (var g in data.goodsDefs) world.goodsDefs[g.goodsId] = g;

 // ScriptableObject 配置：先建运行时实例，再用 JSON 快照覆盖恢复            world.config = WorldConfig.CreateRuntimeInstance();
            if (!string.IsNullOrEmpty(data.configJson))
                JsonUtility.FromJsonOverwrite(data.configJson, world.config);

 // 战争状态恢复（读档后战争闭环继续）            var wars = world.GetWars();
            if (wars != null)
            {
                wars.Clear();
                if (data.wars != null)
                    wars.AddRange(data.wars);
            }

 // 重新初始化子系统（引用类型无法序列化，需要重建）            world.ReinitializeSubsystems();
        }
    }

 /// 存档数据容器（v2，纯 JsonUtility 可序列化） /// 只包含可序列化的数据，引用类型的子系统由 ReinitializeSubsystems 重建

 /// <summary>通用键值包装（JsonUtility 不支持 Dictionary，存档用 List 包装）</summary>

 /// <summary>通用键值包装（bool 值）</summary>

 /// <summary>种族存档 DTO（Dictionary/枚举列表字段转 List 包装）</summary>

 /// <summary>税收系统存档 DTO（taxExemptions 字典转 List 包装）</summary>

 /// <summary>政权存档 DTO（字典/哈希集字段转 List 包装）</summary>

 /// <summary>贸易中心存档 DTO（库存/供需字典转 List 包装）</summary>
}
