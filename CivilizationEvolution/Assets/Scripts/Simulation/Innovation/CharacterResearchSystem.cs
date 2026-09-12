using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;




namespace CivilizationEvolution.Simulation.Innovation
{
    /// <summary>
    /// 角色研究数据（与Character系统串联——通过characterId关联）。
    /// 工匠/学者/商人等角色可以主动研究革新。
    /// 核心逻辑：角色用旧方法实践一定次数后，获得"灵感"，灵感加速研究。
    /// 研究不是免费的——需要消耗物资、时间、设施。
    /// </summary>
    [Serializable]
    public class CharacterResearchData
    {
        /// <summary>角色ID（关联CharacterData.characterId）</summary>
        public int characterId;

        /// <summary>当前正在研究的革新ID（-1=无）</summary>
        public int currentResearchInnovationId = -1;

        /// <summary>角色的研究专长领域（InnovationField，角色在该领域研究效率+50%）</summary>
        public InnovationField specialty = InnovationField.Craft;

        /// <summary>角色的研究能力（0-10，影响研究效率——智力/学习能力/经验）</summary>
        public float researchAbility = 3f;

        /// <summary>灵感值（0-100，旧方法实践积累，满了触发灵感爆发，研究效率翻倍）</summary>
        public float inspiration = 0f;

        /// <summary>是否处于灵感爆发状态（研究效率翻倍，持续一段时间）</summary>
        public bool isInspired = false;

        /// <summary>灵感爆发剩余天数</summary>
        public int inspirationRemainingDays = 0;

        /// <summary>累计研究贡献（该角色累计为革新研究提供的进度总量）</summary>
        public float totalResearchContribution = 0f;

        /// <summary>已完成的革新数量</summary>
        public int completedInnovations = 0;

        public CharacterResearchData(int characterId)
        {
            this.characterId = characterId;
        }

        /// <summary>研究效率系数（研究能力×专长加成×灵感加成）</summary>
        public float ResearchEfficiency
        {
            get
            {
                float efficiency = 0.5f + researchAbility * 0.15f;  // 能力3=0.95, 能力10=2.0
                if (isInspired) efficiency *= 2f;  // 灵感爆发翻倍
                return efficiency;
            }
        }

        /// <summary>开始研究革新</summary>
        public bool StartResearch(int innovationId, InnovationField field)
        {
            if (currentResearchInnovationId >= 0) return false;  // 正在研究其他革新
            currentResearchInnovationId = innovationId;
            // 如果研究领域是专长，额外获得灵感
            if (field == specialty)
            {
                inspiration = Mathf.Min(100f, inspiration + 20f);
            }
            return true;
        }

        /// <summary>完成研究</summary>
        public void CompleteResearch()
        {
            completedInnovations++;
            totalResearchContribution = 0f;
            currentResearchInnovationId = -1;
            // 完成研究后获得灵感（成就感）
            inspiration = Mathf.Min(100f, inspiration + 30f);
        }

        /// <summary>每日Tick（灵感衰减、灵感爆发计时）</summary>
        public void DailyTick()
        {
            // 灵感缓慢衰减
            inspiration = Mathf.Max(0f, inspiration - 0.1f);

            // 灵感爆发计时
            if (isInspired)
            {
                inspirationRemainingDays--;
                if (inspirationRemainingDays <= 0)
                {
                    isInspired = false;
                    inspirationRemainingDays = 0;
                }
            }

            // 灵感满100触发灵感爆发
            if (inspiration >= 100f && !isInspired)
            {
                isInspired = true;
                inspirationRemainingDays = 30;  // 灵感爆发持续30天
                inspiration = 0f;
            }
        }

        /// <summary>旧方法实践（积累灵感）</summary>
        public void PracticeOldMethod(float amount)
        {
            inspiration = Mathf.Min(100f, inspiration + amount * 0.5f);
        }
    }

    /// <summary>
    /// 角色研究管理器（每个政权一个）。
    /// 管理该政权所有研究角色的研究活动，与InnovationTree串联。
    /// 核心逻辑：
    /// 1. 工匠/学者/商人角色可以主动研究革新
    /// 2. 角色用旧方法实践一定次数后，获得灵感
    /// 3. 灵感加速研究效率
    /// 4. 研究需要消耗物资（不是免费的）
    /// </summary>
    public class CharacterResearchManager
    {
        /// <summary>政权ID</summary>
        public int realmId;

        /// <summary>各角色的研究数据（key=characterId）</summary>
        private readonly Dictionary<int, CharacterResearchData> _characterResearch = new Dictionary<int, CharacterResearchData>();

        /// <summary>研究物资消耗（每月每个研究角色消耗的物资量）</summary>
        public const float MonthlyResearchCostPerCharacter = 5f;

        public CharacterResearchManager(int realmId)
        {
            this.realmId = realmId;
        }

        /// <summary>获取角色研究数据（不存在则创建）</summary>
        public CharacterResearchData GetCharacterResearch(int characterId)
        {
            if (!_characterResearch.TryGetValue(characterId, out var data))
            {
                data = new CharacterResearchData(characterId);
                _characterResearch[characterId] = data;
            }
            return data;
        }

        /// <summary>角色开始研究革新</summary>
        public bool StartCharacterResearch(int characterId, int innovationId, InnovationField field)
        {
            var data = GetCharacterResearch(characterId);
            return data.StartResearch(innovationId, field);
        }

        /// <summary>获取正在研究某革新的角色数量（用于InnovationTree的researcherCount参数）</summary>
        public int GetResearcherCount(int innovationId)
        {
            int count = 0;
            foreach (var kv in _characterResearch)
            {
                if (kv.Value.currentResearchInnovationId == innovationId)
                    count++;
            }
            return count;
        }

        /// <summary>获取所有研究角色的总研究效率（用于InnovationTree）</summary>
        public float GetTotalResearchEfficiency(int innovationId)
        {
            float total = 0f;
            foreach (var kv in _characterResearch)
            {
                if (kv.Value.currentResearchInnovationId == innovationId)
                    total += kv.Value.ResearchEfficiency;
            }
            return total;
        }

        /// <summary>每日Tick（所有角色灵感衰减、灵感爆发计时）</summary>
        public void DailyTick()
        {
            foreach (var kv in _characterResearch)
            {
                kv.Value.DailyTick();
            }
        }

        /// <summary>角色旧方法实践（积累灵感）</summary>
        public void PracticeOldMethod(int characterId, float amount)
        {
            var data = GetCharacterResearch(characterId);
            data.PracticeOldMethod(amount);
        }

        /// <summary>完成革新研究（通知所有研究该革新的角色）</summary>
        public void OnInnovationCompleted(int innovationId)
        {
            foreach (var kv in _characterResearch)
            {
                if (kv.Value.currentResearchInnovationId == innovationId)
                {
                    kv.Value.CompleteResearch();
                }
            }
        }

        /// <summary>获取所有研究角色ID列表</summary>
        public List<int> GetAllResearchers()
        {
            return new List<int>(_characterResearch.Keys);
        }
    }
}
