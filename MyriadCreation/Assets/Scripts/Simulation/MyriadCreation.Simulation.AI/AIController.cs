using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace MyriadCreation.Simulation.AI
{
[Serializable]
public class AIController
{
	public int realmId;

	public AIPersonality personality;

	private float _decisionTimer;

	private const float DecisionInterval = 30f;

	private AIGoal _currentGoal;

	private int _targetRealmId = -1;

	private int _targetTileIndex = -1;

	private int _raidCooldown;

	private readonly List<AIIntent> _pendingIntents = new List<AIIntent>();

	private const int RaidInterval = 45;

	/// <summary>建造决策间隔（日）：与 30 日大决策错开，避免同一天所有政权一起开工。</summary>
	private const int BuildInterval = 180;

	private int _buildCooldown = -1;

	/// <summary>密谋决策间隔（日）。</summary>
	private const int IntrigueInterval = MyriadCreation.Core.Data.GameCalendar.DefaultDaysPerYear;

	private int _intrigueCooldown = -1;

	public AIController(int realmId, AIPersonality personality)
	{
		this.realmId = realmId;
		this.personality = personality;
	}

	public void DailyTick(Dictionary<int, RealmData> realms, TileData[] tiles, DiplomacyManager diplomacy, InnovationTree innovations, CharacterManager characters = null, GameWorld world = null)
	{
		_pendingIntents.Clear();
		_decisionTimer++;
		SelectDailyIntent(realms, tiles, innovations, _pendingIntents, world);
		TryRaid(realms, tiles, diplomacy, characters, _pendingIntents);
		TryPlanConstruction(realms, tiles, diplomacy, world);
		TryPlanIntrigue(realms, world);
		if (_decisionTimer >= 30f)
		{
			_decisionTimer = 0f;
			MakeMajorDecision(realms, tiles, diplomacy, _pendingIntents);
		}
	}

	public float GetResearchRate(RealmData realm, TileData[] tiles)
	{
		return CalculateResearchRate(realm, tiles);
	}

	public IReadOnlyList<AIIntent> DrainIntents()
	{
		List<AIIntent> result = new List<AIIntent>(_pendingIntents);
		_pendingIntents.Clear();
		return result;
	}

	/// <summary>
	/// 建造令的生产调用者（P3.3）：按“是否在战 / 财政余量”选建筑，在开发度最高且该类别尚空的地块立计划。
	/// 只调用统一的 ConstructionPlanSystem，不另开建造/库存系统。
	/// </summary>
	private void TryPlanConstruction(Dictionary<int, RealmData> realms, TileData[] tiles, DiplomacyManager diplomacy, GameWorld world)
	{
		if (_buildCooldown < 0) _buildCooldown = Mathf.Abs(realmId) % BuildInterval;
		_buildCooldown++;
		if (_buildCooldown < BuildInterval) return;
		_buildCooldown = 0;
		if (world == null || tiles == null || realms == null || !realms.TryGetValue(realmId, out RealmData realm) || realm == null) return;

		bool atWar = IsAtWar(diplomacy);
		int buildingId = atWar ? 401 : (realm.treasury < 800f ? 100 : 500);
		BuildingSystem buildings = world.Buildings;
		if (buildings == null || !buildings.GetBuildingDef(buildingId).HasValue) return;
		BuildingCategory category = buildings.GetBuildingDef(buildingId).Value.category;
		if (realm.treasury < buildings.GetBuildingDef(buildingId).Value.buildCost) return;

		int bestTile = -1;
		float bestScore = -1f;
		int bestLevel = int.MaxValue;
		for (int i = 0; i < tiles.Length; i++)
		{
			if (!tiles[i].exists || !tiles[i].isLand || tiles[i].ownerRealmId != realmId) continue;
			int[] levels = tiles[i].buildingLevels;
			if (levels == null || (int)category >= levels.Length) continue;
			int level = levels[(int)category];
			float score = tiles[i].development * 100f + PopulationOf(tiles[i]);
			// 同类别等级低的地块优先；同分按 tileIndex 稳定决胜。
			if (level < bestLevel || (level == bestLevel && (score > bestScore || (score == bestScore && (bestTile < 0 || i < bestTile)))))
			{
				bestTile = i;
				bestLevel = level;
				bestScore = score;
			}
		}
		if (bestTile < 0) return;
		world.CreateConstructionPlan(realmId, realm.GetSupremeRulerId(), bestTile, buildingId);
	}

	/// <summary>
	/// 密谋的生产调用者（P3.5）：只对“目标地块已在地图知识内”的敌对/他国君主立项；
	/// 信息不足时不做，避免凭空阴谋。
	/// </summary>
	private void TryPlanIntrigue(Dictionary<int, RealmData> realms, GameWorld world)
	{
		if (_intrigueCooldown < 0) _intrigueCooldown = Mathf.Abs(realmId) % IntrigueInterval;
		_intrigueCooldown++;
		if (_intrigueCooldown < IntrigueInterval) return;
		_intrigueCooldown = 0;
		if (world?.Characters == null || realms == null || !realms.TryGetValue(realmId, out RealmData realm) || realm == null) return;
		CharacterData plotter = world.Characters.GetCharacter(realm.GetSupremeRulerId());
		if (plotter == null || !plotter.isAlive) return;

		foreach (RealmData other in realms.Values)
		{
			if (other == null || other.realmId == realmId) continue;
			CharacterData target = world.Characters.GetCharacter(other.GetSupremeRulerId());
			if (target == null || !target.isAlive) continue;
			if (!world.Knowledge.IsKnown(realmId, target.currentTile)) continue;
			world.CreateIntriguePlan(realmId, plotter.characterId, target.characterId);
			return;
		}
	}

	private static float PopulationOf(TileData tile)
	{
		if (tile.populationBlocks == null) return 0f;
		float people = 0f;
		for (int i = 0; i < tile.populationBlocks.Count; i++) people += tile.populationBlocks[i].count;
		return people;
	}

	private bool IsAtWar(DiplomacyManager diplomacy)
	{
		if (diplomacy == null) return false;
		foreach (KeyValuePair<string, DiplomaticRelation> pair in diplomacy.GetAllRelations())
		{
			DiplomaticRelation relation = pair.Value;
			if (relation != null && relation.isAtWar && (relation.realmAId == realmId || relation.realmBId == realmId)) return true;
		}
		return false;
	}

	private void TryRaid(Dictionary<int, RealmData> realms, TileData[] tiles, DiplomacyManager diplomacy, CharacterManager characters, List<AIIntent> intents)
	{
		_raidCooldown++;
		if (_raidCooldown < 45 || (personality.aggression < 0.45f && personality.expansionBias < 0.55f) || realms == null || !realms.ContainsKey(realmId))
		{
			return;
		}
		IReadOnlyDictionary<string, DiplomaticRelation> allRelations = diplomacy.GetAllRelations();
		int num = -1;
		foreach (KeyValuePair<string, DiplomaticRelation> item in allRelations)
		{
			DiplomaticRelation value = item.Value;
			if (value != null && !value.isAtWar)
			{
				int num2;
				if (value.realmAId == realmId)
				{
					num2 = value.realmBId;
				}
				else
				{
					num2 = ((value.realmBId == realmId) ? value.realmAId : (-1));
				}
				if (num2 >= 0 && num2 != realmId && value.IsHostile && !value.isAtWar)
				{
					num = num2;
					break;
				}
			}
		}
		if (num < 0 || tiles == null)
		{
			return;
		}
		int num3 = -1;
		for (int i = 0; i < tiles.Length; i++)
		{
			TileData tileData = tiles[i];
			if (tileData.ownerRealmId == num)
			{
				num3 = tileData.tileIndex;
				break;
			}
		}
		if (num3 >= 0)
		{
			GameEnums.RaidType raidType = GameEnums.RaidType.VillageRaid;
			if (personality.aggression > 0.6f && Random.value < 0.12f)
			{
				raidType = GameEnums.RaidType.Massacre;
			}
			intents.Add(new AIIntent(AIIntentType.RaidSettlement, realmId, num, num3, 0f, raidType));
			_raidCooldown = 0;
		}
	}

	private void SelectDailyIntent(Dictionary<int, RealmData> realms, TileData[] tiles, InnovationTree innovations, List<AIIntent> intents, GameWorld world = null)
	{
		if (!realms.TryGetValue(realmId, out var _))
		{
			return;
		}
		InnovationDef currentResearch = innovations.GetCurrentResearch(realmId);
		if (currentResearch != null && currentResearch.innovationId != 0)
		{
			return;
		}
		List<InnovationDef> availableInnovations = innovations.GetResearchableInnovations(realmId, world);
		// 内容不完整时列表可能夹带 null；排序比较器会解引用，先剔除。
		availableInnovations.RemoveAll(i => i == null);
		if (availableInnovations.Count <= 0)
		{
			return;
		}
		int currentEra = InnovationEra.GetEraForYear((!((Object)(object)world != (Object)null)) ? 1 : world.currentYear);
		bool hasPreferredDomains = personality.preferredDomains != null && personality.preferredDomains.Count > 0;
		availableInnovations.Sort((InnovationDef a, InnovationDef b) =>
		{
			bool flag = hasPreferredDomains && personality.preferredDomains.Contains(a.Domain);
			bool flag2 = hasPreferredDomains && personality.preferredDomains.Contains(b.Domain);
			if (flag != flag2)
			{
				if (!flag)
				{
					return 1;
				}
				return -1;
			}
			float preferenceWeight = InnovationEra.GetPreferenceWeight(a.era, currentEra);
			float preferenceWeight2 = InnovationEra.GetPreferenceWeight(b.era, currentEra);
			return (preferenceWeight != preferenceWeight2) ? preferenceWeight2.CompareTo(preferenceWeight) : a.researchCost.CompareTo(b.researchCost);
		});
		intents.Add(new AIIntent(AIIntentType.StartResearch, realmId, -1, -1, 0f, GameEnums.RaidType.VillageRaid, AllianceType.DefensiveAlliance, availableInnovations[0].innovationId));
	}

	private float CalculateResearchRate(RealmData realm, TileData[] tiles)
	{
		float num = 0.5f;
		float num2 = 0f;
		int num3 = 0;
		foreach (int coreTile in realm.coreTiles)
		{
			if (coreTile >= 0 && coreTile < tiles.Length)
			{
				num2 += tiles[coreTile].development;
				num3++;
			}
		}
		if (num3 > 0)
		{
			num *= 1f + num2 / (float)num3;
		}
		return num * personality.researchMultiplier;
	}

	private void MakeMajorDecision(Dictionary<int, RealmData> realms, TileData[] tiles, DiplomacyManager diplomacy, List<AIIntent> intents, GameWorld world = null)
	{
		if (!realms.TryGetValue(realmId, out var value))
		{
			return;
		}
		Dictionary<AIGoal, float> dictionary = new Dictionary<AIGoal, float>
		{
			[AIGoal.ExpandTerritory] = CalculateExpansionUtility(value, tiles, diplomacy),
			[AIGoal.ImproveEconomy] = CalculateEconomyUtility(value, tiles),
			[AIGoal.Diplomacy] = CalculateDiplomacyUtility(value, realms, diplomacy),
			[AIGoal.Consolidate] = CalculateConsolidationUtility(value, tiles),
			[AIGoal.MilitaryBuildUp] = CalculateMilitaryUtility(value, tiles)
		};
		AIGoal aIGoal = AIGoal.None;
		float num = 0f;
		foreach (KeyValuePair<AIGoal, float> item in dictionary)
		{
			if (item.Value > num)
			{
				num = item.Value;
				aIGoal = item.Key;
			}
		}
		_currentGoal = aIGoal;
		ExecuteGoal(aIGoal, value, realms, tiles, diplomacy, intents);
	}

	private float CalculateExpansionUtility(RealmData realm, TileData[] tiles, DiplomacyManager diplomacy)
	{
		return personality.expansionBias * 50f + (float)Mathf.Max(0, 10 - realm.coreTiles.Count) * 5f + Mathf.Min(realm.treasury / 1000f, 20f) + Random.Range(0f, 20f);
	}

	private float CalculateEconomyUtility(RealmData realm, TileData[] tiles)
	{
		float num = personality.economicBias * 40f;
		num += Mathf.Max(0f, 500f - realm.treasury) / 50f;
		float num2 = 0f;
		int num3 = 0;
		foreach (int coreTile in realm.coreTiles)
		{
			if (coreTile >= 0 && coreTile < tiles.Length)
			{
				num2 += tiles[coreTile].development;
				num3++;
			}
		}
		if (num3 > 0)
		{
			num += (1f - num2 / (float)num3) * 30f;
		}
		return num;
	}

	private float CalculateDiplomacyUtility(RealmData realm, Dictionary<int, RealmData> realms, DiplomacyManager diplomacy)
	{
		float num = personality.diplomaticBias * 40f;
		foreach (RealmData value in realms.Values)
		{
			if (value.realmId != realmId)
			{
				DiplomaticRelation relation = diplomacy.GetRelation(realmId, value.realmId);
				if (relation != null && relation.isAtWar)
				{
					num += 20f;
				}
				if (relation != null && relation.relation < -30f)
				{
					num += 10f;
				}
			}
		}
		return num;
	}

	private float CalculateConsolidationUtility(RealmData realm, TileData[] tiles)
	{
		float num = 0f;
		float num2 = 0f;
		int num3 = 0;
		foreach (int coreTile in realm.coreTiles)
		{
			if (coreTile >= 0 && coreTile < tiles.Length)
			{
				num2 += tiles[coreTile].stability;
				num3++;
			}
		}
		if (num3 > 0)
		{
			num += (100f - num2 / (float)num3) * 0.5f;
		}
		return num + realm.CalculateRebellionRisk() * 0.3f;
	}

	private float CalculateMilitaryUtility(RealmData realm, TileData[] tiles)
	{
		return personality.militaryBias * 40f + Random.Range(0f, 15f);
	}

	private void DoDiplomacy(RealmData realm, Dictionary<int, RealmData> realms, DiplomacyManager diplomacy, List<AIIntent> intents)
	{
		foreach (RealmData value in realms.Values)
		{
			if (value == null || value.realmId == realmId)
			{
				continue;
			}
			DiplomaticRelation relation = diplomacy.GetRelation(realmId, value.realmId);
			if (relation != null && !relation.isAtWar)
			{
				if (relation.relation > 40f)
				{
					intents.Add(new AIIntent(AIIntentType.ProposeAlliance, realmId, value.realmId));
				}
				else if (relation.relation < -20f && realm.treasury > 500f)
				{
					intents.Add(new AIIntent(AIIntentType.SendGift, realmId, value.realmId, -1, 100f));
				}
			}
		}
	}

	private void ExecuteGoal(AIGoal goal, RealmData realm, Dictionary<int, RealmData> realms, TileData[] tiles, DiplomacyManager diplomacy, List<AIIntent> intents)
	{
		switch (goal)
		{
		case AIGoal.ExpandTerritory:
			FindWeakNeighborAndDeclareWar(realm, realms, tiles, diplomacy, intents);
			break;
		case AIGoal.ImproveEconomy:
			intents.Add(new AIIntent(AIIntentType.ImproveEconomy, realmId));
			break;
		case AIGoal.Diplomacy:
			DoDiplomacy(realm, realms, diplomacy, intents);
			break;
		case AIGoal.Consolidate:
			intents.Add(new AIIntent(AIIntentType.ConsolidateRealm, realmId));
			break;
		case AIGoal.MilitaryBuildUp:
			intents.Add(new AIIntent(AIIntentType.MilitaryBuildUp, realmId));
			break;
		}
	}

	private void FindWeakNeighborAndDeclareWar(RealmData realm, Dictionary<int, RealmData> realms, TileData[] tiles, DiplomacyManager diplomacy, List<AIIntent> intents)
	{
		RealmData realmData = null;
		float num = float.MaxValue;
		foreach (RealmData value in realms.Values)
		{
			if (value.realmId == realmId)
			{
				continue;
			}
			DiplomaticRelation relation = diplomacy.GetRelation(realmId, value.realmId);
			if (relation != null && !relation.isAtWar && !(relation.relation > 30f))
			{
				float num2 = value.coreTiles.Count;
				if (num2 < num && num2 < (float)realm.coreTiles.Count)
				{
					num = num2;
					realmData = value;
				}
			}
		}
		if (realmData != null && realm.treasury > 200f)
		{
			intents.Add(new AIIntent(AIIntentType.DeclareWar, realmId, realmData.realmId, actorCharacterId: realm.GetSupremeRulerId()));
		}
	}

	public AIGoal GetCurrentGoal()
	{
		return _currentGoal;
	}

	public int GetTargetRealm()
	{
		return _targetRealmId;
	}

	public float DecisionTimer => _decisionTimer;

	public int TargetTileIndex => _targetTileIndex;

	public int RaidCooldown => _raidCooldown;

	public AIControllerSaveData CaptureState()
	{
		return new AIControllerSaveData
		{
			realmId = realmId,
			personality = personality,
			decisionTimer = _decisionTimer,
			currentGoal = (int)_currentGoal,
			targetRealmId = _targetRealmId,
			targetTileIndex = _targetTileIndex,
			raidCooldown = _raidCooldown
		};
	}

	public void RestoreState(AIControllerSaveData saved)
	{
		if (saved == null) return;
		realmId = saved.realmId;
		personality = saved.personality;
		if (personality.preferredDomains == null) personality.preferredDomains = new List<InnovationDomain>();
		_decisionTimer = saved.decisionTimer;
		_currentGoal = (AIGoal)saved.currentGoal;
		_targetRealmId = saved.targetRealmId;
		_targetTileIndex = saved.targetTileIndex;
		_raidCooldown = saved.raidCooldown;
		_pendingIntents.Clear();
	}

	public void SyncPersonality(CharacterData ruler)
	{
		if (ruler != null && !personality.fixedArchetype)
		{
			float num = (ruler.boldness + 100f) / 200f;
			float num2 = (ruler.greed + 100f) / 200f;
			float num3 = (ruler.honor + 100f) / 200f;
			float num4 = (ruler.vengefulness + 100f) / 200f;
			float num5 = (ruler.compassion + 100f) / 200f;
			float num6 = (ruler.rationality + 100f) / 200f;
			AIPersonality p = personality;
			p.expansionBias = Mathf.Clamp(0.4f + (num - 0.5f) * 0.6f + (num4 - 0.5f) * 0.3f, 0.05f, 0.95f);
			p.economicBias = Mathf.Clamp(0.4f + (num2 - 0.5f) * 0.8f, 0.05f, 0.95f);
			p.diplomaticBias = Mathf.Clamp(0.4f + (num3 - 0.5f) * 0.6f + (num6 - 0.5f) * 0.3f, 0.05f, 0.95f);
			p.militaryBias = Mathf.Clamp(0.35f + (num4 - 0.5f) * 0.5f + (num - 0.5f) * 0.3f - (num5 - 0.5f) * 0.3f, 0.05f, 0.95f);
			p.aggression = Mathf.Clamp(0.4f + (num - 0.5f) * 0.5f + (num4 - 0.5f) * 0.4f - (num5 - 0.5f) * 0.4f, 0.05f, 0.95f);
			p.riskTolerance = Mathf.Clamp(0.5f + (num - 0.5f) * 0.5f - (num6 - 0.5f) * 0.4f, 0.05f, 0.95f);
			p.personalityName = ClassifyName(p);
			personality = p;
		}
	}

	private static string ClassifyName(AIPersonality p)
	{
		float[] array = new float[5] { p.expansionBias, p.economicBias, p.diplomaticBias, p.militaryBias, p.aggression };
		int num = 0;
		for (int i = 1; i < array.Length; i++)
		{
			if (array[i] > array[num])
			{
				num = i;
			}
		}
		return (new string[5] { "征服王", "建设王", "外交家", "军事统帅", "侵略者" })[num];
	}
}
}
