using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Diplomacy;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public class DiplomacyManager
{
	private readonly Dictionary<int, RealmData> _realms;

	private readonly Dictionary<string, DiplomaticRelation> _relations = new Dictionary<string, DiplomaticRelation>();

	private readonly List<Subordination> _subordinations = new List<Subordination>();

	private int _nextTreatyId = 1;

	public int CurrentDay { get; set; }

	public WarRules WarRules { get; set; } = WarRules.Default();

	public Chronicle Chronicle { get; set; }

	public bool EstablishEmbassy(int realmA, int realmB)
	{
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation == null)
		{
			return false;
		}
		relation.hasDiplomaticRelations = true;
		ModifyRelation(realmA, realmB, 5f, "建立外交关系");
		return true;
	}

	public bool SeverRelations(int realmA, int realmB)
	{
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation == null)
		{
			return false;
		}
		relation.hasDiplomaticRelations = false;
		relation.activeAlliances.Clear();
		ModifyRelation(realmA, realmB, -30f, "断绝外交关系");
		ModifyTrust(realmA, realmB, -25f);
		return true;
	}

	public bool SendGift(int fromId, int toId, float amount)
	{
		if (GetRelation(fromId, toId) == null || !_realms.ContainsKey(fromId))
		{
			return false;
		}
		if (_realms[fromId].treasury < amount)
		{
			return false;
		}
		_realms[fromId].treasury -= amount;
		float num = Mathf.Min(20f, amount / 100f);
		ModifyRelation(fromId, toId, num, $"赠送礼物 {amount}");
		ModifyTrust(fromId, toId, num * 0.5f);
		return true;
	}

	public bool DiplomaticInsult(int fromId, int toId, string insult)
	{
		if (GetRelation(fromId, toId) == null)
		{
			return false;
		}
		ModifyRelation(fromId, toId, -15f, "外交侮辱：" + insult);
		ModifyTrust(fromId, toId, -10f);
		ModifyThreat(toId, fromId, 10f);
		return true;
	}

	public bool ImposeEmbargo(int fromId, int toId)
	{
		DiplomaticRelation relation = GetRelation(fromId, toId);
		if (relation == null)
		{
			return false;
		}
		relation.hasTradeEmbargo = true;
		ModifyRelation(fromId, toId, -20f, "贸易禁运");
		ModifyThreat(toId, fromId, 15f);
		return true;
	}

	public bool LiftEmbargo(int fromId, int toId)
	{
		DiplomaticRelation relation = GetRelation(fromId, toId);
		if (relation == null)
		{
			return false;
		}
		relation.hasTradeEmbargo = false;
		ModifyRelation(fromId, toId, 10f, "解除贸易禁运");
		return true;
	}

	public bool HasMilitaryAccess(int fromRealm, int throughRealm)
	{
		return GetRelation(fromRealm, throughRealm)?.activeAlliances.Exists((Alliance a) => a.isActive && a.militaryAccess) ?? false;
	}

	public Alliance ProposeAlliance(int realmA, int realmB, AllianceType type)
	{
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation == null || relation.isAtWar)
		{
			return null;
		}
		float num = type switch
		{
			AllianceType.NonAggressionPact => -20f, 
			AllianceType.DefensiveAlliance => 30f, 
			AllianceType.OffensiveAlliance => 50f, 
			AllianceType.TotalAlliance => 70f, 
			AllianceType.Faction => 80f, 
			_ => 0f, 
		};
		if (relation.relation < num)
		{
			return null;
		}
		if (relation.activeAlliances.Exists((Alliance a) => a.type == type && a.isActive))
		{
			return null;
		}
		Alliance alliance = new Alliance
		{
			type = type,
			realmAId = realmA,
			realmBId = realmB,
			signedDay = CurrentDay,
			durationDays = -1,
			relationRequirement = num
		};
		switch (type)
		{
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
			break;
		}
		relation.activeAlliances.Add(alliance);
		ModifyRelation(realmA, realmB, 10f, "签订" + GetAllianceName(type));
		ModifyTrust(realmA, realmB, 15f);
		return alliance;
	}

	public bool BreakAlliance(int realmA, int realmB, AllianceType type)
	{
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation == null)
		{
			return false;
		}
		Alliance alliance = relation.activeAlliances.Find((Alliance a) => a.type == type);
		if (alliance == null)
		{
			return false;
		}
		alliance.isActive = false;
		relation.activeAlliances.Remove(alliance);
		ModifyRelation(realmA, realmB, -15f, "撕毁" + GetAllianceName(type));
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
			_ => type.ToString(), 
		};
	}

	public List<Alliance> GetAlliancesOfRealm(int realmId)
	{
		List<Alliance> list = new List<Alliance>();
		foreach (DiplomaticRelation value in _relations.Values)
		{
			if (value.realmAId == realmId || value.realmBId == realmId)
			{
				list.AddRange(value.activeAlliances);
			}
		}
		return list;
	}

	public Subordination EstablishSubordination(int suzerainId, int vassalId, SubordinationType type)
	{
		DiplomaticRelation relation = GetRelation(suzerainId, vassalId);
		if (relation == null)
		{
			return null;
		}
		Subordination subordination = new Subordination
		{
			type = type,
			suzerainId = suzerainId,
			vassalId = vassalId,
			establishedDay = CurrentDay
		};
		switch (type)
		{
		case SubordinationType.Tributary:
			subordination.tributeRatio = 0.1f;
			subordination.autonomy = 0.9f;
			break;
		case SubordinationType.Vassal:
			subordination.tributeRatio = 0.15f;
			subordination.militaryObligation = true;
			subordination.foreignPolicyControl = true;
			subordination.autonomy = 0.65f;
			break;
		case SubordinationType.Associate:
			subordination.foreignPolicyControl = true;
			subordination.militaryObligation = true;
			subordination.autonomy = 0.45f;
			break;
		case SubordinationType.Protectorate:
			subordination.foreignPolicyControl = true;
			subordination.autonomy = 0.3f;
			break;
		case SubordinationType.Puppet:
			subordination.foreignPolicyControl = true;
			subordination.militaryObligation = true;
			subordination.successionControl = true;
			subordination.autonomy = 0.1f;
			break;
		}
		_subordinations.Add(subordination);
		relation.subordination = subordination;
		if (_realms.TryGetValue(vassalId, out var value))
		{
			value.suzerainId = suzerainId;
		}
		if (_realms.TryGetValue(suzerainId, out var value2))
		{
			value2.vassalIds.Add(vassalId);
		}
		return subordination;
	}

	public Subordination GetSubordination(int realmA, int realmB)
	{
		return _subordinations.Find((Subordination s) => s.isActive && ((s.suzerainId == realmA && s.vassalId == realmB) || (s.suzerainId == realmB && s.vassalId == realmA)));
	}

	public bool ReleaseSubordination(int suzerainId, int vassalId)
	{
		Subordination subordination = _subordinations.Find((Subordination s) => s.suzerainId == suzerainId && s.vassalId == vassalId && s.isActive);
		if (subordination == null)
		{
			return false;
		}
		subordination.isActive = false;
		DiplomaticRelation relation = GetRelation(suzerainId, vassalId);
		if (relation != null)
		{
			relation.subordination = null;
		}
		ModifyRelation(suzerainId, vassalId, -15f, "解除从属关系");
		Debug.Log((object)$"[Diplomacy] 政权 {suzerainId} 解除对 {vassalId} 的从属关系");
		return true;
	}

	public bool EstablishPersonalUnion(int realmA, int realmB, SpecialBondType bond)
	{
		if (realmA == realmB || bond == SpecialBondType.None)
		{
			return false;
		}
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation == null)
		{
			return false;
		}
		if (relation.subordination != null && relation.subordination.isActive)
		{
			Debug.LogWarning((object)"[Diplomacy] 存在从属关系时不可建立君合国（主权状态与特殊纽带互斥）");
			return false;
		}
		relation.SetSpecialBond(bond);
		Debug.Log((object)$"[Diplomacy] 政权 {realmA} 与 {realmB} 建立特殊纽带：{bond}");
		return true;
	}

	public bool GrantIndependence(int suzerainId, int vassalId)
	{
		Subordination subordination = _subordinations.Find((Subordination s) => s.suzerainId == suzerainId && s.vassalId == vassalId && s.isActive);
		if (subordination == null)
		{
			return false;
		}
		subordination.isActive = false;
		_subordinations.Remove(subordination);
		DiplomaticRelation relation = GetRelation(suzerainId, vassalId);
		if (relation != null && relation.subordination == subordination)
		{
			relation.subordination = null;
		}
		if (_realms.TryGetValue(vassalId, out var value))
		{
			value.suzerainId = -1;
		}
		if (_realms.TryGetValue(suzerainId, out var value2))
		{
			value2.vassalIds.Remove(vassalId);
		}
		ModifyRelation(suzerainId, vassalId, -20f, "附庸独立");
		return true;
	}

	public IReadOnlyList<Subordination> GetAllSubordinations()
	{
		return _subordinations;
	}

	public bool DeclareWar(int attackerId, int defenderId, string reason)
	{
		DiplomaticRelation relation = GetRelation(attackerId, defenderId);
		if (relation == null || relation.isAtWar)
		{
			return false;
		}
		if (relation.truceUntilDay >= 0 && CurrentDay < relation.truceUntilDay)
		{
			AddEventTo(relation, DiplomaticEventType.DemandRejected, $"{_realms[attackerId].realmName} 欲开战，但停战期未满（至第 {relation.truceUntilDay} 日）");
			return false;
		}
		relation.isAtWar = true;
		relation.warDeclaredDay = CurrentDay;
		relation.hostilityLevel = 100f;
		relation.relation = Mathf.Min(relation.relation, -50f);
		if (_realms.TryGetValue(attackerId, out var value))
		{
			MovementControlSystem.OnWarDeclared(value, defenderId);
		}
		if (_realms.TryGetValue(defenderId, out var value2))
		{
			MovementControlSystem.OnWarDeclared(value2, attackerId);
		}
		relation.trust = Mathf.Min(relation.trust, 10f);
		relation.threat = Mathf.Max(relation.threat, 90f);
		relation.activeAlliances.Clear();
		relation.AddEvent(new DiplomaticEvent
		{
			type = DiplomaticEventType.WarDeclaration,
			description = _realms[attackerId].realmName + " 对 " + _realms[defenderId].realmName + " 宣战：" + reason,
			relationChange = -50f,
			trustChange = -40f,
			threatChange = 40f
		});
		Chronicle?.Add("war", _realms[attackerId].realmName + " 对 " + _realms[defenderId].realmName + " 宣战：" + reason, true, attackerId, defenderId);
		if (WarRules == null || WarRules.allowAllianceIntervention)
		{
			NotifyAlliesOfWar(attackerId, defenderId);
		}
		return true;
	}

	public bool DeclareWarWithJustification(int attackerId, int defenderId, CasusBelli casusBelli, WarGoal warGoal, string reason = "")
	{
		DiplomaticRelation relation = GetRelation(attackerId, defenderId);
		if (relation == null || relation.isAtWar)
		{
			return false;
		}
		if (relation.truceUntilDay >= 0 && CurrentDay < relation.truceUntilDay)
		{
			return false;
		}
		if (casusBelli != null && !casusBelli.IsValid(CurrentDay))
		{
			casusBelli = null;
		}
		(float, float, float) tuple = WarJustificationSystem.CalculateDeclarationPenalties(casusBelli, isSurpriseAttack: false);
		if (casusBelli != null)
		{
			casusBelli.isUsed = true;
			reason = (string.IsNullOrEmpty(reason) ? WarJustificationSystem.GetCBDescription(casusBelli.type) : reason);
		}
		else
		{
			reason = (string.IsNullOrEmpty(reason) ? "无借口宣战" : reason);
		}
		relation.isAtWar = true;
		relation.warDeclaredDay = CurrentDay;
		relation.hostilityLevel = 100f;
		relation.relation = Mathf.Min(relation.relation, -50f);
		relation.trust = Mathf.Min(relation.trust, 10f);
		relation.threat = Mathf.Max(relation.threat, 90f);
		relation.activeAlliances.Clear();
		relation.activeWarGoals.Clear();
		if (warGoal != null)
		{
			warGoal.attackerRealmId = attackerId;
			warGoal.defenderRealmId = defenderId;
			relation.activeWarGoals.Add(warGoal);
		}
		if (_realms.TryGetValue(attackerId, out var value))
		{
			value.prestige = Mathf.Max(0f, value.prestige - tuple.Item1);
			value.stability = Mathf.Max(0f, value.stability - tuple.Item2);
			MovementControlSystem.OnWarDeclared(value, defenderId);
		}
		if (_realms.TryGetValue(defenderId, out var value2))
		{
			MovementControlSystem.OnWarDeclared(value2, attackerId);
		}
		relation.AddEvent(new DiplomaticEvent
		{
			type = DiplomaticEventType.WarDeclaration,
			description = _realms[attackerId].realmName + " 对 " + _realms[defenderId].realmName + " 宣战：" + reason + ((warGoal != null) ? $"（战争目标：{warGoal.type}）" : ""),
			relationChange = -50f,
			trustChange = -40f,
			threatChange = 40f
		});
		Chronicle?.Add("war", _realms[attackerId].realmName + " 对 " + _realms[defenderId].realmName + " 宣战：" + reason, true, attackerId, defenderId);
		if (WarRules == null || WarRules.allowAllianceIntervention)
		{
			NotifyAlliesOfWar(attackerId, defenderId);
		}
		UpdateConflictLevel(relation);
		return true;
	}

	public List<CasusBelli> GetValidCasusBelli(int holderRealmId, int targetRealmId)
	{
		DiplomaticRelation relation = GetRelation(holderRealmId, targetRealmId);
		if (relation == null)
		{
			return new List<CasusBelli>();
		}
		return WarJustificationSystem.GetValidCasusBelli(relation.casusBelliList, holderRealmId, targetRealmId, CurrentDay);
	}

	public List<GameEnums.WarGoalType> GetSupportedWarGoals(int holderRealmId, int targetRealmId)
	{
		List<CasusBelli> validCasusBelli = GetValidCasusBelli(holderRealmId, targetRealmId);
		HashSet<GameEnums.WarGoalType> hashSet = new HashSet<GameEnums.WarGoalType>();
		foreach (CasusBelli item in validCasusBelli)
		{
			foreach (GameEnums.WarGoalType supportedWarGoal in WarJustificationSystem.GetSupportedWarGoals(item.type))
			{
				hashSet.Add(supportedWarGoal);
			}
		}
		return new List<GameEnums.WarGoalType>(hashSet);
	}

	public void IncreaseHostility(int realmA, int realmB, float amount, string reason = "")
	{
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation != null)
		{
			float hostilityLevel = relation.hostilityLevel;
			relation.hostilityLevel = Mathf.Clamp(relation.hostilityLevel + amount, 0f, 100f);
			if (hostilityLevel < 50f && relation.hostilityLevel >= 50f)
			{
				relation.hostileSinceDay = CurrentDay;
				relation.AddEvent(new DiplomaticEvent
				{
					type = DiplomaticEventType.BorderIncident,
					description = _realms[realmA].realmName + " 与 " + _realms[realmB].realmName + " 进入敌对状态：" + reason,
					relationChange = -10f,
					threatChange = 15f
				});
				Chronicle?.Add("diplomacy", _realms[realmA].realmName + " 与 " + _realms[realmB].realmName + " 进入敌对状态", false, realmA, realmB);
			}
		}
	}

	public void DecreaseHostility(int realmA, int realmB, float amount, string reason = "")
	{
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation != null)
		{
			float hostilityLevel = relation.hostilityLevel;
			relation.hostilityLevel = Mathf.Clamp(relation.hostilityLevel - amount, 0f, 100f);
			if (hostilityLevel >= 50f && relation.hostilityLevel < 50f && !relation.isAtWar)
			{
				relation.hostileSinceDay = -1;
				relation.AddEvent(new DiplomaticEvent
				{
					type = DiplomaticEventType.TreatySigned,
					description = _realms[realmA].realmName + " 与 " + _realms[realmB].realmName + " 解除敌对状态：" + reason,
					relationChange = 10f,
					threatChange = -10f
				});
			}
		}
	}

	public void SetHostile(int realmA, int realmB, bool hostile, string reason = "")
	{
		if (hostile)
		{
			IncreaseHostility(realmA, realmB, 100f, reason);
		}
		else
		{
			DecreaseHostility(realmA, realmB, 100f, reason);
		}
	}

	public bool SurpriseAttack(int attackerId, int defenderId, int attackTileIndex = -1)
	{
		DiplomaticRelation relation = GetRelation(attackerId, defenderId);
		if (relation == null || relation.isAtWar)
		{
			return false;
		}
		if (relation.truceUntilDay >= 0 && CurrentDay < relation.truceUntilDay)
		{
			AddEventTo(relation, DiplomaticEventType.DemandRejected, _realms[attackerId].realmName + " 欲不宣而战，但停战期未满");
			return false;
		}
		bool isHostile = relation.IsHostile;
		string text = (isHostile ? "敌对状态下直接攻击" : "不宣而战");
		relation.lastSurpriseAttackerId = attackerId;
		relation.lastSurpriseAttackDay = CurrentDay;
		relation.isAtWar = true;
		relation.warDeclaredDay = CurrentDay;
		relation.relation = Mathf.Min(relation.relation, -60f);
		relation.trust = Mathf.Min(relation.trust, 5f);
		relation.threat = Mathf.Max(relation.threat, 95f);
		relation.hostilityLevel = 100f;
		relation.activeAlliances.Clear();
		if (_realms.TryGetValue(attackerId, out var value))
		{
			MovementControlSystem.OnWarDeclared(value, defenderId);
		}
		if (_realms.TryGetValue(defenderId, out var value2))
		{
			MovementControlSystem.OnWarDeclared(value2, attackerId);
		}
		if (!isHostile)
		{
			ApplySurpriseAttackPenalties(attackerId, defenderId, relation);
		}
		relation.AddEvent(new DiplomaticEvent
		{
			type = DiplomaticEventType.WarDeclaration,
			description = _realms[attackerId].realmName + " 对 " + _realms[defenderId].realmName + " " + text + ((attackTileIndex >= 0) ? $"（地块 #{attackTileIndex}）" : ""),
			relationChange = -60f,
			trustChange = -45f,
			threatChange = 45f
		});
		Chronicle?.Add("war", _realms[attackerId].realmName + " 对 " + _realms[defenderId].realmName + " " + text, true, attackerId, defenderId);
		if (WarRules == null || WarRules.allowAllianceIntervention)
		{
			NotifyAlliesOfWar(attackerId, defenderId);
		}
		return true;
	}

	private void ApplySurpriseAttackPenalties(int attackerId, int defenderId, DiplomaticRelation rel)
	{
		RealmData realmData = (_realms.ContainsKey(attackerId) ? _realms[attackerId] : null);
		if (realmData == null)
		{
			return;
		}
		float num = 30f + rel.trust * 0.3f;
		realmData.prestige = Mathf.Max(0f, realmData.prestige - num);
		float num2 = 15f + Mathf.Abs(rel.relation) * 0.1f;
		realmData.stability = Mathf.Max(0f, realmData.stability - num2);
		foreach (int key in _realms.Keys)
		{
			if (key != attackerId && key != defenderId)
			{
				DiplomaticRelation relation = GetRelation(key, defenderId);
				if (relation != null && relation.relation > 20f)
				{
					float num3 = (relation.relation - 20f) * 0.3f;
					ModifyRelation(key, attackerId, 0f - num3, "谴责不宣而战");
					IncreaseHostility(key, attackerId, num3 * 0.5f, "谴责 " + _realms[attackerId].realmName + " 的不宣而战");
				}
			}
		}
		RealmData realmData2 = (_realms.ContainsKey(defenderId) ? _realms[defenderId] : null);
		if (realmData2 != null)
		{
			realmData2.stability = Mathf.Min(100f, realmData2.stability + 10f);
		}
		Chronicle?.Add("diplomacy", $"{_realms[attackerId].realmName} 因不宣而战损失 {num:F0} 名声、{num2:F0} 稳定度", false, attackerId, defenderId);
	}

	public string GetHostilityDescription(int realmA, int realmB)
	{
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation == null)
		{
			return "未知";
		}
		if (relation.isAtWar)
		{
			return "战争中";
		}
		if (relation.hostilityLevel >= 80f)
		{
			return "极度敌对";
		}
		if (relation.hostilityLevel >= 50f)
		{
			return "敌对状态";
		}
		if (relation.hostilityLevel >= 30f)
		{
			return "关系紧张";
		}
		if (relation.hostilityLevel >= 10f)
		{
			return "略有摩擦";
		}
		return "关系正常";
	}

	public bool BorderSkirmish(int realmA, int realmB)
	{
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation == null || relation.isAtWar)
		{
			return false;
		}
		IncreaseHostility(realmA, realmB, 3f, "边境摩擦");
		IncreaseHostility(realmB, realmA, 3f, "边境摩擦");
		ModifyRelation(realmA, realmB, -2f, "敌对行动");
		UpdateConflictLevel(relation);
		Chronicle?.Add("diplomacy", _realms[realmA].realmName + " 与 " + _realms[realmB].realmName + " 发生边境摩擦", false, realmA, realmB);
		return true;
	}

	private void UpdateConflictLevel(DiplomaticRelation rel)
	{
		if (rel.isAtWar)
		{
			rel.conflictLevel = ((rel.hostilityLevel >= 90f) ? GameEnums.ConflictLevel.TotalWar : GameEnums.ConflictLevel.LimitedWar);
		}
		else if (rel.hostilityLevel >= 50f)
		{
			rel.conflictLevel = GameEnums.ConflictLevel.Hostility;
		}
		else if (rel.hostilityLevel >= 20f)
		{
			rel.conflictLevel = GameEnums.ConflictLevel.Tension;
		}
		else
		{
			rel.conflictLevel = GameEnums.ConflictLevel.Peace;
		}
	}

	public bool CheckInvasionTriggerWar(int armyOwnerRealmId, int tileOwnerRealmId, int tileIndex)
	{
		if (armyOwnerRealmId == tileOwnerRealmId)
		{
			return false;
		}
		if (tileOwnerRealmId < 0)
		{
			return false;
		}
		DiplomaticRelation relation = GetRelation(armyOwnerRealmId, tileOwnerRealmId);
		if (relation == null || relation.isAtWar)
		{
			return false;
		}
		if (relation.IsHostile)
		{
			IncreaseHostility(tileOwnerRealmId, armyOwnerRealmId, 8f, "军队入侵领土");
			ModifyRelation(tileOwnerRealmId, armyOwnerRealmId, -5f, "敌对行动");
			UpdateConflictLevel(relation);
			if (relation.hostilityLevel >= 85f && Random.value < 0.3f)
			{
				DeclareWar(tileOwnerRealmId, armyOwnerRealmId, "驱逐入侵军队");
				return true;
			}
			return false;
		}
		return SurpriseAttack(armyOwnerRealmId, tileOwnerRealmId, tileIndex);
	}

	public string GetConflictLevelDescription(int realmA, int realmB)
	{
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation == null)
		{
			return "未知";
		}
		return relation.conflictLevel switch
		{
			GameEnums.ConflictLevel.Peace => "和平", 
			GameEnums.ConflictLevel.Tension => "紧张", 
			GameEnums.ConflictLevel.Hostility => "敌对（可劫掠）", 
			GameEnums.ConflictLevel.LimitedWar => "有限战争", 
			GameEnums.ConflictLevel.TotalWar => "全面战争", 
			_ => "未知", 
		};
	}

	public Treaty OfferPeace(int realmA, int realmB, float warReparations, int territoryCessionCount)
	{
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation == null || !relation.isAtWar)
		{
			return null;
		}
		Treaty treaty = new Treaty
		{
			treatyId = _nextTreatyId++,
			treatyName = "和平条约",
			signerAId = realmA,
			signerBId = realmB,
			signedDay = CurrentDay
		};
		if (warReparations > 0f)
		{
			treaty.clauses.Add(new TreatyClause
			{
				type = TreatyClauseType.WarReparations,
				description = $"战争赔款 {warReparations}",
				value = warReparations
			});
		}
		treaty.isActive = true;
		relation.activeTreaties.Add(treaty);
		relation.isAtWar = false;
		relation.relation = Mathf.Max(relation.relation, -30f);
		relation.truceUntilDay = ((WarRules != null) ? WarRules.GetTruceUntilDay(CurrentDay, WarRules.truceYears) : (CurrentDay + 1825));
		relation.AddEvent(new DiplomaticEvent
		{
			type = DiplomaticEventType.PeaceTreaty,
			description = $"签订和平条约，赔款 {warReparations}，停战至第 {relation.truceUntilDay} 日",
			relationChange = 30f
		});
		Chronicle?.Add("peace", _realms[realmA].realmName + " 与 " + _realms[realmB].realmName + " 签订和平条约" + ((warReparations > 0f) ? $"（赔款 {warReparations}）" : ""), true, realmA, realmB);
		return treaty;
	}

	/// <summary>
	/// 以条约结束战争：战争运行态的真源仍是 <see cref="WarState"/>，这里只负责把条约挂到外交投影并执行其停战/同盟类条款。
	/// 割地等实体效果由条约条款的后续执行器处理，不在外交层硬写。
	/// </summary>
	public bool SignPeaceTreaty(int realmA, int realmB, Treaty treaty, int day, string reason = "战争结束")
	{
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation == null || treaty == null)
		{
			return false;
		}
		treaty.isPeaceTreaty = true;
		treaty.isActive = true;
		if (treaty.truceUntilDay <= 0)
		{
			treaty.truceUntilDay = day + ((WarRules != null) ? WarRules.truceYears * 365 : 1825);
		}
		relation.isAtWar = false;
		relation.activeTreaties.Add(treaty);
		WarJustificationSystem.ExecutePeaceTreaty(treaty, relation, day);
		relation.AddEvent(new DiplomaticEvent
		{
			type = DiplomaticEventType.PeaceTreaty,
			description = $"{reason}：{_realms[realmA].realmName} 与 {_realms[realmB].realmName} 签订《{treaty.treatyName}》，停战至第 {relation.truceUntilDay} 日",
			relationChange = 20f
		});
		Chronicle?.Add("peace", _realms[realmA].realmName + " 与 " + _realms[realmB].realmName + " 签订《" + treaty.treatyName + "》", true, realmA, realmB);
		return true;
	}

	public void ForcePeace(int realmA, int realmB, int day, int truceYears, string reason = "战争结束")
	{
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation != null)
		{
			relation.isAtWar = false;
			relation.truceUntilDay = day + truceYears * 365;
			relation.AddEvent(new DiplomaticEvent
			{
				type = DiplomaticEventType.PeaceTreaty,
				description = $"{reason}：{_realms[realmA].realmName} 与 {_realms[realmB].realmName} 停战（至第 {relation.truceUntilDay} 日）",
				relationChange = 10f
			});
			Chronicle?.Add("peace", _realms[realmA].realmName + " 与 " + _realms[realmB].realmName + " " + reason, true, realmA, realmB);
		}
	}

	private void NotifyAlliesOfWar(int attackerId, int defenderId)
	{
		foreach (DiplomaticRelation value in _relations.Values)
		{
			foreach (Alliance activeAlliance in value.activeAlliances)
			{
				if (activeAlliance.isActive && activeAlliance.mutualDefense && (activeAlliance.realmAId == defenderId || activeAlliance.realmBId == defenderId))
				{
					int num = ((activeAlliance.realmAId == defenderId) ? activeAlliance.realmBId : activeAlliance.realmAId);
					if (num != attackerId)
					{
						DeclareWar(num, attackerId, "防御同盟义务");
					}
				}
			}
		}
	}

	public bool AreAtWar(int realmA, int realmB)
	{
		return GetRelation(realmA, realmB)?.isAtWar ?? false;
	}

	public DiplomacyManager(Dictionary<int, RealmData> realms)
	{
		_realms = realms;
		InitializeAllRelations();
	}

	/// <summary>
	/// 政权表在构造后生成（InitializeWorld → GenerateTerrain）时，构造期建好的关系表是空的；
	/// 生成政权后必须重建一次，否则新局所有双边关系都查不到，宣战/同盟入口会静默失败。
	/// 幂等：已存在的关系不覆盖。
	/// </summary>
	public void EnsureRelations()
	{
		InitializeAllRelations();
	}

	private string GetRelationKey(int a, int b)
	{
		if (a >= b)
		{
			return $"{b}_{a}";
		}
		return $"{a}_{b}";
	}

	private void InitializeAllRelations()
	{
		List<int> list = new List<int>(_realms.Keys);
		for (int i = 0; i < list.Count; i++)
		{
			for (int j = i + 1; j < list.Count; j++)
			{
				string relationKey = GetRelationKey(list[i], list[j]);
				if (!_relations.ContainsKey(relationKey))
				{
					_relations[relationKey] = new DiplomaticRelation
					{
						realmAId = list[i],
						realmBId = list[j],
						relation = Random.Range(-20f, 20f),
						trust = Random.Range(30f, 70f),
						threat = Random.Range(30f, 70f)
					};
				}
			}
		}
	}

	public DiplomaticRelation GetOrCreateRelation(int realmA, int realmB)
	{
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation != null)
		{
			return relation;
		}
		DiplomaticRelation diplomaticRelation = new DiplomaticRelation
		{
			realmAId = realmA,
			realmBId = realmB
		};
		_relations[GetRelationKey(realmA, realmB)] = diplomaticRelation;
		return diplomaticRelation;
	}

	public DiplomaticRelation GetRelation(int realmA, int realmB)
	{
		string relationKey = GetRelationKey(realmA, realmB);
		if (_relations.TryGetValue(relationKey, out var value))
		{
			return value;
		}
		return null;
	}

	public void ModifyRelation(int realmA, int realmB, float delta, string reason)
	{
		DiplomaticRelation orCreateRelation = GetOrCreateRelation(realmA, realmB);
		orCreateRelation.relation = Mathf.Clamp(orCreateRelation.relation + delta, -100f, 100f);
		orCreateRelation.AddEvent(new DiplomaticEvent
		{
			type = DiplomaticEventType.EmbassyEstablished,
			description = reason,
			relationChange = delta
		});
	}

	public void ModifyTrust(int realmA, int realmB, float delta)
	{
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation != null)
		{
			relation.trust = Mathf.Clamp(relation.trust + delta, 0f, 100f);
		}
	}

	public void ModifyThreat(int realmA, int realmB, float delta)
	{
		DiplomaticRelation relation = GetRelation(realmA, realmB);
		if (relation != null)
		{
			relation.threat = Mathf.Clamp(relation.threat + delta, 0f, 100f);
		}
	}

	public (bool success, bool warDeclared, float lootValue) RaidSettlement(int raiderId, int targetId, int tileIndex, GameEnums.RaidType raidType, TileData[] tiles = null)
	{
		DiplomaticRelation relation = GetRelation(raiderId, targetId);
		if (relation == null || relation.isAtWar)
		{
			return (success: false, warDeclared: false, lootValue: 0f);
		}
		bool isHostile = relation.IsHostile;
		relation.lastRaidAttackerId = raiderId;
		relation.lastRaidDay = CurrentDay;
		relation.raidCount++;
		if (raidType == GameEnums.RaidType.Massacre && tiles != null && tileIndex >= 0 && tileIndex < tiles.Length)
		{
			TileData tileData = tiles[tileIndex];
			if (tileData.populationBlocks != null)
			{
				for (int i = 0; i < tileData.populationBlocks.Count; i++)
				{
					PopulationBlock value = tileData.populationBlocks[i];
					value.count *= 0.7f;
					tileData.populationBlocks[i] = value;
				}
			}
		}
		CasusBelli item = WarJustificationSystem.GenerateRaidReprisalCB(targetId, raiderId, tileIndex, CurrentDay, raidType);
		relation.casusBelliList.Add(item);
		float item2 = raidType switch
		{
			GameEnums.RaidType.BorderSkirmish => Random.Range(10f, 50f), 
			GameEnums.RaidType.VillageRaid => Random.Range(50f, 200f), 
			GameEnums.RaidType.TownAttack => Random.Range(150f, 500f), 
			GameEnums.RaidType.SupplyRaiding => Random.Range(30f, 150f), 
			GameEnums.RaidType.SlaveRaiding => Random.Range(80f, 300f), 
			GameEnums.RaidType.Massacre => Random.Range(400f, 1200f), 
			_ => 50f, 
		};
		float num = raidType switch
		{
			GameEnums.RaidType.BorderSkirmish => 5f, 
			GameEnums.RaidType.VillageRaid => 15f, 
			GameEnums.RaidType.TownAttack => 30f, 
			GameEnums.RaidType.SupplyRaiding => 10f, 
			GameEnums.RaidType.SlaveRaiding => 25f, 
			GameEnums.RaidType.Massacre => 50f, 
			_ => 10f, 
		};
		IncreaseHostility(targetId, raiderId, num, raidType.ToString() + "劫掠");
		ModifyRelation(targetId, raiderId, (0f - num) * 0.8f, "劫掠报复");
		bool item3 = false;
		if (!isHostile)
		{
			if (Random.value < 0.6f || raidType == GameEnums.RaidType.TownAttack)
			{
				DeclareWar(targetId, raiderId, "报复" + raidType.ToString() + "劫掠");
				item3 = true;
			}
		}
		else if ((relation.raidCount >= 3 || relation.hostilityLevel >= 90f) && Random.value < 0.4f)
		{
			DeclareWar(targetId, raiderId, "报复持续劫掠");
			item3 = true;
			relation.raidCount = 0;
		}
		UpdateConflictLevel(relation);
		string text = raidType.ToString();
		relation.AddEvent(new DiplomaticEvent
		{
			type = DiplomaticEventType.BorderIncident,
			description = _realms[raiderId].realmName + " 对 " + _realms[targetId].realmName + " 发动" + text + "（地块 #" + tileIndex + "）",
			relationChange = (0f - num) * 0.8f,
			threatChange = num
		});
		Chronicle?.Add("war", _realms[raiderId].realmName + " 对 " + _realms[targetId].realmName + " 发动" + text, raidType == GameEnums.RaidType.TownAttack, raiderId, targetId);
		return (success: true, warDeclared: item3, lootValue: item2);
	}

	private static void AddEventTo(DiplomaticRelation rel, DiplomaticEventType type, string description)
	{
		rel.AddEvent(new DiplomaticEvent
		{
			type = type,
			description = description
		});
	}

	public void DailyTick()
	{
		foreach (DiplomaticRelation value3 in _relations.Values)
		{
			value3.DailyDecay();
			for (int num = value3.activeAlliances.Count - 1; num >= 0; num--)
			{
				if (!value3.activeAlliances[num].CheckConditions(value3))
				{
					value3.activeAlliances[num].isActive = false;
					value3.activeAlliances.RemoveAt(num);
				}
			}
		}
		foreach (Subordination subordination in _subordinations)
		{
			if (subordination.isActive && _realms.TryGetValue(subordination.vassalId, out var value) && _realms.TryGetValue(subordination.suzerainId, out var value2))
			{
				float num2 = value.treasury * subordination.tributeRatio / 365f;
				value.treasury -= num2;
				value2.treasury += num2;
			}
		}
	}

	public List<DiplomaticRelation> CaptureRelations()
	{
		var result = new List<DiplomaticRelation>(_relations.Values);
		result.Sort((a, b) =>
		{
			int byRealm = a.realmAId.CompareTo(b.realmAId);
			return byRealm != 0 ? byRealm : a.realmBId.CompareTo(b.realmBId);
		});
		return result;
	}

	public List<Subordination> CaptureSubordinations()
	{
		var result = new List<Subordination>(_subordinations);
		result.Sort((a, b) =>
		{
			int bySuzerain = a.suzerainId.CompareTo(b.suzerainId);
			return bySuzerain != 0 ? bySuzerain : a.vassalId.CompareTo(b.vassalId);
		});
		return result;
	}

	/// <summary>读档还原外交四表；relations/subordinations 为 null 时表示旧档缺段，保留当前初始化。</summary>
	public void RestoreDiplomacy(List<DiplomaticRelation> relations, List<Subordination> subordinations)
	{
		if (relations == null && subordinations == null) return;

		_relations.Clear();
		_subordinations.Clear();
		_nextTreatyId = 1;
		if (relations != null)
		{
			foreach (DiplomaticRelation relation in relations)
			{
				if (relation == null) continue;
				_relations[GetRelationKey(relation.realmAId, relation.realmBId)] = relation;
				if (relation.activeTreaties == null) continue;
				foreach (Treaty treaty in relation.activeTreaties)
					if (treaty != null && treaty.treatyId >= _nextTreatyId) _nextTreatyId = treaty.treatyId + 1;
			}
		}

		if (subordinations != null)
		{
			foreach (Subordination subordination in subordinations)
			{
				if (subordination == null) continue;
				_subordinations.Add(subordination);
				DiplomaticRelation relation = GetRelation(subordination.suzerainId, subordination.vassalId);
				if (relation != null) relation.subordination = subordination;
			}
		}
	}

	public IReadOnlyDictionary<string, DiplomaticRelation> GetAllRelations()
	{
		return _relations;
	}
}
}
