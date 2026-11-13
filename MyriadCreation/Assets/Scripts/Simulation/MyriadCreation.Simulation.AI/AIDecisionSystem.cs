using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.AI
{
public class AIDecisionSystem
{
	private GameWorld _world;

	private Dictionary<int, AIState> _aiStates = new Dictionary<int, AIState>();

	private float _decisionTimer;

	private const float DecisionInterval = 10f;

	public AIDecisionSystem(GameWorld world)
	{
		_world = world;
	}

	public void InitializeAI(int realmId)
	{
		if (!_aiStates.ContainsKey(realmId))
		{
			AIState aIState = new AIState
			{
				realmId = realmId
			};
			RealmData realm = GetRealm(realmId);
			if (realm != null && realm.laws != null)
			{
				LawEffects.LawEffectData lawEffectData = LawEffects.CalculateEffects(realm.laws);
				aIState.expansionism = Mathf.Clamp01(0.5f + lawEffectData.expansionism);
				aIState.aggression = Mathf.Clamp01(0.5f + lawEffectData.aggressiveDiplomacy / 20f);
				aIState.diplomacyFocus = Mathf.Clamp01(0.5f + lawEffectData.allianceDesirability / 20f);
				aIState.economyFocus = Mathf.Clamp01(0.5f + lawEffectData.tradeEfficiency / 4f);
				aIState.militaryFocus = Mathf.Clamp01(0.5f + lawEffectData.armyMorale / 2f);
			}
			_aiStates[realmId] = aIState;
			Debug.Log((object)$"[AI] 政权 {realm?.realmName} AI初始化: 扩张性={aIState.expansionism:F2}, 侵略性={aIState.aggression:F2}");
		}
	}

	public void Tick(float deltaTime)
	{
		if ((Object)(object)_world == (Object)null || _world.realms == null)
		{
			return;
		}
		_decisionTimer += deltaTime;
		if (_decisionTimer < 10f)
		{
			return;
		}
		_decisionTimer = 0f;
		foreach (KeyValuePair<int, RealmData> realm in _world.realms)
		{
			RealmData value = realm.Value;
			if (value != null)
			{
				if (!_aiStates.ContainsKey(value.realmId))
				{
					InitializeAI(value.realmId);
				}
				UpdateAIState(value);
				MakeDecisions(value);
			}
		}
	}

	private void UpdateAIState(RealmData realm)
	{
		AIState aIState = _aiStates[realm.realmId];
		if (realm.stability < 30f)
		{
			aIState.stabilityConcern = 1f;
			aIState.currentStance = AIStance.Peaceful;
		}
		else if (realm.stability < 60f)
		{
			aIState.stabilityConcern = 0.5f;
		}
		else
		{
			aIState.stabilityConcern = 0f;
		}
		if (realm.treasury < 200f)
		{
			aIState.economyFocus = Mathf.Min(1f, aIState.economyFocus + 0.2f);
			aIState.currentStance = AIStance.Economic;
		}
		UpdateThreatAssessment(realm, aIState);
		if (aIState.warReadiness > 0.8f && aIState.aggression > 0.5f)
		{
			aIState.currentStance = AIStance.Aggressive;
		}
		else if (aIState.threatenedBy.Count > 0)
		{
			aIState.currentStance = AIStance.Defensive;
		}
	}

	private void UpdateThreatAssessment(RealmData realm, AIState state)
	{
		state.threatenedBy.Clear();
		state.rivals.Clear();
		foreach (KeyValuePair<int, RealmData> realm2 in _world.realms)
		{
			RealmData value = realm2.Value;
			if (value != null && value.realmId != realm.realmId && IsBordering(realm.realmId, value.realmId))
			{
				state.rivals.Add(value.realmId);
				if (CalculateThreat(realm, value) > 0.6f)
				{
					state.threatenedBy.Add(value.realmId);
				}
			}
		}
	}

	private float CalculateThreat(RealmData self, RealmData other)
	{
		float num = 0f;
		float num2 = GetMilitaryPower(other.realmId) / Mathf.Max(1f, GetMilitaryPower(self.realmId));
		num += Mathf.Clamp01(num2 - 0.5f) * 0.4f;
		if (_aiStates.TryGetValue(other.realmId, out var value))
		{
			num += value.aggression * 0.3f;
			num += value.expansionism * 0.3f;
		}
		return Mathf.Clamp01(num);
	}

	private void MakeDecisions(RealmData realm)
	{
		AIState aIState = _aiStates[realm.realmId];
		if (aIState.stabilityConcern > 0.5f)
		{
			MakeInternalDecisions(realm, aIState);
			return;
		}
		switch (aIState.currentStance)
		{
		case AIStance.Peaceful:
			MakeInternalDecisions(realm, aIState);
			MakeDiplomaticDecisions(realm, aIState);
			break;
		case AIStance.Defensive:
			MakeDefensiveDecisions(realm, aIState);
			MakeDiplomaticDecisions(realm, aIState);
			break;
		case AIStance.Expansionist:
		case AIStance.Aggressive:
			MakeMilitaryDecisions(realm, aIState);
			break;
		case AIStance.Diplomatic:
			MakeDiplomaticDecisions(realm, aIState);
			break;
		case AIStance.Economic:
			MakeEconomicDecisions(realm, aIState);
			break;
		}
	}

	private void MakeInternalDecisions(RealmData realm, AIState state)
	{
		if (realm.stability < 50f)
		{
			_ = realm.taxSystem;
			realm.treasury = Mathf.Max(0f, realm.treasury - 50f);
			realm.stability = Mathf.Min(100f, realm.stability + 5f);
		}
		if (state.economyFocus > 0.6f)
		{
			_ = realm.treasury;
			_ = 500f;
		}
	}

	private void MakeEconomicDecisions(RealmData realm, AIState state)
	{
		if (realm.taxSystem != null)
		{
			_ = realm.stability;
			_ = 60f;
		}
	}

	private void MakeDiplomaticDecisions(RealmData realm, AIState state)
	{
		if (state.allies.Count >= 2 || !(state.diplomacyFocus > 0.5f))
		{
			return;
		}
		foreach (int rival in state.rivals)
		{
			_ = rival;
		}
		foreach (KeyValuePair<int, RealmData> realm2 in _world.realms)
		{
			RealmData value = realm2.Value;
			if (value != null && value.realmId != realm.realmId && !state.rivals.Contains(value.realmId) && !state.allies.Contains(value.realmId) && HasCommonThreat(realm.realmId, value.realmId))
			{
				state.allies.Add(value.realmId);
				break;
			}
		}
	}

	private void MakeDefensiveDecisions(RealmData realm, AIState state)
	{
		state.militaryFocus = Mathf.Min(1f, state.militaryFocus + 0.1f);
		if (state.allies.Count == 0)
		{
			MakeDiplomaticDecisions(realm, state);
		}
		state.warReadiness = Mathf.Min(1f, state.warReadiness + 0.1f);
	}

	private void MakeMilitaryDecisions(RealmData realm, AIState state)
	{
		if (state.targetRealmId < 0)
		{
			state.targetRealmId = SelectWarTarget(realm, state);
		}
		if (state.targetRealmId >= 0)
		{
			state.warReadiness = Mathf.Min(1f, state.warReadiness + 0.15f);
			if (state.warReadiness >= 1f)
			{
				Debug.Log((object)$"[AI] 政权 {realm.realmName} 对政权 {state.targetRealmId} 发动战争");
				state.warReadiness = 0f;
				state.currentStance = AIStance.Defensive;
			}
		}
	}

	private int SelectWarTarget(RealmData realm, AIState state)
	{
		int result = -1;
		float num = 0f;
		foreach (int rival in state.rivals)
		{
			RealmData realm2 = GetRealm(rival);
			if (realm2 != null)
			{
				float num2 = 0f;
				float militaryPower = GetMilitaryPower(realm.realmId);
				float militaryPower2 = GetMilitaryPower(rival);
				if (militaryPower > militaryPower2 * 1.2f)
				{
					num2 += 0.4f;
				}
				if (realm2.stability < 40f)
				{
					num2 += 0.2f;
				}
				if (realm2.treasury < 200f)
				{
					num2 += 0.1f;
				}
				num2 += state.aggression * 0.2f;
				if (num2 > num)
				{
					num = num2;
					result = rival;
				}
			}
		}
		if (!(num > 0.5f))
		{
			return -1;
		}
		return result;
	}

	private RealmData GetRealm(int realmId)
	{
		if ((Object)(object)_world != (Object)null && realmId >= 0 && realmId < _world.realms.Count)
		{
			return _world.realms[realmId];
		}
		return null;
	}

	private bool IsBordering(int realmA, int realmB)
	{
		return false;
	}

	private float GetMilitaryPower(int realmId)
	{
		RealmData realm = GetRealm(realmId);
		if (realm == null)
		{
			return 0f;
		}
		return 100f + realm.treasury / 10f;
	}

	private bool HasCommonThreat(int realmA, int realmB)
	{
		if (!_aiStates.TryGetValue(realmA, out var value))
		{
			return false;
		}
		if (!_aiStates.TryGetValue(realmB, out var value2))
		{
			return false;
		}
		foreach (int item in value.threatenedBy)
		{
			if (value2.threatenedBy.Contains(item))
			{
				return true;
			}
		}
		return false;
	}

	public AIState GetAIState(int realmId)
	{
		if (!_aiStates.TryGetValue(realmId, out var value))
		{
			return null;
		}
		return value;
	}
}
}
