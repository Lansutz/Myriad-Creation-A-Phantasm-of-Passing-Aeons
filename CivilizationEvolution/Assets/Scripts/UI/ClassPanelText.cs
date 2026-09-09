using System;
using System.Text;
using System.Collections.Generic;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.UI
{
 /// 阶层 UI 面板文本构建器（纯静态可测，TMP 富文本）
 /// 展示：整体社会概览 + 各阶层详情（人口/满足/忠诚/组织/影响/动荡/支持 + 8维需求条形图 + 主要不满）
    public static class ClassPanelText
    {
 // —— 颜色常量（TMP 富文本）——
        const string C_HEADER = "<color=#E8D5B7>";      // 标题色（暖金）
        const string C_GOOD = "<color=#7CB342>";          // 良好（绿）
        const string C_WARN = "<color=#FFB300>";          // 警告（橙）
        const string C_BAD = "<color=#E53935>";           // 危险（红）
        const string C_DIM = "<color=#888888>";           // 暗淡
        const string C_VALUE = "<color=#FFFFFF>";          // 数值白
        const string C_END = "</color>";

        const int BAR_WIDTH = 10;  // 条形图格数

 /// <summary>需求维度中文名</summary>
        public static string GetDimensionName(ClassNeedDimension d) => d switch
        {
            ClassNeedDimension.Subsistence => "生存保障",
            ClassNeedDimension.Security => "人身安全",
            ClassNeedDimension.TaxBurden => "税负合理",
            ClassNeedDimension.PoliticalAccess => "政治通道",
            ClassNeedDimension.EconomicOpportunity => "经济机会",
            ClassNeedDimension.InstitutionalRecognition => "制度承认",
            ClassNeedDimension.Legitimacy => "合法秩序",
            ClassNeedDimension.Privilege => "特权保障",
            _ => d.ToString()
        };

 /// <summary>分数→颜色标签（0-45红 / 45-65橙 / 65+绿）</summary>
        static string ScoreColor(float score)
        {
            if (score < ClassNeedsSystem.GrievanceThreshold) return C_BAD;
            if (score < 65f) return C_WARN;
            return C_GOOD;
        }

 /// <summary>生成文本条形图（含颜色）</summary>
        static string Bar(float score)
        {
            int filled = Math.Clamp((int)Math.Round(score / 100f * BAR_WIDTH), 0, BAR_WIDTH);
            string bar = new string('█', filled) + new string('░', BAR_WIDTH - filled);
            return $"{ScoreColor(score)}{bar}{C_END}";
        }

 /// <summary>整体社会概览（动荡分/主导阶层/最动荡阶层/总人口）</summary>
        public static string BuildOverview(RealmSociety society)
        {
            if (society == null || society.classes == null || society.classes.Count == 0)
                return $"{C_DIM}（无社会画像数据）{C_END}\n";

            var sb = new StringBuilder();
            sb.AppendLine($"{C_HEADER}── 社会概览 ──{C_END}");
            sb.AppendLine($"整体动荡 {ScoreColor(society.unrestScore)}{society.unrestScore:F0}{C_END}" +
                          $" | 总人口 {C_VALUE}{society.totalPopulation:N0}{C_END}" +
                          $" | 主导阶层 {C_VALUE}{ClassNames.Get(society.dominantClass)}{C_END}" +
                          $" | 最动荡 {ScoreColor(70f)}{ClassNames.Get(society.mostRestlessClass)}{C_END}");
            sb.AppendLine();
            return sb.ToString();
        }

 /// <summary>单个阶层详情（核心指标 + 8维需求条形图 + 主要不满）</summary>
        public static string BuildClassDetail(ClassProfile p)
        {
            if (p == null) return "";
            var sb = new StringBuilder();
            string clsName = ClassNames.Get(p.socialClass);

 // —— 标题行：阶层名 + 人口 + 占比 ——
            sb.AppendLine($"{C_HEADER}▶ {clsName}{C_END}" +
                          $"  人口 {C_VALUE}{p.population:N0}{C_END}" +
                          $"（{C_VALUE}{p.populationShare * 100f:F1}%{C_END}）");

 // —— 核心指标行 ——
            sb.AppendLine($"  满足 {ScoreColor(p.satisfaction)}{p.satisfaction:F0}{C_END}" +
                          $" | 忠诚 {ScoreColor(p.loyalty)}{p.loyalty:F0}{C_END}" +
                          $" | 组织 {C_VALUE}{p.organization:F2}{C_END}" +
                          $" | 影响 {C_VALUE}{p.influence:F0}{C_END}" +
                          $" | 动荡 {ScoreColor(100f - p.unrest)}{p.unrest:F0}{C_END}" +
                          $" | 支持 {ScoreColor(p.support)}{p.support:F0}{C_END}");

 // —— 主要不满 ——
            if (!string.IsNullOrEmpty(p.chiefGrievanceReason) || p.chiefGrievance != default)
            {
                string dimName = p.chiefGrievance != default
                    ? GetDimensionName(p.chiefGrievance)
                    : "未知";
                string reason = string.IsNullOrEmpty(p.chiefGrievanceReason) ? "" : $" — {p.chiefGrievanceReason}";
                sb.AppendLine($"  {C_BAD}主要不满：{dimName}{reason}{C_END}");
            }

 // —— 8维需求条形图（2列布局，4行）——
            if (p.needReport != null && p.needReport.dimensions != null && p.needReport.dimensions.Count > 0)
            {
                sb.AppendLine($"  {C_DIM}── 需求维度 ──{C_END}");
                var dims = new List<ClassNeedDimension>(p.needReport.dimensions.Keys);
 // 按权重顺序排列（枚举顺序即权重表顺序）
                dims.Sort((a, b) => ((int)a).CompareTo((int)b));

                for (int i = 0; i < dims.Count; i += 2)
                {
                    string left = DimBar(dims[i], p.needReport);
                    string right = (i + 1 < dims.Count) ? DimBar(dims[i + 1], p.needReport) : "";
                    sb.AppendLine($"  {left}    {right}");
                }
            }
            sb.AppendLine();
            return sb.ToString();
        }

 /// <summary>单个维度的"名称+条形图+分数"字符串</summary>
        static string DimBar(ClassNeedDimension d, ClassNeedReport report)
        {
            float score = report.dimensions.TryGetValue(d, out var v) ? v.score : 0f;
 // 权重为0的维度（该阶层不关心）暗淡显示
            bool relevant = score > 0f || report.dimensions.ContainsKey(d);
            string name = GetDimensionName(d);
            if (!relevant) return $"{C_DIM}{name,-6} {new string('░', BAR_WIDTH)}  --{C_END}";
            return $"{name,-4} {Bar(score)} {ScoreColor(score)}{score:F0}{C_END}";
        }

 /// <summary>构建完整阶层面板（概览 + 全部阶层详情，按人口占比降序）</summary>
        public static string BuildFull(RealmSociety society)
        {
            var sb = new StringBuilder();
            sb.Append(BuildOverview(society));

            if (society != null && society.classes != null && society.classes.Count > 0)
            {
                sb.AppendLine($"{C_HEADER}── 阶层详情 ──{C_END}");
 // 按人口占比降序排列
                var sorted = new List<KeyValuePair<GameEnums.SocialClass, ClassProfile>>(society.classes);
                sorted.Sort((a, b) => b.Value.populationShare.CompareTo(a.Value.populationShare));
                foreach (var kv in sorted)
                    sb.Append(BuildClassDetail(kv.Value));
            }
            return sb.ToString();
        }
    }
}
