using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Tech
{
    /// <summary>
    /// 革新树系统
    /// 前现代技术革新，有前置依赖、研究点、效果
    /// 数据驱动（2026-08-30 重构）：革新定义由 Innovation/Innovations.json 加载
    /// （ContentRegistry 第十一类，Base/Mods 可覆盖、模组可新增）；
    /// 两级分类：大类（技术/思维/制度/传统）× 子类（见 InnovationTypes）
    /// </summary>
    [System.Serializable]
    public class InnovationTree
    {
        private readonly Dictionary<int, InnovationDef> _innovations = new Dictionary<int, InnovationDef>();
        private readonly Dictionary<int, HashSet<int>> _realmInnovations = new Dictionary<int, HashSet<int>>();
        private readonly Dictionary<int, float> _realmResearchPoints = new Dictionary<int, float>();
        private readonly Dictionary<int, int> _realmCurrentResearch = new Dictionary<int, int>();

        public InnovationTree()
        {
            LoadFromRegistry();
        }

        /// <summary>从内容注册表加载革新定义（未初始化则自动初始化；空表仅告警不崩溃）</summary>
        private void LoadFromRegistry()
        {
            if (!ContentRegistry.IsInitialized)
                ContentRegistry.Initialize();

            _innovations.Clear();
            foreach (var kv in ContentRegistry.Innovations)
                _innovations[kv.Key] = kv.Value;

            if (_innovations.Count == 0)
                Debug.LogWarning("[InnovationTree] 革新定义为空（Innovation/Innovations.json 缺失或未加载）");
        }

        /// <summary>运行时注册/覆盖单个革新（模组热扩展入口）</summary>
        public void RegisterInnovation(InnovationDef def)
        {
            if (def == null || def.innovationId <= 0) return;
            _innovations[def.innovationId] = def;
        }

        /// <summary>开始研究</summary>
        public bool StartResearch(int realmId, int innovationId)
        {
            if (!_innovations.TryGetValue(innovationId, out var def)) return false;
            if (HasInnovation(realmId, innovationId)) return false;

            // 检查前置
            foreach (int prereq in def.prerequisites)
            {
                if (!HasInnovation(realmId, prereq)) return false;
            }

            _realmCurrentResearch[realmId] = innovationId;
            if (!_realmResearchPoints.ContainsKey(realmId))
                _realmResearchPoints[realmId] = 0f;

            return true;
        }

        /// <summary>每日研究Tick</summary>
        public void DailyTick(int realmId, float researchRate)
        {
            if (!_realmCurrentResearch.TryGetValue(realmId, out int innovationId)) return;
            if (!_innovations.TryGetValue(innovationId, out var def)) return;

            if (!_realmResearchPoints.ContainsKey(realmId))
                _realmResearchPoints[realmId] = 0f;

            _realmResearchPoints[realmId] += researchRate;

            if (_realmResearchPoints[realmId] >= def.researchCost)
            {
                CompleteResearch(realmId, innovationId);
            }
        }

        /// <summary>完成研究</summary>
        private void CompleteResearch(int realmId, int innovationId)
        {
            if (!_realmInnovations.ContainsKey(realmId))
                _realmInnovations[realmId] = new HashSet<int>();

            _realmInnovations[realmId].Add(innovationId);
            _realmResearchPoints[realmId] = 0f;
            _realmCurrentResearch.Remove(realmId);

            if (_innovations.TryGetValue(innovationId, out var def))
                Debug.Log($"[Innovation] 政权 {realmId} 完成研究：{def.GetName()}（{def.Domain}/{def.field}）");
        }

        /// <summary>检查是否拥有革新</summary>
        public bool HasInnovation(int realmId, int innovationId)
        {
            return _realmInnovations.TryGetValue(realmId, out var set) && set.Contains(innovationId);
        }

        /// <summary>获取研究进度</summary>
        public float GetResearchProgress(int realmId)
        {
            if (!_realmCurrentResearch.TryGetValue(realmId, out int innovationId)) return 0f;
            if (!_innovations.TryGetValue(innovationId, out var def)) return 0f;
            if (!_realmResearchPoints.TryGetValue(realmId, out float points)) return 0f;
            return points / def.researchCost;
        }

        /// <summary>获取当前研究（null 表示无）</summary>
        public InnovationDef GetCurrentResearch(int realmId)
        {
            if (_realmCurrentResearch.TryGetValue(realmId, out int id) && _innovations.TryGetValue(id, out var def))
                return def;
            return null;
        }

        /// <summary>获取可研究的革新列表</summary>
        public List<InnovationDef> GetAvailableInnovations(int realmId)
        {
            var result = new List<InnovationDef>();
            foreach (var def in _innovations.Values)
            {
                if (HasInnovation(realmId, def.innovationId)) continue;

                bool prereqsMet = true;
                foreach (int prereq in def.prerequisites)
                {
                    if (!HasInnovation(realmId, prereq))
                    {
                        prereqsMet = false;
                        break;
                    }
                }

                if (prereqsMet)
                    result.Add(def);
            }
            return result;
        }

        /// <summary>获取某大类的全部革新（AI 偏好/UI 筛选用）</summary>
        public List<InnovationDef> GetInnovationsByDomain(InnovationDomain domain)
        {
            var result = new List<InnovationDef>();
            foreach (var def in _innovations.Values)
            {
                if (def.Domain == domain)
                    result.Add(def);
            }
            return result;
        }

        /// <summary>获取某子类的全部革新</summary>
        public List<InnovationDef> GetInnovationsByField(InnovationField field)
        {
            var result = new List<InnovationDef>();
            foreach (var def in _innovations.Values)
            {
                if (def.field == field)
                    result.Add(def);
            }
            return result;
        }

        // ===== 查询接口 =====
        public InnovationDef GetInnovation(int id) => _innovations.TryGetValue(id, out var d) ? d : null;
        public IReadOnlyDictionary<int, InnovationDef> GetAllInnovations() => _innovations;
        public HashSet<int> GetRealmInnovations(int realmId) => _realmInnovations.TryGetValue(realmId, out var s) ? s : new HashSet<int>();
        public int GetRealmInnovationCount(int realmId) => _realmInnovations.TryGetValue(realmId, out var s) ? s.Count : 0;
    }
}
