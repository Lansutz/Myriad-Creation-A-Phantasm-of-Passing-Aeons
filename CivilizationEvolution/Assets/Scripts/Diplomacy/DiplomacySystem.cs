using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Diplomacy
{
 /// 外交关系数据
 /// 核心三数值模型：关系值 / 信任度 / 威胁感知


 /// <summary>外交事件记录</summary>


 /// 盟约类型（平等盟约——谱系一：各类型独立平行，无递进关系）


 /// <summary>盟约</summary>


 /// 不平等从属关系类型——主权状态槽位（谱系二：内政自主度从高到低）
 /// 朝贡国(0.9) → 保护国(0.7) → 附属国(0.5) → 附庸国(0.35) → 傀儡国(0.1)
 /// 各类型独立平行，无递进关系


 /// 特殊纽带槽位（谱系三：横向人身/王朝联合）
 /// 独立于主权状态与条约义务；同一对政权可有且仅有一个活跃纽带


 /// <summary>从属关系</summary>


 /// <summary>条约</summary>


 /// <summary>条约条款</summary>


 /// 外交管理器
 /// 处理所有政权间的外交关系、盟约、条约、外交动作
    public partial class DiplomacyManager
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

 /// <summary>获取两国外交关系（不存在则创建——外交动作自动建立关系）</summary>
        public DiplomaticRelation GetOrCreateRelation(int realmA, int realmB)
        {
            var existing = GetRelation(realmA, realmB);
            if (existing != null) return existing;

            var rel = new DiplomaticRelation
            {
                realmAId = realmA,
                realmBId = realmB
            };
            _relations[GetRelationKey(realmA, realmB)] = rel;
            return rel;
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
            var rel = GetOrCreateRelation(realmA, realmB);

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


 // ===== 低烈度冲突（敌对状态下的劫掠/边境摩擦）=====

 /// <summary>劫掠聚落（敌对状态下的低烈度行动，不触发全面战争）</summary>
        public (bool success, bool warDeclared, float lootValue) RaidSettlement(
            int raiderId, int targetId, int tileIndex, GameEnums.RaidType raidType, TileData[] tiles = null)
        {
            var rel = GetRelation(raiderId, targetId);
            if (rel == null || rel.isAtWar) return (false, false, 0f);
            bool isHostile = rel.IsHostile;
            rel.lastRaidAttackerId = raiderId;
            rel.lastRaidDay = CurrentDay;
            rel.raidCount++;

 // 屠城：大规模屠杀——削减地块人口 30%（恐怖威慑）
            if (raidType == GameEnums.RaidType.Massacre && tiles != null
                && tileIndex >= 0 && tileIndex < tiles.Length)
            {
                var tile = tiles[tileIndex];
                if (tile.populationBlocks != null)
                {
                    for (int i = 0; i < tile.populationBlocks.Count; i++)
                    {
                        var pb = tile.populationBlocks[i];
                        pb.count *= 0.7f;
                        tile.populationBlocks[i] = pb;
                    }
                }
            }

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
                GameEnums.RaidType.Massacre => UnityEngine.Random.Range(400f, 1200f),
                _ => 50f
            };
            float hostilityIncrease = raidType switch
            {
                GameEnums.RaidType.BorderSkirmish => 5f,
                GameEnums.RaidType.VillageRaid => 15f,
                GameEnums.RaidType.TownAttack => 30f,
                GameEnums.RaidType.SupplyRaiding => 10f,
                GameEnums.RaidType.SlaveRaiding => 25f,
                GameEnums.RaidType.Massacre => 50f, // 屠城：极高敌意+恐怖威慑
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
        private static void AddEventTo(DiplomaticRelation rel, DiplomaticEventType type, string description)
        {
            rel.AddEvent(new DiplomaticEvent
            {
                type = type,
                description = description
            });
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
    }
}