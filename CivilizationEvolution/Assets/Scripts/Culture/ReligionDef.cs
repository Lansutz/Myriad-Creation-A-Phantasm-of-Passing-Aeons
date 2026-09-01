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

        /// <summary>宇宙观形态（一神/多神/二元/泛灵/祭祀型/mana）</summary>
        public string worldview = "";
        /// <summary>创教者（琐罗亚斯德/摩尼/佛陀/耶稣/穆罕默德——宗教创生事件）</summary>
        public string founder = "";
        /// <summary>正统/异端标记（教派斗争——正统清洗异端）</summary>
        public bool orthodoxy = true;
        /// <summary>美德特质（宗教对性格的判定——引用 PersonalityTraitDatabase 基 id：
        /// 持美德者虔诚+同信仰好感+可成圣人候选）</summary>
        public List<string> virtues = new List<string>();
        /// <summary>罪行特质（宗教对性格的判定——持罪行者虔诚-+罪行标记：
        /// 同一性格在不同宗教判定不同——lustful 天主教=罪行/肉欲高扬信仰=美德）</summary>
        public List<string> sins = new List<string>();
        /// <summary>教统领袖（中性：教宗/牧首/伊玛目/谢赫/祖师）</summary>
        public string headName = "";
        /// <summary>礼仪领袖（可空——米兰主教=安布罗修礼；教宗=拉丁礼）</summary>
        public string riteHeadName = "";
        /// <summary>共融归属（罗马共融/东正共融/乌玛/苏菲道统群）</summary>
        public string communionName = "";
        /// <summary>主流传统标记（传播基准——大众实践；领袖传统=正统基准——
        /// 无领袖传统的宗教用共识[逊尼=乌里玛共识/多神=无中央标准]）</summary>
        public bool isMainstreamTradition = false;
        /// <summary>支柱选择（该节点从 DoctrinePool 选的选项 id——教统的支柱
        /// 以领袖传统为准；选项差异=偏离度来源）</summary>
        public List<string> selectedDoctrines = new List<string>();
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

        /// <summary>
        /// 裂教（Schism——创建新教统——同一宗教内教统分裂）
        /// 条件由调用方判定（传统偏离 80+ + 传承）——1054 大分裂同构
        /// </summary>
        public static ReligionDef CreateSuccession(int parentId, string name, string headName, string rite, string riteFamily)
        {
            int id = _nextId++;
            var def = new ReligionDef
            {
                religionId = id,
                religionName = name,
                parentReligionId = parentId,
                schoolParentId = -1,
                nodeType = ReligionNodeType.Succession,
                hasSuccession = true,
                headName = headName,
                communionName = Get(parentId)?.communionName ?? "",
                riteFamily = riteFamily
            };
            if (!string.IsNullOrEmpty(rite))
                def.rites.Add(rite);
            _religions[id] = def;
            return def;
        }

        /// <summary>
        /// 宗教创生（创建新宗教——新根节点）
        /// 三路径：异端升格（基督教←犹太教）/融合（摩尼教）/独立崇拜升格（雅威→犹太教）
        /// </summary>
        public static ReligionDef CreateReligion(string name, string worldview, string founder, int schoolParentId = -1)
        {
            int id = _nextId++;
            var def = new ReligionDef
            {
                religionId = id,
                religionName = name,
                parentReligionId = -1,
                schoolParentId = schoolParentId,
                nodeType = ReligionNodeType.Religion,
                hasSuccession = true,
                worldview = worldview,
                founder = founder
            };
            _religions[id] = def;
            return def;
        }

        /// <summary>
        /// 偏离度计算（支柱选项差异加权——个人层/传统层共用）
        /// 同选项=0；同支柱不同选项=30（变体）；对立选项=60；无选择对照=按支柱权重
        /// 权重：教义 0.30/仪式 0.20/伦理 0.15/制度 0.15/神话 0.10/体验 0.05/物质 0.05
        /// </summary>
        public static float GetDivergence(ReligionDef a, ReligionDef b)
        {
            if (a == null || b == null) return 0f;
            // 任一方无支柱选择=无既定标准（未成形/原始崇拜）——偏离 0
            if (a.selectedDoctrines.Count == 0 || b.selectedDoctrines.Count == 0) return 0f;

            // 对称偏离：两向差异取平均（a 有 b 无 + b 有 a 无）
            float scoreA = CalcOneWayDivergence(a, b);
            float scoreB = CalcOneWayDivergence(b, a);
            return Mathf.Min(100f, (scoreA + scoreB) * 0.5f);
        }

        private static float CalcOneWayDivergence(ReligionDef from, ReligionDef to)
        {
            float score = 0f;
            foreach (var d in from.selectedDoctrines)
            {
                if (to.selectedDoctrines.Contains(d)) continue; // 同选项=0
                var option = DoctrinePool.Get(d);
                if (option == null) continue;
                float weight = GetPillarWeight(option.pillar);
                // 教义支柱冲突最重（×1.0）——其他支柱 ×0.8（行为/实践分歧轻于教义）
                score += 30f * weight * (option.pillar == "doctrine" ? 1f : 0.8f);
            }
            return score;
        }

        private static float GetPillarWeight(string pillar)
        {
            switch (pillar)
            {
                case "doctrine": return 0.30f;
                case "ritual": return 0.20f;
                case "ethics": return 0.15f;
                case "institution": return 0.15f;
                case "myth": return 0.10f;
                case "experience": return 0.05f;
                case "material": return 0.05f;
                default: return 0.1f;
            }
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
        School,       // 学派（前组织形态·未稳定——无固定仪轨/座堂圣座——可演化）
        ReligiousSchool // 宗教学派（宗教内部思想学派——谶纬/经院学派/教法学派——
                      // 与世俗学派（School）区分：世俗学派=宗教的原料[可宗教化]；
                      // 宗教学派=宗教的产物[宗教产生后才有的内部分支]）
    }
}
