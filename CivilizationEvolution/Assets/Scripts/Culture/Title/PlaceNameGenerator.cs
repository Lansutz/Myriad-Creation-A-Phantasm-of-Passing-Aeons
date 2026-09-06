using System.Collections.Generic;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 地名语义组合引擎（2026-09-03 用户设计——学《地图上发生的事》更细）：
    /// 词干（地形/人名/族群名）× 语义后缀类 → 地名
    /// 自由组合受规则限制（合法语义对）——输出=语言真实词形（非直译）
    /// 例：山崖+城=山崖之城（语言内合成）｜建城者名+城=亚历山大城式纪念名
    /// </summary>
    public static class PlaceNameGenerator
    {
        /// <summary>语义允许矩阵（词干地形类 → 可配后缀类）——规则限制核心</summary>
        private static readonly Dictionary<string, HashSet<string>> AllowedSuffixes = new Dictionary<string, HashSet<string>>
        {
            { "mountain", new HashSet<string> { "city", "fort", "region", "home" } },
            { "plain", new HashSet<string> { "city", "region", "home" } },
            { "cliff", new HashSet<string> { "city", "fort", "region" } },
            { "valley", new HashSet<string> { "city", "region", "home" } },
            { "coast", new HashSet<string> { "city", "port", "region" } },
            { "river", new HashSet<string> { "city", "port", "region", "home" } },
            { "sea", new HashSet<string> { "port", "region" } },
            { "hill", new HashSet<string> { "city", "fort", "region", "home" } },
            { "desert", new HashSet<string> { "city", "region" } },
            { "highland", new HashSet<string> { "fort", "region", "home" } },
        };

        /// <summary>查语言词汇（词干或后缀——按语义类）</summary>
        public static string FindWord(List<PlaceSuffixDef> words, string semantic)
        {
            if (words == null) return "";
            foreach (var w in words)
                if (w.semantic == semantic) return w.word;
            return "";
        }

        /// <summary>是否允许组合（地形语义 × 后缀语义——规则限制）</summary>
        public static bool CanCombine(string stemSemantic, string suffixSemantic)
        {
            if (suffixSemantic == "region" || suffixSemantic == "home") return true; // 模糊地区通配
            if (suffixSemantic == "founded") return true; // 建者城（事件专用——人名词干）
            if (!AllowedSuffixes.TryGetValue(stemSemantic, out var allowed)) return false;
            return allowed.Contains(suffixSemantic);
        }

        /// <summary>
        /// 组合生成地名（词干+后缀——语言真实词形——规则过滤）：
        /// stemWord 词干词（地形词或人名/族群名）——找不到合法组合返回空
        /// </summary>
        public static string Combine(string stemWord, string suffixWord, string stemSemantic, string suffixSemantic)
        {
            if (string.IsNullOrEmpty(stemWord) || string.IsNullOrEmpty(suffixWord)) return "";
            if (!CanCombine(stemSemantic, suffixSemantic)) return "";
            return stemWord + suffixWord; // 语言合成（修饰在前——后缀置后——语序配置待扩展）
        }

        /// <summary>
        /// 从语言生成地名（地形语义→查词干——后缀语义→查词——组合）：
        /// 例：Generate("cliff", "city", lang) → 山崖词+城词
        /// </summary>
        public static string Generate(string stemSemantic, string suffixSemantic, LanguageDef lang)
        {
            if (lang == null) return "";
            string stem = FindWord(lang.terrainWords, stemSemantic);
            string suffix = FindWord(lang.placeSuffixes, suffixSemantic);
            if (string.IsNullOrEmpty(stem) || string.IsNullOrEmpty(suffix)) return "";
            if (!CanCombine(stemSemantic, suffixSemantic)) return "";
            return stem + suffix;
        }

        /// <summary>纪念名（建城者/名人 + 城语义——事件命名——亚历山大式）</summary>
        public static string FounderCity(string founderName, LanguageDef lang)
        {
            if (string.IsNullOrEmpty(founderName) || lang == null) return "";
            string cityWord = FindWord(lang.placeSuffixes, "city");
            if (string.IsNullOrEmpty(cityWord)) return "";
            return founderName + cityWord; // 亚历山大+城=亚历山大城（语言真实词形）
        }
    }
}
