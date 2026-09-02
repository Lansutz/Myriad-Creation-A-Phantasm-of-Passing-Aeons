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
                // 圣君（在位圣明——尧舜式——德行×治理双极致：评价≥卓越[750]+
                // 悲悯荣誉双高[60+]+无饥荒少叛乱——王级苛刻——与圣者[死后封圣]区分）
                if (score >= 750f && c.compassion >= 60f && c.honor >= 60f &&
                    !rec.famineUnderRule && rec.reignYears >= 15f)
                    return PromoteToLegendary(c, "圣君") ? "圣君" : c.epithet;
            }

            // ===== ② 伟大者 the Great（区域影响力——②高评价档——独立于成就分） =====
            if (rec.regionalInfluence >= 0.6f)
            {
                if (GrantEpithet(c, "伟大者")) return "伟大者";
            }

            // ===== ③ 普通绰号（中性事实——行为/性格判定——先行） =====
            if (string.IsNullOrEmpty(c.epithet))
            {
                // 疯子/疯女（临床精神疾病——按性别：男性=疯子/女性=疯女[胡安娜式]）
                if (!string.IsNullOrEmpty(c.mentalDisorderId))
                    return GrantEpithet(c, c.isMale ? "疯子" : "疯女") ? (c.isMale ? "疯子" : "疯女") : "";
                // 无冠者（实权无冕——非在位君主但达成君主级成就——宫相式——
                // 身份型综合先于行为型绰号判定）
                if (!isRuler && EvaluationSystem.CalculateScore(rec) >= 550f &&
                    (rec.warsWon >= 5 || rec.conquests >= 3 || rec.reignYears >= 10f))
                    return GrantEpithet(c, "无冠者") ? "无冠者" : "";

                // ===== 女性专有判定（女性特有境遇——先于通用行为型） =====
                if (!c.isMale)
                {
                    // 圣女（女性封圣——贞德式——圣者女性版）
                    if (rec.canonized) return GrantEpithet(c, "圣女") ? "圣女" : "";
                    // 童贞女王（终身未婚的女王——死时无配偶）
                    if (isRuler && c.spouseId < 0 && rec.reignYears >= 5f)
                        return GrantEpithet(c, "童贞女王") ? "童贞女王" : "";
                    // 母狼（女性残酷叛逆——密谋+低悲悯+报复——伊莎贝拉式）
                    if (c.compassion < -30f && c.vengefulness > 40f && rec.schemesSucceeded >= 3)
                        return GrantEpithet(c, "母狼") ? "母狼" : "";
                    // 黑王后（女性统治者/摄政+阴谋——凯瑟琳·德·美第奇式）
                    if (isRuler && rec.schemesSucceeded >= 5 && c.honor < 0f)
                        return GrantEpithet(c, "黑王后") ? "黑王后" : "";
                    // 富女（富国/富庶稳定女君——勃艮第玛丽式）
                    if (isRuler && rec.reignYears >= 15f && !rec.famineUnderRule
                        && rec.rebellions == 0)
                        return GrantEpithet(c, "富女") ? "富女" : "";
                    // 美人（女性俊美——bodyMarks——美男子女性版）
                    if (HasBodyMark(c, "俊美")) return GrantEpithet(c, "美人") ? "美人" : "";
                }

                // 年轻者（青年路易二世式讽刺：幼年即位[<16]是事实——不够老练才是
                // 核心语义——被摄政架空或决策反复[低理性]——幼稚暗讽）
                if (isRuler && rec.youngAccession &&
                    (rec.ruledUnderRegency || c.rationality < -20f))
                    return GrantEpithet(c, "年轻者") ? "年轻者" : "";
                // 护国公（摄政护主——非君主但掌实权+名正言顺[为幼主/空位摄政——
                // 区别于无冠者[僭越式实权]——克伦威尔式）
                if (!isRuler && rec.ruledUnderRegency && rec.reignYears >= 8f &&
                    EvaluationSystem.CalculateScore(rec) >= 350f)
                    return GrantEpithet(c, "护国公") ? "护国公" : "";
                // 癞病人（病绰号——癞病[麻风]——bodyMarks）
                if (HasBodyMark(c, "癞病")) return GrantEpithet(c, "癞病人") ? "癞病人" : "";
                // 驼背（bodyMarks）
                if (HasBodyMark(c, "驼背")) return GrantEpithet(c, "驼背") ? "驼背" : "";
                // 美男子（外观俊美——bodyMarks）
                if (HasBodyMark(c, "俊美")) return GrantEpithet(c, "美男子") ? "美男子" : "";
                // 金口（雄辩——外交诈术/调停等言辞成就高——非泛演说）
                if (rec.schemesSucceeded >= 6 && c.rationality >= 40f)
                    return GrantEpithet(c, "金口") ? "金口" : "";
                // 背信者（背盟撕约——faithChanges 反向[外交背盟计数近似]——
                // 用 schemesSucceeded 高+honor 低=言而无信者）
                if (c.honor < -40f && rec.schemesSucceeded >= 4)
                    return GrantEpithet(c, "背信者") ? "背信者" : "";
                // 狂暴者（高报复+高大胆+征战——暴怒君主）
                if (c.vengefulness > 60f && c.boldness > 50f && rec.warsWon >= 5)
                    return GrantEpithet(c, "狂暴者") ? "狂暴者" : "";
                // 调停者（促成和平——多次止战——低战+外交）
                if (rec.reignYears >= 15f && rec.warsWon <= 1 && rec.schemesSucceeded >= 3)
                    return GrantEpithet(c, "调停者") ? "调停者" : "";
                // 不幸者（败仗+灾祸——与幸运者成对）
                if (rec.defeatedBattles >= 4 || (rec.famineUnderRule && rec.defeatedBattles >= 2))
                    return GrantEpithet(c, "不幸者") ? "不幸者" : "";
                // 鹰（威仪+大捷+高荣誉——帝国形象）
                if (c.honor >= 50f && rec.warsWon >= 6 && rec.defeatedBattles == 0)
                    return GrantEpithet(c, "鹰") ? "鹰" : "";
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
                // 受爱戴者（无叛乱+宽仁+在位久——民心所向——先于公正者[司法]）
                if (rec.rebellions == 0 && c.compassion >= 40f && rec.reignYears >= 15f)
                    return GrantEpithet(c, "受爱戴者") ? "受爱戴者" : "";
                // 被憎恨者（叛乱多+苛政——与受爱戴者对立）
                if (rec.rebellions >= 3 && c.compassion < 0f)
                    return GrantEpithet(c, "被憎恨者") ? "被憎恨者" : "";
                // 无情者（悲悯极低+铁腕成就——冷酷统治——与仁慈者成对）
                if (c.compassion < -60f && (rec.warsWon >= 3 || rec.threatsResolved >= 2))
                    return GrantEpithet(c, "无情者") ? "无情者" : "";
                // 公正者（司法稳定=无饥荒+无叛乱+在位久——与和平者[对外无战]区分）
                if (!rec.famineUnderRule && rec.reignYears >= 15f && rec.rebellions == 0)
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

            // ===== 第二批判定（行为/特征扩充） =====
            if (string.IsNullOrEmpty(c.epithet))
            {
                // 屠夫（屠城——接 Massacre 系统）
                if (rec.massacres >= 1) return GrantEpithet(c, "屠夫") ? "屠夫" : "";
                // 胖子（肥胖值——接肥胖系统）
                if (c.obesity > 70f) return GrantEpithet(c, "胖子") ? "胖子" : "";
                // 篡位者（篡位上位）
                if (rec.usurpedThrone) return GrantEpithet(c, "篡位者") ? "篡位者" : "";
                // 恐怖者（暴政恐惧统治——低悲悯+镇压[屠城/平叛/征伐]+在位久——伊凡雷帝）
                if (c.compassion < -60f &&
                    (rec.threatsResolved >= 3 || rec.warsWon >= 5 || rec.massacres >= 1)
                    && rec.reignYears >= 10f)
                    return GrantEpithet(c, "恐怖者") ? "恐怖者" : "";
                // 和平者（在位久+无战）
                if (rec.reignYears >= 20f && rec.warsWon == 0 && rec.conquests == 0)
                    return GrantEpithet(c, "和平者") ? "和平者" : "";
                // 学者（学术成就——高于智者门槛）
                if (rec.cultureActs >= 10) return GrantEpithet(c, "学者") ? "学者" : "";
                // 宽宏者（低贪婪+宽仁——与吝啬鬼成对）
                if (c.greed < -40f && c.compassion >= 30f)
                    return GrantEpithet(c, "宽宏者") ? "宽宏者" : "";
                // 吝啬鬼（高贪婪聚敛）
                if (c.greed > 60f) return GrantEpithet(c, "吝啬鬼") ? "吝啬鬼" : "";
                // 幸运者（远征顺遂——好事连连）
                if (rec.expeditions >= 4 && rec.defeatedBattles == 0 && !rec.famineUnderRule)
                    return GrantEpithet(c, "幸运者") ? "幸运者" : "";
                // 铁锤（防御大捷——卫国）
                if (rec.defensiveWins >= 3) return GrantEpithet(c, "铁锤") ? "铁锤" : "";
                // 传道者（宗教传播——高于虔诚者门槛）
                if (rec.religionActs >= 10) return GrantEpithet(c, "传道者") ? "传道者" : "";
                // 懒王（在位久+零建树——不理政被架空）
                if (rec.reignYears >= 15f && rec.warsWon == 0 && rec.conquests == 0
                    && rec.cultureActs == 0 && rec.religionActs == 0)
                    return GrantEpithet(c, "懒王") ? "懒王" : "";
                // 好人（综合评价正+宽仁无大过——低门槛泛褒）
                float sc = EvaluationSystem.CalculateScore(rec);
                if (sc >= 350f && c.compassion >= 30f && c.honor >= 30f && !rec.famineUnderRule
                    && !rec.usurpedThrone)
                    return GrantEpithet(c, "好人") ? "好人" : "";
                // 圣者（死后封圣——联动封圣系统——死亡结算传入 canonized）
                if (rec.canonized) return GrantEpithet(c, "圣者") ? "圣者" : "";
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
                        // 黑/白=军事贵族成对体系（甲色/发色形象——按身份阶梯）：
                        // 骑士→黑/白骑士；继承人/贵族→黑/白王子；君主→黑/白王
                        case "黑色":
                            granted = isRuler ? "黑王" : HasPrinceRole(c) ? "黑王子"
                                : c.role == CharacterRole.Military ? "黑骑士" : null;
                            break;
                        case "白色":
                            granted = isRuler ? "白王" : HasPrinceRole(c) ? "白王子"
                                : c.role == CharacterRole.Military ? "白骑士" : null;
                            break;
                        case "金发": granted = "美发者"; break;
                        case "白发": granted = "白发者"; break;
                        case "黑发": granted = "黑发者"; break;
                    }
                    if (granted != null) return GrantEpithet(c, granted) ? granted : "";
                }
            }
            return c.epithet;
        }

        private static bool HasBodyMark(CharacterData c, string mark)
        {
            if (c.bodyMarks == null) return false;
            return c.bodyMarks.Contains(mark);
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
