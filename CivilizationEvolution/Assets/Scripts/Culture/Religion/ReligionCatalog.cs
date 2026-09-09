using System.Collections.Generic;
using UnityEngine;

namespace CivilizationEvolution.Culture
{
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

 /// 创建传统（动态——宗教演化：新教义/新仪轨定型→新传统节点；
 /// 从属指定教统/传统——任意深度）
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

 /// 创建礼（动态——礼仪实践形成：向指定节点（教统/传统）的
 /// rites 列表添加具体礼名——礼归属可变）
        public static void CreateRite(int religionId, string riteName, string riteFamily = "")
        {
            if (!_religions.TryGetValue(religionId, out var def)) return;
            if (!def.rites.Contains(riteName))
                def.rites.Add(riteName);
            if (!string.IsNullOrEmpty(riteFamily))
                def.riteFamily = riteFamily;
        }

 /// 裂教（Schism——创建新教统——同一宗教内教统分裂）
 /// 条件由调用方判定（传统偏离 80+ + 传承）——1054 大分裂同构
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

 /// 宗教创生（创建新宗教——新根节点）
 /// 三路径：异端升格（基督教←犹太教）/融合（摩尼教）/独立崇拜升格（雅威→犹太教）
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

 /// 偏离度计算（支柱选项差异加权——个人层/传统层共用）
 /// 同选项=0；同支柱不同选项=30（变体）；对立选项=60；无选择对照=按支柱权重
 /// 权重：教义 0.30/仪式 0.20/伦理 0.15/制度 0.15/神话 0.10/体验 0.05/物质 0.05
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

 /// 宗教形态是否可用（支撑革新判定——仿政体成分×革新）：
 /// requiredInnovations 任一持有即可（OR 语义）；空=基础可用；
 /// innovations null=宽松（不校验）
        public static bool IsAvailable(int religionId, System.Func<int, bool> hasInnovation)
        {
            var def = Get(religionId);
            if (def == null) return true;
            if (def.requiredInnovations == null || def.requiredInnovations.Count == 0) return true;
            if (hasInnovation == null) return true; // 宽松模式
            foreach (var req in def.requiredInnovations)
                if (hasInnovation(req)) return true;
            return false;
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
}
