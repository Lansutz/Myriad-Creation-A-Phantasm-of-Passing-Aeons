using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Modding
{
    // ============================================================================
    // 头衔语义标签注册
    // ----------------------------------------------------------------------------
    // 语义层 ≠ 语言词根层 ≠ 后世固化的翻译词。
    // 一个头衔 = { kind 类别 } + { 一个或多个 authority 权威来源 } +
    //           { 零个或多个 modifier 程度/范围修饰 }。
    // 例：罗马"皇帝" = monarch + [military, religious, dynastic, civic]
    //                  + [great, divine]
    //     渲染时按当前文化的词表出 Imperator·Augustus·Caesar·Princeps。
    //
    // 权威来源必须回到"这个头衔凭什么说话"，而不是后世翻译。
    // 例：汉语"王"本义是斧钺=军事征伐，不能把"王"和"军事领袖"并列。
    // ============================================================================

    /// <summary>头衔四大类（TitleDef.kind 的合法值）</summary>
    public static class TitleKinds
    {
        /// <summary>君主头衔：政权顶端的那个人的称呼（王、可汗、大公……）</summary>
        public const string Monarch = "monarch";
        /// <summary>官僚头衔：行政层级上的职事（郡守、县令、总督……）</summary>
        public const string Bureaucratic = "bureaucratic";
        /// <summary>爵位头衔：分封/贵族体系里的位阶（公侯伯子男、领主……）</summary>
        public const string Noble = "noble";
        /// <summary>荣誉/宗教/继承头衔：奥古斯都、哈里发、苏丹、法老、凯撒——
        /// 原本不是君主，是堆叠在统治者身上才固化成最高权威的</summary>
        public const string Honorific = "honorific";
        /// <summary>国名后缀：王国/帝国/汗国（不是头衔本身，是政权名层）</summary>
        public const string RealmSuffix = "realmSuffix";

        public static readonly string[] All =
        {
            Monarch, Bureaucratic, Noble, Honorific, RealmSuffix
        };
    }

    /// <summary>
    /// 权威来源——这个头衔"凭什么说话"。
    /// 一个头衔可以同时挂多个来源（罗马皇帝就是军事+宗教+继承+共和外壳）。
    /// </summary>
    public static class SemanticAuthority
    {
        /// <summary>军事征伐：王（斧钺）、Imperator、dux、amir、可汗——
        /// 靠带兵打仗和胜利获得的权威</summary>
        public const string Military = "military";
        /// <summary>长老/议事：宗长 knyaz、patriarch、senate、archon、长者——
        /// 靠年龄、氏族辈分、议事传统获得的权威</summary>
        public const string ElderCouncil = "elder_council";
        /// <summary>父系/家族：paterfamilias、父、家主、族长——
        /// 靠血缘和家长身份获得的权威</summary>
        public const string PaternalClan = "paternal_clan";
        /// <summary>神权/祭祀：pontifex maximus、augustus（尊神）、caliph（继承者）、
        /// 神授主——靠祭祀、神意、宗教教义获得的权威</summary>
        public const string Religious = "religious";
        /// <summary>主从/财富：dominus（主人）、sultan（"权力"本身）、sahib（所有者）——
        /// 靠财富、保护关系、人身依附获得的权威</summary>
        public const string LordMaster = "lord_master";
        /// <summary>继承/家族名：Caesar 原本是尤利乌斯家族名，后来变成副帝/储君头衔——
        /// 靠血缘继承和王朝身份获得的权威</summary>
        public const string DynasticName = "dynastic_name";
        /// <summary>共和/公民：princeps（第一公民）、consul、archon——
        /// 靠公民集体、共和职务、同列之首获得的权威</summary>
        public const string CivicRepublic = "civic_republic";
        /// <summary>宫殿/机构：pharaoh = pr-ꜥꜣ"大房子"，原本指王宫——
        /// 靠所居宫殿/官署机构本身获得的权威</summary>
        public const string PalaceInstitution = "palace_institution";
        /// <summary>选举/部众推举：由部落大会、贵族会议自由推举产生——
        /// 靠被推举这一行为本身获得的权威</summary>
        public const string ElectionAssembly = "election_assembly";
        /// <summary>贡赋/宗主：霸主、hegemon、suzerain——
        /// 靠他国纳贡、承认宗主权获得的权威</summary>
        public const string TributeOverlord = "tribute_overlord";

        public static readonly string[] All =
        {
            Military, ElderCouncil, PaternalClan, Religious, LordMaster,
            DynasticName, CivicRepublic, PalaceInstitution, ElectionAssembly, TributeOverlord
        };
    }

    /// <summary>
    /// 程度/范围修饰词——叠加在权威来源之上，表"更大/更高/更全"。
    /// 这些是语义层的修饰，不是语言学词根；语言渲染层负责把它们拼成实际词。
    /// 印度头衔能叠三四层就是这个机制（mahā+rāja+adhi+rāja）。
    /// </summary>
    public static class SemanticModifier
    {
        /// <summary>伟大：mahā / megas / magnus——"被增大、被抬升"</summary>
        public const string Great = "great";
        /// <summary>至高：parama / hyper / arch-——"在最顶端"</summary>
        public const string Supreme = "supreme";
        /// <summary>全/普世：pan- / omni- / universal——"覆盖全部"</summary>
        public const string AllUniversal = "all_universal";
        /// <summary>首/总：arch- / chief / head——"居首位的"</summary>
        public const string Chief = "chief";
        /// <summary>副/次级：vice- / sub- / under——"副的，不是正主"</summary>
        public const string SubVice = "sub_vice";
        /// <summary>神圣/尊荣：augustus 本身就是"被神圣化"——宗教性的提升，
        /// 区别于 military 的"打出来"和 supreme 的"位置高"</summary>
        public const string DivineSacred = "divine_sacred";
        /// <summary>第一公民/同列之首：princeps——名义上与众人平等，
        /// 只是"第一个"，不是君主</summary>
        public const string FirstAmongEquals = "first_among_equals";
        /// <summary>万王/王上王：shahanshah / rajadhiraja——
        /// 修饰"王"这个本体之上再加一层"王之上"</summary>
        public const string OverAllKings = "over_all_kings";

        public static readonly string[] All =
        {
            Great, Supreme, AllUniversal, Chief, SubVice,
            DivineSacred, FirstAmongEquals, OverAllKings
        };
    }

    /// <summary>语义标签工具：校验/查询</summary>
    public static class SemanticTags
    {
        /// <summary>校验一个字符串是否是合法的权威来源</summary>
        public static bool IsValidAuthority(string tag)
            => !string.IsNullOrEmpty(tag) && Array.IndexOf(SemanticAuthority.All, tag) >= 0;

        /// <summary>校验一个字符串是否是合法的程度修饰</summary>
        public static bool IsValidModifier(string tag)
            => !string.IsNullOrEmpty(tag) && Array.IndexOf(SemanticModifier.All, tag) >= 0;

        /// <summary>校验一个字符串是否是合法的头衔类别</summary>
        public static bool IsValidKind(string kind)
            => !string.IsNullOrEmpty(kind) && Array.IndexOf(TitleKinds.All, kind) >= 0;
    }
}
