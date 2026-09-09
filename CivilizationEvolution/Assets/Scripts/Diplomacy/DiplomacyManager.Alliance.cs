using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;
using CivilizationEvolution.War;
using CivilizationEvolution.Character;

namespace CivilizationEvolution.Diplomacy
{
 /// DiplomacyManager.Alliance —— 同盟（提议/解除/查询）（partial class，与 DiplomacySystem.cs 共享字段）    public partial class DiplomacyManager
    {

 // ===== 盟约系统 =====
 /// <summary>提议盟约</summary>        public Alliance ProposeAlliance(int realmA, int realmB, AllianceType type)
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null || rel.isAtWar) return null;

 // 检查关系要求            float requiredRelation = type switch
            {
                AllianceType.NonAggressionPact => -20f,
                AllianceType.DefensiveAlliance => 30f,
                AllianceType.OffensiveAlliance => 50f,
                AllianceType.TotalAlliance => 70f,
                AllianceType.Faction => 80f,
                _ => 0f
            };

            if (rel.relation < requiredRelation) return null;

 // 去重：同类型活跃盟约已存在则不重复缔结（修复：原实现可被 AI 每30天重复叠加）            if (rel.activeAlliances.Exists(a => a.type == type && a.isActive)) return null;

            var alliance = new Alliance
            {
                type = type,
                realmAId = realmA,
                realmBId = realmB,
                signedDay = CurrentDay,
                durationDays = -1,
                relationRequirement = requiredRelation
            };

 // 设置盟约效果（5种平等盟约）            switch (type)
            {
                case AllianceType.NonAggressionPact:
 // 互不侵犯：无军事效果，仅承诺不开战                    break;
                case AllianceType.DefensiveAlliance:
                    alliance.mutualDefense = true;
                    break;
                case AllianceType.OffensiveAlliance:
                    alliance.jointOffensive = true;
                    break;
                case AllianceType.TotalAlliance:
                    alliance.mutualDefense = true;
                    alliance.jointOffensive = true;
                    alliance.militaryAccess = true;
                    break;
                case AllianceType.Faction:
                    alliance.mutualDefense = true;
                    alliance.jointOffensive = true;
                    alliance.militaryAccess = true;
 // 阵营：额外的集体安全效果（由阵营系统处理）                    break;
            }

            rel.activeAlliances.Add(alliance);
            ModifyRelation(realmA, realmB, 10f, $"签订{GetAllianceName(type)}");
            ModifyTrust(realmA, realmB, 15f);

            return alliance;
        }


 /// <summary>解除盟约</summary>        public bool BreakAlliance(int realmA, int realmB, AllianceType type)
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null) return false;

            var alliance = rel.activeAlliances.Find(a => a.type == type);
            if (alliance == null) return false;

            alliance.isActive = false;
            rel.activeAlliances.Remove(alliance);
            ModifyRelation(realmA, realmB, -15f, $"撕毁{GetAllianceName(type)}");
            ModifyTrust(realmA, realmB, -20f);
            return true;
        }


        private string GetAllianceName(AllianceType type)
        {
            return type switch
            {
                AllianceType.NonAggressionPact => "互不侵犯条约",
                AllianceType.DefensiveAlliance => "防御同盟",
                AllianceType.OffensiveAlliance => "进攻同盟",
                AllianceType.TotalAlliance => "全面同盟",
                AllianceType.Faction => "阵营",
                _ => type.ToString()
            };
        }


        public List<Alliance> GetAlliancesOfRealm(int realmId)
        {
            var result = new List<Alliance>();
            foreach (var rel in _relations.Values)
            {
                if (rel.realmAId == realmId || rel.realmBId == realmId)
                    result.AddRange(rel.activeAlliances);
            }
            return result;
        }

    }
}
