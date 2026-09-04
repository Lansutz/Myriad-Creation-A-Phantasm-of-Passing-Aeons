using System.Collections.Generic;
using System.Text;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Thought;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 政权总览面板文本（设计定稿：点政权→人口/国库/官职/宗教聚合——
    /// 全局数值不上顶栏——政权级数据集中于此）：
    /// 聚合各已有系统（RealmSociety.totalPopulation 人口/Economy 国库/
    /// officeHolders 官职/ReligionDef 国教）——纯静态可测
    /// </summary>
    public static class RealmOverviewText
    {
        /// <summary>构建政权总览（realm+society 必传——官职/宗教可选聚合）</summary>
        public static string Build(RealmData realm, RealmSociety society,
            IReadOnlyDictionary<int, string> officeDisplay = null,
            ReligionDef stateReligion = null, string patronSaintName = "",
            bool isPlayerRealm = false,
            IReadOnlyList<AdminDivision> adminDivisions = null,
            int? selectedDivisionId = null)
        {
            var sb = new StringBuilder();
            if (realm == null)
            {
                sb.AppendLine("=== 政权总览 ===");
                sb.AppendLine("（未选中政权——点击地图上的政权查看）");
                return sb.ToString();
            }

            string tag = isPlayerRealm ? "（本家）" : "";
            sb.AppendLine($"=== {realm.realmName}{tag} ===");

            // 人口（RealmSociety.totalPopulation——已有系统——count×50 人）
            long pop = society != null ? (long)(society.totalPopulation * 50f) : 0;
            sb.AppendLine($"人口：{pop:N0} 人");

            // 国力
            sb.AppendLine($"国库：{realm.treasury:F0} | 稳定：{realm.stability:F0} | 集权：{realm.centralization:F2}");

            // 政体摘要（成分——最简：交接方式名）
            if (realm.composition != null)
            {
                var comp = realm.composition;
                // 政体二制（神权制=成分组合[ReligiousCouncil+教阶]——粗显君主/共和）
                string sov = comp.supremeSovereignty == GovernmentConstraints.SupremeSovereignty.Monarchy
                    ? "君主制" : "共和制";
                string succ = GovernmentConstraints.GetComponentName(
                    GovernmentConstraints.GovernmentDimension.SupremeSuccession,
                    comp.supremeSuccession.primary);
                sb.AppendLine($"政体：{sov}（交接：{succ}）");
            }

            // 国教（宗教聚合）
            if (stateReligion != null)
            {
                string religionLine = $"国教：{stateReligion.religionName}";
                if (!string.IsNullOrEmpty(patronSaintName))
                    religionLine += $" | 主保圣人：{patronSaintName}";
                sb.AppendLine(religionLine);
            }

            // 官职体系（officeHolders 聚合）
            if (officeDisplay != null && officeDisplay.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("── 官职体系 ──");
                foreach (var kv in officeDisplay)
                    sb.AppendLine(kv.Value);
            }

            // 行政区划树（治理层级——分封 4/郡县 2-5——树状缩进——
            // 选中区划时显示其详情[RealmDivisionText]）
            if (adminDivisions != null && adminDivisions.Count > 1)
            {
                sb.AppendLine();
                sb.AppendLine("── 行政区划 ──");
                AppendTree(sb, adminDivisions, realm.realmId, -1, selectedDivisionId); // 根 parent=-1
            }

            return sb.ToString();
        }

        /// <summary>树状递归（层缩进——标记选中）</summary>
        private static void AppendTree(System.Text.StringBuilder sb,
            IReadOnlyList<AdminDivision> all, int realmId, int parentId, int? selected)
        {
            foreach (var d in all)
            {
                if (d.realmId != realmId || d.parentDivisionId != parentId) continue;
                string mark = selected.HasValue && selected.Value == d.divisionId ? "▶ " : "  ";
                string title = string.IsNullOrEmpty(d.titleId) ? "" : $"（{d.titleId}）";
                sb.AppendLine($"{mark}{new string('　', d.level - 1)}{d.name}{title}");
                AppendTree(sb, all, realmId, d.divisionId, selected); // 递归子区划
            }
        }
    }
}
