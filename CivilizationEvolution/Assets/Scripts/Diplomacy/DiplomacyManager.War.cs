using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;
using CivilizationEvolution.War;
using CivilizationEvolution.Character;

namespace CivilizationEvolution.Diplomacy
{
 /// DiplomacyManager.War —— 战争与敌对（宣战/战争借口/敌对度/突袭/边境摩擦/和平条约）（partial class，与 DiplomacySystem.cs 共享字段）    public partial class DiplomacyManager
    {

 // ===== 战争与和平 =====
 /// <summary>宣战</summary>        public bool DeclareWar(int attackerId, int defenderId, string reason)
        {
            var rel = GetRelation(attackerId, defenderId);
            if (rel == null || rel.isAtWar) return false;

 // 停战检查（WarRules.truceYears——借鉴《地图上发生的事》truce_until_tick）            if (rel.truceUntilDay >= 0 && CurrentDay < rel.truceUntilDay)
            {
                AddEventTo(rel, DiplomaticEventType.DemandRejected,
                    $"{_realms[attackerId].realmName} 欲开战，但停战期未满（至第 {rel.truceUntilDay} 日）");
                return false;
            }

            rel.isAtWar = true;
            rel.warDeclaredDay = CurrentDay;
            rel.hostilityLevel = 100f;
            rel.relation = Mathf.Min(rel.relation, -50f);

 // 战争爆发：自动撤销双方的军事通行权            if (_realms.TryGetValue(attackerId, out var atkRealm))
                CivilizationEvolution.Military.MovementControlSystem.OnWarDeclared(atkRealm, defenderId);
            if (_realms.TryGetValue(defenderId, out var defRealm))
                CivilizationEvolution.Military.MovementControlSystem.OnWarDeclared(defRealm, attackerId);
            rel.trust = Mathf.Min(rel.trust, 10f);
            rel.threat = Mathf.Max(rel.threat, 90f);

 // 解除所有盟约            rel.activeAlliances.Clear();

            rel.AddEvent(new DiplomaticEvent
            {
                type = DiplomaticEventType.WarDeclaration,
                description = $"{_realms[attackerId].realmName} 对 {_realms[defenderId].realmName} 宣战：{reason}",
                relationChange = -50f,
                trustChange = -40f,
                threatChange = 40f
            });

 // 编年史（重大）            Chronicle?.Add("war", $"{_realms[attackerId].realmName} 对 {_realms[defenderId].realmName} 宣战：{reason}",
                major: true, attackerId, defenderId);

 // 通知同盟国（WarRules.allowAllianceIntervention 控制）            if (WarRules == null || WarRules.allowAllianceIntervention)
                NotifyAlliesOfWar(attackerId, defenderId);
            return true;
        }


 /// <summary>带战争借口和战争目标的宣战（三层分离：借口→目标→条约）</summary>        public bool DeclareWarWithJustification(int attackerId, int defenderId,
            CasusBelli casusBelli, WarGoal warGoal, string reason = "")
        {
            var rel = GetRelation(attackerId, defenderId);
            if (rel == null || rel.isAtWar) return false;

 // 停战检查            if (rel.truceUntilDay >= 0 && CurrentDay < rel.truceUntilDay) return false;

 // 验证战争借口有效性            if (casusBelli != null && !casusBelli.IsValid(CurrentDay))
                casusBelli = null; // 借口无效，按无借口处理

 // 计算宣战惩罚（有借口惩罚小，无借口惩罚大）            var penalties = WarJustificationSystem.CalculateDeclarationPenalties(casusBelli, false);

 // 标记战争借口已使用            if (casusBelli != null)
            {
                casusBelli.isUsed = true;
                reason = string.IsNullOrEmpty(reason) ?
                    WarJustificationSystem.GetCBDescription(casusBelli.type) : reason;
            }
            else
            {
                reason = string.IsNullOrEmpty(reason) ? "无借口宣战" : reason;
            }

 // 触发战争            rel.isAtWar = true;
            rel.warDeclaredDay = CurrentDay;
            rel.hostilityLevel = 100f;
            rel.relation = Mathf.Min(rel.relation, -50f);
            rel.trust = Mathf.Min(rel.trust, 10f);
            rel.threat = Mathf.Max(rel.threat, 90f);
            rel.activeAlliances.Clear();

 // 设置战争目标            rel.activeWarGoals.Clear();
            if (warGoal != null)
            {
                warGoal.attackerRealmId = attackerId;
                warGoal.defenderRealmId = defenderId;
                rel.activeWarGoals.Add(warGoal);
            }

 // 应用宣战惩罚            if (_realms.TryGetValue(attackerId, out var atkRealm))
            {
                atkRealm.prestige = Mathf.Max(0f, atkRealm.prestige - penalties.prestigePenalty);
                atkRealm.stability = Mathf.Max(0f, atkRealm.stability - penalties.stabilityPenalty);
                CivilizationEvolution.Military.MovementControlSystem.OnWarDeclared(atkRealm, defenderId);
            }
            if (_realms.TryGetValue(defenderId, out var defRealm))
                CivilizationEvolution.Military.MovementControlSystem.OnWarDeclared(defRealm, attackerId);

            rel.AddEvent(new DiplomaticEvent
            {
                type = DiplomaticEventType.WarDeclaration,
                description = $"{_realms[attackerId].realmName} 对 {_realms[defenderId].realmName} 宣战：{reason}" +
                              (warGoal != null ? $"（战争目标：{warGoal.type}）" : ""),
                relationChange = -50f, trustChange = -40f, threatChange = 40f
            });

            Chronicle?.Add("war",
                $"{_realms[attackerId].realmName} 对 {_realms[defenderId].realmName} 宣战：{reason}",
                major: true, attackerId, defenderId);

            if (WarRules == null || WarRules.allowAllianceIntervention)
                NotifyAlliesOfWar(attackerId, defenderId);

            UpdateConflictLevel(rel);
            return true;
        }


 /// <summary>获取某政权对另一政权的有效战争借口列表</summary>        public List<CasusBelli> GetValidCasusBelli(int holderRealmId, int targetRealmId)
        {
            var rel = GetRelation(holderRealmId, targetRealmId);
            if (rel == null) return new List<CasusBelli>();
            return WarJustificationSystem.GetValidCasusBelli(
                rel.casusBelliList, holderRealmId, targetRealmId, CurrentDay);
        }


 /// <summary>根据战争借口获取可选战争目标类型</summary>        public List<GameEnums.WarGoalType> GetSupportedWarGoals(int holderRealmId, int targetRealmId)
        {
            var cbs = GetValidCasusBelli(holderRealmId, targetRealmId);
            var result = new HashSet<GameEnums.WarGoalType>();
            foreach (var cb in cbs)
                foreach (var goal in WarJustificationSystem.GetSupportedWarGoals(cb.type))
                    result.Add(goal);
            return new List<GameEnums.WarGoalType>(result);
        }


 // ===== 敌对状态管理（不宣而战机制）=====
 /// <summary>增加敌对程度（边境摩擦、外交抗议、间谍事件等）</summary>        public void IncreaseHostility(int realmA, int realmB, float amount, string reason = "")
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null) return;
            float oldLevel = rel.hostilityLevel;
            rel.hostilityLevel = Mathf.Clamp(rel.hostilityLevel + amount, 0f, 100f);
            if (oldLevel < 50f && rel.hostilityLevel >= 50f)
            {
                rel.hostileSinceDay = CurrentDay;
                rel.AddEvent(new DiplomaticEvent { type = DiplomaticEventType.BorderIncident, description = $"{_realms[realmA].realmName} 与 {_realms[realmB].realmName} 进入敌对状态：{reason}", relationChange = -10f, threatChange = 15f });
                Chronicle?.Add("diplomacy", $"{_realms[realmA].realmName} 与 {_realms[realmB].realmName} 进入敌对状态", major: false, realmA, realmB);
            }
        }


 /// <summary>降低敌对程度（外交缓和、和亲、贸易协定等）</summary>        public void DecreaseHostility(int realmA, int realmB, float amount, string reason = "")
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null) return;
            float oldLevel = rel.hostilityLevel;
            rel.hostilityLevel = Mathf.Clamp(rel.hostilityLevel - amount, 0f, 100f);
            if (oldLevel >= 50f && rel.hostilityLevel < 50f && !rel.isAtWar)
            {
                rel.hostileSinceDay = -1;
                rel.AddEvent(new DiplomaticEvent { type = DiplomaticEventType.TreatySigned, description = $"{_realms[realmA].realmName} 与 {_realms[realmB].realmName} 解除敌对状态：{reason}", relationChange = 10f, threatChange = -10f });
            }
        }


 /// <summary>直接设置敌对状态（用于事件/剧情）</summary>        public void SetHostile(int realmA, int realmB, bool hostile, string reason = "")
        {
            if (hostile) IncreaseHostility(realmA, realmB, 100f, reason);
            else DecreaseHostility(realmA, realmB, 100f, reason);
        }


 /// <summary>不宣而战（军队直接攻击/入侵触发战争）。敌对状态下无惩罚，非敌对状态下有惩罚</summary>        public bool SurpriseAttack(int attackerId, int defenderId, int attackTileIndex = -1)
        {
            var rel = GetRelation(attackerId, defenderId);
            if (rel == null || rel.isAtWar) return false;
            if (rel.truceUntilDay >= 0 && CurrentDay < rel.truceUntilDay) { AddEventTo(rel, DiplomaticEventType.DemandRejected, $"{_realms[attackerId].realmName} 欲不宣而战，但停战期未满"); return false; }
            bool isHostile = rel.IsHostile;
            string attackType = isHostile ? "敌对状态下直接攻击" : "不宣而战";
            rel.lastSurpriseAttackerId = attackerId;
            rel.lastSurpriseAttackDay = CurrentDay;
            rel.isAtWar = true;
            rel.warDeclaredDay = CurrentDay;
            rel.relation = Mathf.Min(rel.relation, -60f);
            rel.trust = Mathf.Min(rel.trust, 5f);
            rel.threat = Mathf.Max(rel.threat, 95f);
            rel.hostilityLevel = 100f;
            rel.activeAlliances.Clear();

 // 战争爆发：自动撤销双方的军事通行权            if (_realms.TryGetValue(attackerId, out var atkRealm2))
                CivilizationEvolution.Military.MovementControlSystem.OnWarDeclared(atkRealm2, defenderId);
            if (_realms.TryGetValue(defenderId, out var defRealm2))
                CivilizationEvolution.Military.MovementControlSystem.OnWarDeclared(defRealm2, attackerId);
            if (!isHostile) ApplySurpriseAttackPenalties(attackerId, defenderId, rel);
            rel.AddEvent(new DiplomaticEvent { type = DiplomaticEventType.WarDeclaration, description = $"{_realms[attackerId].realmName} 对 {_realms[defenderId].realmName} {attackType}" + (attackTileIndex >= 0 ? $"（地块 #{attackTileIndex}）" : ""), relationChange = -60f, trustChange = -45f, threatChange = 45f });
            Chronicle?.Add("war", $"{_realms[attackerId].realmName} 对 {_realms[defenderId].realmName} {attackType}", major: true, attackerId, defenderId);
            if (WarRules == null || WarRules.allowAllianceIntervention) NotifyAlliesOfWar(attackerId, defenderId);
            return true;
        }


 /// <summary>不宣而战的惩罚（非敌对状态下率先发动战争）</summary>        private void ApplySurpriseAttackPenalties(int attackerId, int defenderId, DiplomaticRelation rel)
        {
            var attacker = _realms.ContainsKey(attackerId) ? _realms[attackerId] : null;
            if (attacker == null) return;
            float prestigeLoss = 30f + rel.trust * 0.3f;
            attacker.prestige = Mathf.Max(0f, attacker.prestige - prestigeLoss);
            float stabilityLoss = 15f + Mathf.Abs(rel.relation) * 0.1f;
            attacker.stability = Mathf.Max(0f, attacker.stability - stabilityLoss);
            foreach (var otherId in _realms.Keys)
            {
                if (otherId == attackerId || otherId == defenderId) continue;
                var otherRel = GetRelation(otherId, defenderId);
                if (otherRel == null) continue;
                if (otherRel.relation > 20f)
                {
                    float relationPenalty = (otherRel.relation - 20f) * 0.3f;
                    ModifyRelation(otherId, attackerId, -relationPenalty, "谴责不宣而战");
                    IncreaseHostility(otherId, attackerId, relationPenalty * 0.5f, $"谴责 {_realms[attackerId].realmName} 的不宣而战");
                }
            }
            var defender = _realms.ContainsKey(defenderId) ? _realms[defenderId] : null;
            if (defender != null) defender.stability = Mathf.Min(100f, defender.stability + 10f);
            Chronicle?.Add("diplomacy", $"{_realms[attackerId].realmName} 因不宣而战损失 {prestigeLoss:F0} 名声、{stabilityLoss:F0} 稳定度", major: false, attackerId, defenderId);
        }



 /// <summary>获取敌对程度描述（用于UI显示）</summary>        public string GetHostilityDescription(int realmA, int realmB)
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null) return "未知";
            if (rel.isAtWar) return "战争中";
            if (rel.hostilityLevel >= 80f) return "极度敌对";
            if (rel.hostilityLevel >= 50f) return "敌对状态";
            if (rel.hostilityLevel >= 30f) return "关系紧张";
            if (rel.hostilityLevel >= 10f) return "略有摩擦";
            return "关系正常";
        }


 /// <summary>边境摩擦（最小规模低烈度冲突，不触发战争）</summary>        public bool BorderSkirmish(int realmA, int realmB)
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null || rel.isAtWar) return false;
            IncreaseHostility(realmA, realmB, 3f, "边境摩擦");
            IncreaseHostility(realmB, realmA, 3f, "边境摩擦");
            ModifyRelation(realmA, realmB, -2f, "敌对行动");
            UpdateConflictLevel(rel);
            Chronicle?.Add("diplomacy", _realms[realmA].realmName + " 与 " + _realms[realmB].realmName + " 发生边境摩擦", major: false, realmA, realmB);
            return true;
        }


 /// <summary>更新冲突等级（根据敌对程度和战争状态）</summary>        private void UpdateConflictLevel(DiplomaticRelation rel)
        {
            if (rel.isAtWar)
                rel.conflictLevel = rel.hostilityLevel >= 90f ? GameEnums.ConflictLevel.TotalWar : GameEnums.ConflictLevel.LimitedWar;
            else if (rel.hostilityLevel >= 50f)
                rel.conflictLevel = GameEnums.ConflictLevel.Hostility;
            else if (rel.hostilityLevel >= 20f)
                rel.conflictLevel = GameEnums.ConflictLevel.Tension;
            else
                rel.conflictLevel = GameEnums.ConflictLevel.Peace;
        }


 /// <summary>军队入侵检查（敌对状态下不自动触发全面战争，而是增加敌对程度）</summary>        public bool CheckInvasionTriggerWar(int armyOwnerRealmId, int tileOwnerRealmId, int tileIndex)
        {
            if (armyOwnerRealmId == tileOwnerRealmId) return false;
            if (tileOwnerRealmId < 0) return false;
            var rel = GetRelation(armyOwnerRealmId, tileOwnerRealmId);
            if (rel == null || rel.isAtWar) return false;
            if (rel.IsHostile)
            {
                IncreaseHostility(tileOwnerRealmId, armyOwnerRealmId, 8f, "军队入侵领土");
                ModifyRelation(tileOwnerRealmId, armyOwnerRealmId, -5f, "敌对行动");
                UpdateConflictLevel(rel);
                if (rel.hostilityLevel >= 85f && UnityEngine.Random.value < 0.3f)
                { DeclareWar(tileOwnerRealmId, armyOwnerRealmId, "驱逐入侵军队"); return true; }
                return false;
            }
            else
            {
                return SurpriseAttack(armyOwnerRealmId, tileOwnerRealmId, tileIndex);
            }
        }


 /// <summary>获取冲突等级描述（用于UI显示）</summary>        public string GetConflictLevelDescription(int realmA, int realmB)
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null) return "未知";
            return rel.conflictLevel switch
            {
                GameEnums.ConflictLevel.Peace => "和平",
                GameEnums.ConflictLevel.Tension => "紧张",
                GameEnums.ConflictLevel.Hostility => "敌对（可劫掠）",
                GameEnums.ConflictLevel.LimitedWar => "有限战争",
                GameEnums.ConflictLevel.TotalWar => "全面战争",
                _ => "未知"
            };
        }


 /// <summary>求和/签订和平条约</summary>        public Treaty OfferPeace(int realmA, int realmB, float warReparations, int territoryCessionCount)
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null || !rel.isAtWar) return null;

            var treaty = new Treaty
            {
                treatyId = _nextTreatyId++,
                treatyName = "和平条约",
                signerAId = realmA,
                signerBId = realmB,
                signedDay = CurrentDay
            };

            if (warReparations > 0)
            {
                treaty.clauses.Add(new TreatyClause
                {
                    type = TreatyClauseType.WarReparations,
                    description = $"战争赔款 {warReparations}",
                    value = warReparations
                });
            }

            treaty.isActive = true;
            rel.activeTreaties.Add(treaty);
            rel.isAtWar = false;
            rel.relation = Mathf.Max(rel.relation, -30f);

 // 停战期（WarRules.truceYears——和平后强制休战）            rel.truceUntilDay = WarRules != null
                ? WarRules.GetTruceUntilDay(CurrentDay, WarRules.truceYears)
                : CurrentDay + 5 * 365;

            rel.AddEvent(new DiplomaticEvent
            {
                type = DiplomaticEventType.PeaceTreaty,
                description = $"签订和平条约，赔款 {warReparations}，停战至第 {rel.truceUntilDay} 日",
                relationChange = 30f
            });

 // 编年史（重大）            Chronicle?.Add("peace",
                $"{_realms[realmA].realmName} 与 {_realms[realmB].realmName} 签订和平条约" +
                (warReparations > 0 ? $"（赔款 {warReparations}）" : ""),
                major: true, realmA, realmB);

            return treaty;
        }


 /// 强制和平（战争闭环——战争胜利/白和后的自动停战） /// 结束战争状态 + 按 WarRules.truceYears 设置停战期        public void ForcePeace(int realmA, int realmB, int day, int truceYears, string reason = "战争结束")
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null) return;

            rel.isAtWar = false;
            rel.truceUntilDay = day + truceYears * 365;

            rel.AddEvent(new DiplomaticEvent
            {
                type = DiplomaticEventType.PeaceTreaty,
                description = $"{reason}：{_realms[realmA].realmName} 与 {_realms[realmB].realmName} 停战（至第 {rel.truceUntilDay} 日）",
                relationChange = 10f
            });

            Chronicle?.Add("peace", $"{_realms[realmA].realmName} 与 {_realms[realmB].realmName} {reason}",
                major: true, realmA, realmB);
        }


 // ===== 内部辅助 =====
        private void NotifyAlliesOfWar(int attackerId, int defenderId)
        {
 // 遍历全部外交关系：与防御方有 mutualDefense 盟约的第三方加入对攻击方宣战 // （修复：原实现要求 rel.isAtWar 才处理，导致防御同盟义务永不触发）            foreach (var rel in _relations.Values)
            {
                foreach (var alliance in rel.activeAlliances)
                {
                    if (!alliance.isActive || !alliance.mutualDefense) continue;

                    bool defendsTarget = alliance.realmAId == defenderId || alliance.realmBId == defenderId;
                    if (!defendsTarget) continue;

                    int allyId = alliance.realmAId == defenderId ? alliance.realmBId : alliance.realmAId;
                    if (allyId == attackerId) continue; // 防御方即攻击方自身（无意义）跳过

                    DeclareWar(allyId, attackerId, "防御同盟义务");
                }
            }
        }


        public bool AreAtWar(int realmA, int realmB)
        {
            var rel = GetRelation(realmA, realmB);
            return rel != null && rel.isAtWar;
        }

    }
}
