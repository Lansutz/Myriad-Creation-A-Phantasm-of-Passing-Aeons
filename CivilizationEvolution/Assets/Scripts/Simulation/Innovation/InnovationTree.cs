using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Tech
{
 /// 革新树系统
 /// 前现代技术革新，有前置依赖、研究点、效果
 /// 数据驱动：革新定义由 Innovation/Innovations.json 加载
 /// （ContentRegistry 第十一类，Base/Mods 可覆盖、模组可新增）；
 /// 两级分类：大类（技术/思维/制度/传统）× 子类（见 InnovationTypes）
    [System.Serializable]
    public class InnovationTree
    {
        private readonly Dictionary<int, InnovationDef> _innovations = new Dictionary<int, InnovationDef>();
        private readonly Dictionary<int, HashSet<int>> _realmInnovations = new Dictionary<int, HashSet<int>>();
        private readonly Dictionary<int, float> _realmResearchPoints = new Dictionary<int, float>();
        private readonly Dictionary<int, int> _realmCurrentResearch = new Dictionary<int, int>();

 /// <summary>革新完成事件（realmId, innovationId——阶层出现/政体改革等联动订阅）</summary>
        public event System.Action<int, int> OnInnovationCompleted;

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

 // 检查前置（AND 全满足 + OR 任一满足；无前置则通过）
            if (!ArePrerequisitesMet(null, realmId, def)) return false; // 物产检查需要world，此处先跳过（研究开始时由调用方确保条件）

            _realmCurrentResearch[realmId] = innovationId;
            if (!_realmResearchPoints.ContainsKey(realmId))
                _realmResearchPoints[realmId] = 0f;

            return true;
        }

 /// <summary>前置检查：prerequisites 全部持有 + prerequisitesAny 至少一项持有（空列表视为通过）</summary>
        public bool ArePrerequisitesMet(GameWorld world, int realmId, InnovationDef def)
        {
            foreach (int prereq in def.prerequisites)
            {
                if (!HasInnovation(realmId, prereq)) return false;
            }
            if (def.prerequisitesAny != null && def.prerequisitesAny.Count > 0)
            {
                bool anyMet = false;
                foreach (int alt in def.prerequisitesAny)
                {
                    if (HasInnovation(realmId, alt)) { anyMet = true; break; }
                }
                if (!anyMet) return false;
            }
            // 物产前置（AND：全部满足，含累计产量门槛）
            if (def.requiredAllResources != null)
            {
                foreach (int resId in def.requiredAllResources)
                {
                    if (!HasResource(world, realmId, resId, def.allowTradeResource, def.requiredResourceAmount))
                        return false;
                }
            }
            // 物产前置（OR：至少一个满足，含累计产量门槛）
            if (def.requiredAnyResources != null && def.requiredAnyResources.Count > 0)
            {
                bool anyRes = false;
                foreach (int resId in def.requiredAnyResources)
                {
                    if (HasResource(world, realmId, resId, def.allowTradeResource, def.requiredResourceAmount))
                    { anyRes = true; break; }
                }
                if (!anyRes) return false;
            }
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

 // 完成事件（阶层出现检测/政体改革联动订阅）
            OnInnovationCompleted?.Invoke(realmId, innovationId);
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

 /// 直接前置（仅最近一层——多链各一条；UI 展示不推全链）
 /// 返回 (prerequisites AND 链, prerequisitesAny OR 链) 的直接前置 ID
        public (List<int> and, List<int> or) GetDirectPrerequisites(int innovationId)
        {
            if (!_innovations.TryGetValue(innovationId, out var def))
                return (new List<int>(), new List<int>());
            return (new List<int>(def.prerequisites), new List<int>(def.prerequisitesAny));
        }

 /// <summary>获取可研究的革新列表</summary>
        public List<InnovationDef> GetAvailableInnovations(int realmId)
        {
            var result = new List<InnovationDef>();
            foreach (var def in _innovations.Values)
            {
                if (HasInnovation(realmId, def.innovationId)) continue;

                if (ArePrerequisitesMet(null, realmId, def))
                    result.Add(def);
            }
            return result;
        }

 // ===== 学习速率机制（速率由多种参数共同构成） =====
 /// 学习难度（前置完成比例 0~1）：
 /// 前置全完成=1.0（没有困难，速度很快）；缺前置=0.4 + 0.6×完成比例
 /// （需要花时间——超前学习/链未补齐时学习慢）
        public float GetLearningDifficulty(int realmId, int innovationId)
        {
            if (!_innovations.TryGetValue(innovationId, out var def)) return 0f;

            int total = def.prerequisites.Count;
            int done = 0;
            foreach (int prereq in def.prerequisites)
                if (HasInnovation(realmId, prereq)) done++;

 // OR 前置：任一满足即算完成
            if (def.prerequisitesAny != null && def.prerequisitesAny.Count > 0)
            {
                total += 1;
                bool anyMet = false;
                foreach (int alt in def.prerequisitesAny)
                    if (HasInnovation(realmId, alt)) { anyMet = true; break; }
                if (anyMet) done++;
            }

            if (total == 0) return 1f;
            float ratio = (float)done / total;
            return 0.4f + 0.6f * ratio;
        }

 /// 有效研究速率（速率=基础×学习难度×文化亲和加成）
 /// 文化亲和：革新的 field 名 或 affinityTags 与文化的 innovationAffinities
 /// 匹配 → ×1.25（Laethis 亲和 Agriculture/Craft/Script 是 field 级；
 /// Clay/Manor 等是节点级标签——两级都查）
        public float GetEffectiveResearchRate(int realmId, int innovationId, float baseRate,
            Culture.CultureData culture)
        {
            float rate = baseRate * GetLearningDifficulty(realmId, innovationId);

            if (culture != null && _innovations.TryGetValue(innovationId, out var def))
            {
                bool affinity = culture.HasInnovationAffinity(def.field.ToString());
                if (!affinity && def.affinityTags != null)
                {
                    foreach (var tag in def.affinityTags)
                    {
                        if (culture.HasInnovationAffinity(tag)) { affinity = true; break; }
                    }
                }
                if (affinity) rate *= 1.25f;
            }
            return rate;
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

 // ===== 研究进度系统（实践驱动：资源产量积累经验） =====
 /// <summary>各革新的研究进度（key=innovationId）</summary>
        private readonly Dictionary<int, InnovationProgress> _innovationProgress = new Dictionary<int, InnovationProgress>();

        /// <summary>各政权各物资的累计产量（key="realmId_goodsId"，用于物产数量门槛）</summary>
        private readonly Dictionary<string, float> _resourceCumulativeOutput = new Dictionary<string, float>();

 /// <summary>获取革新的研究进度（不存在则创建）</summary>
        public InnovationProgress GetProgress(int innovationId)
        {
            if (!_innovationProgress.TryGetValue(innovationId, out var p))
            {
                p = new InnovationProgress { innovationId = innovationId };
                _innovationProgress[innovationId] = p;
            }
            return p;
        }

 /// <summary>检查政权是否拥有某物产（控制地块上的已发现资源点）</summary>
        public bool HasResource(GameWorld world, int realmId, int goodsId, bool allowTrade, float minAmount = 0f)
        {
            if (world == null || world.tiles == null) return false;
            bool hasPoint = false;
            for (int i = 0; i < world.tiles.Length; i++)
            {
                ref var tile = ref world.tiles[i];
                if (!tile.exists || tile.ownerRealmId != realmId) continue;
                if (tile.resources == null) continue;
                foreach (var res in tile.resources)
                {
                    if (res.goodsId == goodsId && (res.discovered || res.developed))
                    { hasPoint = true; break; }
                }
                if (hasPoint) break;
            }
            if (!hasPoint) return false;
            if (minAmount > 0f)
            {
                string key = realmId + "_" + goodsId;
                if (_resourceCumulativeOutput.TryGetValue(key, out float cum) && cum >= minAmount)
                    return true;
                return false;
            }
            return true;
        }

        private void AccumulateResourceOutput(int realmId, int goodsId, float monthlyOutput)
        {
            if (monthlyOutput <= 0f) return;
            string key = realmId + "_" + goodsId;
            _resourceCumulativeOutput.TryGetValue(key, out float cum);
            _resourceCumulativeOutput[key] = cum + monthlyOutput;
        }

        public float GetResourceCumulativeOutput(int realmId, int goodsId)
        {
            string key = realmId + "_" + goodsId;
            return _resourceCumulativeOutput.TryGetValue(key, out float cum) ? cum : 0f;
        }

 /// <summary>获取某物产的政权月产量（简化版：开发中资源点的丰度×开发程度之和）</summary>
        public float GetResourceMonthlyOutput(GameWorld world, int realmId, int goodsId)
        {
            if (world == null || world.tiles == null) return 0f;
            float total = 0f;
            for (int i = 0; i < world.tiles.Length; i++)
            {
                ref var tile = ref world.tiles[i];
                if (!tile.exists || tile.ownerRealmId != realmId) continue;
                if (tile.resources == null) continue;
                foreach (var res in tile.resources)
                {
                    if (res.goodsId == goodsId && res.developed)
                        total += res.EffectiveOutput * 10f;
                }
            }
            return total;
        }

 /// <summary>每月更新研究进度（实践驱动：基础+资源实践+规模+人员+建筑）</summary>
        /// <summary>每月更新研究进度（实践驱动：累计产量×品质上限+边际递减+角色研究）</summary>
        /// <param name="world">游戏世界</param>
        /// <param name="realmId">政权ID</param>
        /// <param name="innovationId">革新ID</param>
        /// <param name="monthlyOutput">本月相关物资产量（累加到累计产量）</param>
        /// <param name="averageQuality">相关加工品的平均品质（0-10，决定产量上限和经验系数）</param>
        /// <param name="researcherCount">主动研究该革新的角色数量（工匠/学者/商人）</param>
        /// <param name="hasFacility">是否有相关建筑</param>
        public void MonthlyTickProgress(GameWorld world, int realmId, int innovationId,
            float monthlyOutput, float averageQuality, int researcherCount, bool hasFacility)
        {
            if (!_innovations.TryGetValue(innovationId, out var def)) return;
            if (HasInnovation(realmId, innovationId)) return;

            var p = GetProgress(innovationId);
            p.isAvailable = ArePrerequisitesMet(world, realmId, def);
            if (!p.isAvailable)
            {
                p.lockedReason = GetLockedReason(world, realmId, def);
                p.monthlyGain = 0f;
                return;
            }

            p.gainBreakdown.Clear();

            // 1. 累计产量（实践的基础——生产越多经验越多，但有品质上限）
            p.cumulativeOutput += monthlyOutput;
            p.averageQuality = averageQuality;
            if (monthlyOutput > 0f) p.oldMethodPracticeCount++;
            // 分别累加每种相关物资的累计产量（用于物产数量门槛）
            if (def.requiredAnyResources != null)
                foreach (int resId in def.requiredAnyResources)
                    AccumulateResourceOutput(realmId, resId, GetResourceMonthlyOutput(world, realmId, resId));
            if (def.requiredAllResources != null)
                foreach (int resId in def.requiredAllResources)
                    AccumulateResourceOutput(realmId, resId, GetResourceMonthlyOutput(world, realmId, resId));

            // 2. 品质决定产量上限：品质1=50, 品质5=250, 品质10=500
            // 累计产量超过上限后，实践经验不再增长——必须提升品质才能继续积累
            float outputCap = averageQuality * InnovationProgressConfig.OutputCapPerQuality;
            float effectiveOutput = Mathf.Min(p.cumulativeOutput, outputCap);

            // 3. 产量经验 = log(1 + 有效产量) × 基础系数 × 品质系数
            // 品质系数：标准品质(5)为1.0，每高1级+10%，每低1级-10%
            float outputExperience = 0f;
            if (effectiveOutput > 0f)
            {
                float qualityMultiplier = 1f + (averageQuality - 5f) * InnovationProgressConfig.QualityCoefficientPerLevel;
                outputExperience = Mathf.Log(1f + effectiveOutput) * InnovationProgressConfig.OutputExperienceBase * Mathf.Max(0.1f, qualityMultiplier);
                p.gainBreakdown["生产实践"] = outputExperience;
            }
            if (p.cumulativeOutput > outputCap)
            {
                p.gainBreakdown["产量已达品质上限(需提升品质)"] = 0f;
            }

            // 4. 角色研究（与Character系统串联——工匠/学者/商人主动研究）
            // 每个研究角色每月提供基础研究进度，旧方法实践次数够多后角色可能获得灵感加成
            float characterResearch = researcherCount * InnovationProgressConfig.CharacterResearchBase;
            if (p.oldMethodPracticeCount >= InnovationProgressConfig.OldMethodPracticeThreshold)
            {
                // 旧方法实践够多次后，角色有灵感，研究效率翻倍
                characterResearch *= 2f;
                p.gainBreakdown["角色灵感加成"] = characterResearch * 0.5f;
            }
            if (researcherCount > 0)
            {
                p.gainBreakdown["角色研究"] = characterResearch;
            }

            // 5. 设施加成
            float facilityGain = 0f;
            if (hasFacility)
            {
                facilityGain = InnovationProgressConfig.FacilityBonus;
                p.gainBreakdown["设施支持"] = facilityGain;
            }

            // 6. 基础观察（极慢）
            float baseGain = InnovationProgressConfig.BaseMonthlyGain;
            p.gainBreakdown["基础观察"] = baseGain;

            // 7. 合计基础增长
            float rawGain = baseGain + outputExperience + characterResearch + facilityGain;

            // 8. 边际递减系数：进度越高，增长越慢（连续函数，不是硬门槛）
            // 递减系数 = 1 / (1 + progress / DiminishingBase)
            float diminishingFactor = 1f / (1f + p.progress / InnovationProgressConfig.DiminishingBase);
            p.gainBreakdown["边际递减系数"] = diminishingFactor;

            float gain = rawGain * diminishingFactor;
            gain = Mathf.Min(gain, InnovationProgressConfig.MaxMonthlyGain);

            p.monthlyGain = gain;
            p.progress += gain;
            p.lockedReason = "";

            if (p.progress >= 100f)
            {
                CompleteResearch(realmId, innovationId);
                p.progress = 100f;
            }
        }

        /// <summary>获取与革新相关的资源月产量（OR取最大，AND取最小）</summary>
        private float GetRelevantResourceOutput(GameWorld world, int realmId, InnovationDef def)
        {
            float output = 0f;
            if (def.requiredAnyResources != null && def.requiredAnyResources.Count > 0)
            {
                foreach (int resId in def.requiredAnyResources)
                    output = Mathf.Max(output, GetResourceMonthlyOutput(world, realmId, resId));
            }
            if (def.requiredAllResources != null && def.requiredAllResources.Count > 0)
            {
                float minOutput = float.MaxValue;
                foreach (int resId in def.requiredAllResources)
                    minOutput = Mathf.Min(minOutput, GetResourceMonthlyOutput(world, realmId, resId));
                if (minOutput < float.MaxValue) output = minOutput;
            }
            return output;
        }

 /// <summary>获取革新锁定原因的可读文本</summary>
        private string GetLockedReason(GameWorld world, int realmId, InnovationDef def)
        {
            foreach (int prereq in def.prerequisites)
            {
                if (!HasInnovation(realmId, prereq))
                {
                    var prereqDef = GetInnovation(prereq);
                    return "需要前置革新：" + (prereqDef != null ? prereqDef.GetName() : prereq.ToString());
                }
            }
            if (def.requiredAllResources != null)
            {
                foreach (int resId in def.requiredAllResources)
                {
                    if (!HasResource(world, realmId, resId, def.allowTradeResource, def.requiredResourceAmount))
                    {
                        string missingName = (world != null && world.goodsDefs != null && world.goodsDefs.TryGetValue(resId, out var missingGood)) ? missingGood.goodsName : resId.ToString();
                        return "缺少物产：" + missingName;
                    }
                }
            }
            if (def.requiredAnyResources != null && def.requiredAnyResources.Count > 0)
            {
                bool any = false;
                foreach (int resId in def.requiredAnyResources)
                {
                    if (HasResource(world, realmId, resId, def.allowTradeResource, def.requiredResourceAmount)) { any = true; break; }
                }
                if (!any)
                {
                    var names = new List<string>();
                    foreach (int resId in def.requiredAnyResources)
                    {
                        names.Add((world != null && world.goodsDefs != null && world.goodsDefs.TryGetValue(resId, out var anyGood)) ? anyGood.goodsName : resId.ToString());
                    }
                    return "需要以下物产之一：" + string.Join("/", names);
                }
            }
            return "条件未满足";
        }
    }
}
