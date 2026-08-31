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
    /// <summary>
    /// 存档系统（v2）
    /// v1 使用 BinaryFormatter，在 Unity 6 中已被禁用（运行时抛 NotSupportedException，存档静默失败），
    /// v2 改为 JsonUtility + 可序列化 DTO：Dictionary/HashSet 转 List 包装（JsonUtility 不支持字典），
    /// 零第三方依赖；JSON 文本亦为 WebGL 等沙箱平台迁移留有余地。
    /// 存档结构变更时递增 GameConstants.SaveVersion。
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

                string json = File.ReadAllText(filePath);
                var saveData = JsonUtility.FromJson<SaveData>(json);
                if (saveData == null || saveData.tiles == null)
                {
                    // 非法 JSON / v1 二进制存档无法解析
                    Debug.LogError($"[SaveSystem] 读档失败: {filePath}（存档格式无效或为 v1 二进制存档，请新建游戏）");
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

            // 注：AddComponent 时 GameWorld.Awake 已先跑 InitializeWorld（含默认种族/文化/unitDefs），
            // 此处用存档数据整体覆盖；unitDefs 不入档，保留默认集。
            world.races.Clear();
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

            // ScriptableObject 配置：先建运行时实例，再用 JSON 快照覆盖恢复
            world.config = WorldConfig.CreateRuntimeInstance();
            if (!string.IsNullOrEmpty(data.configJson))
                JsonUtility.FromJsonOverwrite(data.configJson, world.config);

            // 战争状态恢复（读档后战争闭环继续）
            var wars = world.GetWars();
            if (wars != null)
            {
                wars.Clear();
                if (data.wars != null)
                    wars.AddRange(data.wars);
            }

            // 重新初始化子系统（引用类型无法序列化，需要重建）
            world.ReinitializeSubsystems();
        }
    }

    /// <summary>
    /// 存档数据容器（v2，纯 JsonUtility 可序列化）
    /// 只包含可序列化的数据，引用类型的子系统由 ReinitializeSubsystems 重建
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

        // 核心数据（TileData/CultureData/GoodsDef/TradeRoute 无字典，可直接序列化）
        public TileData[] tiles;
        public List<RaceDTO> races;
        public List<CultureData> cultures;
        public List<RealmDTO> realms;
        public List<TradeCenterDTO> tradeCenters;
        public List<GoodsDef> goodsDefs;
        public string configJson; // WorldConfig(ScriptableObject)的JSON快照

        // 战争状态（WarState 无字典字段，可直接序列化——读档恢复战争闭环）
        public List<WarState> wars;
    }

    /// <summary>通用键值包装（JsonUtility 不支持 Dictionary，存档用 List 包装）</summary>
    [Serializable]
    public class IntFloatEntry
    {
        public int key;
        public float value;
        public IntFloatEntry() { }
        public IntFloatEntry(int k, float v) { key = k; value = v; }
    }

    /// <summary>通用键值包装（bool 值）</summary>
    [Serializable]
    public class IntBoolEntry
    {
        public int key;
        public bool value;
        public IntBoolEntry() { }
        public IntBoolEntry(int k, bool v) { key = k; value = v; }
    }

    /// <summary>种族存档 DTO（Dictionary/枚举列表字段转 List 包装）</summary>
    [Serializable]
    public class RaceDTO
    {
        public int raceId;
        public string raceName;
        public string description;
        public float baseLifespan;
        public float growthRate;
        public float reproductionRate;
        public float physicalStrength;
        public float diseaseResistance;
        public float environmentalTolerance;
        public float visualAcuity;
        public float auditoryRange;
        public float olfactorySensitivity;
        public float cognitiveCapacity;
        public float transformativity;
        public List<IntFloatEntry> productionModifiers = new List<IntFloatEntry>();
        public List<IntFloatEntry> consumptionModifiers = new List<IntFloatEntry>();
        public float infantryBonus;
        public float cavalryBonus;
        public float navyBonus;
        public float moraleBase;
        public List<int> preferredBiomes = new List<int>();
        public float coldTolerance;
        public float heatTolerance;
        public float aridityTolerance;
        public float humidityTolerance;
        public float altitudeTolerance;
        // DNA 基准与基因频率（v3 存档新增；旧档缺失字段走默认值）
        public float intelligenceBaseline;
        public float martialBaseline;
        public float lifespanBaseYears;
        public float lifespanRangeYears;
        public float resistanceBaseline;
        public List<LocusFrequency> locusFrequencies = new List<LocusFrequency>();

        public static RaceDTO FromRaceData(RaceData r)
        {
            var dto = new RaceDTO
            {
                raceId = r.raceId,
                raceName = r.raceName,
                description = r.description,
                baseLifespan = r.baseLifespan,
                growthRate = r.growthRate,
                reproductionRate = r.reproductionRate,
                physicalStrength = r.physicalStrength,
                diseaseResistance = r.diseaseResistance,
                environmentalTolerance = r.environmentalTolerance,
                visualAcuity = r.visualAcuity,
                auditoryRange = r.auditoryRange,
                olfactorySensitivity = r.olfactorySensitivity,
                cognitiveCapacity = r.cognitiveCapacity,
                transformativity = r.transformativity,
                infantryBonus = r.infantryBonus,
                cavalryBonus = r.cavalryBonus,
                navyBonus = r.navyBonus,
                moraleBase = r.moraleBase,
                coldTolerance = r.coldTolerance,
                heatTolerance = r.heatTolerance,
                aridityTolerance = r.aridityTolerance,
                humidityTolerance = r.humidityTolerance,
                altitudeTolerance = r.altitudeTolerance,
                intelligenceBaseline = r.intelligenceBaseline,
                martialBaseline = r.martialBaseline,
                lifespanBaseYears = r.lifespanBaseYears,
                lifespanRangeYears = r.lifespanRangeYears,
                resistanceBaseline = r.resistanceBaseline,
                locusFrequencies = r.locusFrequencies ?? new List<LocusFrequency>()
            };
            if (r.productionModifiers != null)
                foreach (var kv in r.productionModifiers) dto.productionModifiers.Add(new IntFloatEntry((int)kv.Key, kv.Value));
            if (r.consumptionModifiers != null)
                foreach (var kv in r.consumptionModifiers) dto.consumptionModifiers.Add(new IntFloatEntry((int)kv.Key, kv.Value));
            if (r.preferredBiomes != null)
                foreach (var b in r.preferredBiomes) dto.preferredBiomes.Add((int)b);
            return dto;
        }

        public RaceData ToRaceData()
        {
            var r = new RaceData
            {
                raceId = raceId,
                raceName = raceName,
                description = description,
                baseLifespan = baseLifespan,
                growthRate = growthRate,
                reproductionRate = reproductionRate,
                physicalStrength = physicalStrength,
                diseaseResistance = diseaseResistance,
                environmentalTolerance = environmentalTolerance,
                visualAcuity = visualAcuity,
                auditoryRange = auditoryRange,
                olfactorySensitivity = olfactorySensitivity,
                cognitiveCapacity = cognitiveCapacity,
                transformativity = transformativity,
                infantryBonus = infantryBonus,
                cavalryBonus = cavalryBonus,
                navyBonus = navyBonus,
                moraleBase = moraleBase,
                coldTolerance = coldTolerance,
                heatTolerance = heatTolerance,
                aridityTolerance = aridityTolerance,
                humidityTolerance = humidityTolerance,
                altitudeTolerance = altitudeTolerance,
                intelligenceBaseline = intelligenceBaseline,
                martialBaseline = martialBaseline,
                lifespanBaseYears = lifespanBaseYears,
                lifespanRangeYears = lifespanRangeYears,
                resistanceBaseline = resistanceBaseline,
                locusFrequencies = locusFrequencies ?? new List<LocusFrequency>()
            };
            foreach (var e in productionModifiers) r.productionModifiers[(GameEnums.GoodsCategory)e.key] = e.value;
            foreach (var e in consumptionModifiers) r.consumptionModifiers[(GameEnums.GoodsCategory)e.key] = e.value;
            foreach (int b in preferredBiomes) r.preferredBiomes.Add((GameEnums.BiomeType)b);
            return r;
        }
    }

    /// <summary>税收系统存档 DTO（taxExemptions 字典转 List 包装）</summary>
    [Serializable]
    public class TaxSystemDTO
    {
        public float agriculturalTax = 0.1f;
        public float headTax = 0.05f;
        public float tradeTax = 0.1f;
        public float miningTax = 0.15f;
        public float craftTax = 0.1f;
        public float livestockTax = 0.08f;
        public float luxuryTax = 0.3f;
        public float saltMonopolyTax = 0.5f;
        public float wartimeSpecialTax = 0f;
        public List<IntBoolEntry> taxExemptions = new List<IntBoolEntry>();

        public static TaxSystemDTO FromTaxSystem(TaxSystem t)
        {
            var dto = new TaxSystemDTO
            {
                agriculturalTax = t.agriculturalTax,
                headTax = t.headTax,
                tradeTax = t.tradeTax,
                miningTax = t.miningTax,
                craftTax = t.craftTax,
                livestockTax = t.livestockTax,
                luxuryTax = t.luxuryTax,
                saltMonopolyTax = t.saltMonopolyTax,
                wartimeSpecialTax = t.wartimeSpecialTax
            };
            if (t.taxExemptions != null)
                foreach (var kv in t.taxExemptions) dto.taxExemptions.Add(new IntBoolEntry((int)kv.Key, kv.Value));
            return dto;
        }

        public TaxSystem ToTaxSystem()
        {
            var t = new TaxSystem
            {
                agriculturalTax = agriculturalTax,
                headTax = headTax,
                tradeTax = tradeTax,
                miningTax = miningTax,
                craftTax = craftTax,
                livestockTax = livestockTax,
                luxuryTax = luxuryTax,
                saltMonopolyTax = saltMonopolyTax,
                wartimeSpecialTax = wartimeSpecialTax
            };
            foreach (var e in taxExemptions) t.taxExemptions[(GameEnums.SocialClass)e.key] = e.value;
            return t;
        }
    }

    /// <summary>政权存档 DTO（字典/哈希集字段转 List 包装）</summary>
    [Serializable]
    public class RealmDTO
    {
        public int realmId;
        public string realmName;
        /// <summary>政体七维成分组合（整体序列化；GovernmentComposition 及其成员类全部 [Serializable]，无 Dictionary）</summary>
        public GovernmentComposition composition = new GovernmentComposition();
        public float treasury;
        public float prestige;
        public float stability;
        public float centralization;
        public TaxSystemDTO taxSystem = new TaxSystemDTO();
        public CurrencySystem currencySystem = new CurrencySystem();
        public List<IntFloatEntry> classRelations = new List<IntFloatEntry>();
        public List<int> coreTiles = new List<int>();
        public List<int> claimedTiles = new List<int>();
        public int suzerainId = -1;
        public List<int> vassalIds = new List<int>();

        public static RealmDTO FromRealmData(RealmData r)
        {
            var dto = new RealmDTO
            {
                realmId = r.realmId,
                realmName = r.realmName,
                composition = r.composition,
                treasury = r.treasury,
                prestige = r.prestige,
                stability = r.stability,
                centralization = r.centralization,
                taxSystem = TaxSystemDTO.FromTaxSystem(r.taxSystem),
                currencySystem = r.currencySystem,
                suzerainId = r.suzerainId
            };
            if (r.classRelations != null)
                foreach (var kv in r.classRelations) dto.classRelations.Add(new IntFloatEntry((int)kv.Key, kv.Value));
            if (r.coreTiles != null)
                foreach (int t in r.coreTiles) dto.coreTiles.Add(t);
            if (r.claimedTiles != null)
                foreach (int t in r.claimedTiles) dto.claimedTiles.Add(t);
            if (r.vassalIds != null)
                dto.vassalIds.AddRange(r.vassalIds);
            return dto;
        }

        public RealmData ToRealmData()
        {
            // 构造函数会初始化 classRelations 默认值与 taxSystem/currencySystem，随后整体覆盖
            var r = new RealmData
            {
                realmId = realmId,
                realmName = realmName,
                composition = composition,
                treasury = treasury,
                prestige = prestige,
                stability = stability,
                centralization = centralization,
                taxSystem = taxSystem.ToTaxSystem(),
                currencySystem = currencySystem,
                suzerainId = suzerainId
            };
            r.classRelations.Clear();
            foreach (var e in classRelations) r.classRelations[(GameEnums.SocialClass)e.key] = e.value;
            r.coreTiles.Clear();
            foreach (int t in coreTiles) r.coreTiles.Add(t);
            r.claimedTiles.Clear();
            foreach (int t in claimedTiles) r.claimedTiles.Add(t);
            r.vassalIds.Clear();
            r.vassalIds.AddRange(vassalIds);
            return r;
        }
    }

    /// <summary>贸易中心存档 DTO（库存/供需字典转 List 包装）</summary>
    [Serializable]
    public class TradeCenterDTO
    {
        public int regionId;
        public string centerName;
        public int centerTileIndex;
        public List<IntFloatEntry> inventory = new List<IntFloatEntry>();
        public float inventoryCapacity;
        public List<TradeRoute> tradeRoutes = new List<TradeRoute>();
        public List<IntFloatEntry> localDemand = new List<IntFloatEntry>();
        public List<IntFloatEntry> localSupply = new List<IntFloatEntry>();

        public static TradeCenterDTO FromTradeCenter(TradeCenter tc)
        {
            var dto = new TradeCenterDTO
            {
                regionId = tc.regionId,
                centerName = tc.centerName,
                centerTileIndex = tc.centerTileIndex,
                inventoryCapacity = tc.inventoryCapacity,
                tradeRoutes = tc.tradeRoutes
            };
            if (tc.inventory != null)
                foreach (var kv in tc.inventory) dto.inventory.Add(new IntFloatEntry(kv.Key, kv.Value));
            if (tc.localDemand != null)
                foreach (var kv in tc.localDemand) dto.localDemand.Add(new IntFloatEntry(kv.Key, kv.Value));
            if (tc.localSupply != null)
                foreach (var kv in tc.localSupply) dto.localSupply.Add(new IntFloatEntry(kv.Key, kv.Value));
            return dto;
        }

        public TradeCenter ToTradeCenter()
        {
            var tc = new TradeCenter
            {
                regionId = regionId,
                centerName = centerName,
                centerTileIndex = centerTileIndex,
                inventoryCapacity = inventoryCapacity,
                tradeRoutes = tradeRoutes
            };
            foreach (var e in inventory) tc.inventory[e.key] = e.value;
            foreach (var e in localDemand) tc.localDemand[e.key] = e.value;
            foreach (var e in localSupply) tc.localSupply[e.key] = e.value;
            return tc;
        }
    }
}
