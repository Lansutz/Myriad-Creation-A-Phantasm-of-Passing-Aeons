using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Tech
{
    /// <summary>
    /// 革新树系统
    /// 前现代技术革新，有前置依赖、研究点、效果
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
            InitializeInnovationTree();
        }

        private void InitializeInnovationTree()
        {
            // 农业革新
            AddInnovation(100, "刀耕火种", InnovationCategory.Agriculture, 1, 100f, new List<int>());
            AddInnovation(101, "休耕制", InnovationCategory.Agriculture, 1, 150f, new List<int> { 100 });
            AddInnovation(102, "轮作制", InnovationCategory.Agriculture, 2, 300f, new List<int> { 101 });
            AddInnovation(103, "三圃制", InnovationCategory.Agriculture, 2, 400f, new List<int> { 102 });
            AddInnovation(104, "重犁", InnovationCategory.Agriculture, 2, 350f, new List<int> { 101 });
            AddInnovation(105, "马轭", InnovationCategory.Agriculture, 2, 250f, new List<int> { 104 });
            AddInnovation(106, "水磨", InnovationCategory.Agriculture, 3, 500f, new List<int> { 103, 105 });
            AddInnovation(107, "灌溉技术", InnovationCategory.Agriculture, 3, 600f, new List<int> { 103 });

            // 手工业革新
            AddInnovation(200, "陶器制作", InnovationCategory.Craft, 1, 100f, new List<int>());
            AddInnovation(201, "青铜冶炼", InnovationCategory.Craft, 1, 200f, new List<int> { 200 });
            AddInnovation(202, "冶铁术", InnovationCategory.Craft, 2, 400f, new List<int> { 201 });
            AddInnovation(203, "炼钢术", InnovationCategory.Craft, 3, 700f, new List<int> { 202 });
            AddInnovation(204, "纺织机", InnovationCategory.Craft, 2, 350f, new List<int> { 200 });
            AddInnovation(205, "造纸术", InnovationCategory.Craft, 3, 500f, new List<int> { 202 });
            AddInnovation(206, "印刷术", InnovationCategory.Craft, 4, 1000f, new List<int> { 205 });
            AddInnovation(207, "火药", InnovationCategory.Craft, 4, 1200f, new List<int> { 203, 205 });

            // 军事革新
            AddInnovation(300, "青铜武器", InnovationCategory.Military, 1, 150f, new List<int> { 201 });
            AddInnovation(301, "铁制武器", InnovationCategory.Military, 2, 350f, new List<int> { 202, 300 });
            AddInnovation(302, "骑兵战术", InnovationCategory.Military, 2, 400f, new List<int>());
            AddInnovation(303, "重装骑兵", InnovationCategory.Military, 3, 600f, new List<int> { 301, 302 });
            AddInnovation(304, "弩", InnovationCategory.Military, 2, 300f, new List<int> { 300 });
            AddInnovation(305, "攻城器械", InnovationCategory.Military, 2, 400f, new List<int> { 300 });
            AddInnovation(306, "城堡建筑", InnovationCategory.Military, 3, 500f, new List<int> { 305 });
            AddInnovation(307, "火器", InnovationCategory.Military, 4, 1500f, new List<int> { 207, 303 });

            // 航海革新
            AddInnovation(400, "独木舟", InnovationCategory.Naval, 1, 80f, new List<int>());
            AddInnovation(401, "帆船", InnovationCategory.Naval, 1, 200f, new List<int> { 400 });
            AddInnovation(402, "桨帆船", InnovationCategory.Naval, 2, 400f, new List<int> { 401 });
            AddInnovation(403, "罗盘", InnovationCategory.Naval, 2, 300f, new List<int> { 205 });
            AddInnovation(404, "卡拉维尔帆船", InnovationCategory.Naval, 3, 700f, new List<int> { 402, 403 });
            AddInnovation(405, "克拉克帆船", InnovationCategory.Naval, 3, 900f, new List<int> { 404 });
            AddInnovation(406, "盖伦帆船", InnovationCategory.Naval, 4, 1200f, new List<int> { 405 });

            // 政治/社会革新
            AddInnovation(500, "部落联盟", InnovationCategory.Political, 1, 100f, new List<int>());
            AddInnovation(501, "封建制度", InnovationCategory.Political, 2, 300f, new List<int> { 500 });
            AddInnovation(502, "中央集权", InnovationCategory.Political, 3, 600f, new List<int> { 501 });
            AddInnovation(503, "官僚制度", InnovationCategory.Political, 3, 500f, new List<int> { 502 });
            AddInnovation(504, "科举制度", InnovationCategory.Political, 3, 700f, new List<int> { 503, 205 });
            AddInnovation(505, "成文法", InnovationCategory.Political, 2, 300f, new List<int> { 200 });
            AddInnovation(506, "民法典", InnovationCategory.Political, 3, 600f, new List<int> { 505 });

            // 文化/宗教革新
            AddInnovation(600, "文字", InnovationCategory.Cultural, 1, 200f, new List<int> { 200 });
            AddInnovation(601, "一神教", InnovationCategory.Cultural, 2, 400f, new List<int> { 600 });
            AddInnovation(602, "哲学", InnovationCategory.Cultural, 2, 350f, new List<int> { 600 });
            AddInnovation(603, "大学", InnovationCategory.Cultural, 3, 600f, new List<int> { 602, 205 });
            AddInnovation(604, "文艺复兴", InnovationCategory.Cultural, 4, 1500f, new List<int> { 603, 206 });

            // 经济革新
            AddInnovation(700, "以物易物", InnovationCategory.Economic, 1, 50f, new List<int>());
            AddInnovation(701, "铸币", InnovationCategory.Economic, 1, 200f, new List<int> { 700, 201 });
            AddInnovation(702, "银行业", InnovationCategory.Economic, 3, 600f, new List<int> { 701 });
            AddInnovation(703, "纸币", InnovationCategory.Economic, 3, 500f, new List<int> { 701, 205 });
            AddInnovation(704, "股份公司", InnovationCategory.Economic, 4, 1200f, new List<int> { 702 });
            AddInnovation(705, "复式记账", InnovationCategory.Economic, 2, 300f, new List<int> { 701 });
        }

        private void AddInnovation(int id, string name, InnovationCategory category, int era, float cost, List<int> prerequisites)
        {
            _innovations[id] = new InnovationDef
            {
                innovationId = id,
                innovationName = name,
                category = category,
                era = era,
                researchCost = cost,
                prerequisites = prerequisites
            };
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
                Debug.Log($"[Innovation] 政权 {realmId} 完成研究：{def.innovationName}");
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

        /// <summary>获取当前研究</summary>
        public InnovationDef? GetCurrentResearch(int realmId)
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

        // ===== 查询接口 =====
        public InnovationDef? GetInnovation(int id) => _innovations.TryGetValue(id, out var d) ? d : null;
        public IReadOnlyDictionary<int, InnovationDef> GetAllInnovations() => _innovations;
        public HashSet<int> GetRealmInnovations(int realmId) => _realmInnovations.TryGetValue(realmId, out var s) ? s : new HashSet<int>();
        public int GetRealmInnovationCount(int realmId) => _realmInnovations.TryGetValue(realmId, out var s) ? s.Count : 0;
    }

    /// <summary>革新定义</summary>
    [System.Serializable]
    public struct InnovationDef
    {
        public int innovationId;
        public string innovationName;
        public InnovationCategory category;
        public int era; // 时代 1-4
        public float researchCost;
        public List<int> prerequisites;
        public string description;
    }

    public enum InnovationCategory
    {
        Agriculture,
        Craft,
        Military,
        Naval,
        Political,
        Cultural,
        Economic
    }
}
