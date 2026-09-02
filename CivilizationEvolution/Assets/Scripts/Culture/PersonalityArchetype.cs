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
    /// 生前：行为成就→绰号（征服者/大冒险家……）
    /// 死后：谥号/尊号（华夏式——按一生行为定谥：文/武/明/仁 vs 厉/灵/幽）
    /// </summary>
    public static class EpithetSystem
    {
        /// <summary>授予绰号（行为成就阈值判定——一次授予不覆盖）</summary>
        public static bool TryGrantEpithet(CharacterData c, string epithet)
        {
            if (c == null || string.IsNullOrEmpty(epithet)) return false;
            if (!string.IsNullOrEmpty(c.epithet)) return false; // 已有绰号不覆盖
            c.epithet = epithet;
            return true;
        }

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
