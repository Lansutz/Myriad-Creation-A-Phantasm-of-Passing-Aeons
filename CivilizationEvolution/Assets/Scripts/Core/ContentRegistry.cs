using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Race;
using CivilizationEvolution.Role;
using CivilizationEvolution.Tech;

namespace CivilizationEvolution.Core
{
    /// <summary>
    /// 内容注册表（数据驱动架构，企划书 1.2 模组扩展规范）
    /// 启动时扫描 StreamingAssets/Base（内置内容）与 StreamingAssets/Mods（模组内容），目录同构：
    ///   Culture/&lt;文化包名&gt;/CultureData.json + 名字池CSV（CharacterFirstNames_Male/Female.csv、CharacterLastNames.csv、CityNames.csv）
    ///   Race/RaceDefs.json（顶层包装 { "races": [...] }）
    ///   Ethos/Ethos.json（顶层包装 { "ethos": [...] }——族群精神定义表）
    ///   Tradition/Traditions.json（顶层包装 { "traditions": [...] }——文化传统定义表）
    ///   FamilyTradition/FamilyTraditions.json（顶层包装 { "familyTraditions": [...] }——家族传统定义表）
    ///   CharacterTemplate/CharacterTemplates.json（顶层包装 { "templates": [...] }——角色生成模板表）
    ///   Dna/DnaDefs.json（顶层包装 { "defs": [...] }——DNA 天赋/遗传病定义表，模组可扩展）
    ///   MentalHealth/MentalHealthDefs.json（顶层包装 { "disorders": [...] }——精神疾病定义表，模组可扩展）
    ///   Innovation/Innovations.json（顶层包装 { "innovations": [...] }——革新定义表，两级分类：大类+子类，模组可扩展）
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

        [Serializable]
        private class FamilyTraditionsWrapper
        {
            public List<FamilyTraditionDef> familyTraditions = new List<FamilyTraditionDef>();
        }

        [Serializable]
        private class CharacterTemplatesWrapper
        {
            public List<CharacterTemplateDef> templates = new List<CharacterTemplateDef>();
        }

        [Serializable]
        private class DnaDefsWrapper
        {
            public List<TalentDefectDef> defs = new List<TalentDefectDef>();
        }

        [Serializable]
        private class MentalHealthDefsWrapper
        {
            public List<MentalDisorderDef> disorders = new List<MentalDisorderDef>();
        }

        [Serializable]
        private class InnovationsWrapper
        {
            public List<InnovationDef> innovations = new List<InnovationDef>();
        }

        public static Dictionary<int, CultureContentPack> Cultures { get; private set; } = new Dictionary<int, CultureContentPack>();
        public static Dictionary<int, RaceData> Races { get; private set; } = new Dictionary<int, RaceData>();

        // ===== 模组化定义表（族群/族群精神/文化传统/语言/家族传统/角色模板，按 Id 覆盖） =====
        public static Dictionary<string, EthosDef> Ethos { get; private set; } = new Dictionary<string, EthosDef>();
        public static Dictionary<string, TraditionDef> Traditions { get; private set; } = new Dictionary<string, TraditionDef>();
        public static Dictionary<string, LanguageDef> Languages { get; private set; } = new Dictionary<string, LanguageDef>();
        public static Dictionary<string, EthnicGroupDef> EthnicGroups { get; private set; } = new Dictionary<string, EthnicGroupDef>();
        public static Dictionary<string, FamilyTraditionDef> FamilyTraditions { get; private set; } = new Dictionary<string, FamilyTraditionDef>();
        public static Dictionary<string, CharacterTemplateDef> CharacterTemplates { get; private set; } = new Dictionary<string, CharacterTemplateDef>();
        public static Dictionary<string, TalentDefectDef> TalentDefects { get; private set; } = new Dictionary<string, TalentDefectDef>();
        public static Dictionary<string, MentalDisorderDef> MentalDisorders { get; private set; } = new Dictionary<string, MentalDisorderDef>();
        public static Dictionary<int, InnovationDef> Innovations { get; private set; } = new Dictionary<int, InnovationDef>();
        public static Dictionary<int, ReligionDef> Religions { get; private set; } = new Dictionary<int, ReligionDef>();
        public static Dictionary<string, DoctrineOptionDef> Doctrines { get; private set; } = new Dictionary<string, DoctrineOptionDef>();

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
            FamilyTraditions = new Dictionary<string, FamilyTraditionDef>();
            CharacterTemplates = new Dictionary<string, CharacterTemplateDef>();
            TalentDefects = new Dictionary<string, TalentDefectDef>();
            MentalDisorders = new Dictionary<string, MentalDisorderDef>();
            Innovations = new Dictionary<int, InnovationDef>();

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
                $"族群精神 {Ethos.Count}，文化传统 {Traditions.Count}，语言 {Languages.Count}，族群 {EthnicGroups.Count}，" +
                $"家族传统 {FamilyTraditions.Count}，角色模板 {CharacterTemplates.Count}，DNA 天赋缺陷 {TalentDefects.Count}，" +
                $"精神疾病 {MentalDisorders.Count}，革新 {Innovations.Count}");
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
            FamilyTraditions.Clear();
            CharacterTemplates.Clear();
            TalentDefects.Clear();
            MentalDisorders.Clear();
            Innovations.Clear();
        }

        // ===== 查询接口 =====

        public static bool TryGetCulture(int id, out CultureContentPack pack) => Cultures.TryGetValue(id, out pack);
        public static bool TryGetRace(int id, out RaceData race) => Races.TryGetValue(id, out race);
        public static bool TryGetEthos(string id, out EthosDef def) => Ethos.TryGetValue(id, out def);
        public static bool TryGetTradition(string id, out TraditionDef def) => Traditions.TryGetValue(id, out def);
        public static bool TryGetLanguage(string id, out LanguageDef def) => Languages.TryGetValue(id, out def);
        public static bool TryGetEthnicGroup(string id, out EthnicGroupDef def) => EthnicGroups.TryGetValue(id, out def);
        public static bool TryGetFamilyTradition(string id, out FamilyTraditionDef def) => FamilyTraditions.TryGetValue(id, out def);
        public static bool TryGetCharacterTemplate(string id, out CharacterTemplateDef def) => CharacterTemplates.TryGetValue(id, out def);
        public static bool TryGetTalentDefect(string id, out TalentDefectDef def) => TalentDefects.TryGetValue(id, out def);
        public static bool TryGetMentalDisorder(string id, out MentalDisorderDef def) => MentalDisorders.TryGetValue(id, out def);
        public static bool TryGetInnovation(int id, out InnovationDef def) => Innovations.TryGetValue(id, out def);

        /// <summary>
        /// 从文化包提取随机名字（type: 0男名 1女名 2姓氏 3城名，可传 null 随机池）
        /// 2026-09-03 升级：语言池优先（文化→languageId→LanguageDef 男/女/姓/城池——
        /// 同语言文化共享名字——模组化）——空回退文化包旧池（兼容旧数据）
        /// </summary>
        public static string GetRandomName(CultureContentPack pack, int type, System.Random rng = null)
        {
            rng = rng ?? new System.Random();
            List<string> list = null;

            // 语言池优先（文化挂语言——语言内名字池）
            if (pack != null && pack.data != null && !string.IsNullOrEmpty(pack.data.languageId)
                && TryGetLanguage(pack.data.languageId, out var lang))
            {
                list = type switch
                {
                    1 => lang.femaleNames,
                    2 => lang.familyNames,
                    3 => lang.cityNames,
                    _ => lang.maleNames
                };
            }
            if (list == null || list.Count == 0)
            {
                // 回退文化包旧池（兼容）
                if (pack == null) return "无名";
                list = type switch
                {
                    1 => pack.names.femaleNames,
                    2 => pack.names.lastNames,
                    3 => pack.names.cityNames,
                    _ => pack.names.maleNames
                };
                if (list.Count == 0 && type != 2) list = pack.names.lastNames;
                if (list.Count == 0) return pack.data.cultureName;
            }
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

            string religionFile = Path.Combine(root, "Religion", "Religions.json");
            if (File.Exists(religionFile))
            {
                try { LoadReligions(religionFile); }
                catch (Exception e) { Debug.LogWarning($"[ContentRegistry] 宗教定义加载失败：{e.Message}"); }
            }

            string doctrineFile = Path.Combine(root, "Religion", "Doctrines.json");
            if (File.Exists(doctrineFile))
            {
                try { LoadDoctrines(doctrineFile); }
                catch (Exception e) { Debug.LogWarning($"[ContentRegistry] 教义池加载失败：{e.Message}"); }
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

            string familyTraditionFile = Path.Combine(root, "FamilyTradition", "FamilyTraditions.json");
            if (File.Exists(familyTraditionFile))
            {
                try { LoadFamilyTraditions(familyTraditionFile); }
                catch (Exception e) { Debug.LogWarning($"[ContentRegistry] 家族传统定义加载失败：{e.Message}"); }
            }

            string characterTemplateFile = Path.Combine(root, "CharacterTemplate", "CharacterTemplates.json");
            if (File.Exists(characterTemplateFile))
            {
                try { LoadCharacterTemplates(characterTemplateFile); }
                catch (Exception e) { Debug.LogWarning($"[ContentRegistry] 角色模板定义加载失败：{e.Message}"); }
            }

            string dnaDefsFile = Path.Combine(root, "Dna", "DnaDefs.json");
            if (File.Exists(dnaDefsFile))
            {
                try { LoadDnaDefs(dnaDefsFile); }
                catch (Exception e) { Debug.LogWarning($"[ContentRegistry] DNA 天赋缺陷定义加载失败：{e.Message}"); }
            }

            string mentalHealthFile = Path.Combine(root, "MentalHealth", "MentalHealthDefs.json");
            if (File.Exists(mentalHealthFile))
            {
                try { LoadMentalHealthDefs(mentalHealthFile); }
                catch (Exception e) { Debug.LogWarning($"[ContentRegistry] 精神疾病定义加载失败：{e.Message}"); }
            }

            string innovationFile = Path.Combine(root, "Innovation", "Innovations.json");
            if (File.Exists(innovationFile))
            {
                try { LoadInnovations(innovationFile); }
                catch (Exception e) { Debug.LogWarning($"[ContentRegistry] 革新定义加载失败：{e.Message}"); }
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
            // 语言级名字池 CSV（2026-09-03 升级：同语言目录可选
            // CharacterFirstNames_Male/Female.csv+LastNames+CityNames——
            // 语言共享名字——模组化；文化级 CSV 仍兼容[回退链]）
            PackCsv(def.maleNames, Path.Combine(dir, "CharacterFirstNames_Male.csv"));
            PackCsv(def.femaleNames, Path.Combine(dir, "CharacterFirstNames_Female.csv"));
            PackCsv(def.familyNames, Path.Combine(dir, "CharacterLastNames.csv"));
            PackCsv(def.cityNames, Path.Combine(dir, "CityNames.csv"));
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

        /// <summary>加载家族传统（FamilyTradition）定义表（企划书 9.4：家族团结度/家法/家族文化偏移）</summary>
        private static void LoadFamilyTraditions(string path)
        {
            var wrapper = JsonUtility.FromJson<FamilyTraditionsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.familyTraditions == null) return;
            foreach (var def in wrapper.familyTraditions)
            {
                if (def == null || string.IsNullOrEmpty(def.traditionId)) continue;
                FamilyTraditions[def.traditionId] = def;
            }
        }

        /// <summary>加载角色模板（CharacterTemplate）定义表（第九篇角色生成参数模板）</summary>
        private static void LoadCharacterTemplates(string path)
        {
            var wrapper = JsonUtility.FromJson<CharacterTemplatesWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.templates == null) return;
            foreach (var def in wrapper.templates)
            {
                if (def == null || string.IsNullOrEmpty(def.templateId)) continue;
                CharacterTemplates[def.templateId] = def;
            }
        }

        /// <summary>加载 DNA 天赋/遗传病（TalentDefect）定义表（DNA 文档：模组可新增天赋/遗传病列表）</summary>
        private static void LoadDnaDefs(string path)
        {
            var wrapper = JsonUtility.FromJson<DnaDefsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.defs == null) return;
            foreach (var def in wrapper.defs)
            {
                if (def == null || string.IsNullOrEmpty(def.id)) continue;
                TalentDefects[def.id] = def;
            }
        }

        /// <summary>加载精神疾病（MentalDisorder）定义表（模组可新增疾病类型）</summary>
        private static void LoadMentalHealthDefs(string path)
        {
            var wrapper = JsonUtility.FromJson<MentalHealthDefsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.disorders == null) return;
            foreach (var def in wrapper.disorders)
            {
                if (def == null || string.IsNullOrEmpty(def.id)) continue;
                MentalDisorders[def.id] = def;
            }
        }

        /// <summary>加载宗教定义（三级谱系：宗教→宗派→传统）</summary>
        private static void LoadReligions(string path)
        {
            var wrapper = JsonUtility.FromJson<ReligionListWrapper>(File.ReadAllText(path));
            Religions.Clear();
            if (wrapper == null || wrapper.religions == null) return;
            foreach (var r in wrapper.religions)
                if (r != null) Religions[r.religionId] = r;
            ReligionCatalog.Load(new List<ReligionDef>(Religions.Values));
            ReligionCatalog.EnsureColors();
        }

        [System.Serializable]
        private class ReligionListWrapper
        {
            public List<ReligionDef> religions = new List<ReligionDef>();
        }

        /// <summary>加载教义池（七支柱选项——中性词汇+宗教专属风味化）</summary>
        private static void LoadDoctrines(string path)
        {
            var wrapper = JsonUtility.FromJson<DoctrineListWrapper>(File.ReadAllText(path));
            Doctrines.Clear();
            if (wrapper == null || wrapper.doctrines == null) return;
            foreach (var d in wrapper.doctrines)
                if (d != null && !string.IsNullOrEmpty(d.optionId))
                    Doctrines[d.optionId] = d;
            DoctrinePool.Load(new List<DoctrineOptionDef>(Doctrines.Values));
        }

        [System.Serializable]
        private class DoctrineListWrapper
        {
            public List<DoctrineOptionDef> doctrines = new List<DoctrineOptionDef>();
        }

        /// <summary>加载革新（Innovation）定义表（两级分类：大类+子类；模组可新增）</summary>
        private static void LoadInnovations(string path)
        {
            var wrapper = JsonUtility.FromJson<InnovationsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.innovations == null) return;
            foreach (var def in wrapper.innovations)
            {
                if (def == null || def.innovationId <= 0) continue;
                Innovations[def.innovationId] = def;
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