using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Innovation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.Religion;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;








namespace CivilizationEvolution.Simulation.Characters
{
    public class PersonalityTrait
    {
        public string traitId;
        public string traitName;
        public string description;
        public TraitTier tier = TraitTier.Basic;
        public TraitCategory category = TraitCategory.Personality;

 // 属性修正
        public float martialMod = 0f;
        public float diplomacyMod = 0f;
        public float stewardshipMod = 0f;
        public float intrigueMod = 0f;
        public float learningMod = 0f;
        public float warfareMod = 0f;
        public float charmMod = 0f;       // 魅力修正（与 MentalDisorderDef 字段对齐，统一七维属性修正）

 // 互斥特质
        public List<string> conflictingTraits = new List<string>();

 // 前置特质
        public List<string> requiredTraits = new List<string>();

 /// <summary>获取特质时触发</summary>
        public virtual void OnAcquired(CharacterData character) { }

 /// <summary>失去特质时触发</summary>
        public virtual void OnRemoved(CharacterData character) { }

 /// <summary>每日效果</summary>
        public virtual void ApplyDailyEffect(CharacterData character) { }

 /// <summary>检查是否与角色现有特质冲突</summary>
        public bool IsCompatibleWith(CharacterData character)
        {
            foreach (var existing in character.traits)
            {
                if (conflictingTraits.Contains(existing.traitId))
                    return false;
            }
            return true;
        }
    }
}
