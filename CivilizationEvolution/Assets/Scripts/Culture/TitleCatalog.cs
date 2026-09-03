using System.Collections.Generic;
using System.Linq;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 头衔目录查询（TitleDef 数据驱动表——ContentRegistry.Titles——
    /// 三类[官僚/贵族/君主]+国名后缀——文化专属优先回退通用——
    /// 位阶值柔性比较/同级权重选择）
    /// </summary>
    public static class TitleCatalog
    {
        /// <summary>取头衔（titleId）</summary>
        public static TitleDef Get(string titleId)
        {
            if (string.IsNullOrEmpty(titleId)) return null;
            return ContentRegistry.Titles.TryGetValue(titleId, out var t) ? t : null;
        }

        /// <summary>某类头衔（kind——按位阶降序——可选文化专属优先）</summary>
        public static List<TitleDef> ByKind(string kind, int cultureId = -1)
        {
            var list = new List<TitleDef>();
            foreach (var t in ContentRegistry.Titles.Values)
            {
                if (t.kind != kind) continue;
                // 文化专属优先收集；通用也收集（回退用）
                if (t.cultureId >= 0 && t.cultureId != cultureId) continue;
                list.Add(t);
            }
            list.Sort((a, b) => b.rank.CompareTo(a.rank));
            return list;
        }

        /// <summary>某类最高位阶头衔（文化专属优先——无专属回退通用）</summary>
        public static TitleDef Highest(string kind, int cultureId = -1)
        {
            TitleDef exclusive = null;
            TitleDef fallback = null;
            foreach (var t in ContentRegistry.Titles.Values)
            {
                if (t.kind != kind) continue;
                if (t.cultureId == cultureId)
                {
                    if (exclusive == null || t.rank > exclusive.rank) exclusive = t;
                }
                else if (t.cultureId < 0)
                {
                    if (fallback == null || t.rank > fallback.rank) fallback = t;
                }
            }
            return exclusive ?? fallback;
        }

        /// <summary>同级内权重选择（多个候选——权重加权随机——同级微差的文化偏好）</summary>
        public static TitleDef PickByWeight(List<TitleDef> candidates, System.Random rng = null)
        {
            if (candidates == null || candidates.Count == 0) return null;
            rng = rng ?? new System.Random();
            float total = 0f;
            foreach (var c in candidates) total += c.weight;
            float roll = (float)(rng.NextDouble() * total);
            foreach (var c in candidates)
            {
                roll -= c.weight;
                if (roll <= 0f) return c;
            }
            return candidates[candidates.Count - 1];
        }

        /// <summary>位阶比较（柔性实数——大等级=整数部分——同级微差=小数——
        /// 返回 a 是否高于 b）</summary>
        public static bool IsHigher(TitleDef a, TitleDef b)
            => a != null && b != null && a.rank > b.rank;
    }
}
