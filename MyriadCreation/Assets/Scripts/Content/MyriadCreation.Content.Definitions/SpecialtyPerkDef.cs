using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MyriadCreation.Content.Definitions
{
    /// <summary>
    /// 专精节点：达到专精值与真实实践后解锁一个操作/战术。
    /// 解锁必须是动词或规则分支，不是平铺数值加成。
    /// </summary>
    [Serializable]
    public class SpecialtyPerkDef
    {
        public string perkId = "";
        public string perkName = "";
        public SpecialtyType specialty;
        public float requiredSkill = 50f;
        public float requiredPractice = 100f;
        public string unlocksAction = "";
        public string unlocksTactic = "";
        public string description = "";
    }

    /// <summary>专精节点目录（StreamingAssets 数据驱动；mod 可替换同 id 条目）。</summary>
    public static class SpecialtyPerkCatalog
    {
        [Serializable]
        private class Wrapper
        {
            public List<SpecialtyPerkDef> perks = new List<SpecialtyPerkDef>();
        }

        private static readonly Dictionary<string, SpecialtyPerkDef> _perks =
            new Dictionary<string, SpecialtyPerkDef>();

        public static IReadOnlyDictionary<string, SpecialtyPerkDef> All => _perks;

        public static int Count => _perks.Count;

        public static SpecialtyPerkDef Get(string perkId)
        {
            return !string.IsNullOrEmpty(perkId) && _perks.TryGetValue(perkId, out SpecialtyPerkDef def) ? def : null;
        }

        public static void Initialize(string streamingAssetsRoot)
        {
            _perks.Clear();
            if (string.IsNullOrEmpty(streamingAssetsRoot)) return;
            string path = Path.Combine(Path.Combine(streamingAssetsRoot, "Base"), "Specialty/SpecialtyPerks.json");
            if (!File.Exists(path)) return;
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(File.ReadAllText(path));
            if (wrapper?.perks == null) return;
            foreach (SpecialtyPerkDef def in wrapper.perks)
            {
                if (def == null || string.IsNullOrEmpty(def.perkId)) continue;
                _perks[def.perkId] = def;
            }
        }

        public static List<SpecialtyPerkDef> ForSpecialty(SpecialtyType specialty)
        {
            var result = new List<SpecialtyPerkDef>();
            foreach (SpecialtyPerkDef def in _perks.Values)
            {
                if (def.specialty == specialty) result.Add(def);
            }
            result.Sort((a, b) => a.requiredPractice.CompareTo(b.requiredPractice));
            return result;
        }
    }
}
