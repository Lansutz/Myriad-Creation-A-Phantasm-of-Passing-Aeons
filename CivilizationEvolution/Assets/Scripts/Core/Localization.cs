using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CivilizationEvolution.Core
{
    /// <summary>
    /// 本地化管理器（键→文本，CK3 localization 模式）
    /// 数据/代码只存键；显示文本按语言查表；缺键回退键名（开发期可见）
    /// 文件：StreamingAssets/Base/Localization/&lt;语言&gt;.json（如 zh-Hans.json）
    ///       Mods/Localization/&lt;语言&gt;.json 覆盖 Base（模组扩展）
    /// JSON 格式：{ "entries": [ { "key": "...", "value": "..." } ] }
    /// </summary>
    public static class Localization
    {
        public static string CurrentLanguage { get; private set; } = "zh-Hans";
        public static bool IsLoaded { get; private set; } = false;

        private static readonly Dictionary<string, string> _table = new Dictionary<string, string>();

        /// <summary>幂等初始化（可切换语言；切换时重载）</summary>
        public static void Initialize(string language = "zh-Hans")
        {
            if (IsLoaded && CurrentLanguage == language) return;

            CurrentLanguage = language;
            _table.Clear();

            string root = Application.streamingAssetsPath;
            if (Directory.Exists(root))
            {
                LoadLanguageFile(Path.Combine(root, "Base", "Localization", $"{language}.json"));
                LoadLanguageFile(Path.Combine(root, "Mods", "Localization", $"{language}.json")); // Mods 覆盖
            }

            IsLoaded = true;
            Debug.Log($"[Localization] 加载完成：{language}，{_table.Count} 键");
        }

        /// <summary>重置（测试用）</summary>
        public static void Reset()
        {
            IsLoaded = false;
            _table.Clear();
        }

        /// <summary>查询文本；缺键回退键名</summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            return _table.TryGetValue(key, out var v) ? v : key;
        }

        /// <summary>查询文本；缺键返回 fallback</summary>
        public static string Get(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key)) return fallback ?? "";
            return _table.TryGetValue(key, out var v) ? v : (fallback ?? key);
        }

        public static bool Has(string key) => !string.IsNullOrEmpty(key) && _table.ContainsKey(key);

        private static void LoadLanguageFile(string path)
        {
            if (!File.Exists(path)) return;
            try
            {
                var wrapper = JsonUtility.FromJson<LocalizationWrapper>(File.ReadAllText(path));
                if (wrapper == null || wrapper.entries == null) return;
                foreach (var e in wrapper.entries)
                {
                    if (string.IsNullOrEmpty(e.key)) continue;
                    _table[e.key] = e.value;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Localization] 加载失败：{path} — {e.Message}");
            }
        }

        [Serializable]
        private class LocalizationWrapper
        {
            public List<LocalizationEntry> entries = new List<LocalizationEntry>();
        }

        [Serializable]
        private class LocalizationEntry
        {
            public string key;
            public string value;
        }
    }
}
