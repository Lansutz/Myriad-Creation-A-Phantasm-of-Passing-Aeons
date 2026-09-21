using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Culture;
using CivilizationEvolution.Simulation.Innovation;
using CivilizationEvolution.Simulation.Religion;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.World.Biome;

namespace CivilizationEvolution.Simulation.Modding
{
    /// <summary>
    /// Compatibility facade for legacy consumers. Provider composition lives in ContentRuntime.
    /// New code should depend on ContentResolvers / IContentResolver.
    /// </summary>
    public static class ContentRegistry
    {
        [Serializable] public class NamePoolData
        {
            public List<string> maleNames = new List<string>();
            public List<string> femaleNames = new List<string>();
            public List<string> lastNames = new List<string>();
            public List<string> cityNames = new List<string>();
        }
        public class CultureContentPack
        {
            public CultureData data;
            public NamePoolData names = new NamePoolData();
            public string packagePath = "";
        }

        public static Dictionary<int, CultureContentPack> Cultures { get; private set; } = new Dictionary<int, CultureContentPack>();
        public static Dictionary<int, RaceData> Races { get; private set; } = new Dictionary<int, RaceData>();
        public static Dictionary<string, EthosDef> Ethos { get; private set; } = new Dictionary<string, EthosDef>();
        public static Dictionary<string, TraditionDef> Traditions { get; private set; } = new Dictionary<string, TraditionDef>();
        public static Dictionary<string, LanguageDef> Languages { get; private set; } = new Dictionary<string, LanguageDef>();
        public static Dictionary<string, EthnicGroupDef> EthnicGroups { get; private set; } = new Dictionary<string, EthnicGroupDef>();
        public static Dictionary<string, FamilyTraditionDef> FamilyTraditions { get; private set; } = new Dictionary<string, FamilyTraditionDef>();
        public static Dictionary<string, TitleDef> Titles { get; private set; } = new Dictionary<string, TitleDef>();
        public static Dictionary<string, CharacterTemplateDef> CharacterTemplates { get; private set; } = new Dictionary<string, CharacterTemplateDef>();
        public static Dictionary<string, TalentDefectDef> TalentDefects { get; private set; } = new Dictionary<string, TalentDefectDef>();
        public static Dictionary<string, MentalDisorderDef> MentalDisorders { get; private set; } = new Dictionary<string, MentalDisorderDef>();
        public static Dictionary<int, InnovationDef> Innovations { get; private set; } = new Dictionary<int, InnovationDef>();
        public static Dictionary<int, ReligionDef> Religions { get; private set; } = new Dictionary<int, ReligionDef>();
        public static Dictionary<string, DoctrineOptionDef> Doctrines { get; private set; } = new Dictionary<string, DoctrineOptionDef>();
        public static Dictionary<int, BiomeDef> Biomes => BiomeRegistry.Overrides;
        public static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            if (IsInitialized) return;
            Cultures = new Dictionary<int, CultureContentPack>(); Races = new Dictionary<int, RaceData>();
            Ethos = new Dictionary<string, EthosDef>(); Traditions = new Dictionary<string, TraditionDef>();
            Languages = new Dictionary<string, LanguageDef>(); EthnicGroups = new Dictionary<string, EthnicGroupDef>();
            FamilyTraditions = new Dictionary<string, FamilyTraditionDef>(); CharacterTemplates = new Dictionary<string, CharacterTemplateDef>();
            TalentDefects = new Dictionary<string, TalentDefectDef>(); MentalDisorders = new Dictionary<string, MentalDisorderDef>();
            Innovations = new Dictionary<int, InnovationDef>(); Religions = new Dictionary<int, ReligionDef>();
            Doctrines = new Dictionary<string, DoctrineOptionDef>(); Titles = new Dictionary<string, TitleDef>();
            BiomeRegistry.Overrides.Clear();

            string root = Application.streamingAssetsPath;
            if (!System.IO.Directory.Exists(root))
                Debug.LogWarning($"[ContentRegistry] 未找到 StreamingAssets 目录：{root}");
            else
                ContentRuntime.Load(root, Cultures, Races, Ethos, Traditions, Languages, EthnicGroups,
                    FamilyTraditions, CharacterTemplates, TalentDefects, MentalDisorders, Innovations,
                    Religions, Doctrines, Titles, Biomes);
            IsInitialized = true;
        }

        public static void Reset()
        {
            IsInitialized = false;
            Cultures.Clear(); Races.Clear(); Ethos.Clear(); Traditions.Clear(); Languages.Clear();
            EthnicGroups.Clear(); FamilyTraditions.Clear(); CharacterTemplates.Clear(); TalentDefects.Clear();
            MentalDisorders.Clear(); Innovations.Clear(); Religions.Clear(); Doctrines.Clear(); Titles.Clear(); Biomes.Clear();
        }

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
        public static bool TryGetReligion(int id, out ReligionDef def) => Religions.TryGetValue(id, out def);
        public static bool TryGetDoctrine(string id, out DoctrineOptionDef def) => Doctrines.TryGetValue(id, out def);
        public static bool TryGetTitle(string id, out TitleDef def) => Titles.TryGetValue(id, out def);
        public static bool TryGetBiome(int id, out BiomeDef def) => Biomes.TryGetValue(id, out def);

        public static string GetRandomName(CultureContentPack pack, int type, System.Random rng = null)
        {
            rng = rng ?? new System.Random();
            List<string> list = null;
            if (pack != null && pack.data != null && !string.IsNullOrEmpty(pack.data.languageId)
                && TryGetLanguage(pack.data.languageId, out var lang))
                list = type switch { 1 => lang.femaleNames, 2 => lang.familyNames, 3 => lang.cityNames, _ => lang.maleNames };
            if (list == null || list.Count == 0)
            {
                if (pack == null) return "无名";
                list = type switch { 1 => pack.names.femaleNames, 2 => pack.names.lastNames, 3 => pack.names.cityNames, _ => pack.names.maleNames };
                if (list.Count == 0 && type != 2) list = pack.names.lastNames;
                if (list.Count == 0) return pack.data.cultureName;
            }
            return list[rng.Next(list.Count)];
        }
    }
}
