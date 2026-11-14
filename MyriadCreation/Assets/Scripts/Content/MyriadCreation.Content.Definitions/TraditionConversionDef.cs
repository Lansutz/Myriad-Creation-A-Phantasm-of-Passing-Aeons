using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MyriadCreation.Content.Definitions
{
    /// <summary>
    /// 传统→教义/法律的转换条目：传统先在文化中固化，成熟度达标才可被法典化或宗教吸收。
    /// </summary>
    [Serializable]
    public class TraditionConversionDef
    {
        public string traditionId = "";
        /// <summary>doctrine = 写入国教教义；law = 推动立法。</summary>
        public string targetKind = "doctrine";
        public string targetId = "";
        public float minimumMaturity = 0.3f;
    }

    public static class TraditionConversionCatalog
    {
        [Serializable]
        private class Wrapper
        {
            public List<TraditionConversionDef> conversions = new List<TraditionConversionDef>();
        }

        private static readonly Dictionary<string, List<TraditionConversionDef>> _byTradition =
            new Dictionary<string, List<TraditionConversionDef>>();

        public static void Initialize(string streamingAssetsRoot)
        {
            _byTradition.Clear();
            if (string.IsNullOrEmpty(streamingAssetsRoot)) return;
            string path = Path.Combine(Path.Combine(streamingAssetsRoot, "Base"), "Tradition/TraditionConversions.json");
            if (!File.Exists(path)) return;
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(File.ReadAllText(path));
            if (wrapper?.conversions == null) return;
            foreach (TraditionConversionDef def in wrapper.conversions)
            {
                if (def == null || string.IsNullOrEmpty(def.traditionId) || string.IsNullOrEmpty(def.targetId)) continue;
                if (!_byTradition.TryGetValue(def.traditionId, out List<TraditionConversionDef> list))
                {
                    _byTradition[def.traditionId] = list = new List<TraditionConversionDef>();
                }
                list.Add(def);
            }
        }

        public static IReadOnlyList<TraditionConversionDef> ForTradition(string traditionId)
        {
            if (!string.IsNullOrEmpty(traditionId) && _byTradition.TryGetValue(traditionId, out List<TraditionConversionDef> list))
            {
                return list;
            }
            return Array.Empty<TraditionConversionDef>();
        }
    }
}
