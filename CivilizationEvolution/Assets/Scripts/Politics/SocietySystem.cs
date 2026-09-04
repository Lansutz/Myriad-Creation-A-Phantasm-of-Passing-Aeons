using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Politics
{
    // =====================================================================================
    // 社会系统（Society System）
    // -------------------------------------------------------------------------------------
    // 职责：把"地块人口块的阶层结构"与"阶层需求满足度"合成为政权级社会画像，
    //       并以需求满足度驱动阶层好感（RealmData.classRelations），替代旧的"机械 Lerp 回 50"。
    //
    // 政治力学（中性影响力 + 方向由满足度决定）：
    //   influence（政治影响力）= 人口份额×组织化 + 制度性在位基底
    //   grievance（不满度）     = (100 - 满足度)/100
    //   unrest（反对/动荡能量） = influence × grievance   ——派系与反叛的燃料
    //   support（支持能量）     = influence × 满足度/100   ——现政权的支柱
    // 人口是基础（用户强调：阶层人口数量必然导致后果），组织化决定同样人口的政治效能，
    // 制度基底解释王室/当政贵族"人少却掌权"。
    // =====================================================================================

    /// <summary>单个阶层在政权内的社会画像</summary>
    [Serializable]
    public class ClassProfile
    {
        public GameEnums.SocialClass socialClass;
        public float population;            // 该阶层总人口（跨地块汇总）
        [Range(0f, 1f)] public float populationShare; // 占政权总人口比例
        [Range(0f, 100f)] public float satisfaction;   // 需求综合满足度（来自 ClassNeedReport）
        [Range(0f, 1f)] public float organization;     // 组织化系数
        [Range(0f, 100f)] public float influence;      // 政治影响力（中性）
        [Range(0f, 100f)] public float unrest;         // 反对/动荡能量
        [Range(0f, 100f)] public float support;        // 支持能量
        [Range(0f, 100f)] public float loyalty;        // 阶层忠诚（=平滑后的 classRelations）
        public ClassNeedDimension chiefGrievance;      // 主要不满维度
        public string chiefGrievanceReason = "";       // 主因文本
        public ClassNeedReport needReport;             // 详细需求报告（UI/AI 用）
    }

    /// <summary>政权级社会画像</summary>
    [Serializable]
    public class RealmSociety
    {
        public int realmId;
        [System.NonSerialized]
        public Dictionary<GameEnums.SocialClass, ClassProfile> classes
            = new Dictionary<GameEnums.SocialClass, ClassProfile>();
        public float totalPopulation;
        [Range(0f, 100f)] public float unrestScore;    // 整体社会动荡（影响力加权怨气）
        public GameEnums.SocialClass dominantClass = GameEnums.SocialClass.Peasant;     // 人口最多
        public GameEnums.SocialClass mostRestlessClass = GameEnums.SocialClass.Peasant; // 动荡能量最高

        public ClassProfile Get(GameEnums.SocialClass c) =>
            classes.TryGetValue(c, out var p) ? p : null;
    }

    /// <summary>
    /// 社会管理器：统计阶层人口 → 评估需求 → 计算政治能量 → 驱动阶层好感。
    /// 无外部子系统依赖（情境由 RealmSituation 传入，人口由 tiles 统计）。
    /// </summary>
    public class SocietyManager
    {
        public ClassNeedsSystem Needs { get; } = new ClassNeedsSystem();

        /// <summary>阶层固有组织化基准（血缘网络/教会/行会/国家机器/分散程度不同）</summary>
        static float BaseOrganization(GameEnums.SocialClass cls) => cls switch
        {
            GameEnums.SocialClass.Royalty => 0.95f,       // 掌握国家机器
            GameEnums.SocialClass.NobilityClergy => 0.75f,// 血缘/教阶/地产网络
            GameEnums.SocialClass.MerchantFreeman => 0.55f,// 行会/商会/城市共同体
            GameEnums.SocialClass.Peasant => 0.25f,       // 分散务农，天然难组织
            GameEnums.SocialClass.Slave => 0.10f,         // 被束缚、极难组织，唯绝境爆发
            _ => 0.3f
        };

        /// <summary>制度性在位基底：不靠人数的掌权阶层（王室/当政贵族）</summary>
        static float InstitutionalBase(GameEnums.SocialClass cls, RealmSituation sit)
        {
            float b = cls switch
            {
                GameEnums.SocialClass.Royalty => 42f,
                GameEnums.SocialClass.NobilityClergy => 14f,
                _ => 0f
            };
            // 政治通道越通畅，体制内阶层的制度性影响力越强
            b += sit.GetPoliticalAccess(cls) * 12f;
            return b;
        }

        /// <summary>
        /// 统计并评估一个政权的社会结构。
        /// tiles：全图地块（按 ownerRealmId 过滤本政权核心+领有地块）。
        /// </summary>
        public RealmSociety EvaluateRealm(RealmData realm, TileData[] tiles, RealmSituation sit,
            IReadOnlyList<int> realmTiles = null)
        {
            var society = new RealmSociety { realmId = realm.realmId };

            // 1) 汇总各阶层人口（优化 2026-09-04：调用方传领地索引——
            // 替代 核心+全扫补占领 的 N×tiles——无索引时原逻辑兼容）
            var popByClass = new Dictionary<GameEnums.SocialClass, float>();
            float totalPop = 0f;
            if (realmTiles != null)
            {
                foreach (int idx in realmTiles)
                {
                    if (idx < 0 || idx >= tiles.Length) continue;
                    Accumulate(tiles[idx], popByClass, ref totalPop);
                }
            }
            else
            {
                foreach (int idx in realm.coreTiles)
                {
                    if (idx < 0 || idx >= tiles.Length) continue;
                    Accumulate(tiles[idx], popByClass, ref totalPop);
                }
                // 领有但未核心化的地块也计入（占领地社会压力同样存在）
                for (int i = 0; i < tiles.Length; i++)
                {
                    if (tiles[i].ownerRealmId != realm.realmId) continue;
                    if (realm.coreTiles.Contains(i)) continue;
                    Accumulate(tiles[i], popByClass, ref totalPop);
                }
            }
            society.totalPopulation = totalPop;

            // 2) 逐阶层评估需求与政治能量
            var needReports = Needs.EvaluateRealm(sit);
            float unrestSum = 0f, influenceSum = 0f;
            float maxPop = -1f, maxUnrest = -1f;

            foreach (GameEnums.SocialClass cls in Enum.GetValues(typeof(GameEnums.SocialClass)))
            {
                float pop = popByClass.GetValueOrDefault(cls, 0f);
                var report = needReports[cls];
                float share = totalPop > 0f ? Mathf.Clamp01(pop / totalPop) : 0f;

                // 组织化：基准 × 政治通道微调（有通道更易在体制内组织；奴隶恒低）
                float org = BaseOrganization(cls);
                if (cls != GameEnums.SocialClass.Slave)
                    org = Mathf.Clamp01(org + (sit.GetPoliticalAccess(cls) - 0.3f) * 0.15f);

                // 影响力 = 人口份额×组织化×100 + 制度基底
                float influence = Mathf.Clamp(share * org * 100f + InstitutionalBase(cls, sit), 0f, 100f);
                float grievance = Mathf.Clamp01((100f - report.overallSatisfaction) / 100f);
                float unrest = influence * grievance;
                float support = influence * (1f - grievance);

                var profile = new ClassProfile
                {
                    socialClass = cls,
                    population = pop,
                    populationShare = share,
                    satisfaction = report.overallSatisfaction,
                    organization = org,
                    influence = influence,
                    unrest = unrest,
                    support = support,
                    loyalty = realm.classRelations.GetValueOrDefault(cls, 50f),
                    chiefGrievance = report.worstDimension,
                    chiefGrievanceReason = report.Get(report.worstDimension).reason,
                    needReport = report
                };
                society.classes[cls] = profile;

                if (pop > maxPop) { maxPop = pop; society.dominantClass = cls; }
                if (unrest > maxUnrest) { maxUnrest = unrest; society.mostRestlessClass = cls; }
                unrestSum += unrest;
                influenceSum += influence;
            }
            // 整体动荡 = 各阶层反对能量占总影响力之比（0~100）
            society.unrestScore = influenceSum > 0f ? Mathf.Clamp(unrestSum / influenceSum * 100f, 0f, 100f) : 0f;
            return society;
        }

        static void Accumulate(TileData tile, Dictionary<GameEnums.SocialClass, float> acc, ref float total)
        {
            if (tile.populationBlocks == null) return;
            foreach (var pb in tile.populationBlocks)
            {
                acc[pb.socialClass] = acc.GetValueOrDefault(pb.socialClass, 0f) + pb.count;
                total += pb.count;
            }
        }

        /// <summary>
        /// 以需求满足度驱动阶层好感（替代旧的机械 Lerp 回 50）。
        /// 好感缓慢趋近满足度——存在滞后：一时粮荒不会立刻造反，长期被压迫也不会立刻原谅。
        /// 同时人口规模影响收敛速度（人多势众的阶层情绪传导更快）。
        /// </summary>
        public void ApplyClassRelations(RealmData realm, RealmSociety society, float daysPerTick)
        {
            foreach (var kv in society.classes)
            {
                var cls = kv.Key;
                var profile = kv.Value;
                float current = realm.classRelations.GetValueOrDefault(cls, 50f);
                // 基准日收敛率 1.5%/天；不满下降比满足上升略快（积怨易、立信难）
                float rate = current > profile.satisfaction ? 0.02f : 0.012f;
                float t = Mathf.Clamp01(rate * daysPerTick);
                float next = Mathf.Lerp(current, profile.satisfaction, t);
                realm.classRelations[cls] = Mathf.Clamp(next, 0f, 100f);
                profile.loyalty = realm.classRelations[cls];
            }
        }
    }
}
