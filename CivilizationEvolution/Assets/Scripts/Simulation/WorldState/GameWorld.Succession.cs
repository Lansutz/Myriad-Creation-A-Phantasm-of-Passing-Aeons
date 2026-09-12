using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.AI;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Culture;
using CivilizationEvolution.Simulation.Diplomacy;
using CivilizationEvolution.Simulation.Disaster;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Innovation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.Religion;
using CivilizationEvolution.Simulation.Settlement;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.Warfare;
using CivilizationEvolution.World.Biome;
using CivilizationEvolution.World.Climate;
using CivilizationEvolution.World.Generation;
using CivilizationEvolution.World.Hydrology;
using CivilizationEvolution.World.Settlement;
using CivilizationEvolution.World.Terrain;

















namespace CivilizationEvolution.Simulation.WorldState
{
 /// GameWorld.Succession —— 继位扶正与战争成就（统治者死亡→继承人→争议判定→政体变迁注入）（partial class，与 GameWorld.cs 共享字段与子系统）
    public partial class GameWorld
    {

 /// 战争结束 → 政体变迁关键节点注入：按战败方领土被占比例区分普通战败与外部征服。
 /// 事件烈度仍需盖过"基础阈值+制度黏性"才真正开窗——低张力战败会被现制度吸收。
        private void NotifyWarDefeat(WarState war, int day)
        {
            if (_regimeDynamics == null) return;
            int winner = war.winnerId;
            int loser = winner == war.attackerId ? war.defenderId : war.attackerId;
            if (!realms.ContainsKey(loser)) return;

            int ownTiles = 0, occupiedByWinner = 0;
            foreach (var t in tiles)
            {
                if (!t.exists || t.ownerRealmId != loser) continue;
                ownTiles++;
                if (t.occupyingRealmId == winner) occupiedByWinner++;
            }
            float occupiedRatio = ownTiles > 0 ? (float)occupiedByWinner / ownTiles : 0f;
            var type = occupiedRatio >= 0.30f
                ? CriticalJunctureType.ForeignConquest
                : CriticalJunctureType.WarDefeat;
            float loserStability = realms[loser].stability;
            float severity = Mathf.Clamp(55f + occupiedRatio * 70f + (100f - loserStability) * 0.15f, 0f, 100f);
            _regimeDynamics.NotifyEvent(day, loser, type, severity);
        }


 /// 统治者更替 → 政体变迁关键节点注入（供未来继位系统 / 宫廷事件调用）。
 /// disputed=继位争议（绝嗣/幼主/僭夺）→继承危机；平稳继位但新君能力卓绝且大胆→强势改革者窗口；
 /// 平庸且无争议的平稳继位不打开窗口（制度平稳延续）。
        public void NotifyRulerTransition(int realmId, int newRulerCharId, bool disputed, int day = -1)
        {
            if (_regimeDynamics == null || !realms.ContainsKey(realmId)) return;
            int d = day >= 0 ? day : currentDay;

            if (disputed)
            {
                float severity = Mathf.Clamp(50f + (100f - realms[realmId].stability) * 0.25f, 0f, 100f);
                _regimeDynamics.NotifyEvent(d, realmId, CriticalJunctureType.SuccessionCrisis, severity);
                return;
            }

            var ruler = _characterManager?.GetCharacter(newRulerCharId);
            if (ruler == null) return;
            float competence = (ruler.social + ruler.management + ruler.conspiracy
                              + ruler.prowess + ruler.military + ruler.scholarship) / 6f;
            if (competence >= 70f && ruler.boldness >= 20f)
                _regimeDynamics.NotifyEvent(d, realmId, CriticalJunctureType.StrongReformer, competence * 0.8f);
        }


 /// <summary>每日继位检查：统治者死亡 → 扶正/争议 → 编年史 + 政体变迁注入</summary>
        private void CheckRulerSuccessions()
        {
            foreach (var realm in realms.Values)
            {
                var result = SuccessionSystem.ExecuteSuccession(realm, _characterManager, currentDay);
                if (!result.triggered) continue;

 // 死亡统治者一生评估（绰号+谥号+评价——行为计数器数据源）
                if (result.deadRulerId >= 0)
                    EvaluateDeadRulerLife(result.deadRulerId, realm);

 // 新君即位记录（即位日/幼主标记——年轻者绰号数据）
                if (result.succeeded && result.newRulerId >= 0)
                {
                    var nr = _characterManager?.GetCharacter(result.newRulerId);
                    if (nr != null && nr.accessionDay < 0)
                    {
                        nr.accessionDay = currentDay;
                        if (nr.age < 16) nr.achievements.youngAccession = true;
 // 摄政架空标记（幼主+争议或稳定度低——简化：争议=权臣摄政）
                        if (result.disputed) nr.achievements.ruledUnderRegency = true;
                    }
                }

                if (result.disputed)
                {
                    _chronicle?.Add("succession_crisis",
                        $"{realm.realmName} 继承危机：{result.reason}", major: true, realm.realmId);
                    NotifyRulerTransition(realm.realmId, -1, true, currentDay);
                }
                else if (result.succeeded)
                {
                    var newRuler = _characterManager?.GetCharacter(result.newRulerId);
                    string rulerName = newRuler != null ? $"{newRuler.firstName} {newRuler.lastName}" : "?";
                    _chronicle?.Add("succession",
                        $"{realm.realmName} 新君即位：{rulerName}", major: true, realm.realmId);
                    NotifyRulerTransition(realm.realmId, result.newRulerId, false, currentDay);
                }
            }
        }


 /// <summary>战争胜利/失败计数器（统治者 achievements）</summary>
        private void AddWarAchievement(int realmId, bool won)
        {
            var ruler = GetRealmRuler(realmId);
            if (ruler == null) return;
            if (won) ruler.achievements.warsWon++;
            else ruler.achievements.defeatedBattles++;
        }


 /// <summary>防御大捷计数器（卫国——铁锤判定）</summary>
        private void AddDefensiveWin(int realmId)
        {
            var ruler = GetRealmRuler(realmId);
            if (ruler != null) ruler.achievements.defensiveWins++;
        }


        private CharacterData GetRealmRuler(int realmId)
        {
            if (realmId < 0 || realmId >= realms.Count) return null;
            var realm = realms[realmId];
            if (realm == null) return null;
            int rulerId = realm.GetSupremeRulerId();
            return rulerId >= 0 ? _characterManager?.GetCharacter(rulerId) : null;
        }


 /// 死亡统治者一生评估（行为计数器→评价/绰号/谥号）：
 /// reignYears 由即位日算——regionalInfluence 由领土规模近似——
 /// 授予绰号+谥号+编年史
        private void EvaluateDeadRulerLife(int charId, RealmData realm)
        {
            var c = _characterManager?.GetCharacter(charId);
            if (c == null || c.epithetEvaluated) return;
            c.epithetEvaluated = true;

 // 在位年数（即位日-死亡日）
            if (c.accessionDay >= 0 && c.deathDay >= 0)
            {
                int days = c.deathDay - c.accessionDay + (c.deathYear - c.birthYear) * 365;
                c.achievements.reignYears = Mathf.Max(0f, days / 365f);
            }
 // 区域影响力近似（死亡时政权领土占全图比例×2 clamp——区域前列≈0.6+）
            float landShare = realm != null ? GetRealmLandShare(realm.realmId) : 0f;
            c.achievements.regionalInfluence = Mathf.Clamp01(landShare * 3f);

            string epithet = EpithetSystem.EvaluateAndGrant(c, c.achievements);
            string posthumous = EpithetSystem.DeterminePosthumousTitle(c,
                c.achievements.warsWon, c.achievements.conquests, c.achievements.famineUnderRule);
            if (!string.IsNullOrEmpty(posthumous)) c.posthumousTitle = posthumous;

            string name = $"{c.firstName} {c.lastName}";
            string epi = string.IsNullOrEmpty(epithet) ? "" : $"「{epithet}」";
            string post = string.IsNullOrEmpty(posthumous) ? "" : $"（谥{posthumous}）";
            if (!string.IsNullOrEmpty(epithet) || !string.IsNullOrEmpty(posthumous))
                _chronicle?.Add("life_eval",
                    $"{name}{epi}{post} 逝世——一生盖棺定论", major: true, realm?.realmId ?? -1);
        }


 /// <summary>政权陆地占比（0-1——领土数/总陆地块——区域影响力近似源）</summary>
        private float GetRealmLandShare(int realmId)
        {
            int owned = 0, total = 0;
            foreach (var t in tiles)
            {
                if (!t.isLand) continue;
                total++;
                if (t.ownerRealmId == realmId) owned++;
            }
            return total > 0 ? (float)owned / total : 0f;
        }


        private int GetRealmDivisibleEstates(int realmId) => 1; // 简化：领地可分数=1（细化待领地系统）


        private Politics.InheritanceLaw GetEffectiveLaw(int realmId) => Politics.InheritanceLaw.Primogeniture(); // 简化：默认长子继承

    }
}
