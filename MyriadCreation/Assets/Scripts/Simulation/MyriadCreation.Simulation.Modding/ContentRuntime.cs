using System;
using System.Collections.Generic;
using System.IO;
using MyriadCreation.Simulation.State;
using MyriadCreation.World.Biome;
using MyriadCreation.Simulation.Culture;
using UnityEngine;

using MyriadCreation.Content.Modding;
namespace MyriadCreation.Simulation.ContentHost
{
    /// <summary>
    /// P2：Simulation host 侧的运行时内容加载器（仅 CultureData / RaceData / BiomeDef 映射）。
    /// 纯定义 JSON 解析已迁 ContentDefinitionIndex；本类保留 Culture/Race/Biome 的 host 运行时对象构造，
    /// 并沿用 ContentSourceCatalog（Base→Mods 优先级）加载顺序。
    /// </summary>
    internal static class ContentRuntime
    {
        [Serializable]
        private class RaceDefsWrapper
        {
            public List<RaceData> races = new List<RaceData>();
        }

        [Serializable]
        private class BiomesWrapper
        {
            public List<BiomeDef> biomes = new List<BiomeDef>();
        }

        private static void LoadCultureRoot(string r, IContentStore<int, ContentRegistry.CultureContentPack> t)
        {
            string path = Path.Combine(r, "Culture");
            if (!Directory.Exists(path)) return;
            string[] directories = Directory.GetDirectories(path);
            for (int i = 0; i < directories.Length; i++)
            {
                LoadCulturePack(directories[i], t);
            }
        }

        private static void LoadRaceRoot(string r, IContentStore<int, RaceData> t)
        {
            string path = Path.Combine(r, "Race/RaceDefs.json");
            if (File.Exists(path))
            {
                LoadRaceDefs(path, t);
            }
        }

        private static void LoadBiomeRoot(string r, IContentStore<int, BiomeDef> t)
        {
            string path = Path.Combine(r, "Biome/Biomes.json");
            if (File.Exists(path))
            {
                LoadBiomes(path, t);
            }
        }

        private static void LoadCulturePack(string dir, IContentStore<int, ContentRegistry.CultureContentPack> target)
        {
            string path = Path.Combine(dir, "CultureData.json");
            if (!File.Exists(path)) return;
            CultureData cultureData = JsonUtility.FromJson<CultureData>(File.ReadAllText(path));
            if (cultureData == null || cultureData.cultureId <= 0)
            {
                Debug.LogWarning((object)("[ContentRegistry] 文化包 " + Path.GetFileName(dir) + " 定义无效（cultureId 缺失）"));
                return;
            }
            ContentRegistry.CultureContentPack pack = new ContentRegistry.CultureContentPack
            {
                data = cultureData,
                packagePath = dir
            };
            PackCsv(pack.names.maleNames, Path.Combine(dir, "CharacterFirstNames_Male.csv"));
            PackCsv(pack.names.femaleNames, Path.Combine(dir, "CharacterFirstNames_Female.csv"));
            PackCsv(pack.names.lastNames, Path.Combine(dir, "CharacterLastNames.csv"));
            PackCsv(pack.names.cityNames, Path.Combine(dir, "CityNames.csv"));
            bool overwritten = target.TryGet(cultureData.cultureId, out _);
            target.Set(cultureData.cultureId, pack);
            if (overwritten)
            {
                Debug.Log((object)("[ContentRegistry] 文化 [" + cultureData.cultureName + "] 被 Mods 覆盖"));
            }
        }

        private static void LoadRaceDefs(string path, IContentStore<int, RaceData> target)
        {
            RaceDefsWrapper wrapper = JsonUtility.FromJson<RaceDefsWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.races == null) return;
            foreach (RaceData race in wrapper.races)
            {
                if (race != null && race.raceId > 0)
                {
                    target.Set(race.raceId, race);
                }
            }
        }

        private static void LoadBiomes(string path, IContentStore<int, BiomeDef> target)
        {
            BiomesWrapper wrapper = JsonUtility.FromJson<BiomesWrapper>(File.ReadAllText(path));
            if (wrapper == null || wrapper.biomes == null) return;
            foreach (BiomeDef biome in wrapper.biomes)
            {
                if (biome != null)
                {
                    target.Set(biome.biomeId, biome);
                }
            }
            Debug.Log((object)$"[ContentRuntime] 群系定义加载：{wrapper.biomes.Count} 个");
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

        /// <summary>加载 host 运行时内容（Culture/Race/Biome），不触碰纯定义索引。</summary>
        public static void LoadRuntime(string root, Dictionary<int, ContentRegistry.CultureContentPack> cultures,
            Dictionary<int, RaceData> races, Dictionary<int, BiomeDef> biomes)
        {
            var providerCatalog = new ContentProviderCatalog();
            providerCatalog.Add(new DelegateContentProvider<int, ContentRegistry.CultureContentPack>("Culture", LoadCultureRoot),
                new DictionaryContentStore<int, ContentRegistry.CultureContentPack>(cultures));
            providerCatalog.Add(new DelegateContentProvider<int, RaceData>("Race", LoadRaceRoot),
                new DictionaryContentStore<int, RaceData>(races));
            // Biome 合法使用 biomeId=0（「冲积平原」），必须显式允许默认键，
            // 否则第一个条目就抛 "Content id cannot be the default key"，整张群系表加载失败。
            providerCatalog.Add(new DelegateContentProvider<int, BiomeDef>("Biome", LoadBiomeRoot),
                new DictionaryContentStore<int, BiomeDef>(biomes, allowDefaultKey: true));

            foreach (ContentSource source in ContentSourceCatalog.CreateDefault(root))
            {
                if (source.Exists)
                {
                    providerCatalog.Load(source);
                }
            }
        }
    }
}
