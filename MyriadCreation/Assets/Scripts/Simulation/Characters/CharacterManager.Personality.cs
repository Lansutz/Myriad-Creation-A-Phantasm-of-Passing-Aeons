using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Culture;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.Religion;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.Warfare;
using CivilizationEvolution.Simulation.WorldState;








namespace CivilizationEvolution.Simulation.Characters
{
 /// CharacterManager.Personality —— 心理与人格（压力/恐惧/精神障碍/人格漂移/天赋缺陷）（partial class，与 CharacterSystem.cs 共享字段）
    public partial class CharacterManager
    {

 // ===== 角色数值机制（饮食/精神疾病） =====
 /// 饮食联动（肥胖驱动，企划书上限型数值）：
 /// 每日从角色所属政权核心地块的贸易中心扣 1 单位粮食；
 /// 吃上 → 肥胖按身份增速（贵族/统治者吃得好，体力身份增长慢）；
 /// 缺粮 → 肥胖下降 + 压力上升
        private void DailyDiet()
        {
            if (Economy == null || Tiles == null || Realms == null) return;

            foreach (var c in _characters.Values)
            {
                if (!c.isAlive || c.realmId < 0) continue;
                if (!Realms.TryGetValue(c.realmId, out var realm) || realm.coreTiles.Count == 0) continue;

                int firstTile = -1;
                foreach (int t in realm.coreTiles) { firstTile = t; break; }
                if (firstTile < 0 || firstTile >= Tiles.Length) continue;

                int regionId = Tiles[firstTile].regionId;
                var tc = Economy.GetTradeCenter(regionId);
                if (tc != null && tc.RemoveGoods(0, 1f))
                {
                    float gain = c.role switch
                    {
                        CharacterRole.Ruler or CharacterRole.Noble => 0.04f,
                        CharacterRole.Military or CharacterRole.Commoner => 0.015f,
                        _ => 0.025f
                    };
                    c.obesity = Mathf.Clamp(c.obesity + gain, 0f, 100f);
                }
                else
                {
                    c.obesity = Mathf.Max(0f, c.obesity - 0.03f);
                    c.stress = Mathf.Min(100f, c.stress + 2f);
                }
            }
        }


 /// 精神疾病触发与缓解（简单版，角色级状态机）：
 /// - 触发：压力>80 持续 90 天 → 抑郁/焦虑；恐惧>80 → 偏执；高龄+低学识 → 失智
 /// - 缓解：压力<30 持续 120 天 → 康复（失智不可逆）
        private void CheckMentalDisorders()
        {
            foreach (var c in _characters.Values)
            {
                if (!c.isAlive) continue;

                if (string.IsNullOrEmpty(c.mentalDisorderId))
                {
                    if (c.highStressDays >= MentalHealthSystem.HighStressTriggerDays)
                    {
                        c.mentalDisorderId = UnityEngine.Random.value < 0.6f
                            ? MentalDisorderIds.Depression : MentalDisorderIds.Anxiety;
                        c.highStressDays = 0;
                        Debug.Log($"[Mental] {c.fullName} 罹患{MentalHealthSystem.GetDisorderName(c)}（长期高压）");
                    }
                    else if (c.dread > MentalHealthSystem.DreadParanoiaThreshold && UnityEngine.Random.value < 0.002f)
                    {
                        c.mentalDisorderId = MentalDisorderIds.Paranoia;
                        Debug.Log($"[Mental] {c.fullName} 罹患偏执（深度恐惧）");
                    }
                    else if (c.age >= MentalHealthSystem.DementiaAge
                        && c.scholarship < MentalHealthSystem.DementiaLearningGate)
                    {
                        float risk = 0.0005f * (c.age - MentalHealthSystem.DementiaAge + 1) / 10f;
                        if (UnityEngine.Random.value < risk)
                        {
                            c.mentalDisorderId = MentalDisorderIds.Dementia;
                            Debug.Log($"[Mental] {c.fullName} 罹患失智（年迈心智衰退）");
                        }
                    }
                }
                else
                {
                    var def = MentalHealthSystem.GetDef(c.mentalDisorderId);
                    if (def == null || !def.reversible) continue;

                    if (c.stress < 30f)
                    {
                        c.lowStressRecoveryDays++;
                        if (c.lowStressRecoveryDays >= MentalHealthSystem.LowStressRecoveryDays)
                        {
                            Debug.Log($"[Mental] {c.fullName} 从{def.GetName()}中康复");
                            c.mentalDisorderId = "";
                            c.lowStressRecoveryDays = 0;
                        }
                    }
                    else
                    {
                        c.lowStressRecoveryDays = Mathf.Max(0, c.lowStressRecoveryDays - 1);
                    }
                }
            }
        }


 // ===== 角色数值公共接口（事件/战争/疾病/AI 调用） =====
 /// 人格亲和漂移：已有角色对的关系按七维亲和度缓慢调整
 /// （借鉴 CK3 More Personality Depth 的 same/opposite opinion 机制；
 /// 仅作用于已建立的关系，不主动创建新关系）
        private void PersonalityOpinionDrift()
        {
            var chars = GetAliveCharacters();
            for (int i = 0; i < chars.Count; i++)
            {
                for (int j = i + 1; j < chars.Count; j++)
                {
                    var a = chars[i];
                    var b = chars[j];
                    if (!a.relations.TryGetValue(b.characterId, out var rel)) continue;

                    float affinity = a.GetPersonalityAffinity(b);
                    if (Mathf.Abs(affinity) < 0.5f) continue;

                    rel.opinion = Mathf.Clamp(rel.opinion + affinity * 0.002f, -200f, 200f);
                    a.relations[b.characterId] = rel; // struct 回写
                }
            }
        }


 /// <summary>施加压力（战争/缺粮/重大事件）</summary>
        public void AddStress(int characterId, float amount)
        {
            var c = GetCharacter(characterId);
            if (c != null) c.stress = Mathf.Clamp(c.stress + amount, 0f, 100f);
        }


 /// <summary>施加恐惧（处决/暴行/恐怖事件）</summary>
        public void AddDread(int characterId, float amount)
        {
            var c = GetCharacter(characterId);
            if (c != null) c.dread = Mathf.Clamp(c.dread + amount, 0f, 100f);
        }


 /// <summary>人格维度修正（枚举入口，事件驱动漂移）</summary>
        public void ModifyPersonality(int characterId, PersonalityDimension dimension, float delta)
        {
            var c = GetCharacter(characterId);
            c?.AddPersonality(dimension, delta);
        }


 /// <summary>人格维度修正（字符串键重载，事件 JSON 数据驱动用；内部解析到枚举）</summary>
        public void ModifyPersonality(int characterId, string dimension, float delta)
        {
            if (PersonalityDimensions.TryParse(dimension, out var d))
                ModifyPersonality(characterId, d, delta);
        }


 /// <summary>治愈精神疾病（贤者/事件/医学革新；失智不可逆）</summary>
        public bool CureMentalDisorder(int characterId)
        {
            var c = GetCharacter(characterId);
            if (c == null || string.IsNullOrEmpty(c.mentalDisorderId)) return false;
            var def = MentalHealthSystem.GetDef(c.mentalDisorderId);
            if (def != null && !def.reversible) return false;
            Debug.Log($"[Mental] {c.fullName} 经治疗摆脱{def?.GetName() ?? "病痛"}");
            c.mentalDisorderId = "";
            c.highStressDays = 0;
            c.lowStressRecoveryDays = 0;
            return true;
        }


 // ===== 天赋/缺陷应用 =====
        private void ApplyTalentDefectEffect(CharacterData c, DnaExpression expr)
        {
            var def = DnaSystem.FindDef(expr.talentId);
            if (def != null) ApplyDef(c, def);
            def = DnaSystem.FindDef(expr.defectId);
            if (def != null) ApplyDef(c, def);
        }

    }
}
