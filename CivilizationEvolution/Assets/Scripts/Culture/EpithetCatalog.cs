using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Culture
{
    /// <summary>绰号语义色彩（正/中/贬/双向——诗人=可褒可贬）</summary>
    public enum EpithetConnotation
    {
        Positive,  // 褒（狮子/智者）
        Neutral,   // 中性事实（征服者——时代语境）
        Negative,  // 贬（儿皇帝）
        Dual       // 双向语境（诗人——真才=褒/文人误国=贬）
    }

    /// <summary>绰号档位（普通/王级/传奇——王级=特质统治者化的历史形象）</summary>
    public enum EpithetTier
    {
        Common,    // 普通（任何身份——行为/性格/特征/外貌）
        Kingly,    // 王级（统治者专属——X王——苛刻——诗人王/疯王/征服王/冒险王）
        Great      // 伟大者（区域影响力——②高评价档）
    }

    /// <summary>绰号定义（数据驱动——id/名/语义/档位/判定源）</summary>
    public class EpithetDef
    {
        public string id;
        public string name;
        public EpithetConnotation connotation = EpithetConnotation.Neutral;
        public EpithetTier tier = EpithetTier.Common;
        public string note = ""; // 历史参照/说明
    }

    /// <summary>
    /// 绰号表（数据驱动——判定逻辑在 EpithetSystem——本表供显示/查询）
    /// 分类：性格型/行为型/宗教型/动物型/外貌型/贬讽型 + 王级 + 传奇
    /// </summary>
    public static class EpithetCatalog
    {
        private static Dictionary<string, EpithetDef> _defs;

        public static Dictionary<string, EpithetDef> All
        {
            get
            {
                if (_defs == null) Build();
                return _defs;
            }
        }

        public static EpithetDef Get(string id)
            => All.TryGetValue(id, out var d) ? d : null;

        private static void Add(string id, string name, EpithetConnotation c, EpithetTier t, string note)
            => _defs[id] = new EpithetDef { id = id, name = name, connotation = c, tier = t, note = note };

        private static void Build()
        {
            _defs = new Dictionary<string, EpithetDef>();

            // ===== 性格型 =====
            Add("epithet_brave", "勇敢者", EpithetConnotation.Positive, EpithetTier.Common, "大胆高+胜仗——勇武之士");
            Add("epithet_patient", "忍耐者", EpithetConnotation.Positive, EpithetTier.Common, "长期逆境坚持——低报复+在位久+多解危");
            Add("epithet_merciful", "仁慈者", EpithetConnotation.Positive, EpithetTier.Common, "悲悯极高+少征战——宽政");
            Add("epithet_just", "公正者", EpithetConnotation.Positive, EpithetTier.Common, "无饥荒+少叛乱+在位久——司法稳定");
            Add("epithet_cautious", "谨慎者", EpithetConnotation.Neutral, EpithetTier.Common, "低大胆+无败仗——谋定后动");
            Add("epithet_merciless", "无情者", EpithetConnotation.Negative, EpithetTier.Common,
                "悲悯极低+铁腕成就——冷酷高效两面（与仁慈者成对）——无情者哈康");

            // ===== 行为型 =====
            Add("epithet_conqueror", "征服者", EpithetConnotation.Neutral, EpithetTier.Common, "征服 5 块——征服者威廉");
            Add("epithet_victorious", "常胜者", EpithetConnotation.Positive, EpithetTier.Common, "胜仗多+败仗少");
            Add("epithet_liberator", "解放者", EpithetConnotation.Positive, EpithetTier.Common, "解放被占领地/拯救");
            Add("epithet_unifier", "统一者", EpithetConnotation.Positive, EpithetTier.Common, "统一法理区");
            Add("epithet_restorer", "中兴者", EpithetConnotation.Positive, EpithetTier.Common, "重建——战后/灾后恢复");
            Add("epithet_lawgiver", "立法者", EpithetConnotation.Positive, EpithetTier.Common, "成文法/法典成就——梭伦");
            Add("epithet_builder", "建设者", EpithetConnotation.Positive, EpithetTier.Common, "大兴土木/建筑革新");
            Add("epithet_watchful", "警觉者", EpithetConnotation.Positive, EpithetTier.Common, "化解密谋/叛乱 5 次");
            Add("epithet_poet", "诗人", EpithetConnotation.Dual, EpithetTier.Common, "诗作/文艺行为——褒=诗才传世/贬=文人误国");
            Add("epithet_traveler", "远行者", EpithetConnotation.Neutral, EpithetTier.Common, "远征远行多——见过世面");

            // ===== 宗教型 =====
            Add("epithet_apostate", "叛教者", EpithetConnotation.Negative, EpithetTier.Common, "公开改宗背弃原信仰——叛教者尤利安");
            Add("epithet_confessor", "忏悔者", EpithetConnotation.Positive, EpithetTier.Common, "虔诚+宽仁——爱德华忏悔者");
            Add("epithet_martyr", "殉道者", EpithetConnotation.Positive, EpithetTier.Common, "因信仰而死");
            Add("epithet_pious", "虔诚者", EpithetConnotation.Positive, EpithetTier.Common, "宗教行为多——虔诚者路易");

            // ===== 动物型（中性观察） =====
            Add("epithet_fox", "狐狸", EpithetConnotation.Neutral, EpithetTier.Common, "诈术/外交欺诈——机敏与不可信两面");
            Add("epithet_lion", "狮子", EpithetConnotation.Positive, EpithetTier.Common, "正面战功卓著——狮心王");
            Add("epithet_wolf", "狼", EpithetConnotation.Negative, EpithetTier.Common, "征服残暴——枭掠");

            // ===== 外貌/身体型（bodyMarks 判定——成对体系） =====
            Add("epithet_bald", "秃头", EpithetConnotation.Neutral, EpithetTier.Common, "秃顶——秃头查理");
            Add("epithet_lame", "瘸子", EpithetConnotation.Neutral, EpithetTier.Common, "跛足——提摩太·瘸子");
            Add("epithet_blind", "瞎子", EpithetConnotation.Neutral, EpithetTier.Common, "失明——瞎子约翰[波希米亚]");
            Add("epithet_short", "矮子", EpithetConnotation.Neutral, EpithetTier.Common, "矮小——矮子丕平");
            Add("epithet_fairhair", "美发者", EpithetConnotation.Positive, EpithetTier.Common,
                "金发——美发哈拉尔德（发色自然系——任何身份）");
            Add("epithet_whitehair", "白发者", EpithetConnotation.Neutral, EpithetTier.Common, "白发——发色自然系（任何身份）");
            Add("epithet_blackhair", "黑发者", EpithetConnotation.Neutral, EpithetTier.Common, "黑发——发色自然系（任何身份）");
            Add("epithet_black_knight", "黑骑士", EpithetConnotation.Neutral, EpithetTier.Common,
                "黑甲+骑士身份——军事贵族成对体系第一级");
            Add("epithet_white_knight", "白骑士", EpithetConnotation.Neutral, EpithetTier.Common,
                "白甲+骑士身份——成对体系（圣殿骑士白袍形象）");
            Add("epithet_redbeard", "红胡子", EpithetConnotation.Neutral, EpithetTier.Common, "红须——巴巴罗萨");
            Add("epithet_black_prince", "黑王子", EpithetConnotation.Neutral, EpithetTier.Common,
                "黑甲/黑发+继承人/贵族——黑王子爱德华（军事贵族成对体系——平民不适用）");
            Add("epithet_white_prince", "白王子", EpithetConnotation.Neutral, EpithetTier.Common,
                "白甲/白发+继承人/贵族——成对体系（军事贵族专属）");
            Add("epithet_black_king", "黑王", EpithetConnotation.Neutral, EpithetTier.Kingly,
                "黑甲/黑发+在位君主——黑王（亲征/发色形象）");
            Add("epithet_white_king", "白王", EpithetConnotation.Neutral, EpithetTier.Kingly,
                "白甲/白发+在位君主——白王（形象）");

            // ===== 贬讽型 =====
            Add("epithet_landless", "无地者", EpithetConnotation.Negative, EpithetTier.Common, "失地——无地王约翰（调侃）");
            Add("epithet_vassal_king", "儿皇帝", EpithetConnotation.Negative, EpithetTier.Common, "傀儡附庸君主——石敬瑭");
            Add("epithet_madman", "疯子", EpithetConnotation.Neutral, EpithetTier.Common, "精神疾病（临床——mentalDisorderId）——查理六世");

            // ===== 王级（统治者化历史形象——苛刻） =====
            Add("epithet_mad_king", "疯王", EpithetConnotation.Negative, EpithetTier.Kingly,
                "NPD 式统治风格（未必有病）：自恋傲慢[arrogant]+偏执[paranoid]+喜怒无常[高报复+低理性]+任性妄为[高大胆+低荣誉]——卡利古拉/尼禄");
            Add("epithet_poet_king", "诗人王", EpithetConnotation.Positive, EpithetTier.Kingly,
                "经历型传奇：行吟诗人出身[远行多]+贤君[评价杰出]——哈拉尔德·哈德拉达/苏格兰詹姆斯一世");
            Add("epithet_conqueror_king", "征服王", EpithetConnotation.Positive, EpithetTier.Kingly,
                "跨文化区大征服[征服 12+胜仗 15+]——亚历山大大帝级");
            Add("epithet_adventurer_king", "冒险王", EpithetConnotation.Positive, EpithetTier.Kingly,
                "一场史诗大冒险 或 大量冒险事迹累积——马可波罗只是冒险者非冒险王");

            // ===== 高评价档 =====
            Add("epithet_great", "伟大者", EpithetConnotation.Positive, EpithetTier.Great,
                "区域影响力≥0.6[区域内前列·中等偏上]——阿尔弗雷德式——发放较多非严苛");
            Add("epithet_holy_king", "圣君", EpithetConnotation.Positive, EpithetTier.Kingly,
                "在位圣明[尧舜式]——德行×治理双极致：评价≥卓越[750]+悲悯荣誉双高[60+]+
                无饥荒少叛乱——王级苛刻——几乎不可得——与圣者[死后封圣·普通]区分");
        }
    }
}
