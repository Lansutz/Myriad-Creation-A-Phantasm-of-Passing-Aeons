using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Politics
{
    /// <summary>
    /// 政体效果门面：负责把当前政体成分（GovernmentComposition）换算成数值效果并缓存，
    /// 以及在政权创建 / 手动改制时应用这些效果。
    ///
    /// 重要设计边界（历史制度主义）：
    /// 本系统【不做】"压力累积到阈值就自动切换政体"的机械演化。
    /// 政体成分的实际变更只有两条合法路径：
    ///   1) RegimeChangeDynamics：结构张力只定义"可能性"，关键节点窗口内派系博弈后
    ///      经 GovernmentReform.Reform（含革新可行性硬约束）落地；
    ///   2) 玩家手动改制（ChangeGovernment，UI 层已先校验革新前置）。
    /// 本类每 Tick 只做一件事：让缓存效果与可能被上述路径改变的成分保持同步。
    /// </summary>
    public class GovernmentSystem
    {
        private GameWorld _world;
        private readonly Dictionary<int, GovernmentEffects.GovernmentEffectData> _cachedEffects =
            new Dictionary<int, GovernmentEffects.GovernmentEffectData>();
        // 上一次同步时的政体成分指纹，用于识别"成分是否被外部路径改变"
        private readonly Dictionary<int, string> _lastFingerprint = new Dictionary<int, string>();
        private float _refreshTimer = 0f;
        private const float RefreshInterval = 30f; // 每30秒刷新一次效果缓存（节流，非演化周期）

        public GovernmentSystem(GameWorld world)
        {
            _world = world;
        }

        /// <summary>政权创建时初始化政体效果</summary>
        public void InitializeRealmGovernment(RealmData realm)
        {
            if (realm == null || realm.composition == null) return;

            var effects = GovernmentEffects.CalculateEffects(realm.composition);
            _cachedEffects[realm.realmId] = effects;
            _lastFingerprint[realm.realmId] = Fingerprint(realm.composition);
            ApplyGovernmentEffects(realm, effects);

            Debug.Log($"[GovernmentSystem] 政权 {realm.realmName} 政体初始化: {effects.realmNameSuffix}, 扩张性={effects.expansionism:F2}, 集权度={effects.centralization:F2}");
        }

        /// <summary>把政体效果应用到政权数据（仅在初始化 / 成分确实变化 / 手动改制时调用，保证幂等）</summary>
        private void ApplyGovernmentEffects(RealmData realm, GovernmentEffects.GovernmentEffectData effects)
        {
            // 集权度由政体成分直接决定（幂等赋值，不累积）
            realm.centralization = Mathf.Clamp(effects.centralization, 0f, 1f);
            // 其余瞬时修正（稳定度、税收/军事系数）在此扩展；稳定度类一次性修正禁止在刷新 Tick 中重复叠加。
        }

        /// <summary>每 Tick：仅刷新效果缓存；成分被外部路径改变时同步幂等字段</summary>
        public void Tick(float deltaTime)
        {
            if (_world == null || _world.realms == null) return;

            _refreshTimer += deltaTime;
            if (_refreshTimer < RefreshInterval) return;
            _refreshTimer = 0f;

            foreach (var kv in _world.realms)
            {
                var realm = kv.Value;
                if (realm == null) continue;
                RefreshRealmEffects(realm);
            }
        }

        /// <summary>重算效果缓存；若成分相对上次发生变化，则同步幂等效果字段（不做自动改制）</summary>
        private void RefreshRealmEffects(RealmData realm)
        {
            if (realm.composition == null) return;

            var effects = GovernmentEffects.CalculateEffects(realm.composition);
            _cachedEffects[realm.realmId] = effects;

            string fp = Fingerprint(realm.composition);
            string prev = _lastFingerprint.GetValueOrDefault(realm.realmId);
            if (prev != fp)
            {
                // 成分已被 RegimeChangeDynamics / GovernmentReform 等外部路径改变：
                // 仅同步幂等字段（centralization），一次性稳定度冲击由落地路径自行记录。
                ApplyGovernmentEffects(realm, effects);
                _lastFingerprint[realm.realmId] = fp;
            }
        }

        /// <summary>七维成分指纹（主+次成分），用于检测政体成分是否变化</summary>
        private static string Fingerprint(GovernmentComposition c)
        {
            if (c == null) return string.Empty;
            return string.Join("|",
                c.supremeSuccession?.primary ?? -1,
                c.supremeScope?.primary ?? -1,
                c.centralSuccession?.primary ?? -1,
                c.centralInstitution?.primary ?? -1,
                c.localSuccession?.primary ?? -1,
                c.localScope?.primary ?? -1,
                c.spatialStructure?.primary ?? -1);
        }

        /// <summary>获取政权的政体效果（缓存）</summary>
        public GovernmentEffects.GovernmentEffectData GetRealmEffects(int realmId)
        {
            return _cachedEffects.TryGetValue(realmId, out var effects) ? effects : null;
        }

        /// <summary>
        /// 手动改变政体（玩家操作 / 事件）。整体替换成分；革新前置应由 UI / GovernmentReform 先校验。
        /// </summary>
        public void ChangeGovernment(int realmId, GovernmentComposition newComposition)
        {
            if (_world == null || _world.realms == null) return;
            if (realmId < 0 || realmId >= _world.realms.Count) return;

            var realm = _world.realms[realmId];
            if (realm == null) return;

            realm.composition = newComposition;

            var effects = GovernmentEffects.CalculateEffects(newComposition);
            _cachedEffects[realmId] = effects;
            _lastFingerprint[realmId] = Fingerprint(newComposition);
            ApplyGovernmentEffects(realm, effects);

            // 政体变化会导致稳定度下降（一次性冲击）
            realm.stability = Mathf.Clamp(realm.stability - 20f, 0f, 100f);

            Debug.Log($"[GovernmentSystem] 政权 {realm.realmName} 政体手动变更为: {effects.realmNameSuffix}");
        }

        /// <summary>检查政体效果要求的革新是否已具备（基于效果数据里声明的革新名集合）</summary>
        public bool CheckInnovationRequirements(int realmId, HashSet<string> unlockedInnovations)
        {
            var effects = GetRealmEffects(realmId);
            if (effects == null) return true;

            foreach (var req in effects.requiredInnovations)
            {
                if (!unlockedInnovations.Contains(req))
                    return false;
            }
            return true;
        }

        /// <summary>获取政体缺失的革新列表</summary>
        public List<string> GetMissingInnovations(int realmId, HashSet<string> unlockedInnovations)
        {
            var missing = new List<string>();
            var effects = GetRealmEffects(realmId);
            if (effects == null) return missing;

            foreach (var req in effects.requiredInnovations)
            {
                if (!unlockedInnovations.Contains(req))
                    missing.Add(req);
            }
            return missing;
        }
    }
}
