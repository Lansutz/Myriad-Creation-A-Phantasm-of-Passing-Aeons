using System.Collections.Generic;
using System.Text;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 行政区划详情页文本（多级下钻——点政权总览里的区划→本页）：
    /// 区划名称/层级/辖境（地块数+人口）/治理头衔/子区划列表
    /// </summary>
    public static class RealmDivisionText
    {
        /// <summary>构建区划详情（division+全列表[子区划查询用]）</summary>
        public static string Build(AdminDivision division, IReadOnlyList<AdminDivision> all,
            long population = -1, string holderName = "", string realmName = "")
        {
            var sb = new StringBuilder();
            if (division == null)
            {
                sb.AppendLine("（区划不存在）");
                return sb.ToString();
            }

            string realmTag = string.IsNullOrEmpty(realmName) ? "" : $"【{realmName}】";
            sb.AppendLine($"=== {realmTag}{division.name} ===");
            sb.AppendLine($"层级：第 {division.level} 级行政区" +
                (division.level == 1 ? "（政权本身——治理根）" : ""));

            // 辖境
            sb.AppendLine($"辖境：{division.tiles.Count} 地块" +
                (population >= 0 ? $"｜人口 {population:N0}" : ""));

            // 治理头衔
            if (!string.IsNullOrEmpty(division.titleId))
            {
                var title = TitleCatalog.Get(division.titleId);
                string tName = title != null ? title.titleId : division.titleId;
                sb.AppendLine($"治理头衔：{tName}" +
                    (string.IsNullOrEmpty(holderName) ? "（空缺）" : $"｜治理者：{holderName}"));
            }

            // 子区划列表（下一级）
            var children = new List<AdminDivision>();
            if (all != null)
                foreach (var d in all)
                    if (d.parentDivisionId == division.divisionId) children.Add(d);
            if (children.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("── 下级区划 ──");
                foreach (var c in children)
                    sb.AppendLine($"· {c.name}（{c.tiles.Count} 地块）");
            }
            return sb.ToString();
        }
    }
}
