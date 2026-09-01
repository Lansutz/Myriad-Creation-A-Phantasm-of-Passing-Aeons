using System.Collections.Generic;
using UnityEngine;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 宗教组织节点（双维度谱系：组织父 × 学派父）
    /// 组织父（parentReligionId）：组织谱系树——宗教→宗派（组织性分裂·互斥）
    ///   →传统（思想性传承·可共存）→分支传统（传播/再分化——任意深度）
    /// 学派父（schoolParentId）：思想源链——礼仪祖先学派（如亚述教会←
    ///   赛琉基亚-泰西封学派/狄奥多若——与聂斯托里无关）
    /// 类型（nodeType）：教统=组织性（有主教链/法脉）；传统=思想性（可创建）；学派=前组织形态
    ///   （未稳定——无固定仪轨/座堂圣座——可演化为礼/传统/宗派）
    /// 教统（hasSuccession）：组织连续性（使徒统绪/法脉）——宗派必须有；
    ///   无教统学派（如聂斯托里）只能思想并入，不能独立组织化
    /// </summary>
    [System.Serializable]
    public class ReligionDef
    {
        public int religionId;
        /// <summary>内置名（本地化表有 &lt;id&gt;_name 键时优先）</summary>
        public string religionName;
        /// <summary>组织父（-1=宗教根；&gt;=0=宗派/传统/分支——组织谱系树）</summary>
        public int parentReligionId = -1;
        /// <summary>学派父（-1=无思想源；&gt;=0=祖先学派——思想谱系独立于组织谱系）</summary>
        public int schoolParentId = -1;
        /// <summary>节点类型（学派=前组织形态——可演化）</summary>
        public ReligionNodeType nodeType = ReligionNodeType.Succession;
        /// <summary>教统（使徒统绪/法脉——组织连续性；宗派必须有；无教统学派不能独立组织化）</summary>
        public bool hasSuccession = true;
        /// <summary>
        /// 具体礼名列表（狭义——如"科普特礼"/"埃塞俄比亚礼"：
        /// 同一礼仪传统下可有多个具体礼——埃塞俄比亚礼属亚历山大传统）
        /// </summary>
        public List<string> rites = new List<string>();
        /// <summary>
        /// 礼仪传统（广义——五大礼仪传统之一：罗马传统/拜占庭传统/
        /// 亚历山大传统/安条克传统/东叙利亚传统；亚美尼亚=独立传统）
        /// ——具体礼名（rites）从属于礼仪传统（riteFamily）
        /// </summary>
        public string riteFamily = "";
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

        
        private static int _nextId = 1000;

        /// <summary>
        /// 创建传统（动态——宗教演化：新教义/新仪轨定型→新传统节点；
        /// 从属指定教统/传统——任意深度）
        /// </summary>
        public static ReligionDef CreateTradition(int parentId, string name, string school, bool hasSuccession = false)
        {
            int id = _nextId++;
            var def = new ReligionDef
            {
                religionId = id,
                religionName = name,
                parentReligionId = parentId,
                schoolParentId = -1,
                nodeType = ReligionNodeType.Tradition,
                hasSuccession = hasSuccession,
                school = school
            };
            _religions[id] = def;
            return def;
        }

        /// <summary>
        /// 创建礼（动态——礼仪实践形成：向指定节点（教统/传统）的
        /// rites 列表添加具体礼名——礼归属可变）
        /// </summary>
        public static void CreateRite(int religionId, string riteName, string riteFamily = "")
        {
            if (!_religions.TryGetValue(religionId, out var def)) return;
            if (!def.rites.Contains(riteName))
                def.rites.Add(riteName);
            if (!string.IsNullOrEmpty(riteFamily))
                def.riteFamily = riteFamily;
        }

        /// <summary>地图色（按级别：宗教=根色 / 教统=自身色 / 传统=rite/school 哈希偏移色）</summary>
        public static Color GetColor(int religionId, ReligionMapLevel level)
        {
            var def = Get(religionId);
            if (def == null) return Color.gray;

            switch (level)
            {
                case ReligionMapLevel.Religion:
                    return GetRoot(religionId)?.color ?? def.color;
                case ReligionMapLevel.Succession:
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
        Religion,     // 宗教（根）
        Succession,   // 教统（组织性单位——原宗派）
        Tradition     // 传统（礼拜仪轨/教义学派）
    }

    /// <summary>宗教节点类型（组织性质——非树级别）</summary>
    public enum ReligionNodeType
    {
        Religion,     // 宗教（根·组织最大单位）
        Succession,   // 教统（组织性单位·互斥——必有主教链/法脉——原"宗派"：
                      // 罗马公教会/东正教/各自主教会=独立教统）
        Tradition,    // 传统（思想性传承·可共存——禅宗/唯识宗/法华宗——可创建）
        School        // 学派（前组织形态·未稳定——无固定仪轨/座堂圣座——可演化）
    }
}
