using System.Collections.Generic;
using System.Text;
using CivilizationEvolution.Thought;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 宗教面板文本生成（纯静态可测——封圣/教义池改革/教统信息）：
    /// 教统信息（领袖/仪典语言/热忱/教阶）→ 支柱选择（可改革——
    /// 偏离度提示）→ 圣人列表（封圣产物）→ 教义池候选
    /// </summary>
    public static class ReligionPanelText
    {
        /// <summary>构建面板文本（国教教统 + 支柱 + 圣人）</summary>
        public static string Build(ReligionDef succession, FaithSystem faith, int statePatronSaintId)
        {
            var sb = new StringBuilder();
            if (succession == null)
            {
                sb.AppendLine("=== 宗教 ===");
                sb.AppendLine("当前政权未确立国教（可在外交/政体界面选择）");
                return sb.ToString();
            }

            sb.AppendLine($"=== {succession.religionName} ===");
            if (!string.IsNullOrEmpty(succession.headName))
                sb.AppendLine($"领袖：{succession.headName}");
            if (!string.IsNullOrEmpty(succession.communionName))
                sb.AppendLine($"共融：{succession.communionName}");
            if (!string.IsNullOrEmpty(succession.liturgicalLanguage))
                sb.AppendLine($"仪式语言：{succession.liturgicalLanguage}");
            if (!string.IsNullOrEmpty(succession.scripturalLanguage))
                sb.AppendLine($"经典语言：{succession.scripturalLanguage}");
            if (!string.IsNullOrEmpty(succession.riteFamily))
                sb.AppendLine($"礼仪传统：{succession.riteFamily}（{string.Join("/", succession.rites)}）");
            if (succession.hasSuccession)
                sb.AppendLine("传承：教统（主教链/法脉——组织性）");
            if (statePatronSaintId > 0)
                sb.AppendLine($"政权主保圣人：{GetSaintName(faith, statePatronSaintId)}");

            // 热忱（大圣战可用性）
            if (faith != null)
            {
                sb.AppendLine($"信仰热忱：{faith.fervor:F0}/100" +
                    (faith.CanDeclareGreatHolyWar() ? "（大圣战可用！）" : "（热忱≥60 或需领袖方可大圣战）"));
                sb.AppendLine($"教阶：{GetHierarchyName(faith.hierarchyLevel)}");
            }

            // 支柱选择（教义池——可改革——偏离度来源）
            sb.AppendLine();
            sb.AppendLine("--- 支柱选择 ---");
            if (succession.selectedDoctrines == null || succession.selectedDoctrines.Count == 0)
                sb.AppendLine("（未确立教义——改革可选项见下方候选池）");
            else
                foreach (var opt in succession.selectedDoctrines)
                {
                    var def = DoctrinePool.Get(opt);
                    if (def != null)
                        sb.AppendLine($"[{PillarName(def.pillar)}] {def.optionName}");
                }

            // 圣人列表（封圣产物——主保候选池）
            if (faith != null)
            {
                var saints = CanonizationSystem.GetSaints(faith.faithId);
                if (saints.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("--- 圣人（封圣） ---");
                    foreach (var s in saints)
                        sb.AppendLine($"{s.saintName}（{s.domain}——虔诚 {s.canonizationPiety:F0}）");
                }
            }
            return sb.ToString();
        }

        /// <summary>教义池改革候选文本（某支柱的可替换选项——中性+专属过滤）</summary>
        public static string BuildReformCandidates(ReligionDef succession, string pillar)
        {
            if (succession == null) return "";
            var sb = new StringBuilder();
            var options = DoctrinePool.GetOptions(pillar, succession.religionId);
            sb.AppendLine($"--- {PillarName(pillar)}改革候选 ---");
            foreach (var o in options)
                sb.AppendLine($"{o.optionId}：{o.optionName}" +
                    (o.exclusiveReligionIds.Count > 0 ? "（专属）" : ""));
            return sb.ToString();
        }

        private static string PillarName(string pillar)
        {
            switch (pillar)
            {
                case "doctrine": return "教义";
                case "ethics": return "伦理教法";
                case "ritual": return "仪式";
                case "experience": return "体验";
                case "institution": return "组织";
                case "myth": return "神话";
                case "material": return "物质";
                default: return pillar;
            }
        }

        private static string GetHierarchyName(int level)
        {
            switch (level)
            {
                case 0: return "无教阶（松散）";
                case 1: return "教区制";
                case 2: return "主教区制";
                case 3: return "大主教区制";
                case 4: return "枢机团/牧首会议";
                default: return level.ToString();
            }
        }

        private static string GetSaintName(FaithSystem faith, int saintId)
        {
            if (faith == null) return saintId.ToString();
            foreach (var s in CanonizationSystem.GetSaints(faith.faithId))
                if (s.saintId == saintId) return s.saintName;
            return saintId.ToString();
        }
    }
}
