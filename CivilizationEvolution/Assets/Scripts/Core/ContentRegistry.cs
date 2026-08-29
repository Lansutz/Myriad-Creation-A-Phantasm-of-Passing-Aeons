using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Race;

namespace CivilizationEvolution.Core
{
    /// <summary>
    /// 内容注册表（数据驱动架构，企划书 1.2 模组扩展规范）
    /// 启动时扫描 StreamingAssets/Base（内置内容）与 StreamingAssets/Mods（模组内容），目录同构：
    ///   Culture/&lt;文化包名&gt;/CultureData.json + 名字池CSV（CharacterFirstNames_Male/Female.csv、CharacterLastNames.csv、CityNames.csv）
    ///   Race/RaceDefs.json（顶层包装 { "races": [...] }）
    ///   Ethos/Ethos.json（顶层包装 { "ethos": [...] }——族群精神定义表）
    ///   Tradition/Traditions.json（顶层包装 { "traditions": [...] }——文化传统定义表）
    ///   Language/&lt;语言名&gt;/Language.json（语言定义）
    ///   EthnicGroup/EthnicGroups.json（顶层包装 { "groups": [...] }——族群实体）
    /// Mods 与 Base 同名 Id 后者覆盖，实现内容热扩展。
    /// 使用 File/Directory 直读，适用于 Standalone；WebGL 等沙箱平台加载失败时注册表为空（不崩溃）。
    /// </summary>
    public static class ContentRegistry
    {
        /// <summary>文化名字池（五个 CSV 汇总）</summary>
        [Serializable]
        public class NamePoolData
        {
            public List<string> maleNames = new List<string>();
            public List<string> femaleNames = new List<string>();
            public List<string> lastNames = new List<string>();
            public List<string> cityNames = new List<string>();
        }

        /// <summary>文化内容包：文化数据 + 名字池 + 包路径</summary>
        public class CultureContentPack
        {
            public CultureData data;
            public NamePoolData names = new NamePoolData();
            public string packagePath = "";
        }

        [Serializable]
        private class RaceDefsWrapper
        {
            public List<RaceData> races = new List<RaceData>();
        }

        [Serializable]
        private class EthosWrapper
        {
            public List<EthosDef> ethos = new List<EthosDef>();
        }

        [Serializable]
        private class TraditionsWrapper
        {
            public List<TraditionDef> traditions = new List<TraditionDef>();
        }

        [Serializable]
        private class EthnicGroupsWrapper
        {
            public List<EthnicGroupDef> groups = new List<EthnicGroupDef>();
        }

        public static Dictionary<int, CultureContentPack> Cultures { get; private set; } = new Dictionary<int, CultureContentPack>();
        public static Dictionary<int, RaceData> Races { get; private set; } = new Dictionary<int, RaceData>();

        // ===== 模组化定义表（族群/族群精神/文化传统/语言，按 Id 覆盖） =====
        public static Dictionary<string, EthosDef> Ethos { get; private set; } = new Dictionary<string, EthosDef>();
        public static Dictionary<string, TraditionDef> Traditions { get; private set; } = new Dictionary<string, TraditionDef>();
        public static Dictionary<string, LanguageDef> Languages { get; private set; } = new Dictionary<string, LanguageDef>();
        public static Dictionary<string, EthnicGroupDef> EthnicGroups { get; private set; } = new Dictionary<string, EthnicGroupDef>();

        public static bool IsInitialized { get; private set; } = false;

        /// <summary>初始化内容注册表（幂等，可重复调用）</summary>
        public static void Initialize()
        {
            if (IsInitialized) return;
            Cultures = new Dictionary<int, CultureContentPack>();
            Races = new Dictionary<int, RaceData>();
            Ethos = new Dictionary<string, EthosDef>();
            Traditions = new Dictionary<string, TraditionDef>();
            Languages = new Dictionary<string, LanguageDef>();
            EthnicGroups = new Dictionary<string, EthnicGroupDef>();

            string root = Application.streamingAssetsPath;
            if (!Directory.Exists(root))
            {
                Debug.LogWarning($"[ContentRegistry] 未找到 StreamingAssets 目录：{root}");
            }
            else
            {
                LoadContentRoot(Path.Combine(root, "Base"));
                LoadContentRoot(Path.Combine(root, "Mods")); // Mods 后载，覆盖同名
            }

            IsInitialized = true;
            Debug.Log($"[ContentRegistry] 内容加载完成：文化 {Cultures.Count}，种族 {Races.Count}，" +
                $"族群精神 {Ethos.Count}，文化传统 {Traditions.Count}，语言 {Languages.Count}，族群 {EthnicGroups.Count}");
        }

        /// <summary>重置注册表（编辑器重载数据时使用）</summary>
        public static void Reset()
        {
            IsInitialized = false;
            Cultures.Clear();
            Races.Clear();
            Ethos.Clear();
            Traditions.Clear();
            Languages.Clear();
            EthnicGroups.Clear();
        }

        // ===== 查询接口 =====

        public static bool TryGetCulture(int id, out CultureContentPack pack) => Cultures.TryGetValue(id, out pack);
        public static bool TryGetRace(int id, out RaceData race) => Races.TryGetValue(id, out race);
        public static bool TryGetEthos(string id, out EthosDef def) => Ethos.TryGetValue(id, out def);
        public static bool TryGetTradition(string id, out TraditionDef def) => Traditions.TryGetValue(id, out def);
        public static bool TryGetLanguage(string id, out LanguageDef def) => Languages.TryGetValue(id, out def);
        public static bool TryGetEthnicGroup(string id, out EthnicGroupDef def) => EthnicGroups.TryGetValue(id, out def);

        /// <summary>从文化包提取随机名字（type: 0男名 1女名 2姓氏 3城名，可传 null 随机池）</summary>
        public static string GetRandomName(CultureContentPack pack, int type, System.Random rng = null)
        {
            rng = rng ?? new System.Random();
            var list = type switch
            {
                1 => pack.names.femaleNames,
                2 => pack.names.lastNames,
                3 => pack.names.cityNames,
                _ => pack.names.maleNames
            };
            // 空池回退姓氏或包名
            if (list.Count == 0 && type != 2) list = pack.names.lastNames;
            if (list.Count == 0) return pack.data.cultureName;
            return list[rng.Next(list.Count)];
        }

        // ===== 内容扫描 =====

        /// <summary>扫描一个内容根（Base 或 Mods）</summary>
        private static void LoadContentRoot(string root)
        {
            if (!Directory.Exists(root)) return;

            string cultureDir = Path.Combine(root, "Culture");
            if (Directory.Exists(cultureDir))
            {
                foreach (var dir in Directory.GetDirectories(cultureDir))
                {
                    try { LoadCulturePack(dir); }
                    catch (Exception e) { Debug.LogWarning($"[ContentRegistry] 文化包 {Path.GetFileName(dir)} 加载失败：{e.Message}"); }
                }
            }

            string raceFile = Path.Combine(root, "Race", "RaceDefs.json");
            if (File.Exists(raceFile))
            {
                try { LoadRaceDefs(raceFile); }
                catch (Exception e) { Debug.LogWarning($"[ContentRegistry] 种族定义加载失败：{e.Message}"); }
            }

            // ===== 模组化定义表 =====
            string ethosFile = Path.Combine(root, "Ethos", "Ethos.json");
            if (File.Exists(ethosFile))
            {
                try { LoadEthos(ethosFile); }
                catch (Exception e) { Debug.LogWarning($"[ContentRegistry] 族群精神定义加载失败：{e.Message}"); }
            }

            string traditionFile = Path.Combine(root, "Tradition", "Traditions.json");
            if (File.Exists(traditionFile))
            {
                try { LoadTraditions(traditionFile); }
                catch (Exception e) { Debug.LogWarning($"[ContentRegistry] 文化传统定义加载失败：{e.Message}"); }
            }

            string languageDir = Path.Combine(root, "Language");
            if (Directory.Exists(languageDir))
            {
                foreach (var dir in Directory.GetDirectories(languageDir))
                {
                    try { LoadLanguage(dir); }
                    catch (Exception e) { Debug.LogWarning($"[ContentRegistry] 语言包 {Path.GetFileName(dir)} 加载失败：{e.Message}"); }
                }
            }

            string groupFile = Path.Combine(root, "EthnicGroup", "EthnicGroups.json");
            if (File.Exists(groupFile))
            {
                try { LoadEthnicGroups(groupFile); }
                catch (Exception e) { Debug.LogWarning($"[ContentRegistry] 族群定义加载失败：{e.Message}"); }
            }
        }

        /// <summary>加载单个文化包目录</summary>
        private static void LoadCulturePack(string dir)
        {
            string defFile = Path.Combine(dir, "CultureData.json");
            if (!File.Exists(defFile)) return;

            var data = JsonUtility.FromJson<CultureData>(File.ReadAllText(defFile));
            if (data == null || data.cultureId <= 0)
            {
                Debug.LogWarning($"[ContentRegistry] 文化包 {Path.GetFileName(dir)} 定义无效（cultureId 缺失）");
                return;
            }

            var pack = new CultureContentPack { data = data, packagePath = dir };
            PackCsv(pack.names.maleNames, Path.Combine(dir, "CharacterFirstNames_Male.csv"));
            PackCsv(pack.names.femaleNames, Path.Combine(dir, "CharacterFirstNames_Female.csv"));
            PackCsv(pack.names.lastNames, Path.Combine(dir, "CharacterLastNames.csv"));
            PackCsv(pack.names.cityNames, Path.Combine(dir, "CityNames.csv"));

            bool overwritten = Cultures.ContainsKey(data.cultureId);
            Cultures[data.cultureId] = pack;
            if (overwritten)
                Debug.Log($"[ContentRegistry] 文化 [{data.cultureName}] 被 Mods 覆盖");
        }

        /// <summary>加载种族定义文件</summary>
        private static void LoadRaceDefs(string path)
        {
            var wrapper = JsonUtility.FromJson<RaceDefsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.races == null) return;
            foreach (var race in wrapper.races)
            {
                if (race == null || race.raceId <= 0) continue;
                Races[race.raceId] = race;
            }
        }

        /// <summary>加载族群精神（Ethos）定义表</summary>
        private static void LoadEthos(string path)
        {
            var wrapper = JsonUtility.FromJson<EthosWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.ethos == null) return;
            foreach (var def in wrapper.ethos)
            {
                if (def == null || string.IsNullOrEmpty(def.ethosId)) continue;
                Ethos[def.ethosId] = def;
            }
        }

        /// <summary>加载文化传统（Tradition）定义表</summary>
        private static void LoadTraditions(string path)
        {
            var wrapper = JsonUtility.FromJson<TraditionsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.traditions == null) return;
            foreach (var def in wrapper.traditions)
            {
                if (def == null || string.IsNullOrEmpty(def.traditionId)) continue;
                Traditions[def.traditionId] = def;
            }
        }

        /// <summary>加载单个语言包目录（Language/&lt;语言名&gt;/Language.json）</summary>
        private static void LoadLanguage(string dir)
        {
            string defFile = Path.Combine(dir, "Language.json");
            if (!File.Exists(defFile)) return;
            var def = JsonUtility.FromJson<LanguageDef>(File.ReadAllText(defFile));
            if (def == null || string.IsNullOrEmpty(def.languageId))
            {
                Debug.LogWarning($"[ContentRegistry] 语言包 {Path.GetFileName(dir)} 定义无效（languageId 缺失）");
                return;
            }
            Languages[def.languageId] = def;
        }

        /// <summary>加载族群（EthnicGroup）定义</summary>
        private static void LoadEthnicGroups(string path)
        {
            var wrapper = JsonUtility.FromJson<EthnicGroupsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.groups == null) return;
            foreach (var def in wrapper.groups)
            {
                if (def == null || string.IsNullOrEmpty(def.groupId)) continue;
                EthnicGroups[def.groupId] = def;
            }
        }

        /// <summary>解析名字池 CSV（格式：id,name，支持 # 注释行与空行）</summary>
        private static void PackCsv(List<string> target, string path)
        {
            if (!File.Exists(path)) return;
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int comma = line.IndexOf(',');
                string name = (comma >= 0 ? line.Substring(comma + 1) : line).Trim();
                if (name.Length > 0) target.Add(name);
            }
        }
    }
}