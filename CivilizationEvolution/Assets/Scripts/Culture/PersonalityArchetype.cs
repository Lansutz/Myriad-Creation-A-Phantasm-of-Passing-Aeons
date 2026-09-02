using System.Text;
using UnityEngine;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 原型学术画像描述器（性格组合→学术化画像短句——CK3 trait 描述风格）
    /// 非称号：原型=性格的学术画像（大五人格式维度词汇）——
    /// 称号/绰号/诨号/尊号是另一系统（行为后验——见 EpithetSystem）
    /// 108 特质组合全覆盖：任意七维画像总能生成描述（维度高/中/低三分段）
    /// </summary>
    public static class PersonalityArchetype
    {
        // ===== 学术维度（七维 → 大五式人格维度） =====

        /// <summary>进取性（大胆映射——≈外向性/敢为性）</summary>
        public static float Assertiveness(CharacterData c)
            => c == null ? 0.5f : (c.boldness + 100f) / 200f;

        /// <summary>仁厚性（悲悯×荣誉——≈宜人性——心性宽厚与信义）</summary>
        public static float Benevolence(CharacterData c)
            => c == null ? 0.5f : ((c.compassion + 100f) / 200f * 0.6f + (c.honor + 100f) / 200f * 0.4f);

        /// <summary>支配欲（贪婪×报复——≈宜人性负向/支配性）</summary>
        public static float Dominance(CharacterData c)
            => c == null ? 0.5f : ((c.greed + 100f) / 200f * 0.5f + (c.vengefulness + 100f) / 200f * 0.5f);

        /// <summary>审慎性（理性映射——≈尽责性/理智性）</summary>
        public static float Prudence(CharacterData c)
            => c == null ? 0.5f : (c.rationality + 100f) / 200f;

        /// <summary>信仰度（虔信映射——独立维度）</summary>
        public static float Devoutness(CharacterData c)
            => c == null ? 0.5f : (c.piety + 100f) / 200f;

        // ===== 学术短语库（高/中/低三分段——大五人格测试报告式学术语） =====

        private static string[] AssertiveDesc = { "进取性偏低，性喜守成，不尚开拓", "进取性居中，能守能攻，视势而为", "进取性突出，锐意开拓，敢为天下先" };
        private static string[] BenevolentDesc = { "宜人性偏低，心性峻刻，寡恩薄情", "宜人性适中，恩怨分明，不失仁厚", "宜人性偏高，宽厚温良，待人接物以仁" };
        private static string[] DominantDesc = { "支配欲淡泊，恬退自守，不慕权柄", "支配欲适中，善用权柄而不滥用", "支配欲炽盛，好胜争强，志在执掌" };
        private static string[] PrudentDesc = { "审慎性偏低，率性任情，凭好恶决断", "审慎性适中，谋定后动，不轻涉险", "审慎性突出，冷静理智，深谋远虑" };
        private static string[] DevoutDesc = { "信仰度淡漠，礼神多出于仪节", "信仰度适中，敬神而不狂信", "信仰度笃深，虔敬事神，以教立身" };

        private static string Segment(float v) => v >= 0.65f ? "2" : v <= 0.35f ? "0" : "1";

        /// <summary>
        /// 学术画像短句（CK3 trait 描述风格——散文式学术语）：
        /// 主句=进取×仁厚交叉画像（人格底色）＋ 修饰句=其余显著维度（±0.15 外）
        /// </summary>
        public static string Describe(CharacterData c)
        {
            if (c == null) return "性情未明";

            float asrt = Assertiveness(c);
            float bene = Benevolence(c);
            float dom = Dominance(c);
            float pru = Prudence(c);
            float dev = Devoutness(c);

            var sb = new StringBuilder();
            string asrtS = AssertiveDesc[int.Parse(Segment(asrt))];
            string beneS = BenevolentDesc[int.Parse(Segment(bene))];
            string domS = DominantDesc[int.Parse(Segment(dom))];
            string pruS = PrudentDesc[int.Parse(Segment(pru))];
            string devS = DevoutDesc[int.Parse(Segment(dev))];

            // 主句：人格底色（进取×仁厚交叉——两种气质大类）
            bool assertive = asrt >= 0.5f;
            bool benevolent = bene >= 0.5f;
            // 主句：纯性格底色（中性——不挂钩身份/职业——同一性格可以是
            // 农人也可以是君王——称号/尊号是另一系统的产出）
            if (assertive && benevolent)
                sb.Append("性进取而心仁厚：");
            else if (assertive && !benevolent)
                sb.Append("性雄烈而心峻刻：");
            else if (!assertive && benevolent)
                sb.Append("性宽和而好守成：");
            else
                sb.Append("性深沉而内敛：");

            sb.Append(asrtS).Append("；").Append(beneS).Append("；");

            // 修饰句：显著维度（偏离中庸 ±0.15 外——突出特征补充）
            bool any = false;
            if (Mathf.Abs(dom - 0.5f) > 0.15f) { sb.Append(domS); any = true; }
            if (Mathf.Abs(pru - 0.5f) > 0.15f) { if (any) sb.Append("；"); sb.Append(pruS); any = true; }
            if (Mathf.Abs(dev - 0.5f) > 0.15f) { if (any) sb.Append("；"); sb.Append(devS); }
            sb.Append("。");
            return sb.ToString();
        }

        /// <summary>原型类型标签（学术画像简名——非称号——内部归类用）</summary>
        public static string TypeName(CharacterData c)
        {
            if (c == null) return "未知";
            bool assertive = Assertiveness(c) >= 0.5f;
            bool benevolent = Benevolence(c) >= 0.5f;
            float dom = Dominance(c);
            float dev = Devoutness(c);
            if (dev >= 0.65f) return assertive ? "虔信进取型" : "虔信守成型";
            if (dom >= 0.65f) return assertive ? "雄略支配型" : "阴鸷权谋型";
            if (assertive && benevolent) return "仁厚开拓型";
            if (assertive) return "刚烈果决型";
            if (benevolent) return "温良守成型";
            return "沉静内敛型";
        }
    }

    /// <summary>
    /// 称号/绰号/诨号系统（行为后验荣誉——与原型[性格画像]分离）：
    /// 三档：普通绰号（中性事实——征服者/狐狸/狮子……）→
    /// 伟大者 the Great（评价≥优秀线——区域影响力中等偏上——发放较多——
    /// 历史参照：阿尔弗雷德/卡努特——不是严苛评价）→
    /// 传奇特殊（征服王/冒险王/诗人王——传奇线+领域——极难——亚历山大级）
    /// 谥号（死后——华夏式——按一生行为定谥）
    /// </summary>
    public static class EpithetSystem
    {
        /// <summary>授予绰号（已有不覆盖——除非升格传奇）</summary>
        public static bool GrantEpithet(CharacterData c, string epithet)
        {
            if (c == null || string.IsNullOrEmpty(epithet)) return false;
            if (!string.IsNullOrEmpty(c.epithet)) return false;
            c.epithet = epithet;
            return true;
        }

        /// <summary>传奇升格（征服者→征服王——覆盖普通绰号）</summary>
        public static bool PromoteToLegendary(CharacterData c, string legendaryEpithet)
        {
            if (c == null || string.IsNullOrEmpty(legendaryEpithet)) return false;
            c.epithet = legendaryEpithet; // 覆盖（一生最终评价）
            return true;
        }

        /// <summary>
        /// 行为评估并授予绰号（返回授予的绰号——空=未达任何条件）：
        /// 普通绰号=单项行为阈值；伟大者=评价≥优秀（350）；传奇=评价≥传奇（900）+领域
        /// </summary>
        public static string EvaluateAndGrant(CharacterData c, EvaluationSystem.AchievementRecord rec)
        {
            if (c == null) return "";
            float score = EvaluationSystem.CalculateScore(rec);
            EvaluationLevel level = EvaluationSystem.LevelFromScore(score);
            bool isRuler = c.role == CharacterRole.Ruler;

            // ===== ① 王级·传奇（苛刻——统治者化历史形象） =====
            if (isRuler)
            {
                // 征服王（跨文化大征服——亚历山大级——900 传奇线+苛刻领域）
                if (level >= EvaluationLevel.Legendary && rec.conquests >= 12 && rec.warsWon >= 15)
                    return PromoteToLegendary(c, "征服王") ? "征服王" : c.epithet;
                // 冒险王（一场史诗大冒险[传奇线+远征≥8 且征服≥3=大远征] 或 大量冒险事迹[远征≥12]）
                if (level >= EvaluationLevel.Legendary &&
                    ((rec.expeditions >= 8 && rec.conquests >= 3) || rec.expeditions >= 12))
                    return PromoteToLegendary(c, "冒险王") ? "冒险王" : c.epithet;
                // 诗人王（经历型：行吟远行多[远征≥5]+贤君[评价≥杰出 550]——哈拉尔德式）
                if (rec.expeditions >= 5 && score >= 550f && rec.poetryActs >= 3)
                    return PromoteToLegendary(c, "诗人王") ? "诗人王" : c.epithet;
                // 疯王（NPD 式统治风格——未必有病——卡利古拉/尼禄：自恋傲慢+偏执+
                // 喜怒无常[高报复+低理性]+任性妄为[高大胆+低荣誉]——统治 10 年+行为证据）
                if (HasTraitLevel(c, "arrogant", 2) && HasTraitAny(c, "paranoid") &&
                    c.vengefulness > 30f && c.rationality < -20f &&
                    c.boldness > 30f && c.honor < 0f && rec.reignYears >= 10f)
                    return GrantEpithet(c, "疯王") ? "疯王" : c.epithet;
            }

            // ===== ② 伟大者 the Great（区域影响力——②高评价档——独立于成就分） =====
            if (rec.regionalInfluence >= 0.6f)
            {
                if (GrantEpithet(c, "伟大者")) return "伟大者";
            }

            // ===== ③ 普通绰号（中性事实——行为/性格判定——先行） =====
            if (string.IsNullOrEmpty(c.epithet))
            {
                // 疯子（临床精神疾病——mentalDisorderId——任何身份——非疯王）
                if (!string.IsNullOrEmpty(c.mentalDisorderId))
                    return GrantEpithet(c, "疯子") ? "疯子" : "";
                // 征服者
                if (rec.conquests >= 5) return GrantEpithet(c, "征服者") ? "征服者" : "";
                // 狐狸（诈术）
                if (rec.schemesSucceeded >= 5) return GrantEpithet(c, "狐狸") ? "狐狸" : "";
                // 狮子（正面战功）
                if (rec.warsWon >= 8 && rec.defeatedBattles <= 2)
                    return GrantEpithet(c, "狮子") ? "狮子" : "";
                // 常胜者（无败仗）
                if (rec.warsWon >= 5 && rec.defeatedBattles == 0)
                    return GrantEpithet(c, "常胜者") ? "常胜者" : "";
                // 诗人（诗作——双向语义——真才或文人误国看语境）
                if (rec.poetryActs >= 4) return GrantEpithet(c, "诗人") ? "诗人" : "";
                // 智者（文治）
                if (rec.cultureActs >= 6) return GrantEpithet(c, "智者") ? "智者" : "";
                // 虔诚者（宗教）
                if (rec.religionActs >= 6) return GrantEpithet(c, "虔诚者") ? "虔诚者" : "";
                // 叛教者（改宗 2 次+——公开背弃原信仰）
                if (rec.faithChanges >= 2) return GrantEpithet(c, "叛教者") ? "叛教者" : "";
                // 警觉者（解危）
                if (rec.threatsResolved >= 5) return GrantEpithet(c, "警觉者") ? "警觉者" : "";
                // 忍耐者（低报复+在位久+解危多——逆境坚持）
                if (rec.reignYears >= 20f && c.vengefulness < 0f && rec.threatsResolved >= 3)
                    return GrantEpithet(c, "忍耐者") ? "忍耐者" : "";
                // 仁慈者（悲悯极高+少征战）
                if (c.compassion > 60f && rec.warsWon <= 2)
                    return GrantEpithet(c, "仁慈者") ? "仁慈者" : "";
                // 公正者（无饥荒+少叛乱+在位久）
                if (!rec.famineUnderRule && rec.reignYears >= 15f)
                    return GrantEpithet(c, "公正者") ? "公正者" : "";
                // 谨慎者（低大胆+无败仗）
                if (c.boldness < -40f && rec.defeatedBattles == 0 && rec.warsWon >= 1)
                    return GrantEpithet(c, "谨慎者") ? "谨慎者" : "";
                // 勇敢者（高大胆+胜仗）
                if (c.boldness > 50f && rec.warsWon >= 3)
                    return GrantEpithet(c, "勇敢者") ? "勇敢者" : "";
                // 远行者（远征多——见过世面——诗人王的基础）
                if (rec.expeditions >= 4) return GrantEpithet(c, "远行者") ? "远行者" : "";
                // 无地者（失地+未再征服——反讽）
                if (rec.lostAllLands > 0 && rec.conquests == 0)
                    return GrantEpithet(c, "无地者") ? "无地者" : "";
                // 狼（征服+低悲悯——残暴征战）
                if (rec.conquests >= 4 && c.compassion < -40f)
                    return GrantEpithet(c, "狼") ? "狼" : "";
            }

            // ===== ④ 外貌/身体型（bodyMarks——事件/伤病写入——任何身份——
            // 成对体系：黑/白——统治者=王/继承人或贵族=王子——其余身份=者） =====
            if (string.IsNullOrEmpty(c.epithet) && c.bodyMarks != null)
            {
                foreach (var mark in c.bodyMarks)
                {
                    string granted = null;
                    switch (mark)
                    {
                        case "秃顶": granted = "秃头"; break;
                        case "跛足": granted = "瘸子"; break;
                        case "失明": granted = "瞎子"; break;
                        case "矮小": granted = "矮子"; break;
                        case "红须": granted = "红胡子"; break;
                        case "黑色": granted = isRuler ? "黑王" : HasPrinceRole(c) ? "黑王子" : "黑者"; break;
                        case "白色": granted = isRuler ? "白王" : HasPrinceRole(c) ? "白王子" : "白者"; break;
                    }
                    if (granted != null) return GrantEpithet(c, granted) ? granted : "";
                }
            }
            return c.epithet;
        }

        private static bool HasTraitLevel(CharacterData c, string baseId, int minLevel)
        {
            if (c.traits == null) return false;
            foreach (var t in c.traits)
                if (t.traitId == baseId + "_" + minLevel || t.traitId == baseId + "_3")
                    if (t.traitId.EndsWith("_" + minLevel) || (minLevel == 2 && t.traitId.EndsWith("_3")))
                        return true;
            return false;
        }

        private static bool HasTraitAny(CharacterData c, string baseId)
        {
            if (c.traits == null) return false;
            foreach (var t in c.traits)
                if (t.traitId.StartsWith(baseId + "_")) return true;
            return false;
        }

        private static bool HasPrinceRole(CharacterData c)
            => c.role == CharacterRole.Heir || c.role == CharacterRole.Noble;

            /// <summary>
        /// 华夏谥号判定（死后——按一生行为定谥——上谥/平谥/下谥）：
        /// 基于行为统计（征战/仁政/文治——简化用七维+计数）
        /// </summary>
        public static string DeterminePosthumousTitle(CharacterData c, int warsWon, int conquests, bool famineUnderRule)
        {
            if (c == null) return "";
            // 下谥优先（暴行/失德）
            if (c.greed < -50f && c.compassion < -30f) return "厉";
            if (famineUnderRule) return "荒";
            if (c.vengefulness > 60f && c.compassion < -40f) return "暴";
            // 上谥（武功/文治/仁德）
            if (conquests >= 3 || warsWon >= 5) return c.compassion >= 0f ? "武" : "烈";
            if (c.boldness >= 0f && (conquests > 0 || warsWon > 0)) return "武";
            if (c.compassion > 50f) return "仁";
            if (c.rationality > 50f) return "明";
            // 平谥
            return "平";
        }
    }
}
