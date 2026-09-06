using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Diplomacy
{
    [System.Serializable]
    public class DiplomaticRelation
    {
        public int realmAId;
        public int realmBId;

        // 核心三数值
        [Range(-100f, 100f)] public float relation = 0f;      // 关系值：-100死敌 ~ 100亲密盟友
        [Range(0f, 100f)] public float trust = 50f;            // 信任度：0完全不信任 ~ 100完全信任
        [Range(0f, 100f)] public float threat = 50f;           // 威胁感知：0无威胁 ~ 100致命威胁

        // 状态
        public bool isAtWar = false;
        public bool hasTradeEmbargo = false;
        public bool hasDiplomaticRelations = true;
        public int warDeclaredDay = -1;
        /// <summary>停战到期日（WarRules.truceYears 决定；-1=无停战）</summary>
        public int truceUntilDay = -1;

        // ===== 敌对状态（不宣而战机制）=====
        /// <summary>敌对程度（0-100），≥50进入敌对状态，可直接攻击无惩罚</summary>
        [Range(0f, 100f)] public float hostilityLevel = 0f;

        /// <summary>敌对状态开始日（-1=无敌对状态）</summary>
        public int hostileSinceDay = -1;

        /// <summary>是否处于敌对状态（敌对程度≥50或处于战争）</summary>
        public bool IsHostile => hostilityLevel >= 50f || isAtWar;

        /// <summary>最近一次不宣而战的发起者（-1=无），用于惩罚计算</summary>
        public int lastSurpriseAttackerId = -1;

        /// <summary>最近一次不宣而战的日期（-1=无）</summary>
        public int lastSurpriseAttackDay = -1;

        /// <summary>冲突等级（区分敌对状态和战争状态）</summary>
        public GameEnums.ConflictLevel conflictLevel = GameEnums.ConflictLevel.Peace;

        /// <summary>最近一次劫掠的发起者（-1=无）</summary>
        public int lastRaidAttackerId = -1;

        /// <summary>最近一次劫掠日期（-1=无）</summary>
        public int lastRaidDay = -1;

        /// <summary>劫掠累计次数（用于判断是否升级为战争）</summary>
        public int raidCount = 0;

        /// <summary>双方的战争借口列表（holderRealmId区分持有方）</summary>
        public List<CasusBelli> casusBelliList = new List<CasusBelli>();

        /// <summary>当前战争的战争目标列表（仅在战争中有效）</summary>
        public List<WarGoal> activeWarGoals = new List<WarGoal>();

        // 历史事件记录
        public List<DiplomaticEvent> eventHistory = new List<DiplomaticEvent>();

        // 活跃盟约
        public List<Alliance> activeAlliances = new List<Alliance>();
        public List<Treaty> activeTreaties = new List<Treaty>();

        // ===== 外交三槽位（用户定稿：主权状态/条约义务/特殊纽带 彻底解耦） =====

        /// <summary>槽位1·主权状态（null=独立国；由 DiplomacyManager 同步挂载）</summary>
        public Subordination subordination;

        /// <summary>槽位3·特殊纽带（无/君合国/共主邦联；独立于从属与盟约）</summary>
        public SpecialBondType specialBond = SpecialBondType.None;

        /// <summary>设置特殊纽带（同一对政权同时仅一个活跃纽带）</summary>
        public void SetSpecialBond(SpecialBondType bond)
        {
            specialBond = bond;
        }

        /// <summary>解除特殊纽带</summary>
        public void ClearSpecialBond()
        {
            specialBond = SpecialBondType.None;
        }

        /// <summary>槽位1查询：以 selfId 视角返回主权状态（独立=null）</summary>
        public SubordinationType? GetSovereigntyStatus(int selfId)
        {
            if (subordination == null || !subordination.isActive) return null;
            return subordination.suzerainId == selfId
                ? null // 宗主视角：自身是宗主，非从属
                : subordination.type;
        }

        /// <summary>槽位2查询：条约义务（平级盟约列表）</summary>
        public List<Alliance> GetTreatyObligations() => activeAlliances;

        /// <summary>槽位2查询：是否承担某类盟约义务</summary>
        public bool HasTreatyObligation(AllianceType type)
        {
            foreach (var a in activeAlliances)
                if (a.type == type && a.isActive) return true;
            return false;
        }

        /// <summary>计算综合外交态度</summary>
        public float CalculateOverallAttitude()
        {
            // 关系值权重0.5，信任度权重0.3，威胁感知负权重0.2
            return relation * 0.5f + (trust - 50f) * 0.6f - (threat - 50f) * 0.4f;
        }

        /// <summary>判断是否愿意谈判</summary>
        public bool IsWillingToNegotiate()
        {
            return hasDiplomaticRelations && CalculateOverallAttitude() > -60f;
        }

        /// <summary>添加外交事件</summary>
        public void AddEvent(DiplomaticEvent evt)
        {
            eventHistory.Add(evt);
            if (eventHistory.Count > 100)
                eventHistory.RemoveAt(0);
        }

        /// <summary>每日关系自然衰减</summary>
        public void DailyDecay()
        {
            // 关系值向0回归
            relation = Mathf.Lerp(relation, 0f, 0.001f);
            // 信任度向50回归
            trust = Mathf.Lerp(trust, 50f, 0.0005f);
            // 威胁感知向50回归
            threat = Mathf.Lerp(threat, 50f, 0.001f);
        }
    }
}
