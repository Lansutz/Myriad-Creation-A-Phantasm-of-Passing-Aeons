using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Politics
{
    // =====================================================================================
    // 阶层需求系统（Class Needs System）
    // -------------------------------------------------------------------------------------
    // 设计链条（唯物史观）：
    //   人口块阶层结构（人口数量）→ 各阶层多维需求 → 需求满足度（对接税/粮/战/灾/政体/革新）
    //   → 阶层综合满足度（驱动 classRelations）→ 阶层政治能量（人口×不满×组织化）→ 派系/政体变迁
    //
    // 解耦原则：本系统不直接依赖 Economy/War/Disaster/Innovation 等子系统，
    // 由 GameWorld 每个政治 Tick 采集各系统指标，组装成 RealmSituation 情境快照后传入。
    // 这样需求计算逻辑纯粹、可单测、可被模组替换权重表，也绕开了革新 int/string 双轨问题。
    // =====================================================================================

    /// <summary>
    /// 阶层需求维度。每个阶层只关心其中若干维（无关维度权重为 0，不参与归一化）。
    /// 维度拆分是为了精确定位"哪个系统导致该阶层不满"，供 UI 提示与 AI 决策。
    /// </summary>
    public enum ClassNeedDimension
    {
        Subsistence,             // 生存保障：粮食/温饱（农民、奴隶最敏感）
        Security,                // 人身安全：战乱、本土劫掠、治安、灾害
        TaxBurden,               // 税负合理度：对应阶层税种税率（由 TaxSystem 算痛感）
        PoliticalAccess,         // 政治参与/上升通道：政体是否给该阶层通道（选举/议会/科举/城市特许）
        EconomicOpportunity,     // 经济机会：贸易畅通、货币稳定、市场（自由民最敏感）
        InstitutionalRecognition,// 制度承认：该阶层是否被革新/制度承认为合法存在
        Legitimacy,              // 合法性与秩序：政权稳定度、统治合法性（王室/贵族敏感）
        Privilege                // 特权保障：世袭权、免税、土地保障（贵族敏感）
    }

    /// <summary>
    /// 政权情境快照：某一时刻从各子系统采集的、与阶层需求相关的客观指标。
    /// 全部指标已归一化，ClassNeedsSystem 不关心数据来源。
    /// </summary>
    [Serializable]
    public class RealmSituation
    {
        public int realmId;

        // —— 生存 ——
        [Range(0f, 1.5f)] public float foodSecurity = 1f;   // 食品库存/需求比，1=刚好满足，&gt;1有余，&lt;1短缺

        // —— 安全 ——
        [Range(0f, 100f)] public float publicOrder = 60f;   // 政权地块平均治安/秩序
        public bool atWar;                                  // 是否处于战争状态
        public bool warOnHomeSoil;                          // 本土是否有交战/占领（比 atWar 更伤）
        [Range(0f, 100f)] public float disasterSeverity;    // 近期灾害/饥荒/瘟疫严重度

        // —— 税负：各阶层税负痛感 0~100（越高越痛；由 TaxSystem.GetTaxSatisfactionImpact 换算）——
        [System.NonSerialized]
        public Dictionary<GameEnums.SocialClass, float> taxPain = new Dictionary<GameEnums.SocialClass, float>();

        // —— 政治通道：各阶层在当前政体下的通道畅通度 0~1（由 PoliticalAccessAnalyzer 解析 composition）——
        [System.NonSerialized]
        public Dictionary<GameEnums.SocialClass, float> politicalAccess = new Dictionary<GameEnums.SocialClass, float>();

        // —— 经济 ——
        [Range(0f, 1f)] public float tradeFlow = 1f;        // 贸易路线畅通度（被劫/封锁则下降）
        [Range(0f, 1f)] public float monetaryStability = 1f;// 货币稳定度（1-通胀归一）

        // —— 制度承认：各阶层是否被制度/革新承认为合法存在（SocialClassAvailability 预计算）——
        [System.NonSerialized]
        public Dictionary<GameEnums.SocialClass, bool> classRecognized = new Dictionary<GameEnums.SocialClass, bool>();

        // —— 合法性 ——
        [Range(0f, 100f)] public float stability = 50f;     // 政权稳定度（RealmData.stability）
        [Range(0f, 100f)] public float legitimacy = 50f;    // 统治合法性（威望/正统/信仰，外部综合）

        // —— 特权保障：贵族世袭/免税/土地特权被政体保障的程度 0~1 ——
        [Range(0f, 1f)] public float privilegeSecurity = 1f;

        public float GetTaxPain(GameEnums.SocialClass cls) => taxPain != null ? taxPain.GetValueOrDefault(cls, 20f) : 20f;
        public float GetPoliticalAccess(GameEnums.SocialClass cls) => politicalAccess != null ? politicalAccess.GetValueOrDefault(cls, 0.2f) : 0.2f;
        public bool IsRecognized(GameEnums.SocialClass cls) => classRecognized == null || classRecognized.GetValueOrDefault(cls, true);
    }

    /// <summary>单个需求维度的评估结果</summary>
    [Serializable]
    public struct NeedDimensionScore
    {
        public ClassNeedDimension dimension;
        [Range(0f, 100f)] public float score;   // 满足度 0~100
        public string reason;                   // 低于阈值时的主因（UI/AI 用，如"饥荒"、"商税过重"）
    }

    /// <summary>单个阶层的需求评估报告</summary>
    [Serializable]
    public class ClassNeedReport
    {
        public GameEnums.SocialClass socialClass;
        [System.NonSerialized]
        public Dictionary<ClassNeedDimension, NeedDimensionScore> dimensions = new Dictionary<ClassNeedDimension, NeedDimensionScore>();
        [Range(0f, 100f)] public float overallSatisfaction = 50f;  // 加权综合满足度
        public ClassNeedDimension worstDimension;                  // 最差维度（主要矛盾）
        [Range(0f, 100f)] public float worstScore = 100f;
        public float population;                                   // 该阶层在政权内的总人口（外部统计填入）

        public NeedDimensionScore Get(ClassNeedDimension d) =>
            dimensions.TryGetValue(d, out var v) ? v : default;
    }

    /// <summary>
    /// 阶层需求权重表：每阶层对各需求维度的关心权重（内部自动归一化，模组可覆盖）。
    /// 权重为 0 表示该阶层基本不关心此维度（如奴隶无政治通道诉求、农民几乎不关心参政）。
    /// </summary>
    [Serializable]
    public class ClassNeedWeightTable
    {
        // 顺序对应 ClassNeedDimension 枚举：Subsistence/Security/TaxBurden/PoliticalAccess/
        // EconomicOpportunity/InstitutionalRecognition/Legitimacy/Privilege
        public float[] royalty =        { 0.10f, 0.20f, 0.00f, 0.00f, 0.15f, 0.00f, 0.30f, 0.25f };
        public float[] nobilityClergy = { 0.10f, 0.15f, 0.00f, 0.25f, 0.00f, 0.00f, 0.20f, 0.30f };
        public float[] merchantFreeman ={ 0.15f, 0.10f, 0.20f, 0.25f, 0.30f, 0.00f, 0.00f, 0.00f };
        public float[] peasant =        { 0.35f, 0.25f, 0.25f, 0.05f, 0.00f, 0.00f, 0.10f, 0.00f };
        public float[] slave =          { 0.70f, 0.30f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f };

        public float[] GetWeights(GameEnums.SocialClass cls) => cls switch
        {
            GameEnums.SocialClass.Royalty => royalty,
            GameEnums.SocialClass.NobilityClergy => nobilityClergy,
            GameEnums.SocialClass.MerchantFreeman => merchantFreeman,
            GameEnums.SocialClass.Peasant => peasant,
            GameEnums.SocialClass.Slave => slave,
            _ => peasant
        };
    }

    /// <summary>
    /// 政治通道分析器：解析政体七维成分，判断各阶层在该政体下的政治参与/上升通道畅通度。
    /// 这是"上层建筑是否容纳该阶层"的制度性判断——通道被堵的阶层会持续积累政治能量。
    /// </summary>
    public static class PoliticalAccessAnalyzer
    {
        /// <summary>计算指定阶层在给定政体下的政治通道 0~1</summary>
        public static float GetAccess(GameEnums.SocialClass cls, GovernmentComposition comp)
        {
            if (comp == null) return 0.2f;

            bool isElectiveSupreme = SupremeSuccessionLevel.IsElective((SupremeSuccession)comp.supremeSuccession.primary);
            bool directDemocracy = (SupremeSuccession)comp.supremeSuccession.primary == SupremeSuccession.ElectiveDirect;
            var central = (CentralInstitution)comp.centralInstitution.primary;
            bool hasAssembly = central == CentralInstitution.Assembly;
            bool hasElders = central == CentralInstitution.EldersCouncil;
            bool hasReligious = central == CentralInstitution.ReligiousCouncil;
            bool hasBureaucracy = central == CentralInstitution.BureaucraticCore;
            var localSucc = (LocalSuccession)comp.localSuccession.primary;
            bool examChannel = localSucc == LocalSuccession.Examination;
            bool charterChannel = localSucc == LocalSuccession.CityCharter;
            bool localElective = localSucc == LocalSuccession.Elected;
            bool hereditarySupreme = (SupremeSuccession)comp.supremeSuccession.primary == SupremeSuccession.Hereditary;
            bool localHereditary = localSucc == LocalSuccession.Hereditary;
            // 两院制含平民院、等级会议含第三等级，对自由民更开放
            bool lowerHouse = hasAssembly &&
                (comp.assemblyComposition == AssemblyComposition.Bicameral || comp.assemblyComposition == AssemblyComposition.Estate);

            switch (cls)
            {
                case GameEnums.SocialClass.Royalty:
                    // 君主制下王室通道完整；共和制下无世袭君主，通道弱
                    return hereditarySupreme ? 1f : (isElectiveSupreme ? 0.35f : 0.6f);

                case GameEnums.SocialClass.NobilityClergy:
                {
                    float a = 0.2f;
                    if (hereditarySupreme) a += 0.3f;            // 世袭君主制贵族承统
                    if (localHereditary) a += 0.25f;             // 地方世袭领有
                    if (hasElders) a += 0.3f;                    // 长老议事会=贵族传统
                    if (hasAssembly) a += lowerHouse ? 0.15f : 0.3f; // 议会（贵族院权重高）
                    if (hasReligious) a += 0.25f;                // 宗教会议=教士通道
                    return Mathf.Clamp01(a);
                }

                case GameEnums.SocialClass.MerchantFreeman:
                {
                    float a = 0.15f;
                    if (isElectiveSupreme) a += directDemocracy ? 0.35f : 0.25f; // 选举/公民大会
                    if (hasAssembly) a += lowerHouse ? 0.35f : 0.2f;             // 平民院/等级会议
                    if (examChannel) a += 0.3f;             // 科举=士人入仕
                    if (charterChannel) a += 0.3f;          // 城市特许自治
                    if (localElective) a += 0.2f;           // 地方选举
                    if (hasBureaucracy && examChannel) a += 0.1f; // 官僚中枢+考试=文官通道
                    return Mathf.Clamp01(a);
                }

                case GameEnums.SocialClass.Peasant:
                    // 农民通常被排除在政治之外，仅直接民主（部落/公民大会）时有微弱通道
                    return directDemocracy ? 0.45f : 0.12f;

                case GameEnums.SocialClass.Slave:
                    return 0f; // 奴隶无政治通道

                default:
                    return 0.2f;
            }
        }

        /// <summary>一次性计算全部阶层的政治通道（情境采集时调用，避免重复解析）</summary>
        public static Dictionary<GameEnums.SocialClass, float> GetAllAccess(GovernmentComposition comp)
        {
            var result = new Dictionary<GameEnums.SocialClass, float>();
            foreach (GameEnums.SocialClass cls in Enum.GetValues(typeof(GameEnums.SocialClass)))
                result[cls] = GetAccess(cls, comp);
            return result;
        }
    }

    /// <summary>
    /// 阶层需求系统主体：输入情境快照，输出各阶层多维需求满足度。
    /// 无状态、可并行、可单测；权重表可替换以支持模组。
    /// </summary>
    public class ClassNeedsSystem
    {
        // —— 满足度映射系数（集中常量化，便于调参与模组平衡）——
        const float SubsistenceBase = 40f, SubsistenceSlope = 50f, SubsistenceDisasterPenalty = 0.2f;
        const float WarPenalty = 15f, HomeSoilWarPenalty = 30f, SecurityDisasterPenalty = 0.15f;
        const float EconTradeWeight = 30f, EconMonetaryWeight = 20f, EconBase = 50f;
        const float RecognizedScore = 75f, UnrecognizedScore = 20f;
        /// <summary>维度低于此值视为"主要矛盾"，写入 reason</summary>
        public const float GrievanceThreshold = 45f;

        public ClassNeedWeightTable Weights { get; set; } = new ClassNeedWeightTable();

        /// <summary>评估单个阶层的全部需求维度</summary>
        public ClassNeedReport EvaluateClass(GameEnums.SocialClass cls, RealmSituation sit)
        {
            var report = new ClassNeedReport { socialClass = cls };
            float[] w = Weights.GetWeights(cls);

            Add(report, ClassNeedDimension.Subsistence, EvalSubsistence(cls, sit), SubsistenceReason(sit));
            Add(report, ClassNeedDimension.Security, EvalSecurity(sit), SecurityReason(sit));
            Add(report, ClassNeedDimension.TaxBurden, EvalTaxBurden(cls, sit), TaxReason(cls, sit));
            Add(report, ClassNeedDimension.PoliticalAccess, EvalPoliticalAccess(cls, sit), AccessReason(cls, sit));
            Add(report, ClassNeedDimension.EconomicOpportunity, EvalEconomic(sit), EconReason(sit));
            Add(report, ClassNeedDimension.InstitutionalRecognition, EvalRecognition(cls, sit), RecognitionReason(cls, sit));
            Add(report, ClassNeedDimension.Legitimacy, EvalLegitimacy(sit), LegitimacyReason(sit));
            Add(report, ClassNeedDimension.Privilege, EvalPrivilege(sit), PrivilegeReason(sit));

            // 加权综合（仅权重>0 的维度参与）
            float sumScore = 0f, sumWeight = 0f;
            foreach (ClassNeedDimension d in Enum.GetValues(typeof(ClassNeedDimension)))
            {
                int idx = (int)d;
                if (w[idx] <= 0f) continue;
                if (!report.dimensions.TryGetValue(d, out var sc)) continue;
                sumScore += sc.score * w[idx];
                sumWeight += w[idx];
            }
            report.overallSatisfaction = sumWeight > 0f ? Mathf.Clamp(sumScore / sumWeight, 0f, 100f) : 50f;

            // 找最差维度（仅在有权重的维度中）
            report.worstScore = 100f;
            foreach (var kv in report.dimensions)
            {
                int idx = (int)kv.Key;
                if (idx >= w.Length || w[idx] <= 0f) continue;
                if (kv.Value.score < report.worstScore)
                {
                    report.worstScore = kv.Value.score;
                    report.worstDimension = kv.Key;
                }
            }
            return report;
        }

        /// <summary>评估政权下全部存在的阶层</summary>
        public Dictionary<GameEnums.SocialClass, ClassNeedReport> EvaluateRealm(RealmSituation sit)
        {
            var result = new Dictionary<GameEnums.SocialClass, ClassNeedReport>();
            foreach (GameEnums.SocialClass cls in Enum.GetValues(typeof(GameEnums.SocialClass)))
                result[cls] = EvaluateClass(cls, sit);
            return result;
        }

        // ===== 各维度满足度计算（输出 0~100）=====

        static float EvalSubsistence(GameEnums.SocialClass cls, RealmSituation s)
        {
            // 奴隶生存线更低、更脆弱
            float slope = cls == GameEnums.SocialClass.Slave ? SubsistenceSlope * 0.8f : SubsistenceSlope;
            float v = SubsistenceBase + Mathf.Clamp(s.foodSecurity, 0f, 1.2f) / 1.2f * slope
                      - s.disasterSeverity * SubsistenceDisasterPenalty;
            return Mathf.Clamp(v, 0f, 100f);
        }

        static float EvalSecurity(RealmSituation s)
        {
            float v = s.publicOrder
                      - (s.atWar ? WarPenalty : 0f)
                      - (s.warOnHomeSoil ? HomeSoilWarPenalty : 0f)
                      - s.disasterSeverity * SecurityDisasterPenalty;
            return Mathf.Clamp(v, 0f, 100f);
        }

        static float EvalTaxBurden(GameEnums.SocialClass cls, RealmSituation s)
            => Mathf.Clamp(100f - s.GetTaxPain(cls), 0f, 100f);

        static float EvalPoliticalAccess(GameEnums.SocialClass cls, RealmSituation s)
            => Mathf.Clamp01(s.GetPoliticalAccess(cls)) * 100f;

        static float EvalEconomic(RealmSituation s)
            => Mathf.Clamp(EconBase + Mathf.Clamp01(s.tradeFlow) * EconTradeWeight
                           + Mathf.Clamp01(s.monetaryStability) * EconMonetaryWeight, 0f, 100f);

        static float EvalRecognition(GameEnums.SocialClass cls, RealmSituation s)
            => s.IsRecognized(cls) ? RecognizedScore : UnrecognizedScore;

        static float EvalLegitimacy(RealmSituation s)
            => Mathf.Clamp(s.stability * 0.5f + s.legitimacy * 0.5f, 0f, 100f);

        static float EvalPrivilege(RealmSituation s) => Mathf.Clamp01(s.privilegeSecurity) * 100f;

        // ===== 不满足原因（低于阈值时给出，供 UI/AI）=====
        static string ReasonIfLow(float score, string reason) => score < GrievanceThreshold ? reason : "";
        static string SubsistenceReason(RealmSituation s) => ReasonIfLow(
            SubsistenceBase + Mathf.Clamp(s.foodSecurity, 0f, 1.2f) / 1.2f * SubsistenceSlope, "粮食短缺/饥荒");
        static string SecurityReason(RealmSituation s)
        {
            if (s.warOnHomeSoil) return "本土遭兵燹劫掠";
            if (s.atWar) return "战争征兵与不安";
            if (s.publicOrder < GrievanceThreshold) return "治安崩坏";
            return "";
        }
        static string TaxReason(GameEnums.SocialClass cls, RealmSituation s)
            => ReasonIfLow(100f - s.GetTaxPain(cls), cls == GameEnums.SocialClass.Peasant ? "农业/人头税过重"
                : cls == GameEnums.SocialClass.MerchantFreeman ? "商税/手工业税过重" : "税负过重");
        static string AccessReason(GameEnums.SocialClass cls, RealmSituation s)
            => ReasonIfLow(s.GetPoliticalAccess(cls) * 100f, "被排除在政治通道之外");
        static string EconReason(RealmSituation s)
        {
            if (s.tradeFlow < 0.5f) return "商路受阻/市场萧条";
            if (s.monetaryStability < 0.5f) return "货币贬值通胀";
            return "";
        }
        static string RecognitionReason(GameEnums.SocialClass cls, RealmSituation s)
            => s.IsRecognized(cls) ? "" : "该阶层未获制度承认";
        static string LegitimacyReason(RealmSituation s)
            => ReasonIfLow(s.stability * 0.5f + s.legitimacy * 0.5f, "政权合法性/秩序动摇");
        static string PrivilegeReason(RealmSituation s)
            => ReasonIfLow(s.privilegeSecurity * 100f, "世袭/免税/土地特权受威胁");

        static void Add(ClassNeedReport r, ClassNeedDimension d, float score, string reason)
        {
            r.dimensions[d] = new NeedDimensionScore { dimension = d, score = score, reason = reason };
        }
    }
}
