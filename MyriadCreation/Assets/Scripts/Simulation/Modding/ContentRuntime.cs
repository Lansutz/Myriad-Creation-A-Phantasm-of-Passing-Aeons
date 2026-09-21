using System;using System.Collections.Generic;using System.IO;using UnityEngine;using CivilizationEvolution.Simulation.Characters;using CivilizationEvolution.Simulation.Culture;using CivilizationEvolution.Simulation.Innovation;using CivilizationEvolution.Simulation.Religion;using CivilizationEvolution.Simulation.Society;using CivilizationEvolution.World.Biome;using CivilizationEvolution.World.Climate;using CivilizationEvolution.World.Hydrology;using CivilizationEvolution.World.Terrain;
namespace CivilizationEvolution.Simulation.Modding{internal static class ContentRuntime{        [Serializable]
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

        [Serializable]
        private class BiomesWrapper
        {
            public List<BiomeDef> biomes = new List<BiomeDef>();
        }

        private static void LoadCultureRoot(string r,IContentStore<int,ContentRegistry.CultureContentPack> t){var d=Path.Combine(r,"Culture");if(Directory.Exists(d))foreach(var x in Directory.GetDirectories(d))LoadCulturePack(x,t);}
        private static void LoadRaceRoot(string r,IContentStore<int,RaceData> t){var f=Path.Combine(r,"Race/RaceDefs.json");if(File.Exists(f))LoadRaceDefs(f,t);}
        private static void LoadEthosRoot(string r,IContentStore<string,EthosDef> t){var f=Path.Combine(r,"Ethos/Ethos.json");if(File.Exists(f))LoadEthos(f,t);}
        private static void LoadTraditionRoot(string r,IContentStore<string,TraditionDef> t){var f=Path.Combine(r,"Tradition/Traditions.json");if(File.Exists(f))LoadTraditions(f,t);}
        private static void LoadLanguageRoot(string r,IContentStore<string,LanguageDef> t){var d=Path.Combine(r,"Language");if(Directory.Exists(d))foreach(var x in Directory.GetDirectories(d))LoadLanguage(x,t);}
        private static void LoadEthnicGroupRoot(string r,IContentStore<string,EthnicGroupDef> t){var f=Path.Combine(r,"EthnicGroup/EthnicGroups.json");if(File.Exists(f))LoadEthnicGroups(f,t);}
        private static void LoadFamilyTraditionRoot(string r,IContentStore<string,FamilyTraditionDef> t){var f=Path.Combine(r,"FamilyTradition/FamilyTraditions.json");if(File.Exists(f))LoadFamilyTraditions(f,t);}
        private static void LoadCharacterTemplateRoot(string r,IContentStore<string,CharacterTemplateDef> t){var f=Path.Combine(r,"CharacterTemplate/CharacterTemplates.json");if(File.Exists(f))LoadCharacterTemplates(f,t);}
        private static void LoadDnaRoot(string r,IContentStore<string,TalentDefectDef> t){var f=Path.Combine(r,"Dna/DnaDefs.json");if(File.Exists(f))LoadDnaDefs(f,t);}
        private static void LoadMentalHealthRoot(string r,IContentStore<string,MentalDisorderDef> t){var f=Path.Combine(r,"MentalHealth/MentalHealthDefs.json");if(File.Exists(f))LoadMentalHealthDefs(f,t);}
        private static void LoadInnovationRoot(string r,IContentStore<int,InnovationDef> t){var f=Path.Combine(r,"Innovation/Innovations.json");if(File.Exists(f))LoadInnovations(f,t);}
        private static void LoadReligionRoot(string r,IContentStore<int,ReligionDef> t){var f=Path.Combine(r,"Religion/Religions.json");if(File.Exists(f))LoadReligions(f,t);}
        private static void LoadDoctrineRoot(string r,IContentStore<string,DoctrineOptionDef> t){var f=Path.Combine(r,"Religion/Doctrines.json");if(File.Exists(f))LoadDoctrines(f,t);}
        private static void LoadTitleRoot(string r,IContentStore<string,TitleDef> t){var f=Path.Combine(r,"Title/Titles.json");if(File.Exists(f))LoadTitlesFile(f,t);}
        private static void LoadBiomeRoot(string r,IContentStore<int,BiomeDef> t){var f=Path.Combine(r,"Biome/Biomes.json");if(File.Exists(f))LoadBiomes(f,t);}/// <summary>扫描一个内容根（Base 或 Mods）</summary>
 /// <summary>加载单个文化包目录</summary>
        private static void LoadCulturePack(string dir, IContentStore<int, ContentRegistry.CultureContentPack> target)
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

            bool overwritten = target.TryGet(data.cultureId, out var existing);
            target.Set(data.cultureId, pack);
            if (overwritten)
                Debug.Log($"[ContentRegistry] 文化 [{data.cultureName}] 被 Mods 覆盖");
        }

 /// <summary>加载种族定义文件</summary>
        private static void LoadRaceDefs(string path, IContentStore<int, RaceData> target)
        {
            var wrapper = JsonUtility.FromJson<RaceDefsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.races == null) return;
            foreach (var race in wrapper.races)
            {
                if (race == null || race.raceId <= 0) continue;
                target.Set(race.raceId, race);
            }
        }

 /// <summary>加载族群精神（Ethos）定义表</summary>
        private static void LoadEthos(string path, IContentStore<string, EthosDef> target)
        {
            var wrapper = JsonUtility.FromJson<EthosWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.ethos == null) return;
            foreach (var def in wrapper.ethos)
            {
                if (def == null || string.IsNullOrEmpty(def.ethosId)) continue;
                target.Set(def.ethosId, def);
            }
        }

 /// <summary>加载文化传统（Tradition）定义表</summary>
        private static void LoadTraditions(string path, IContentStore<string, TraditionDef> target)
        {
            var wrapper = JsonUtility.FromJson<TraditionsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.traditions == null) return;
            foreach (var def in wrapper.traditions)
            {
                if (def == null || string.IsNullOrEmpty(def.traditionId)) continue;
                target.Set(def.traditionId, def);
            }
        }

 /// <summary>加载单个语言包目录（Language/&lt;语言名&gt;/Language.json）</summary>
        private static void LoadLanguage(string dir, IContentStore<string, LanguageDef> target)
        {
            string defFile = Path.Combine(dir, "Language.json");
            if (!File.Exists(defFile)) return;
            var def = JsonUtility.FromJson<LanguageDef>(File.ReadAllText(defFile));
            if (def == null || string.IsNullOrEmpty(def.languageId))
            {
                Debug.LogWarning($"[ContentRegistry] 语言包 {Path.GetFileName(dir)} 定义无效（languageId 缺失）");
                return;
            }
 // 语言级名字池 CSV可选 // CharacterFirstNames_Male/Female.csv+LastNames+CityNames—— // 语言共享名字——模组化；文化级 CSV 仍兼容[回退链]）
            PackCsv(def.maleNames, Path.Combine(dir, "CharacterFirstNames_Male.csv"));
            PackCsv(def.femaleNames, Path.Combine(dir, "CharacterFirstNames_Female.csv"));
            PackCsv(def.familyNames, Path.Combine(dir, "CharacterLastNames.csv"));
            PackCsv(def.cityNames, Path.Combine(dir, "CityNames.csv"));
            target.Set(def.languageId, def);
        }

 /// <summary>加载族群（EthnicGroup）定义</summary>
        [Serializable]
        private class TitlesWrapper
        {
            public List<TitleDef> titles = new List<TitleDef>();
        }

 /// <summary>加载头衔表（Title/Titles.json——{ "titles": [...] }——模组覆盖）</summary>
        private static void LoadTitlesFile(string path, IContentStore<string, TitleDef> target)
        {
            if (!File.Exists(path)) return;
            var wrapper = JsonUtility.FromJson<TitlesWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.titles == null) return;
            foreach (var t in wrapper.titles)
                if (t != null && !string.IsNullOrEmpty(t.titleId))
                    target.Set(t.titleId, t); // 同名覆盖（模组优先——后加载赢）
        }

        private static void LoadEthnicGroups(string path, IContentStore<string, EthnicGroupDef> target)
        {
            var wrapper = JsonUtility.FromJson<EthnicGroupsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.groups == null) return;
            foreach (var def in wrapper.groups)
            {
                if (def == null || string.IsNullOrEmpty(def.groupId)) continue;
                target.Set(def.groupId, def);
            }
        }

 /// <summary>加载家族传统（FamilyTradition）定义表（企划书 9.4：家族团结度/家法/家族文化偏移）</summary>
        private static void LoadFamilyTraditions(string path, IContentStore<string, FamilyTraditionDef> target)
        {
            var wrapper = JsonUtility.FromJson<FamilyTraditionsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.familyTraditions == null) return;
            foreach (var def in wrapper.familyTraditions)
            {
                if (def == null || string.IsNullOrEmpty(def.traditionId)) continue;
                Familytarget.Set(def.traditionId, def);
            }
        }

 /// <summary>加载角色模板（CharacterTemplate）定义表（第九篇角色生成参数模板）</summary>
        private static void LoadCharacterTemplates(string path, IContentStore<string, CharacterTemplateDef> target)
        {
            var wrapper = JsonUtility.FromJson<CharacterTemplatesWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.templates == null) return;
            foreach (var def in wrapper.templates)
            {
                if (def == null || string.IsNullOrEmpty(def.templateId)) continue;
                target.Set(def.templateId, def);
            }
        }

 /// <summary>加载 DNA 天赋/遗传病（TalentDefect）定义表（DNA 文档：模组可新增天赋/遗传病列表）</summary>
        private static void LoadDnaDefs(string path, IContentStore<string, TalentDefectDef> target)
        {
            var wrapper = JsonUtility.FromJson<DnaDefsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.defs == null) return;
            foreach (var def in wrapper.defs)
            {
                if (def == null || string.IsNullOrEmpty(def.id)) continue;
                target.Set(def.id, def);
            }
        }

 /// <summary>加载精神疾病（MentalDisorder）定义表（模组可新增疾病类型）</summary>
        private static void LoadMentalHealthDefs(string path, IContentStore<string, MentalDisorderDef> target)
        {
            var wrapper = JsonUtility.FromJson<MentalHealthDefsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.disorders == null) return;
            foreach (var def in wrapper.disorders)
            {
                if (def == null || string.IsNullOrEmpty(def.id)) continue;
                target.Set(def.id, def);
            }
        }

 /// <summary>加载宗教定义（三级谱系：宗教→宗派→传统）</summary>
        private static void LoadReligions(string path, IContentStore<int, ReligionDef> target)
        {
            var wrapper = JsonUtility.FromJson<ReligionListWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.religions == null) return;
            foreach (var r in wrapper.religions)
                if (r != null) target.Set(r.religionId, r);
            ReligionCatalog.Load(new List<ReligionDef>(target.All));
            ReligionCatalog.EnsureColors();
        }

        [System.Serializable]
        private class ReligionListWrapper
        {
            public List<ReligionDef> religions = new List<ReligionDef>();
        }

 /// <summary>加载教义池（七支柱选项——中性词汇+宗教专属风味化）</summary>
        private static void LoadDoctrines(string path, IContentStore<string, DoctrineOptionDef> target)
        {
            var wrapper = JsonUtility.FromJson<DoctrineListWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.doctrines == null) return;
            foreach (var d in wrapper.doctrines)
                if (d != null && !string.IsNullOrEmpty(d.optionId))
                    target.Set(d.optionId, d);
            DoctrinePool.Load(new List<DoctrineOptionDef>(target.All));
        }

        [System.Serializable]
        private class DoctrineListWrapper
        {
            public List<DoctrineOptionDef> doctrines = new List<DoctrineOptionDef>();
        }

 /// <summary>加载革新（Innovation）定义表（两级分类：大类+子类；模组可新增）</summary>
        private static void LoadInnovations(string path, IContentStore<int, InnovationDef> target)
        {
            var wrapper = JsonUtility.FromJson<InnovationsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.innovations == null) return;
            foreach (var def in wrapper.innovations)
            {
                if (def == null || def.innovationId <= 0) continue;
                target.Set(def.innovationId, def);
            }
        }

 /// <summary>加载群系（Biome）定义表（数据驱动，模组可覆盖）</summary>
        private static void LoadBiomes(string path, IContentStore<int, BiomeDef> target)
        {
            var wrapper = JsonUtility.FromJson<BiomesWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.biomes == null) return;
            foreach (var def in wrapper.biomes)
            {
                if (def == null) continue;
                target.Set(def.biomeId, def);
            }
            Debug.Log($"[ContentRuntime] 群系定义加载：{wrapper.biomes.Count} 个");
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
        }        public static void Load(string root,Dictionary<int,ContentRegistry.CultureContentPack> c,Dictionary<int,RaceData> r,Dictionary<string,EthosDef> e,Dictionary<string,TraditionDef> tr,Dictionary<string,LanguageDef> l,Dictionary<string,EthnicGroupDef> eg,Dictionary<string,FamilyTraditionDef> ft,Dictionary<string,CharacterTemplateDef> ct,Dictionary<string,TalentDefectDef> dna,Dictionary<string,MentalDisorderDef> mh,Dictionary<int,InnovationDef> i,Dictionary<int,ReligionDef> rel,Dictionary<string,DoctrineOptionDef> d,Dictionary<string,TitleDef> ti,Dictionary<int,BiomeDef> b){
var p=new ContentProviderCatalog();p.Add(new DelegateContentProvider<int,ContentRegistry.CultureContentPack>("Culture",LoadCultureRoot),new DictionaryContentStore<int,ContentRegistry.CultureContentPack>(c));p.Add(new DelegateContentProvider<int,RaceData>("Race",LoadRaceRoot),new DictionaryContentStore<int,RaceData>(r));p.Add(new DelegateContentProvider<string,EthosDef>("Ethos",LoadEthosRoot),new DictionaryContentStore<string,EthosDef>(e));p.Add(new DelegateContentProvider<string,TraditionDef>("Tradition",LoadTraditionRoot),new DictionaryContentStore<string,TraditionDef>(tr));p.Add(new DelegateContentProvider<string,LanguageDef>("Language",LoadLanguageRoot),new DictionaryContentStore<string,LanguageDef>(l));p.Add(new DelegateContentProvider<string,EthnicGroupDef>("EthnicGroup",LoadEthnicGroupRoot),new DictionaryContentStore<string,EthnicGroupDef>(eg));p.Add(new DelegateContentProvider<string,FamilyTraditionDef>("FamilyTradition",LoadFamilyTraditionRoot),new DictionaryContentStore<string,FamilyTraditionDef>(ft));p.Add(new DelegateContentProvider<string,CharacterTemplateDef>("CharacterTemplate",LoadCharacterTemplateRoot),new DictionaryContentStore<string,CharacterTemplateDef>(ct));p.Add(new DelegateContentProvider<string,TalentDefectDef>("Dna",LoadDnaRoot),new DictionaryContentStore<string,TalentDefectDef>(dna));p.Add(new DelegateContentProvider<string,MentalDisorderDef>("MentalHealth",LoadMentalHealthRoot),new DictionaryContentStore<string,MentalDisorderDef>(mh));p.Add(new DelegateContentProvider<int,InnovationDef>("Innovation",LoadInnovationRoot),new DictionaryContentStore<int,InnovationDef>(i));p.Add(new DelegateContentProvider<int,ReligionDef>("Religion",LoadReligionRoot),new DictionaryContentStore<int,ReligionDef>(rel));p.Add(new DelegateContentProvider<string,DoctrineOptionDef>("Doctrine",LoadDoctrineRoot),new DictionaryContentStore<string,DoctrineOptionDef>(d));p.Add(new DelegateContentProvider<string,TitleDef>("Title",LoadTitleRoot),new DictionaryContentStore<string,TitleDef>(ti));p.Add(new DelegateContentProvider<int,BiomeDef>("Biome",LoadBiomeRoot),new DictionaryContentStore<int,BiomeDef>(b));foreach(var s in ContentSourceCatalog.CreateDefault(root))if(s.Exists)p.Load(s);}
}}
