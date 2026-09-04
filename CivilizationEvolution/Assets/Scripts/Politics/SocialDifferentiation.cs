using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Politics
{
    // =====================================================================================
    // 社会分化系统（Social Differentiation）
    // -------------------------------------------------------------------------------------
    // 唯物史观的阶层生成：人口最初都是农民；当物质/制度条件成熟（剩余产品、手工业、贸易、
    // 公共权力、奴隶制被革新承认），才缓慢地从农业人口中分化出工商自由民、贵族教士与奴隶。
    //
    // 与既有系统的关系：
    //   · "某阶层是否被制度承认"直接复用 RealmSituation.classRecognized
    //     （由 SocialClassAvailability 依据 革新+文化 预算，不在此重复判定）；
    //   · 本系统只做"人口在阶层间的平滑、守恒转移"，不创建角色、不改政体；
    //   · 原始社会（无相关革新）时所有非农阶层未被承认 → 不发生分化，仍是均质农民社会；
    //   · 过程可逆：工商业凋敝 / 阶层不再被承认时，过剩人口回流农业（城镇化可逆）。
    //
    // 守恒铁律：一次再平衡只搬运"收缩总量"，不新增/消灭人口；Royalty（王室）由角色系统
    // 承载、不占地块人口，故不参与分化。
    // =====================================================================================

    /// <summary>社会分化器：无状态静态工具，输入情境快照，就地调整地块人口块的阶层构成</summary>
    public static class SocialDifferentiation
    {
        // —— 目标结构常量（集中常量化，便于调参与模组覆盖）——
        const float MerchantFloor = 0.02f;       // 工商自由民基础占比（被承认后）
        const float MerchantDevWeight = 0.15f;   // 每单位平均发展度增加的工商占比
        const float MerchantTradeWeight = 0.05f; // 贸易畅通度加成
        const float MerchantCeil = 0.28f;        // 工商占比上限（前现代社会）
        const float NobleFloor = 0.02f;          // 贵族教士基础占比
        const float NobleCentralWeight = 0.04f;  // 每单位集权度增加的贵族/官僚占比
        const float NobleCeil = 0.09f;           // 贵族教士上限
        const float SlaveFloor = 0.005f;         // 奴隶基础占比（被承认后）
        const float SlaveWarBoost = 0.045f;      // 战争状态奴隶占比提升（俘获/债役）
        const float SlaveDevWeight = 0.01f;
        const float SlaveCeil = 0.18f;
        const float NonPeasantCeil = 0.42f;      // 非农总和上限——前现代农业社会农民始终占多数
        const float ConvergeAlpha = 0.10f;       // 每次再平衡收敛缺口的比例（缓慢，非跳变）
        const float MinBlockCount = 0.02f;       // 小于此值的人口块并入/清除，避免碎片

        /// <summary>
        /// 对单个政权执行一次社会分化（就地修改其地块人口块）。
        /// 应由主循环以固定间隔（如每 20~30 天）调用一次，配合 ConvergeAlpha 实现缓慢演化。
        /// </summary>
        public static void DifferentiateRealm(RealmData realm, TileData[] tiles, RealmSituation sit,
            IReadOnlyList<int> realmTiles = null)
        {
            if (realm == null || tiles == null || sit == null) return;

            // 1) 政权平均发展度（0~1）与各地块列表
            float devSum = 0f; int devN = 0;
            var tileList = new List<int>();
            foreach (int idx in EnumerateRealmTiles(realm, tiles, realmTiles))
            {
                ref TileData t = ref tiles[idx];
                if (!t.isLand || t.populationBlocks == null || t.populationBlocks.Count == 0) continue;
                tileList.Add(idx);tileList.Add(idx);
                devSum += Mathf.Clamp01(t.development);
                devN++;
            }
            if (tileList.Count == 0) return;
            float avgDev = devN > 0 ? devSum / devN : 0f;

            // 2) 政权级目标占比（由制度承认 + 物质条件决定）
            var goalShare = ComputeGoalShares(realm, sit, avgDev);

            // 3) 逐地块向目标结构做守恒再平衡（城镇/高发展地块承担更多工商人口）
            foreach (int idx in tileList)
            {
                ref TileData t = ref tiles[idx];
                RebalanceTile(ref t, goalShare, Mathf.Clamp01(t.development), Mathf.Max(0.05f, avgDev));
                tiles[idx] = t; // struct 写回
            }
        }

        /// <summary>计算政权级五阶层目标占比（Royalty 不占人口，目标给 0；农民兜底占多数）</summary>
        static Dictionary<GameEnums.SocialClass, float> ComputeGoalShares(
            RealmData realm, RealmSituation sit, float avgDev)
        {
            var g = new Dictionary<GameEnums.SocialClass, float>
            {
                [GameEnums.SocialClass.Royalty] = 0f,
                [GameEnums.SocialClass.Peasant] = 1f,
                [GameEnums.SocialClass.MerchantFreeman] = 0f,
                [GameEnums.SocialClass.NobilityClergy] = 0f,
                [GameEnums.SocialClass.Slave] = 0f
            };

            if (sit.IsRecognized(GameEnums.SocialClass.MerchantFreeman))
                g[GameEnums.SocialClass.MerchantFreeman] = Mathf.Clamp(
                    MerchantFloor + avgDev * MerchantDevWeight +
                    Mathf.Clamp01(sit.tradeFlow) * MerchantTradeWeight, 0f, MerchantCeil);

            if (sit.IsRecognized(GameEnums.SocialClass.NobilityClergy))
                g[GameEnums.SocialClass.NobilityClergy] = Mathf.Clamp(
                    NobleFloor + Mathf.Clamp01(realm.centralization) * NobleCentralWeight, 0f, NobleCeil);

            if (sit.IsRecognized(GameEnums.SocialClass.Slave))
                g[GameEnums.SocialClass.Slave] = Mathf.Clamp(
                    SlaveFloor + (sit.atWar ? SlaveWarBoost : 0f) + avgDev * SlaveDevWeight, 0f, SlaveCeil);

            // 非农总和封顶，保证前现代农业社会以农为主体；超出则等比压缩非农
            float nonPeasant = g[GameEnums.SocialClass.MerchantFreeman]
                             + g[GameEnums.SocialClass.NobilityClergy]
                             + g[GameEnums.SocialClass.Slave];
            if (nonPeasant > NonPeasantCeil && nonPeasant > 0f)
            {
                float k = NonPeasantCeil / nonPeasant;
                g[GameEnums.SocialClass.MerchantFreeman] *= k;
                g[GameEnums.SocialClass.NobilityClergy] *= k;
                g[GameEnums.SocialClass.Slave] *= k;
                nonPeasant = NonPeasantCeil;
            }
            g[GameEnums.SocialClass.Peasant] = 1f - nonPeasant;
            return g;
        }

        /// <summary>
        /// 单个地块内部的守恒再平衡。高发展度地块的工商目标按局部偏差上调，其余归一。
        /// </summary>
        static void RebalanceTile(ref TileData tile,
            Dictionary<GameEnums.SocialClass, float> realmGoal, float tileDev, float avgDev)
        {
            var blocks = tile.populationBlocks;
            float total = 0f;
            foreach (var pb in blocks) total += pb.count;
            if (total < 0.1f) return;

            // 本地块目标占比：工商按"地块发展度/政权平均发展度"偏置（城镇多工商、乡村多农民）
            float bias = Mathf.Clamp(tileDev / Mathf.Max(0.05f, avgDev), 0.3f, 2.2f);
            var localGoal = new Dictionary<GameEnums.SocialClass, float>(realmGoal)
            {
                [GameEnums.SocialClass.MerchantFreeman] =
                    Mathf.Clamp01(realmGoal[GameEnums.SocialClass.MerchantFreeman] * bias)
            };
            float used = localGoal[GameEnums.SocialClass.MerchantFreeman]
                       + localGoal[GameEnums.SocialClass.NobilityClergy]
                       + localGoal[GameEnums.SocialClass.Slave];
            localGoal[GameEnums.SocialClass.Peasant] = Mathf.Max(0f, 1f - used);

            // 当前各阶层人口
            var current = new Dictionary<GameEnums.SocialClass, float>();
            foreach (GameEnums.SocialClass c in Enum.GetValues(typeof(GameEnums.SocialClass))) current[c] = 0f;
            foreach (var pb in blocks) current[pb.socialClass] += pb.count;

            // 期望净变化；本轮只移动"收缩侧 × alpha"，保证平滑且总量守恒
            var delta = new Dictionary<GameEnums.SocialClass, float>();
            float shrinkPool = 0f;
            foreach (GameEnums.SocialClass c in Enum.GetValues(typeof(GameEnums.SocialClass)))
            {
                float target = total * localGoal.GetValueOrDefault(c, 0f);
                float d = target - current.GetValueOrDefault(c, 0f);
                delta[c] = d;
                if (d < 0f) shrinkPool += -d * ConvergeAlpha; // 本轮可释放的人口
            }
            if (shrinkPool < 1e-4f) return;

            // 扩张侧需求总量（用于按比例分配收缩池）
            float growNeed = 0f;
            foreach (var kv in delta) if (kv.Value > 0f) growNeed += kv.Value * ConvergeAlpha;
            if (growNeed < 1e-4f) return;

            // 第一步：从收缩阶层释放人口到池（按各阶层收缩缺口比例）
            var give = new Dictionary<GameEnums.SocialClass, float>();
            foreach (var kv in delta)
            {
                if (kv.Value >= 0f) continue;
                float release = Mathf.Min(-kv.Value * ConvergeAlpha, current[kv.Key]);
                give[kv.Key] = release;
            }

            // 第二步：按扩张需求比例，把池分配给增长阶层
            var gain = new Dictionary<GameEnums.SocialClass, float>();
            foreach (var kv in delta)
            {
                if (kv.Value <= 0f) continue;
                float want = kv.Value * ConvergeAlpha;
                gain[kv.Key] = growNeed > 0f ? want * (shrinkPool / growNeed) : 0f;
            }

            // 执行转移：先从各 give 阶层抽出（可能来自多个块），再补入 gain 阶层
            foreach (var gkv in give)
                Withdraw(blocks, gkv.Key, gkv.Value);
            foreach (var rkv in gain)
                if (rkv.Value > 1e-4f)
                    Deposit(blocks, rkv.Key, rkv.Value, blocks);

            // 清除过小碎片块
            for (int i = blocks.Count - 1; i >= 0; i--)
                if (blocks[i].count < MinBlockCount) blocks.RemoveAt(i);
        }

        /// <summary>从指定阶层的人口块中抽出 amount（跨多个块），struct 写回</summary>
        static void Withdraw(List<PopulationBlock> blocks, GameEnums.SocialClass cls, float amount)
        {
            for (int i = 0; i < blocks.Count && amount > 1e-5f; i++)
            {
                if (blocks[i].socialClass != cls) continue;
                var pb = blocks[i];
                float take = Mathf.Min(pb.count, amount);
                pb.count -= take;
                amount -= take;
                blocks[i] = pb;
            }
        }

        /// <summary>把 amount 补入指定阶层；无此阶层块则新建（继承任一现有块的种族/文化/信仰）</summary>
        static void Deposit(List<PopulationBlock> blocks, GameEnums.SocialClass cls, float amount,
            List<PopulationBlock> templateSource)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].socialClass != cls) continue;
                var pb = blocks[i];
                pb.count += amount;
                blocks[i] = pb;
                return;
            }
            // 新建人口块：继承本地块主体的种族/文化/信仰
            var tpl = templateSource.Count > 0 ? templateSource[0] : default;
            blocks.Add(new PopulationBlock
            {
                socialClass = cls,
                count = amount,
                raceId = tpl.raceId,
                cultureId = tpl.cultureId,
                faithId = tpl.faithId,
                satisfaction = tpl.satisfaction > 0f ? tpl.satisfaction : 55f,
                culturePenetration = tpl.culturePenetration,
                profession = 0
            });
        }

        /// <summary>枚举政权所有领有陆地地块（核心 + 非核心领有，去重）</summary>
        static IEnumerable<int> EnumerateRealmTiles(RealmData realm, TileData[] tiles,
            IReadOnlyList<int> realmTiles = null)
        {
            if (realmTiles != null)
            {
                foreach (int idx in realmTiles)
                    if (idx >= 0 && idx < tiles.Length) yield return idx;
                yield break;
            }
            var seen = new HashSet<int>();
            foreach (int idx in realm.coreTiles)
            {
                if (idx < 0 || idx >= tiles.Length || !seen.Add(idx)) continue;
                yield return idx;
            }
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].ownerRealmId != realm.realmId || !seen.Add(i)) continue;
                yield return i;
            }
        }
    }
}
