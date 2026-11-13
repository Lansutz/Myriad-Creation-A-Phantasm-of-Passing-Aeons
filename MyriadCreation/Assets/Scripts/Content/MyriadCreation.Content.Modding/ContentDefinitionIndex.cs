using System;
using System.Collections.Generic;
using System.IO;
using MyriadCreation.Content.Definitions;
using UnityEngine;

namespace MyriadCreation.Content.Modding
{
    /// <summary>
    /// P2：纯内容定义索引（Content 程序集，只依赖 Core/Content.Definitions）。
    /// 承载原 ContentRuntime 中 Ethos/Heritage/Tradition/Language/EthnicGroup/FamilyTradition/
    /// CharacterTemplate/TalentDefect/MentalDisorder/Innovation/Religion/Doctrine/Title 的 JSON 解析与索引；
    /// 不引用 RaceData/CultureData/BiomeDef/Simulation/World 类型（运行时映射仍留 host facade）。
    /// 无静态构造/自动初始化：由 host ContentRegistry 显式调用 Initialize/Reset，保持原有静态初始化顺序。
    /// </summary>
    public sealed class ContentDefinitionIndex
    {
        /// <summary>Innovation 兜底条目（1/1025）需要的 discoveryGroups 字符串；host 传入 DiscoveryGroups 常量，避免 Content 引用 Simulation.State。</summary>
        public readonly struct InnovationFallbackGroups
        {
            public readonly string merchant;
            public readonly string scholar;
            public readonly string artisan;

            public InnovationFallbackGroups(string merchant, string scholar, string artisan)
            {
                this.merchant = merchant;
                this.scholar = scholar;
                this.artisan = artisan;
            }

            public static InnovationFallbackGroups Default
            {
                get { return new InnovationFallbackGroups("merchant", "scholar", "artisan"); }
            }
        }

        public Dictionary<string, EthosDef> Ethos { get; } = new Dictionary<string, EthosDef>();
        public Dictionary<string, HeritageDef> Heritages { get; } = new Dictionary<string, HeritageDef>();
        public Dictionary<string, TraditionDef> Traditions { get; } = new Dictionary<string, TraditionDef>();
        public Dictionary<string, LanguageDef> Languages { get; } = new Dictionary<string, LanguageDef>();
        public Dictionary<string, EthnicGroupDef> EthnicGroups { get; } = new Dictionary<string, EthnicGroupDef>();
        public Dictionary<string, FamilyTraditionDef> FamilyTraditions { get; } = new Dictionary<string, FamilyTraditionDef>();
        public Dictionary<string, CharacterTemplateDef> CharacterTemplates { get; } = new Dictionary<string, CharacterTemplateDef>();
        public Dictionary<string, TalentDefectDef> TalentDefects { get; } = new Dictionary<string, TalentDefectDef>();
        public Dictionary<string, MentalDisorderDef> MentalDisorders { get; } = new Dictionary<string, MentalDisorderDef>();
        public Dictionary<int, InnovationDef> Innovations { get; } = new Dictionary<int, InnovationDef>();
        public Dictionary<int, ReligionDef> Religions { get; } = new Dictionary<int, ReligionDef>();
        public Dictionary<string, DoctrineOptionDef> Doctrines { get; } = new Dictionary<string, DoctrineOptionDef>();
        public Dictionary<string, TitleDef> Titles { get; } = new Dictionary<string, TitleDef>();
        public Dictionary<int, EquipmentDef> Equipment { get; } = new Dictionary<int, EquipmentDef>();

        public bool IsInitialized { get; private set; }

        private InnovationFallbackGroups _fallback = InnovationFallbackGroups.Default;

        // ===== JSON wrappers（从 ContentRuntime 原样搬入，仅纯定义）=====

        [Serializable]
        private class EthosWrapper
        {
            public List<EthosDef> ethos = new List<EthosDef>();
        }

        [Serializable]
        private class HeritagesWrapper
        {
            public List<HeritageDef> heritages = new List<HeritageDef>();
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
        private class EquipmentWrapper
        {
            public List<EquipmentDef> equipment = new List<EquipmentDef>();
        }

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

        [Serializable]
        private class TitlesWrapper
        {
            public List<TitleDef> titles = new List<TitleDef>();
        }

        [Serializable]
        private class ReligionListWrapper
        {
            public List<ReligionDef> religions = new List<ReligionDef>();
        }

        [Serializable]
        private class EpithetListWrapper
        {
            public List<EpithetDef> epithets = new List<EpithetDef>();
        }

        [Serializable]
        private class DoctrineListWrapper
        {
            public List<DoctrineOptionDef> doctrines = new List<DoctrineOptionDef>();
        }

        /// <summary>
        /// 按 ContentSourceCatalog（Base→Mods 优先级）解析全部纯定义；root 为空/不存在/坏 JSON 安全（沿用原 provider 的 try/catch 行为）。
        /// </summary>
        public void Initialize(string streamingAssetsRoot, InnovationFallbackGroups fallback)
        {
            _fallback = string.IsNullOrEmpty(fallback.merchant) || string.IsNullOrEmpty(fallback.scholar) || string.IsNullOrEmpty(fallback.artisan)
                ? InnovationFallbackGroups.Default
                : fallback;

            ClearDictionaries();

            var providerCatalog = new ContentProviderCatalog();
            providerCatalog.Add(new DelegateContentProvider<string, EthosDef>("Ethos", LoadEthosRoot), new DictionaryContentStore<string, EthosDef>(Ethos));
            providerCatalog.Add(new DelegateContentProvider<string, HeritageDef>("Heritage", LoadHeritageRoot), new DictionaryContentStore<string, HeritageDef>(Heritages));
            providerCatalog.Add(new DelegateContentProvider<string, TraditionDef>("Tradition", LoadTraditionRoot), new DictionaryContentStore<string, TraditionDef>(Traditions));
            providerCatalog.Add(new DelegateContentProvider<string, LanguageDef>("Language", LoadLanguageRoot), new DictionaryContentStore<string, LanguageDef>(Languages));
            providerCatalog.Add(new DelegateContentProvider<string, EthnicGroupDef>("EthnicGroup", LoadEthnicGroupRoot), new DictionaryContentStore<string, EthnicGroupDef>(EthnicGroups));
            providerCatalog.Add(new DelegateContentProvider<string, FamilyTraditionDef>("FamilyTradition", LoadFamilyTraditionRoot), new DictionaryContentStore<string, FamilyTraditionDef>(FamilyTraditions));
            providerCatalog.Add(new DelegateContentProvider<string, CharacterTemplateDef>("CharacterTemplate", LoadCharacterTemplateRoot), new DictionaryContentStore<string, CharacterTemplateDef>(CharacterTemplates));
            providerCatalog.Add(new DelegateContentProvider<string, TalentDefectDef>("Dna", LoadDnaRoot), new DictionaryContentStore<string, TalentDefectDef>(TalentDefects));
            providerCatalog.Add(new DelegateContentProvider<string, MentalDisorderDef>("MentalHealth", LoadMentalHealthRoot), new DictionaryContentStore<string, MentalDisorderDef>(MentalDisorders));
            providerCatalog.Add(new DelegateContentProvider<int, InnovationDef>("Innovation", LoadInnovationRoot), new DictionaryContentStore<int, InnovationDef>(Innovations));
            providerCatalog.Add(new DelegateContentProvider<int, ReligionDef>("Religion", LoadReligionRoot), new DictionaryContentStore<int, ReligionDef>(Religions));
            providerCatalog.Add(new DelegateContentProvider<string, DoctrineOptionDef>("Doctrine", LoadDoctrineRoot), new DictionaryContentStore<string, DoctrineOptionDef>(Doctrines));
            providerCatalog.Add(new DelegateContentProvider<string, TitleDef>("Title", LoadTitleRoot), new DictionaryContentStore<string, TitleDef>(Titles));
            providerCatalog.Add(new DelegateContentProvider<int, EquipmentDef>("Equipment", LoadEquipmentRoot), new DictionaryContentStore<int, EquipmentDef>(Equipment));
            providerCatalog.Add(new DelegateContentProvider<string, EpithetDef>("Epithet", LoadEpithetRoot), new DictionaryContentStore<string, EpithetDef>(new Dictionary<string, EpithetDef>()));

            foreach (ContentSource source in ContentSourceCatalog.CreateDefault(streamingAssetsRoot))
            {
                if (source.Exists)
                {
                    providerCatalog.Load(source);
                }
            }

            IsInitialized = true;
        }

        /// <summary>清空所有定义索引并复位 IsInitialized（host Reset 调用；不触发 Content 静态状态）。</summary>
        public void Reset()
        {
            ClearDictionaries();
            IsInitialized = false;
        }

        private void ClearDictionaries()
        {
            Ethos.Clear();
            Heritages.Clear();
            Traditions.Clear();
            Languages.Clear();
            EthnicGroups.Clear();
            FamilyTraditions.Clear();
            CharacterTemplates.Clear();
            TalentDefects.Clear();
            MentalDisorders.Clear();
            Innovations.Clear();
            Religions.Clear();
            Doctrines.Clear();
            Titles.Clear();
            Equipment.Clear();
            EpithetCatalog.ResetContent();
        }

        // ===== LoadRoot（按 provider 根目录解析）=====

        private void LoadEthosRoot(string r, IContentStore<string, EthosDef> t)
        {
            string path = Path.Combine(r, "Ethos/Ethos.json");
            if (File.Exists(path)) LoadEthos(path, t);
        }

        private void LoadHeritageRoot(string r, IContentStore<string, HeritageDef> t)
        {
            string path = Path.Combine(r, "Heritage/Heritages.json");
            if (File.Exists(path)) LoadHeritages(path, t);
        }

        private void LoadTraditionRoot(string r, IContentStore<string, TraditionDef> t)
        {
            string path = Path.Combine(r, "Tradition/Traditions.json");
            if (File.Exists(path)) LoadTraditions(path, t);
        }

        private void LoadLanguageRoot(string r, IContentStore<string, LanguageDef> t)
        {
            string path = Path.Combine(r, "Language");
            if (!Directory.Exists(path)) return;
            string[] directories = Directory.GetDirectories(path);
            for (int i = 0; i < directories.Length; i++)
            {
                LoadLanguage(directories[i], t);
            }
        }

        private void LoadEthnicGroupRoot(string r, IContentStore<string, EthnicGroupDef> t)
        {
            string path = Path.Combine(r, "EthnicGroup/EthnicGroups.json");
            if (File.Exists(path)) LoadEthnicGroups(path, t);
        }

        private void LoadFamilyTraditionRoot(string r, IContentStore<string, FamilyTraditionDef> t)
        {
            string path = Path.Combine(r, "FamilyTradition/FamilyTraditions.json");
            if (File.Exists(path)) LoadFamilyTraditions(path, t);
        }

        private void LoadCharacterTemplateRoot(string r, IContentStore<string, CharacterTemplateDef> t)
        {
            string path = Path.Combine(r, "CharacterTemplate/CharacterTemplates.json");
            if (File.Exists(path)) LoadCharacterTemplates(path, t);
        }

        private void LoadDnaRoot(string r, IContentStore<string, TalentDefectDef> t)
        {
            string path = Path.Combine(r, "Dna/DnaDefs.json");
            if (File.Exists(path)) LoadDnaDefs(path, t);
        }

        private void LoadMentalHealthRoot(string r, IContentStore<string, MentalDisorderDef> t)
        {
            string path = Path.Combine(r, "MentalHealth/MentalHealthDefs.json");
            if (File.Exists(path)) LoadMentalHealthDefs(path, t);
        }

        private void LoadInnovationRoot(string r, IContentStore<int, InnovationDef> t)
        {
            string path = Path.Combine(r, "Innovation/Innovations.json");
            if (File.Exists(path)) LoadInnovations(path, t);
        }

        private void LoadEpithetRoot(string r, IContentStore<string, EpithetDef> t)
        {
            string path = Path.Combine(r, "Epithet/Epithets.json");
            if (File.Exists(path)) LoadEpithets(path, t);
        }

        private static void LoadEpithets(string path, IContentStore<string, EpithetDef> target)
        {
            EpithetListWrapper wrapper = JsonUtility.FromJson<EpithetListWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.epithets == null) return;
            List<EpithetDef> parsed = new List<EpithetDef>();
            foreach (EpithetDef item in wrapper.epithets)
            {
                if (item != null && !string.IsNullOrEmpty(item.id))
                {
                    target.Set(item.id, item);
                    parsed.Add(item);
                }
            }
            EpithetCatalog.LoadFromContent(parsed);
        }

        private void LoadReligionRoot(string r, IContentStore<int, ReligionDef> t)
        {
            string path = Path.Combine(r, "Religion/Religions.json");
            if (File.Exists(path)) LoadReligions(path, t);
        }

        private void LoadDoctrineRoot(string r, IContentStore<string, DoctrineOptionDef> t)
        {
            string path = Path.Combine(r, "Religion/Doctrines.json");
            if (File.Exists(path)) LoadDoctrines(path, t);
        }

        private void LoadEquipmentRoot(string r, IContentStore<int, EquipmentDef> t)
        {
            string path = Path.Combine(r, "Equipment/Equipment.json");
            if (File.Exists(path)) LoadEquipment(path, t);
        }

        private void LoadEquipment(string path, IContentStore<int, EquipmentDef> target)
        {
            EquipmentWrapper wrapper = JsonUtility.FromJson<EquipmentWrapper>(File.ReadAllText(path));
            if (wrapper?.equipment == null) return;
            foreach (EquipmentDef item in wrapper.equipment)
            {
                if (item != null && item.equipmentId > 0) target.Set(item.equipmentId, item);
            }
        }

        private void LoadTitleRoot(string r, IContentStore<string, TitleDef> t)
        {
            string path = Path.Combine(r, "Title/Titles.json");
            if (File.Exists(path)) LoadTitlesFile(path, t);
        }

        // ===== 具体 JSON 解析（原 ContentRuntime 纯定义段逐行搬入）=====

        private static void LoadEthos(string path, IContentStore<string, EthosDef> target)
        {
            EthosWrapper wrapper = JsonUtility.FromJson<EthosWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.ethos == null) return;
            foreach (EthosDef item in wrapper.ethos)
            {
                if (item != null && !string.IsNullOrEmpty(item.ethosId)) target.Set(item.ethosId, item);
            }
        }

        private static void LoadHeritages(string path, IContentStore<string, HeritageDef> target)
        {
            HeritagesWrapper wrapper = JsonUtility.FromJson<HeritagesWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.heritages == null) return;
            foreach (HeritageDef item in wrapper.heritages)
            {
                if (item != null && !string.IsNullOrEmpty(item.heritageId)) target.Set(item.heritageId, item);
            }
        }

        private static void LoadTraditions(string path, IContentStore<string, TraditionDef> target)
        {
            TraditionsWrapper wrapper = JsonUtility.FromJson<TraditionsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.traditions == null) return;
            foreach (TraditionDef item in wrapper.traditions)
            {
                if (item != null && !string.IsNullOrEmpty(item.traditionId)) target.Set(item.traditionId, item);
            }
        }

        private static void LoadLanguage(string dir, IContentStore<string, LanguageDef> target)
        {
            string path = Path.Combine(dir, "Language.json");
            if (!File.Exists(path)) return;
            LanguageDef def = JsonUtility.FromJson<LanguageDef>(File.ReadAllText(path));
            if (def == null || string.IsNullOrEmpty(def.languageId))
            {
                Debug.LogWarning((object)("[ContentRegistry] 语言包 " + Path.GetFileName(dir) + " 定义无效（languageId 缺失）"));
                return;
            }
            PackCsv(def.maleNames, Path.Combine(dir, "CharacterFirstNames_Male.csv"));
            PackCsv(def.femaleNames, Path.Combine(dir, "CharacterFirstNames_Female.csv"));
            PackCsv(def.familyNames, Path.Combine(dir, "CharacterLastNames.csv"));
            PackCsv(def.cityNames, Path.Combine(dir, "CityNames.csv"));
            target.Set(def.languageId, def);
        }

        private static void LoadTitlesFile(string path, IContentStore<string, TitleDef> target)
        {
            if (!File.Exists(path)) return;
            TitlesWrapper wrapper = JsonUtility.FromJson<TitlesWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.titles == null) return;
            foreach (TitleDef item in wrapper.titles)
            {
                if (item != null && !string.IsNullOrEmpty(item.titleId)) target.Set(item.titleId, item);
            }
        }

        private static void LoadEthnicGroups(string path, IContentStore<string, EthnicGroupDef> target)
        {
            EthnicGroupsWrapper wrapper = JsonUtility.FromJson<EthnicGroupsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.groups == null) return;
            foreach (EthnicGroupDef item in wrapper.groups)
            {
                if (item != null && !string.IsNullOrEmpty(item.groupId)) target.Set(item.groupId, item);
            }
        }

        private static void LoadFamilyTraditions(string path, IContentStore<string, FamilyTraditionDef> target)
        {
            FamilyTraditionsWrapper wrapper = JsonUtility.FromJson<FamilyTraditionsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.familyTraditions == null) return;
            foreach (FamilyTraditionDef item in wrapper.familyTraditions)
            {
                if (item != null && !string.IsNullOrEmpty(item.traditionId)) target.Set(item.traditionId, item);
            }
        }

        private static void LoadCharacterTemplates(string path, IContentStore<string, CharacterTemplateDef> target)
        {
            CharacterTemplatesWrapper wrapper = JsonUtility.FromJson<CharacterTemplatesWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.templates == null) return;
            foreach (CharacterTemplateDef item in wrapper.templates)
            {
                if (item != null && !string.IsNullOrEmpty(item.templateId)) target.Set(item.templateId, item);
            }
        }

        private static void LoadDnaDefs(string path, IContentStore<string, TalentDefectDef> target)
        {
            DnaDefsWrapper wrapper = JsonUtility.FromJson<DnaDefsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.defs == null) return;
            foreach (TalentDefectDef item in wrapper.defs)
            {
                if (item != null && !string.IsNullOrEmpty(item.id)) target.Set(item.id, item);
            }
        }

        private static void LoadMentalHealthDefs(string path, IContentStore<string, MentalDisorderDef> target)
        {
            MentalHealthDefsWrapper wrapper = JsonUtility.FromJson<MentalHealthDefsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.disorders == null) return;
            foreach (MentalDisorderDef item in wrapper.disorders)
            {
                if (item != null && !string.IsNullOrEmpty(item.id)) target.Set(item.id, item);
            }
        }

        private static void LoadReligions(string path, IContentStore<int, ReligionDef> target)
        {
            ReligionListWrapper wrapper = JsonUtility.FromJson<ReligionListWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.religions == null) return;
            foreach (ReligionDef item in wrapper.religions)
            {
                if (item != null) target.Set(item.religionId, item);
            }
            ReligionCatalog.Load(new List<ReligionDef>(target.All));
            ReligionCatalog.EnsureColors();
        }

        private static void LoadDoctrines(string path, IContentStore<string, DoctrineOptionDef> target)
        {
            DoctrineListWrapper wrapper = JsonUtility.FromJson<DoctrineListWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.doctrines == null) return;
            foreach (DoctrineOptionDef item in wrapper.doctrines)
            {
                if (item != null && !string.IsNullOrEmpty(item.optionId)) target.Set(item.optionId, item);
            }
            DoctrinePool.Load(new List<DoctrineOptionDef>(target.All));
        }

        private void LoadInnovations(string path, IContentStore<int, InnovationDef> target)
        {
            // 用 Newtonsoft 而不是 JsonUtility：Unity 的 JsonUtility 对**嵌套深度**硬上限 10 层，
            // 超出部分静默丢弃（不报错）。革新前置树 InnovationPrerequisiteNode.children 正好会超，
            // 结果是深树革新的实际门槛比设计弱。Newtonsoft 无此限制。
            InnovationsWrapper wrapper;
            try
            {
                wrapper = Newtonsoft.Json.JsonConvert.DeserializeObject<InnovationsWrapper>(
                    File.ReadAllText(path),
                    new Newtonsoft.Json.JsonSerializerSettings
                    {
                        // 缺字段/多字段都不炸，内容演化期间容忍
                        MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore,
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                        ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Replace,
                    });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ContentDefinitionIndex] 革新定义解析失败（Newtonsoft）：{path} :: {ex.Message}");
                return;
            }
            if (wrapper == null || wrapper.innovations == null) return;
            foreach (InnovationDef innovation in wrapper.innovations)
            {
                if (innovation == null || innovation.innovationId <= 0) continue;
                if (innovation.providesCapabilities == null || innovation.providesCapabilities.Count == 0)
                {
                    innovation.providesCapabilities = new List<string>();
                    if (innovation.field == InnovationField.Mining)
                    {
                        innovation.providesCapabilities.Add("mining.surface");
                        innovation.providesCapabilities.Add("mining.shaft");
                        innovation.providesCapabilities.Add("mining.deep");
                    }
                    else if (innovation.field == InnovationField.Metallurgy)
                    {
                        innovation.providesCapabilities.Add("heat.high");
                    }
                }
                if (innovation.field == InnovationField.Metallurgy
                    && (innovation.capabilityModifiers == null || innovation.capabilityModifiers.Count == 0))
                {
                    innovation.capabilityModifiers = new List<CapabilityModifier>
                    {
                        new CapabilityModifier { channel = CapabilityChannels.HeatMaxTemperature, add = 700f }
                    };
                }
                if (innovation.discoveryGroups == null || innovation.discoveryGroups.Count == 0)
                {
                    innovation.discoveryGroups = new List<string>();
                    switch (innovation.field)
                    {
                        case InnovationField.Economic:
                        case InnovationField.Transport:
                            innovation.discoveryGroups.Add(_fallback.merchant);
                            break;
                        case InnovationField.Script:
                        case InnovationField.Mathematics:
                        case InnovationField.Philosophy:
                        case InnovationField.NaturalCognition:
                        case InnovationField.Theology:
                        case InnovationField.SocialThought:
                        case InnovationField.Governance:
                        case InnovationField.Law:
                        case InnovationField.Bureaucracy:
                        case InnovationField.Education:
                            innovation.discoveryGroups.Add(_fallback.scholar);
                            break;
                        default:
                            innovation.discoveryGroups.Add(_fallback.artisan);
                            break;
                    }
                }
                target.Set(innovation.innovationId, innovation);
            }
            // 旧内容集缺少“领土王国”革新；补一条数据驱动定义（与原 ContentRuntime 行为一致）。
            target.Set(1025, new InnovationDef
            {
                innovationId = 1025,
                innovationName = "领土王国",
                era = 2,
                field = InnovationField.Governance,
                researchCost = 800,
                prerequisiteTree = InnovationPrerequisiteNode.Leaf(500),
                discoveryGroups = new List<string> { _fallback.scholar }
            });
            target.Set(1, new InnovationDef
            {
                innovationId = 1,
                innovationName = "基础革新",
                era = 1,
                field = InnovationField.Agriculture,
                researchCost = 10,
                discoveryGroups = new List<string> { _fallback.artisan }
            });
        }

        private static void PackCsv(List<string> target, string path)
        {
            if (!File.Exists(path)) return;
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string text = lines[i].Trim();
                if (text.Length == 0 || text.StartsWith("#")) continue;
                int comma = text.IndexOf(',');
                string name = (comma >= 0 ? text.Substring(comma + 1) : text).Trim();
                if (name.Length > 0) target.Add(name);
            }
        }
    }
}
