using System.Collections.Generic;
using UnityEngine;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 宗教定义（三级谱系：宗教 → 宗派 → 传统（礼拜仪轨/教义学派））
    /// - religionId + parentReligionId==-1 = 宗教根
    /// - parentReligionId>=0 = 宗派（从属某宗教）
    /// - rite/school = 传统第三级（礼拜仪轨/教义学派——地图传统模式显示）
    /// </summary>
    [System.Serializable]
    public class ReligionDef
    {
        public int religionId;
        /// <summary>内置名（本地化表有 &lt;id&gt;_name 键时优先）</summary>
        public string religionName;
        /// <summary>父宗教（-1=宗教根；&gt;=0=宗派）</summary>
        public int parentReligionId = -1;
        /// <summary>
        /// 礼拜仪轨列表（传统第三级·高级——如"罗马礼"/"希腊礼"/"科普特礼"：
        /// 礼是实践，与学派同等级但高于学派——礼仪本身包含学派思想）
        /// </summary>
        public List<string> rites = new List<string>();
        /// <summary>教义学派（传统第三级·次级——如"经院学派"/"中观学派"；学派可升格为礼）</summary>
        public string school = "";
        /// <summary>地图基色（RGBA 0-255；未配置时按 id 自动分配）</summary>
        public Color color = Color.white;

        public bool IsRoot => parentReligionId < 0;

        /// <summary>主礼拜仪轨（rites 第一项——传统级显示用；无礼返回空）</summary>
        public string PrimaryRite => rites != null && rites.Count > 0 ? rites[0] : "";
    }

    /// <summary>宗教数据表（ContentRegistry 加载 Religions.json——数据驱动）</summary>
    public static class ReligionCatalog
    {
        private static Dictionary<int, ReligionDef> _religions = new Dictionary<int, ReligionDef>();
        private static Color[] _palette;

        public static void Load(List<ReligionDef> religions)
        {
            _religions.Clear();
            if (religions != null)
                foreach (var r in religions)
                    if (r != null) _religions[r.religionId] = r;
        }

        public static ReligionDef Get(int religionId)
            => _religions.TryGetValue(religionId, out var r) ? r : null;

        public static IReadOnlyDictionary<int, ReligionDef> All => _religions;

        /// <summary>根宗教（沿 parent 链上溯到根）</summary>
        public static ReligionDef GetRoot(int religionId)
        {
            var cur = Get(religionId);
            var seen = new HashSet<int>();
            while (cur != null && !cur.IsRoot && seen.Add(cur.religionId))
                cur = Get(cur.parentReligionId);
            return cur;
        }

        
        /// <summary>地图色（按级别：宗教=根色 / 宗派=自身色 / 传统=rite/school 哈希偏移色）</summary>
        public static Color GetColor(int religionId, ReligionMapLevel level)
        {
            var def = Get(religionId);
            if (def == null) return Color.gray;

            switch (level)
            {
                case ReligionMapLevel.Religion:
                    return GetRoot(religionId)?.color ?? def.color;
                case ReligionMapLevel.Sect:
                    return def.color; // 自身色（所有非根节点=宗派——含裂教深层宗派）
                case ReligionMapLevel.Tradition:
                    // 传统级：礼拜仪轨优先（礼=高级传统），无礼用教义学派（次级）——哈希色相偏移
                    string trad = !string.IsNullOrEmpty(def.PrimaryRite) ? def.PrimaryRite : def.school;
                    int hash = (trad).GetHashCode() & 0x7fffffff;
                    float hue = (hash % 360) / 360f;
                    Color.RGBToHSV(def.color, out float h, out float s, out float v);
                    return Color.HSVToRGB(hue, Mathf.Clamp01(s * 0.7f), Mathf.Clamp01(v * 0.9f));
                default:
                    return def.color;
            }
        }

        /// <summary>自动分配色板（未配置颜色时按 id 取色）</summary>
        public static void EnsureColors()
        {
            if (_palette == null)
            {
                _palette = new Color[16];
                for (int i = 0; i < 16; i++)
                    _palette[i] = Color.HSVToRGB((i * 0.618f) % 1f, 0.6f, 0.85f);
            }
            foreach (var r in _religions.Values)
                if (r.color == Color.white && !r.IsRoot)
                    r.color = _palette[r.religionId % 16];
        }
    }

    /// <summary>宗教地图级别（三级谱系显示）</summary>
    public enum ReligionMapLevel
    {
        Religion,   // 宗教（根）
        Sect,       // 宗派
        Tradition   // 传统（礼拜仪轨/教义学派）
    }
}
