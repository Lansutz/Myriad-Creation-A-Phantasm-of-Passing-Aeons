using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Diplomacy
{
    /// <summary>
    /// 外交关系数据
    /// 核心三数值模型：关系值 / 信任度 / 威胁感知
    /// </summary>
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

    /// <summary>外交事件记录</summary>
    [System.Serializable]
    public struct DiplomaticEvent
    {
        public int day;
        public int year;
        public DiplomaticEventType type;
        public string description;
        public float relationChange;
        public float trustChange;
        public float threatChange;
    }

    public enum DiplomaticEventType
    {
        WarDeclaration,
        PeaceTreaty,
        AllianceFormed,
        AllianceBroken,
        TreatySigned,
        TreatyBroken,
        TradeAgreement,
        TradeEmbargo,
        RoyalMarriage,
        DiplomaticInsult,
        BorderIncident,
        MilitaryAccessGranted,
        MilitaryAccessRevoked,
        Vassalage,
        Independence,
        GiftSent,
        DemandRejected,
        EmbassyEstablished,
        EmbassyClosed
    }

    /// <summary>
    /// 盟约类型（平等盟约——谱系一：各类型独立平行，无递进关系）
    /// </summary>
    public enum AllianceType
    {
        NonAggressionPact,    // 互不侵犯：承诺不开战，可单方撕毁（信誉惩罚）
        DefensiveAlliance,     // 防御同盟：仅被第三方攻击时共同作战
        OffensiveAlliance,     // 进攻同盟：主动宣战时共同作战，防御时不强制
        TotalAlliance,         // 全面同盟：任何情况共同作战，共享军事情报
        Faction                // 阵营：多边军事政治集团，常设协调机构+集体安全
    }

    /// <summary>盟约</summary>
    [System.Serializable]
    public class Alliance
    {
        public AllianceType type;
        public int realmAId;
        public int realmBId;
        public int signedDay;
        public int durationDays; // -1表示永久
        public bool isActive = true;

        // 盟约条款
        public float tradeEfficiencyBonus = 0f;
        public float tariffReduction = 0f;
        public bool mutualDefense = false;
        public bool jointOffensive = false;
        public bool militaryAccess = false;
        public float relationRequirement = 0f;

        /// <summary>检查盟约是否到期</summary>
        public bool IsExpired(int currentDay)
        {
            return durationDays > 0 && currentDay - signedDay > durationDays;
        }

        /// <summary>检查盟约条件是否满足</summary>
        public bool CheckConditions(DiplomaticRelation relation)
        {
            return relation.relation >= relationRequirement && !relation.isAtWar;
        }
    }

    /// <summary>
    /// 不平等从属关系类型——主权状态槽位（用户定稿谱系二：内政自主度从高到低）
    /// 朝贡国(0.9) → 保护国(0.7) → 附属国(0.5) → 附庸国(0.35) → 傀儡国(0.1)
    /// 各类型独立平行，无递进关系
    /// </summary>
    public enum SubordinationType
    {
        Tributary,          // 朝贡：内政完全自主，象征性臣服+进贡（自治度0.9）
        Vassal,             // 附庸：外交权受限，军事义务，内政基本自主（自治度0.65）
        Associate,          // 附属：内政受法定监督（顾问/否决法律），外交国防代理（自治度0.45）
        Protectorate,       // 保护国：内政自主，外交与宣战权完全转让（自治度0.3）
        Puppet              // 傀儡：首脑由宗主指定，一切重大决策需批准（自治度0.1）
    }

    /// <summary>
    /// 特殊纽带槽位（用户定稿谱系三：横向人身/王朝联合）
    /// 独立于主权状态与条约义务；同一对政权可有且仅有一个活跃纽带
    /// </summary>
    public enum SpecialBondType
    {
        None,               // 无特殊纽带
        PersonalUnion,      // 君合国（联统）：同一位君主，独立政府/议会/法律（英-汉诺威、奥匈）
        CompositeMonarchy   // 共主邦联：多个君主国共主，各自保留完整主权机构
    }

    /// <summary>从属关系</summary>
    [System.Serializable]
    public class Subordination
    {
        public SubordinationType type;
        public int suzerainId;  // 宗主国
        public int vassalId;     // 附庸国
        public int establishedDay;
        public bool isActive = true;

        // 从属条款
        public float tributeAmount = 0f;      // 贡赋金额/年
        public float tributeRatio = 0f;       // 贡赋比例（收入的百分比）
        public bool militaryObligation = false; // 军事义务
        public bool foreignPolicyControl = false; // 外交权控制
        public bool successionControl = false;    // 继承权控制
        public float autonomy = 1f;               // 自治度 0~1

        /// <summary>计算年度贡赋</summary>
        public float CalculateAnnualTribute(float vassalIncome)
        {
            return tributeAmount + vassalIncome * tributeRatio;
        }
    }

    /// <summary>条约</summary>
    [System.Serializable]
    public class Treaty
    {
        public int treatyId;
        public string treatyName;
        public int signerAId;
        public int signerBId;
        public int signedDay;
        public int expiryDay = -1;

        public List<TreatyClause> clauses = new List<TreatyClause>();
        public bool isActive = true;

        // ===== 和平条约扩展字段（战争三层分离体系）=====
        public float warScoreAtSigning;  // 签订时的战争分数
        public List<GameEnums.WarGoalType> originalWarGoals = new List<GameEnums.WarGoalType>(); // 原战争目标
        public bool goalsFullyAchieved;   // 战争目标是否完全达成
        public int truceUntilDay;          // 停战到期日
        public bool isPeaceTreaty;         // 是否为和平条约（区别于普通外交条约）

        /// <summary>检查条约是否到期</summary>
        public bool IsExpired(int currentDay)
        {
            return expiryDay > 0 && currentDay > expiryDay;
        }
    }

    /// <summary>条约条款</summary>
    [System.Serializable]
    public struct TreatyClause
    {
        public TreatyClauseType type;
        public string description;
        public float value;
        public int targetRealmId;
        public int fromRealmId;         // 条款执行方（付出代价的一方）
        public int toRealmId;           // 条款受益方
        public int tileIndex;            // 相关地块（-1=无）
        public int regionId;             // 相关地区（-1=无）
        public int durationDays;         // 条款持续时间（-1=永久）
    }

    public enum TreatyClauseType
    {
        TerritoryCession,     // 领土割让
        WarReparations,       // 战争赔款
        PrisonerExchange,     // 战俘交换
        TradeRights,          // 贸易权
        NavigationRights,     // 航行权
        DemilitarizedZone,    // 非军事区
        ArmsLimitation,       // 军备限制
        ReligiousFreedom,     // 宗教自由
        MinorityProtection,   // 少数民族保护
        AllianceCommitment,   // 同盟承诺
        NonInterference,      // 不干涉内政
        ArbitrationAgreement, // 仲裁协定
        Vassalage,            // 附庸关系
        PersonalUnion,        // 共主邦联
        ReleasePrisoners,     // 释放囚犯
        TradePrivileges,      // 贸易特权
        Disarmament,          // 裁军条款
        Humiliation,          // 羞辱条款
        Annexation,           // 吞并（整个政权）
        Independence,         // 承认独立
        BorderDemilitarization, // 边境非军事化
        RoyalMarriage,        // 强制联姻
        CulturalAssimilation, // 文化同化
        WarCrimesTrial,       // 战争罪审判
        ResourceConcession,   // 资源特许权
        Truce                  // 停战协定
    }

    /// <summary>
    /// 外交管理器
    /// 处理所有政权间的外交关系、盟约、条约、外交动作
    /// </summary>
    public class DiplomacyManager
    {
        private readonly Dictionary<int, RealmData> _realms;
        private readonly Dictionary<string, DiplomaticRelation> _relations = new Dictionary<string, DiplomaticRelation>();
        private readonly List<Subordination> _subordinations = new List<Subordination>();
        private int _nextTreatyId = 1;

        /// <summary>当前游戏日（由 GameWorld 每 Tick 同步，用于盟约/条约/事件的时间戳）</summary>
        public int CurrentDay { get; set; } = 0;

        /// <summary>战争规则（由 GameWorld 注入——truce/白和/联盟介入/分数参数）</summary>
        public WarRules WarRules { get; set; } = WarRules.Default();

        /// <summary>编年史（由 GameWorld 注入，null 跳过记录）</summary>
        public Chronicle Chronicle { get; set; }

        public DiplomacyManager(Dictionary<int, RealmData> realms)
        {
            _realms = realms;
            InitializeAllRelations();
        }

        private string GetRelationKey(int a, int b)
        {
            return a < b ? $"{a}_{b}" : $"{b}_{a}";
        }

        /// <summary>初始化所有政权间的外交关系</summary>
        private void InitializeAllRelations()
        {
            var realmIds = new List<int>(_realms.Keys);
            for (int i = 0; i < realmIds.Count; i++)
            {
                for (int j = i + 1; j < realmIds.Count; j++)
                {
                    var key = GetRelationKey(realmIds[i], realmIds[j]);
                    if (!_relations.ContainsKey(key))
                    {
                        _relations[key] = new DiplomaticRelation
                        {
                            realmAId = realmIds[i],
                            realmBId = realmIds[j],
                            relation = UnityEngine.Random.Range(-20f, 20f),
                            trust = UnityEngine.Random.Range(30f, 70f),
                            threat = UnityEngine.Random.Range(30f, 70f)
                        };
                    }
                }
            }
        }

        /// <summary>获取两国外交关系</summary>
        public DiplomaticRelation GetRelation(int realmA, int realmB)
        {
            var key = GetRelationKey(realmA, realmB);
            if (_relations.TryGetValue(key, out var rel))
                return rel;
            return null;
        }

        /// <summary>修改关系值</summary>
        public void ModifyRelation(int realmA, int realmB, float delta, string reason)
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null) return;

            rel.relation = Mathf.Clamp(rel.relation + delta, -100f, 100f);
            rel.AddEvent(new DiplomaticEvent
            {
                type = DiplomaticEventType.EmbassyEstablished,
                description = reason,
                relationChange = delta
            });
        }

        /// <summary>修改信任度</summary>
        public void ModifyTrust(int realmA, int realmB, float delta)
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null) return;
            rel.trust = Mathf.Clamp(rel.trust + delta, 0f, 100f);
        }

        /// <summary>修改威胁感知</summary>
        public void ModifyThreat(int realmA, int realmB, float delta)
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null) return;
            rel.threat = Mathf.Clamp(rel.threat + delta, 0f, 100f);
        }

        // ===== 战争与和平 =====

        /// <summary>宣战</summary>
        public bool DeclareWar(int attackerId, int defenderId, string reason)
        {
            var rel = GetRelation(attackerId, defenderId);
            if (rel == null || rel.isAtWar) return false;

            // 停战检查（WarRules.truceYears——借鉴《地图上发生的事》truce_until_tick）
            if (rel.truceUntilDay >= 0 && CurrentDay < rel.truceUntilDay)
            {
                AddEventTo(rel, DiplomaticEventType.DemandRejected,
                    $"{_realms[attackerId].realmName} 欲开战，但停战期未满（至第 {rel.truceUntilDay} 日）");
                return false;
            }

            rel.isAtWar = true;
            rel.warDeclaredDay = CurrentDay;
            rel.hostilityLevel = 100f;
            rel.relation = Mathf.Min(rel.relation, -50f);

            // 战争爆发：自动撤销双方的军事通行权
            if (_realms.TryGetValue(attackerId, out var atkRealm))
                CivilizationEvolution.Map.MovementControlSystem.OnWarDeclared(atkRealm, defenderId);
            if (_realms.TryGetValue(defenderId, out var defRealm))
                CivilizationEvolution.Map.MovementControlSystem.OnWarDeclared(defRealm, attackerId);
            rel.trust = Mathf.Min(rel.trust, 10f);
            rel.threat = Mathf.Max(rel.threat, 90f);

            // 解除所有盟约
            rel.activeAlliances.Clear();

            rel.AddEvent(new DiplomaticEvent
            {
                type = DiplomaticEventType.WarDeclaration,
                description = $"{_realms[attackerId].realmName} 对 {_realms[defenderId].realmName} 宣战：{reason}",
                relationChange = -50f,
                trustChange = -40f,
                threatChange = 40f
            });

            // 编年史（重大）
            Chronicle?.Add("war", $"{_realms[attackerId].realmName} 对 {_realms[defenderId].realmName} 宣战：{reason}",
                major: true, attackerId, defenderId);

            // 通知同盟国（WarRules.allowAllianceIntervention 控制）
            if (WarRules == null || WarRules.allowAllianceIntervention)
                NotifyAlliesOfWar(attackerId, defenderId);
            return true;
        }

        /// <summary>带战争借口和战争目标的宣战（三层分离：借口→目标→条约）</summary>
        public bool DeclareWarWithJustification(int attackerId, int defenderId,
            CasusBelli casusBelli, WarGoal warGoal, string reason = "")
        {
            var rel = GetRelation(attackerId, defenderId);
            if (rel == null || rel.isAtWar) return false;

            // 停战检查
            if (rel.truceUntilDay >= 0 && CurrentDay < rel.truceUntilDay) return false;

            // 验证战争借口有效性
            if (casusBelli != null && !casusBelli.IsValid(CurrentDay))
                casusBelli = null; // 借口无效，按无借口处理

            // 计算宣战惩罚（有借口惩罚小，无借口惩罚大）
            var penalties = WarJustificationSystem.CalculateDeclarationPenalties(casusBelli, false);

            // 标记战争借口已使用
            if (casusBelli != null)
            {
                casusBelli.isUsed = true;
                reason = string.IsNullOrEmpty(reason) ?
                    WarJustificationSystem.GetCBDescription(casusBelli.type) : reason;
            }
            else
            {
                reason = string.IsNullOrEmpty(reason) ? "无借口宣战" : reason;
            }

            // 触发战争
            rel.isAtWar = true;
            rel.warDeclaredDay = CurrentDay;
            rel.hostilityLevel = 100f;
            rel.relation = Mathf.Min(rel.relation, -50f);
            rel.trust = Mathf.Min(rel.trust, 10f);
            rel.threat = Mathf.Max(rel.threat, 90f);
            rel.activeAlliances.Clear();

            // 设置战争目标
            rel.activeWarGoals.Clear();
            if (warGoal != null)
            {
                warGoal.attackerRealmId = attackerId;
                warGoal.defenderRealmId = defenderId;
                rel.activeWarGoals.Add(warGoal);
            }

            // 应用宣战惩罚
            if (_realms.TryGetValue(attackerId, out var atkRealm))
            {
                atkRealm.prestige = Mathf.Max(0f, atkRealm.prestige - penalties.prestigePenalty);
                atkRealm.stability = Mathf.Max(0f, atkRealm.stability - penalties.stabilityPenalty);
                CivilizationEvolution.Map.MovementControlSystem.OnWarDeclared(atkRealm, defenderId);
            }
            if (_realms.TryGetValue(defenderId, out var defRealm))
                CivilizationEvolution.Map.MovementControlSystem.OnWarDeclared(defRealm, attackerId);

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

        /// <summary>获取某政权对另一政权的有效战争借口列表</summary>
        public List<CasusBelli> GetValidCasusBelli(int holderRealmId, int targetRealmId)
        {
            var rel = GetRelation(holderRealmId, targetRealmId);
            if (rel == null) return new List<CasusBelli>();
            return WarJustificationSystem.GetValidCasusBelli(
                rel.casusBelliList, holderRealmId, targetRealmId, CurrentDay);
        }

        /// <summary>根据战争借口获取可选战争目标类型</summary>
        public List<GameEnums.WarGoalType> GetSupportedWarGoals(int holderRealmId, int targetRealmId)
        {
            var cbs = GetValidCasusBelli(holderRealmId, targetRealmId);
            var result = new HashSet<GameEnums.WarGoalType>();
            foreach (var cb in cbs)
                foreach (var goal in WarJustificationSystem.GetSupportedWarGoals(cb.type))
                    result.Add(goal);
            return new List<GameEnums.WarGoalType>(result);
        }

        // ===== 敌对状态管理（不宣而战机制）=====

        /// <summary>增加敌对程度（边境摩擦、外交抗议、间谍事件等）</summary>
        public void IncreaseHostility(int realmA, int realmB, float amount, string reason = "")
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

        /// <summary>降低敌对程度（外交缓和、和亲、贸易协定等）</summary>
        public void DecreaseHostility(int realmA, int realmB, float amount, string reason = "")
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

        /// <summary>直接设置敌对状态（用于事件/剧情）</summary>
        public void SetHostile(int realmA, int realmB, bool hostile, string reason = "")
        {
            if (hostile) IncreaseHostility(realmA, realmB, 100f, reason);
            else DecreaseHostility(realmA, realmB, 100f, reason);
        }

        /// <summary>不宣而战（军队直接攻击/入侵触发战争）。敌对状态下无惩罚，非敌对状态下有惩罚</summary>
        public bool SurpriseAttack(int attackerId, int defenderId, int attackTileIndex = -1)
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

            // 战争爆发：自动撤销双方的军事通行权
            if (_realms.TryGetValue(attackerId, out var atkRealm2))
                CivilizationEvolution.Map.MovementControlSystem.OnWarDeclared(atkRealm2, defenderId);
            if (_realms.TryGetValue(defenderId, out var defRealm2))
                CivilizationEvolution.Map.MovementControlSystem.OnWarDeclared(defRealm2, attackerId);
            if (!isHostile) ApplySurpriseAttackPenalties(attackerId, defenderId, rel);
            rel.AddEvent(new DiplomaticEvent { type = DiplomaticEventType.WarDeclaration, description = $"{_realms[attackerId].realmName} 对 {_realms[defenderId].realmName} {attackType}" + (attackTileIndex >= 0 ? $"（地块 #{attackTileIndex}）" : ""), relationChange = -60f, trustChange = -45f, threatChange = 45f });
            Chronicle?.Add("war", $"{_realms[attackerId].realmName} 对 {_realms[defenderId].realmName} {attackType}", major: true, attackerId, defenderId);
            if (WarRules == null || WarRules.allowAllianceIntervention) NotifyAlliesOfWar(attackerId, defenderId);
            return true;
        }

        /// <summary>不宣而战的惩罚（非敌对状态下率先发动战争）</summary>
        private void ApplySurpriseAttackPenalties(int attackerId, int defenderId, DiplomaticRelation rel)
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


        /// <summary>获取敌对程度描述（用于UI显示）</summary>
        public string GetHostilityDescription(int realmA, int realmB)
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


        // ===== 低烈度冲突（敌对状态下的劫掠/边境摩擦）=====

        /// <summary>劫掠聚落（敌对状态下的低烈度行动，不触发全面战争）</summary>
        public (bool success, bool warDeclared, float lootValue) RaidSettlement(
            int raiderId, int targetId, int tileIndex, GameEnums.RaidType raidType)
        {
            var rel = GetRelation(raiderId, targetId);
            if (rel == null || rel.isAtWar) return (false, false, 0f);
            bool isHostile = rel.IsHostile;
            rel.lastRaidAttackerId = raiderId;
            rel.lastRaidDay = CurrentDay;
            rel.raidCount++;

            // 被劫掠方获得战争借口（劫掠报复）
            var raidCB = WarJustificationSystem.GenerateRaidReprisalCB(
                targetId, raiderId, tileIndex, CurrentDay, raidType);
            rel.casusBelliList.Add(raidCB);
            float lootValue = raidType switch
            {
                GameEnums.RaidType.BorderSkirmish => UnityEngine.Random.Range(10f, 50f),
                GameEnums.RaidType.VillageRaid => UnityEngine.Random.Range(50f, 200f),
                GameEnums.RaidType.TownAttack => UnityEngine.Random.Range(150f, 500f),
                GameEnums.RaidType.SupplyRaiding => UnityEngine.Random.Range(30f, 150f),
                GameEnums.RaidType.SlaveRaiding => UnityEngine.Random.Range(80f, 300f),
                _ => 50f
            };
            float hostilityIncrease = raidType switch
            {
                GameEnums.RaidType.BorderSkirmish => 5f,
                GameEnums.RaidType.VillageRaid => 15f,
                GameEnums.RaidType.TownAttack => 30f,
                GameEnums.RaidType.SupplyRaiding => 10f,
                GameEnums.RaidType.SlaveRaiding => 25f,
                _ => 10f
            };
            IncreaseHostility(targetId, raiderId, hostilityIncrease, raidType + "劫掠");
            ModifyRelation(targetId, raiderId, -hostilityIncrease * 0.8f, "劫掠报复");
            bool warDeclared = false;
            if (!isHostile)
            {
                if (UnityEngine.Random.value < 0.6f || raidType == GameEnums.RaidType.TownAttack)
                { DeclareWar(targetId, raiderId, "报复" + raidType + "劫掠"); warDeclared = true; }
            }
            else if (rel.raidCount >= 3 || rel.hostilityLevel >= 90f)
            {
                if (UnityEngine.Random.value < 0.4f)
                { DeclareWar(targetId, raiderId, "报复持续劫掠"); warDeclared = true; rel.raidCount = 0; }
            }
            UpdateConflictLevel(rel);
            string raidName = raidType.ToString();
            rel.AddEvent(new DiplomaticEvent { type = DiplomaticEventType.BorderIncident, description = _realms[raiderId].realmName + " 对 " + _realms[targetId].realmName + " 发动" + raidName + "（地块 #" + tileIndex + "）", relationChange = -hostilityIncrease * 0.8f, threatChange = hostilityIncrease });
            Chronicle?.Add("war", _realms[raiderId].realmName + " 对 " + _realms[targetId].realmName + " 发动" + raidName, major: raidType == GameEnums.RaidType.TownAttack, raiderId, targetId);
            return (true, warDeclared, lootValue);
        }

        /// <summary>边境摩擦（最小规模低烈度冲突，不触发战争）</summary>
        public bool BorderSkirmish(int realmA, int realmB)
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

        /// <summary>更新冲突等级（根据敌对程度和战争状态）</summary>
        private void UpdateConflictLevel(DiplomaticRelation rel)
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

        /// <summary>军队入侵检查（敌对状态下不自动触发全面战争，而是增加敌对程度）</summary>
        public bool CheckInvasionTriggerWar(int armyOwnerRealmId, int tileOwnerRealmId, int tileIndex)
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

        /// <summary>获取冲突等级描述（用于UI显示）</summary>
        public string GetConflictLevelDescription(int realmA, int realmB)
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
        private static void AddEventTo(DiplomaticRelation rel, DiplomaticEventType type, string description)
        {
            rel.AddEvent(new DiplomaticEvent
            {
                type = type,
                description = description
            });
        }

        /// <summary>求和/签订和平条约</summary>
        public Treaty OfferPeace(int realmA, int realmB, float warReparations, int territoryCessionCount)
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

            // 停战期（WarRules.truceYears——和平后强制休战）
            rel.truceUntilDay = WarRules != null
                ? WarRules.GetTruceUntilDay(CurrentDay, WarRules.truceYears)
                : CurrentDay + 5 * 365;

            rel.AddEvent(new DiplomaticEvent
            {
                type = DiplomaticEventType.PeaceTreaty,
                description = $"签订和平条约，赔款 {warReparations}，停战至第 {rel.truceUntilDay} 日",
                relationChange = 30f
            });

            // 编年史（重大）
            Chronicle?.Add("peace",
                $"{_realms[realmA].realmName} 与 {_realms[realmB].realmName} 签订和平条约" +
                (warReparations > 0 ? $"（赔款 {warReparations}）" : ""),
                major: true, realmA, realmB);

            return treaty;
        }

        /// <summary>
        /// 强制和平（战争闭环——战争胜利/白和后的自动停战）
        /// 结束战争状态 + 按 WarRules.truceYears 设置停战期
        /// </summary>
        public void ForcePeace(int realmA, int realmB, int day, int truceYears, string reason = "战争结束")
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

        // ===== 盟约系统 =====

        /// <summary>提议盟约</summary>
        public Alliance ProposeAlliance(int realmA, int realmB, AllianceType type)
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null || rel.isAtWar) return null;

            // 检查关系要求
            float requiredRelation = type switch
            {
                AllianceType.NonAggressionPact => -20f,
                AllianceType.DefensiveAlliance => 30f,
                AllianceType.OffensiveAlliance => 50f,
                AllianceType.TotalAlliance => 70f,
                AllianceType.Faction => 80f,
                _ => 0f
            };

            if (rel.relation < requiredRelation) return null;

            // 去重：同类型活跃盟约已存在则不重复缔结（修复：原实现可被 AI 每30天重复叠加）
            if (rel.activeAlliances.Exists(a => a.type == type && a.isActive)) return null;

            var alliance = new Alliance
            {
                type = type,
                realmAId = realmA,
                realmBId = realmB,
                signedDay = CurrentDay,
                durationDays = -1,
                relationRequirement = requiredRelation
            };

            // 设置盟约效果
            switch (type)
            {
                case AllianceType.TradeAgreement:
                    alliance.tradeEfficiencyBonus = 0.2f;
                    alliance.tariffReduction = 0.3f;
                    break;
                case AllianceType.CustomsUnion:
                    alliance.tradeEfficiencyBonus = 0.5f;
                    alliance.tariffReduction = 1f;
                    break;
                case AllianceType.DefensiveAlliance:
                    alliance.mutualDefense = true;
                    break;
                case AllianceType.OffensiveAlliance:
                    alliance.mutualDefense = true;
                    alliance.jointOffensive = true;
                    break;
                case AllianceType.MilitaryAccess:
                    alliance.militaryAccess = true;
                    break;
            }

            rel.activeAlliances.Add(alliance);
            ModifyRelation(realmA, realmB, 10f, $"签订{GetAllianceName(type)}");
            ModifyTrust(realmA, realmB, 15f);

            return alliance;
        }

        /// <summary>解除盟约</summary>
        public bool BreakAlliance(int realmA, int realmB, AllianceType type)
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

        // ===== 从属关系 =====

        /// <summary>建立从属关系</summary>
        public Subordination EstablishSubordination(int suzerainId, int vassalId, SubordinationType type)
        {
            var rel = GetRelation(suzerainId, vassalId);
            if (rel == null) return null;

            var sub = new Subordination
            {
                type = type,
                suzerainId = suzerainId,
                vassalId = vassalId,
                establishedDay = CurrentDay
            };

            // 设置从属条款（5种不平等从属：自治度 朝贡0.9→附庸0.65→附属0.45→保护国0.3→傀儡0.1）
            switch (type)
            {
                case SubordinationType.Tributary:
                    // 朝贡：内政完全自主，象征性臣服+进贡
                    sub.tributeRatio = 0.1f;
                    sub.autonomy = 0.9f;
                    break;
                case SubordinationType.Vassal:
                    // 附庸：外交权受限，军事义务，内政基本自主
                    sub.tributeRatio = 0.15f;
                    sub.militaryObligation = true;
                    sub.foreignPolicyControl = true;
                    sub.autonomy = 0.65f;
                    break;
                case SubordinationType.Associate:
                    // 附属：内政受法定监督（顾问/否决法律），外交国防全权代理
                    sub.foreignPolicyControl = true;
                    sub.militaryObligation = true;
                    sub.autonomy = 0.45f;
                    break;
                case SubordinationType.Protectorate:
                    // 保护国：内政自主，外交与宣战权完全转让
                    sub.foreignPolicyControl = true;
                    sub.autonomy = 0.3f;
                    break;
                case SubordinationType.Puppet:
                    // 傀儡：首脑由宗主指定，一切重大决策需批准
                    sub.foreignPolicyControl = true;
                    sub.militaryObligation = true;
                    sub.successionControl = true;
                    sub.autonomy = 0.1f;
                    break;
            }

            _subordinations.Add(sub);
            rel.subordination = sub; // 同步挂载到关系槽位1

            if (_realms.TryGetValue(vassalId, out var vassal))
                vassal.suzerainId = suzerainId;
            if (_realms.TryGetValue(suzerainId, out var suzerain))
                suzerain.vassalIds.Add(vassalId);

            return sub;
        }

        /// <summary>
        /// 建立特殊纽带（谱系三：君合国/共主邦联——横向人身/王朝联合）
        /// 独立于从属与盟约：双方各自保留主权，仅共享君主
        /// </summary>
        public bool EstablishPersonalUnion(int realmA, int realmB, SpecialBondType bond)
        {
            if (realmA == realmB || bond == SpecialBondType.None) return false;
            var rel = GetRelation(realmA, realmB);
            if (rel == null) return false;
            if (rel.subordination != null && rel.subordination.isActive)
            {
                Debug.LogWarning("[Diplomacy] 存在从属关系时不可建立君合国（主权状态与特殊纽带互斥）");
                return false;
            }
            rel.SetSpecialBond(bond);
            Debug.Log($"[Diplomacy] 政权 {realmA} 与 {realmB} 建立特殊纽带：{bond}");
            return true;
        }

        /// <summary>附庸独立</summary>
        public bool GrantIndependence(int suzerainId, int vassalId)
        {
            var sub = _subordinations.Find(s => s.suzerainId == suzerainId && s.vassalId == vassalId && s.isActive);
            if (sub == null) return false;

            sub.isActive = false;
            _subordinations.Remove(sub);

            // 清理关系槽位1
            var rel = GetRelation(suzerainId, vassalId);
            if (rel != null && rel.subordination == sub)
                rel.subordination = null;

            if (_realms.TryGetValue(vassalId, out var vassal))
                vassal.suzerainId = -1;
            if (_realms.TryGetValue(suzerainId, out var suzerain))
                suzerain.vassalIds.Remove(vassalId);

            ModifyRelation(suzerainId, vassalId, -20f, "附庸独立");
            return true;
        }

        // ===== 外交动作 =====

        /// <summary>派遣使节/建立外交关系</summary>
        public bool EstablishEmbassy(int realmA, int realmB)
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null) return false;
            rel.hasDiplomaticRelations = true;
            ModifyRelation(realmA, realmB, 5f, "建立外交关系");
            return true;
        }

        /// <summary>断绝外交关系</summary>
        public bool SeverRelations(int realmA, int realmB)
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null) return false;
            rel.hasDiplomaticRelations = false;
            rel.activeAlliances.Clear();
            ModifyRelation(realmA, realmB, -30f, "断绝外交关系");
            ModifyTrust(realmA, realmB, -25f);
            return true;
        }

        /// <summary>赠送礼物</summary>
        public bool SendGift(int fromId, int toId, float amount)
        {
            var rel = GetRelation(fromId, toId);
            if (rel == null || !_realms.ContainsKey(fromId)) return false;

            if (_realms[fromId].treasury < amount) return false;
            _realms[fromId].treasury -= amount;

            float relationGain = Mathf.Min(20f, amount / 100f);
            ModifyRelation(fromId, toId, relationGain, $"赠送礼物 {amount}");
            ModifyTrust(fromId, toId, relationGain * 0.5f);
            return true;
        }

        /// <summary>外交侮辱</summary>
        public bool DiplomaticInsult(int fromId, int toId, string insult)
        {
            var rel = GetRelation(fromId, toId);
            if (rel == null) return false;
            ModifyRelation(fromId, toId, -15f, $"外交侮辱：{insult}");
            ModifyTrust(fromId, toId, -10f);
            ModifyThreat(toId, fromId, 10f);
            return true;
        }

        /// <summary>贸易禁运</summary>
        public bool ImposeEmbargo(int fromId, int toId)
        {
            var rel = GetRelation(fromId, toId);
            if (rel == null) return false;
            rel.hasTradeEmbargo = true;
            ModifyRelation(fromId, toId, -20f, "贸易禁运");
            ModifyThreat(toId, fromId, 15f);
            return true;
        }

        /// <summary>解除禁运</summary>
        public bool LiftEmbargo(int fromId, int toId)
        {
            var rel = GetRelation(fromId, toId);
            if (rel == null) return false;
            rel.hasTradeEmbargo = false;
            ModifyRelation(fromId, toId, 10f, "解除贸易禁运");
            return true;
        }

        // ===== 内部辅助 =====

        private void NotifyAlliesOfWar(int attackerId, int defenderId)
        {
            // 遍历全部外交关系：与防御方有 mutualDefense 盟约的第三方加入对攻击方宣战
            // （修复：原实现要求 rel.isAtWar 才处理，导致防御同盟义务永不触发）
            foreach (var rel in _relations.Values)
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

        private string GetAllianceName(AllianceType type)
        {
            return type switch
            {
                AllianceType.NonAggressionPact => "互不侵犯条约",
                AllianceType.DefensiveAlliance => "防御同盟",
                AllianceType.OffensiveAlliance => "进攻同盟",
                AllianceType.TradeAgreement => "贸易协定",
                AllianceType.CustomsUnion => "关税同盟",
                AllianceType.MilitaryAccess => "军事通行权",
                AllianceType.RoyalMarriage => "王室联姻",
                AllianceType.CulturalExchange => "文化交流协定",
                _ => type.ToString()
            };
        }

        /// <summary>每日外交Tick</summary>
        public void DailyTick()
        {
            foreach (var rel in _relations.Values)
            {
                rel.DailyDecay();

                // 检查盟约条件
                for (int i = rel.activeAlliances.Count - 1; i >= 0; i--)
                {
                    if (!rel.activeAlliances[i].CheckConditions(rel))
                    {
                        rel.activeAlliances[i].isActive = false;
                        rel.activeAlliances.RemoveAt(i);
                    }
                }
            }

            // 贡赋结算（简化：每年结算）
            foreach (var sub in _subordinations)
            {
                if (!sub.isActive) continue;
                // 每日累积贡赋
                if (_realms.TryGetValue(sub.vassalId, out var vassal) &&
                    _realms.TryGetValue(sub.suzerainId, out var suzerain))
                {
                    float dailyTribute = vassal.treasury * sub.tributeRatio / 365f;
                    vassal.treasury -= dailyTribute;
                    suzerain.treasury += dailyTribute;
                }
            }
        }

        // ===== 查询接口 =====
        public IReadOnlyDictionary<string, DiplomaticRelation> GetAllRelations() => _relations;
        public IReadOnlyList<Subordination> GetAllSubordinations() => _subordinations;

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

        public bool AreAtWar(int realmA, int realmB)
        {
            var rel = GetRelation(realmA, realmB);
            return rel != null && rel.isAtWar;
        }

        public bool HasMilitaryAccess(int fromRealm, int throughRealm)
        {
            var rel = GetRelation(fromRealm, throughRealm);
            if (rel == null) return false;
            return rel.activeAlliances.Exists(a => a.type == AllianceType.MilitaryAccess && a.militaryAccess);
        }
    }
}