using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Politics
{
    // =====================================================================================
    // 派系系统（Faction System）
    // -------------------------------------------------------------------------------------
    // 定位：阶层是"客观社会结构"（人口块），派系是阶层利益在政治上层建筑中的"组织化行动者"。
    //   - 派系不是凭空生成：它由阶层的政治能量（RealmSociety 中的 influence/unrest）孕育；
    //   - 同一阶层可分化出不同派系，不同阶层也可结盟（阶层基础是权重分布而非单一绑定）；
    //   - 角色（CharacterData）只是派系的领袖/代言人，不是派系的根基；暂无合适有名角色时，
    //     派系可以"无明确领袖"状态存在（底层运动）。
    //   - 政体变迁的"关键节点博弈"中，派系是真正出手的力量（见 RegimeChangeDynamics）。
    //
    // 立场光谱（对现政体的态度）而非简单按阶层划线：
    //   保守派=现制度受益者（力量来自 support）；改革派=体制内调整；激进派=根本变革（力量来自 unrest）；
    //   复辟派=回到更早制度。政纲（开放度/集权度/经济/税负倾向）由阶层基础推导，可被模组模板覆盖。
    // =====================================================================================

    /// <summary>派系对现政体的立场</summary>
    public enum FactionStance
    {
        Conservative,   // 保守派：维护现政体（既得利益）
        Reformist,      // 改革派：体制内渐进调整政体成分
        Radical,        // 激进派：要求根本变革现政体
        Reactionary     // 复辟派：回到更早已被取代的制度
    }

    /// <summary>
    /// 派系政纲：四维方向性诉求（-1~1）。关键节点时据此推导具体目标政体成分。
    /// </summary>
    [Serializable]
    public struct FactionPlatform
    {
        [Range(-1f, 1f)] public float openness;        // 开放度：负=世袭排他，正=选举包容（政治通道）
        [Range(-1f, 1f)] public float centralization;  // 集权度：负=地方分权/封建，正=中央集权/官僚
        [Range(-1f, 1f)] public float commerce;        // 经济取向：负=重农抑商/管制，正=重商/市场
        [Range(-1f, 1f)] public float taxRelief;       // 税负诉求：正=要求减税，负=可接受增税（如备战/福利）

        public static FactionPlatform operator +(FactionPlatform a, FactionPlatform b) => new FactionPlatform
        {
            openness = a.openness + b.openness,
            centralization = a.centralization + b.centralization,
            commerce = a.commerce + b.commerce,
            taxRelief = a.taxRelief + b.taxRelief
        };
        public FactionPlatform Scaled(float k) => new FactionPlatform
        {
            openness = openness * k, centralization = centralization * k,
            commerce = commerce * k, taxRelief = taxRelief * k
        };
        public void Clamp()
        {
            openness = Mathf.Clamp(openness, -1f, 1f);
            centralization = Mathf.Clamp(centralization, -1f, 1f);
            commerce = Mathf.Clamp(commerce, -1f, 1f);
            taxRelief = Mathf.Clamp(taxRelief, -1f, 1f);
        }
    }

    /// <summary>派系</summary>
    [Serializable]
    public class Faction
    {
        public int factionId;
        public int realmId;
        public string factionName = "";
        public FactionStance stance;

        /// <summary>阶层基础：各社会阶层对本派系的支持权重（0~1，可跨阶层结盟）</summary>
        public Dictionary<GameEnums.SocialClass, float> classBacking = new Dictionary<GameEnums.SocialClass, float>();
        /// <summary>主要代表阶层（backing 最高者）</summary>
        public GameEnums.SocialClass primaryClass = GameEnums.SocialClass.Peasant;

        /// <summary>领袖角色ID（-1=暂无有名领袖的底层运动）</summary>
        public int leaderCharacterId = -1;
        /// <summary>派系成员（廷臣/贵族/官员等有名角色）</summary>
        public List<int> memberCharacterIds = new List<int>();

        public FactionPlatform platform;

        [Range(0f, 100f)] public float power;       // 政治力量（由阶层能量+领袖+成员聚合）
        [Range(0f, 100f)] public float cohesion = 60f; // 凝聚力（低则分裂）
        public bool isInGovernment;                 // 是否当前执政/参与执政（保守派通常是）

        public Faction() { }
        public Faction(int id, int realmId, FactionStance stance)
        {
            factionId = id; this.realmId = realmId; this.stance = stance;
        }

        /// <summary>派系是否拥有某阶层的显著支持（&gt;阈值）</summary>
        public bool BackedBy(GameEnums.SocialClass cls, float threshold = 0.2f)
            => classBacking.GetValueOrDefault(cls, 0f) >= threshold;
    }

    /// <summary>
    /// 派系管理器：从社会画像孕育/更新派系、匹配角色领袖、计算力量、提供博弈查询。
    /// 每个政权维护一份派系集合，按政治 Tick 增量更新（不每帧重建，保持派系持续性）。
    /// </summary>
    public class FactionManager
    {
        // realmId → 派系集合
        private readonly Dictionary<int, List<Faction>> _factionsByRealm = new Dictionary<int, List<Faction>>();
        private int _nextFactionId = 1;

        // —— 孕育/存续阈值 ——
        const float SpawnPowerThreshold = 8f;     // 社会能量达到此值才足以支撑一个派系
        const float DissolvePowerThreshold = 3f;  // 力量长期低于此值则派系消亡
        const float SignificantClassShare = 0.05f;// 阶层人口占比低于 5% 难以独立成派

        /// <summary>获取政权全部派系（只读）</summary>
        public IReadOnlyList<Faction> GetFactions(int realmId)
            => _factionsByRealm.TryGetValue(realmId, out var list) ? list : EmptyList;
        static readonly List<Faction> EmptyList = new List<Faction>();

        /// <summary>按立场获取派系</summary>
        public Faction GetFactionByStance(int realmId, FactionStance stance)
        {
            if (!_factionsByRealm.TryGetValue(realmId, out var list)) return null;
            return list.FirstOrDefault(f => f.stance == stance);
        }

        /// <summary>
        /// 按最新社会画像更新政权派系（增量：已存在则更新力量/基础，缺位且社会能量足够则孕育）。
        /// characters：本政权有名角色（用于领袖/成员匹配，可空）。
        /// </summary>
        public void UpdateRealmFactions(RealmSociety society, RealmData realm,
            List<CharacterData> characters = null)
        {
            int realmId = society.realmId;
            if (!_factionsByRealm.TryGetValue(realmId, out var factions))
            {
                factions = new List<Faction>();
                _factionsByRealm[realmId] = factions;
            }

            // 1) 计算四种立场的社会能量与阶层基础
            var stanceEnergy = ComputeStanceEnergy(society);

            // 2) 每种立场：存在则更新，缺位且达阈值则创建
            foreach (FactionStance stance in Enum.GetValues(typeof(FactionStance)))
            {
                var energy = stanceEnergy[stance];
                var faction = factions.FirstOrDefault(f => f.stance == stance);

                if (faction == null)
                {
                    if (energy.totalPower < SpawnPowerThreshold) continue;
                    faction = new Faction(_nextFactionId++, realmId, stance)
                    {
                        factionName = DefaultName(stance),
                        isInGovernment = stance == FactionStance.Conservative
                    };
                    factions.Add(faction);
                }

                // 更新阶层基础与政纲
                faction.classBacking = energy.backing;
                faction.primaryClass = energy.primaryClass;
                faction.platform = BlendPlatform(faction.classBacking, society, stance);

                // 匹配/更新领袖与成员
                MatchCharacters(faction, society, characters);

                // 计算力量
                faction.power = ComputePower(faction, energy, characters);
            }

            // 3) 凝聚力漂移 + 消亡清理
            for (int i = factions.Count - 1; i >= 0; i--)
            {
                var f = factions[i];
                // 力量与凝聚力正反馈：力量强凝聚力缓升，力量弱凝聚力缓降
                f.cohesion = Mathf.Clamp(f.cohesion + (f.power > 20f ? 0.05f : -0.1f), 0f, 100f);
                if (f.power < DissolvePowerThreshold && f.cohesion < 10f)
                    factions.RemoveAt(i);
            }
        }

        // ===== 立场社会能量：把各阶层的 support/unrest 按立场归集 =====
        private struct StanceEnergy
        {
            public float totalPower;
            public Dictionary<GameEnums.SocialClass, float> backing;
            public GameEnums.SocialClass primaryClass;
        }

        private Dictionary<FactionStance, StanceEnergy> ComputeStanceEnergy(RealmSociety society)
        {
            var result = new Dictionary<FactionStance, StanceEnergy>();
            foreach (FactionStance stance in Enum.GetValues(typeof(FactionStance)))
            {
                var backing = new Dictionary<GameEnums.SocialClass, float>();
                float power = 0f; GameEnums.SocialClass primary = GameEnums.SocialClass.Peasant;
                float maxBack = -1f;

                foreach (var kv in society.classes)
                {
                    var cls = kv.Key; var profile = kv.Value;
                    if (profile.populationShare < SignificantClassShare
                        && cls != GameEnums.SocialClass.Royalty
                        && cls != GameEnums.SocialClass.NobilityClergy) continue;

                    // 各立场从一个阶层汲取的"能量比例"
                    float draw = stance switch
                    {
                        // 保守派汲取满意者的支持能量
                        FactionStance.Conservative => profile.support / Mathf.Max(1f, profile.influence),
                        // 改革派：温和不满且有政治通道的阶层（体制内诉求）
                        FactionStance.Reformist => GrievanceBand(profile, 0.15f, 0.55f)
                            * (0.5f + profile.organization),
                        // 激进派：深重不满且通道被堵的阶层
                        FactionStance.Radical => GrievanceBand(profile, 0.45f, 1f)
                            * (1.2f - Mathf.Clamp01(profile.needReport != null
                                ? profile.needReport.dimensions.GetValueOrDefault(ClassNeedDimension.PoliticalAccess).score / 100f : 0.5f)),
                        // 复辟派：在现制度下丧失旧特权的贵族/教士（特权维度严重不满）
                        FactionStance.Reactionary => (cls == GameEnums.SocialClass.NobilityClergy || cls == GameEnums.SocialClass.Royalty)
                            ? GrievanceBand(profile, 0.3f, 1f) * 0.8f : GrievanceBand(profile, 0.5f, 1f) * 0.2f,
                        _ => 0f
                    };
                    draw = Mathf.Clamp01(draw);
                    backing[cls] = draw;
                    float contribution = profile.influence * draw;
                    power += contribution;
                    if (contribution > maxBack) { maxBack = contribution; primary = cls; }
                }
                result[stance] = new StanceEnergy
                {
                    totalPower = Mathf.Clamp(power, 0f, 100f),
                    backing = backing,
                    primaryClass = primary
                };
            }
            return result;
        }

        /// <summary>不满度落在 [lo,hi] 区间的强度（平滑带），用于区分改革/激进人群</summary>
        private static float GrievanceBand(ClassProfile p, float lo, float hi)
        {
            float g = Mathf.Clamp01((100f - p.satisfaction) / 100f);
            if (g <= lo || g >= hi)
            {
                // 软边界：靠近区间仍给少量
                float near = Mathf.Max(0f, 0.15f - Mathf.Min(Mathf.Abs(g - lo), Mathf.Abs(g - hi)));
                return near / 0.15f * 0.3f;
            }
            return 1f;
        }

        // ===== 政纲：由阶层基础加权各阶层的典型诉求 =====
        private static FactionPlatform BlendPlatform(
            Dictionary<GameEnums.SocialClass, float> backing, RealmSociety society, FactionStance stance)
        {
            // 各阶层的"原生政纲倾向"
            var native = new Dictionary<GameEnums.SocialClass, FactionPlatform>
            {
                [GameEnums.SocialClass.Royalty] = new FactionPlatform { openness = -0.6f, centralization = 0.8f, commerce = 0.1f, taxRelief = -0.2f },
                [GameEnums.SocialClass.NobilityClergy] = new FactionPlatform { openness = -0.3f, centralization = -0.5f, commerce = -0.1f, taxRelief = 0.4f },
                [GameEnums.SocialClass.MerchantFreeman] = new FactionPlatform { openness = 0.7f, centralization = 0.2f, commerce = 0.8f, taxRelief = 0.5f },
                [GameEnums.SocialClass.Peasant] = new FactionPlatform { openness = 0.0f, centralization = 0.1f, commerce = -0.3f, taxRelief = 0.8f },
                [GameEnums.SocialClass.Slave] = new FactionPlatform { openness = 0.2f, centralization = 0f, commerce = 0f, taxRelief = 0f }
            };

            var blended = new FactionPlatform(); float wSum = 0f;
            foreach (var kv in backing)
            {
                var cp = society.classes.GetValueOrDefault(kv.Key);
                float w = cp != null ? kv.Value * cp.influence : 0f;
                if (w <= 0f || !native.TryGetValue(kv.Key, out var pl)) continue;
                blended = blended + pl.Scaled(w);
                wSum += w;
            }
            if (wSum > 0f) blended = blended.Scaled(1f / wSum);

            // 立场对政纲的整体偏移：保守派维持现状（趋零=不改），激进派放大诉求，复辟派开放度/集权度回摆
            float stanceMul = stance switch
            {
                FactionStance.Conservative => 0.2f,
                FactionStance.Reformist => 0.8f,
                FactionStance.Radical => 1.3f,
                FactionStance.Reactionary => -0.9f,
                _ => 1f
            };
            blended = blended.Scaled(stanceMul);
            blended.Clamp();
            return blended;
        }

        // ===== 角色匹配：从本政权有名角色中选领袖、吸纳成员 =====
        private void MatchCharacters(Faction faction, RealmSociety society, List<CharacterData> characters)
        {
            faction.memberCharacterIds.Clear();
            if (characters == null) { faction.leaderCharacterId = -1; return; }

            CharacterData best = null; float bestScore = -1f;
            foreach (var c in characters)
            {
                if (c == null || !c.isAlive) continue;
                // 角色阶层须与派系主要基础相容（backing 显著）
                float aff = faction.classBacking.GetValueOrDefault(c.socialClass, 0f);
                if (aff < 0.2f) continue;
                faction.memberCharacterIds.Add(c.characterId);

                // 领袖评分：威望 + 适配六维 + 人格（野心/大胆更愿领头）
                float score = c.prestige * 0.5f
                    + c.diplomacy * 0.2f + c.intrigue * 0.15f + c.stewardship * 0.15f
                    + (c.greed + 100f) * 0.05f + (c.boldness + 100f) * 0.05f + aff * 10f;
                if (score > bestScore) { bestScore = score; best = c; }
            }
            faction.leaderCharacterId = best != null ? best.characterId : -1;
        }

        // ===== 派系力量 = 社会能量 × 凝聚力 × 领袖加成 =====
        private static float ComputePower(Faction faction, StanceEnergy energy, List<CharacterData> characters)
        {
            float basePower = energy.totalPower;
            float cohesionFactor = 0.5f + faction.cohesion / 200f; // 0.5~1
            float power = basePower * cohesionFactor;

            if (faction.leaderCharacterId >= 0 && characters != null)
            {
                var leader = characters.FirstOrDefault(c => c != null && c.characterId == faction.leaderCharacterId);
                if (leader != null)
                {
                    // 威望与能力提供 0~25% 加成（领袖是放大器，不是力量根源）
                    float leaderBonus = (leader.prestige / 1500f) * 0.15f
                        + (leader.diplomacy + leader.intrigue + leader.stewardship) / 300f * 0.10f;
                    power *= 1f + Mathf.Clamp(leaderBonus, 0f, 0.25f);
                }
            }
            return Mathf.Clamp(power, 0f, 100f);
        }

        /// <summary>政权内最强派系（关键节点博弈的主导者候选）</summary>
        public Faction GetDominantFaction(int realmId)
        {
            if (!_factionsByRealm.TryGetValue(realmId, out var list) || list.Count == 0) return null;
            Faction best = null;
            foreach (var f in list)
                if (best == null || f.power > best.power) best = f;
            return best;
        }

        /// <summary>要求"改变现状"的联合力量（改革+激进+复辟）对比"维持现状"力量（保守）</summary>
        public void GetChangeVsStatusQuo(int realmId, out float changePower, out float statusQuoPower)
        {
            changePower = 0f; statusQuoPower = 0f;
            if (!_factionsByRealm.TryGetValue(realmId, out var list)) return;
            foreach (var f in list)
            {
                if (f.stance == FactionStance.Conservative) statusQuoPower += f.power;
                else changePower += f.power;
            }
        }

        static string DefaultName(FactionStance stance) => stance switch
        {
            FactionStance.Conservative => "保守派",
            FactionStance.Reformist => "改革派",
            FactionStance.Radical => "激进派",
            FactionStance.Reactionary => "复辟派",
            _ => "派系"
        };
    }
}
