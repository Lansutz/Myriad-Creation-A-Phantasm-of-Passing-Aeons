using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Politics
{
 // ===================================================================================== // 阶层需求系统（Class Needs System） // ------------------------------------------------------------------------------------- // 设计链条（唯物史观）： // 人口块阶层结构（人口数量）→ 各阶层多维需求 → 需求满足度（对接税/粮/战/灾/政体/革新） // → 阶层综合满足度（驱动 classRelations）→ 阶层政治能量（人口×不满×组织化）→ 派系/政体变迁 // 解耦原则：本系统不直接依赖 Economy/War/Disaster/Innovation 等子系统， // 由 GameWorld 每个政治 Tick 采集各系统指标，组装成 RealmSituation 情境快照后传入。 // 这样需求计算逻辑纯粹、可单测、可被模组替换权重表，也绕开了革新 int/string 双轨问题。 // =====================================================================================
 /// 阶层需求维度。每个阶层只关心其中若干维（无关维度权重为 0，不参与归一化）。
 /// 维度拆分是为了精确定位"哪个系统导致该阶层不满"，供 UI 提示与 AI 决策。

 /// 政权情境快照：某一时刻从各子系统采集的、与阶层需求相关的客观指标。
 /// 全部指标已归一化，ClassNeedsSystem 不关心数据来源。

 /// <summary>单个需求维度的评估结果</summary>

 /// <summary>单个阶层的需求评估报告</summary>

 /// 阶层需求权重表：每阶层对各需求维度的关心权重（内部自动归一化，模组可覆盖）。
 /// 权重为 0 表示该阶层基本不关心此维度（如奴隶无政治通道诉求、农民几乎不关心参政）。

 /// 政治通道分析器：解析政体七维成分，判断各阶层在该政体下的政治参与/上升通道畅通度。
 /// 这是"上层建筑是否容纳该阶层"的制度性判断——通道被堵的阶层会持续积累政治能量。

 /// 阶层需求系统主体：输入情境快照，输出各阶层多维需求满足度。
 /// 无状态、可并行、可单测；权重表可替换以支持模组。
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
