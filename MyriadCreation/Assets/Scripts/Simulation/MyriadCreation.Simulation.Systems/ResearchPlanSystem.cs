using System;
using System.Collections.Generic;
using MyriadCreation.Core.Events;
using MyriadCreation.Simulation.Planning;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using Random = UnityEngine.Random;

namespace MyriadCreation.Simulation.Systems
{
public sealed class ResearchPlanSystem
{
	private readonly GameWorld _world;

	private readonly PlanSystem _plans;

	private readonly InnovationKnowledgeSystem _knowledge = new InnovationKnowledgeSystem();

	private readonly Dictionary<int, ResearchPlanData> _data = new Dictionary<int, ResearchPlanData>();

	private readonly ResearchPlanExecutor _executor;

	private readonly SimulationEventBus _events;

	private readonly HashSet<int> _practiceDirtyCharacters = new HashSet<int>();

	public const float BaseBreakthroughChancePerDay = 0.0005f;

	public const float PracticeScale = 0.01f;

	public const float ScholarshipScale = 0.008f;

	public const float ExperienceScale = 0.005f;

	public const float MaxBreakthroughChancePerDay = 0.03f;

	public InnovationKnowledgeSystem Knowledge => _knowledge;

	public InnovationKnowledgeSaveData CaptureKnowledge() => _knowledge.CaptureState();

	public void RestoreKnowledge(InnovationKnowledgeSaveData saved) => _knowledge.RestoreState(saved);

	public ResearchPlanSystem(GameWorld world, PlanSystem plans)
	{
		_world = world ?? throw new ArgumentNullException("world");
		_plans = plans ?? throw new ArgumentNullException("plans");
		_events = _world.SimulationEvents;
		_executor = new ResearchPlanExecutor(this);
		_plans.RegisterExecutor(_executor);
		_events.Subscribe<PracticeRecordedEvent>(OnPracticeRecorded);
	}

	private void OnPracticeRecorded(PracticeRecordedEvent evt)
	{
		RecordPractice(evt.characterId, evt.innovationId, evt.amount);
	}

	public void Dispose()
	{
		_events.Unsubscribe<PracticeRecordedEvent>(OnPracticeRecorded);
		_plans.UnregisterExecutor(PlanType.Research);
	}

	internal CharacterData GetCharacter(int characterId)
	{
		return _world.Characters?.GetCharacter(characterId);
	}

	public float RecordPractice(int characterId, int innovationId, float amount)
	{
		float num = _knowledge.RecordPractice(characterId, innovationId, amount);
		if (num > 0f && characterId >= 0)
		{
			_practiceDirtyCharacters.Add(characterId);
		}
		return num;
	}

	public int DrainPracticeDirtyCharacters(List<int> destination)
	{
		if (destination == null)
		{
			throw new ArgumentNullException("destination");
		}
		int count = _practiceDirtyCharacters.Count;
		foreach (int practiceDirtyCharacter in _practiceDirtyCharacters)
		{
			destination.Add(practiceDirtyCharacter);
		}
		_practiceDirtyCharacters.Clear();
		return count;
	}

	public int ProcessPracticeBreakthroughs(float deltaDays = 1f)
	{
		if (deltaDays <= 0f)
		{
			return 0;
		}
		List<int> list = new List<int>();
		DrainPracticeDirtyCharacters(list);
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			int characterId = list[i];
			CharacterData character = GetCharacter(characterId);
			if (character != null && character.isAlive && character.realmId >= 0 && TryDailyBreakthrough(characterId, deltaDays))
			{
				num++;
			}
		}
		return num;
	}

	public int GetMastery(int characterId, int innovationId)
	{
		return _knowledge.GetMastery(characterId, innovationId);
	}

	public int DailyTick(float deltaDays = 1f)
	{
		if (_world.Characters == null || deltaDays <= 0f)
		{
			return 0;
		}
		int num = 0;
		foreach (CharacterData item in _world.Characters.EnumerateCharacters())
		{
			if (item != null && item.isAlive && item.realmId >= 0 && TryDailyBreakthrough(item.characterId, deltaDays))
			{
				num++;
			}
		}
		return num;
	}

	public bool TryDailyBreakthrough(int characterId, float deltaDays = 1f)
	{
		CharacterData character = GetCharacter(characterId);
		InnovationTree innovations = _world.Innovations;
		if (character == null || !character.isAlive || innovations == null || character.realmId < 0)
		{
			return false;
		}
		List<InnovationDef> discoveryCandidates = _knowledge.GetDiscoveryCandidates(character, innovations, character.realmId);
		if (discoveryCandidates.Count == 0)
		{
			return false;
		}
		InnovationDef innovationDef = null;
		float num = 0f;
		for (int i = 0; i < discoveryCandidates.Count; i++)
		{
			float practiceRelevance = GetPracticeRelevance(character.characterId, discoveryCandidates[i]);
			if (practiceRelevance > num)
			{
				num = practiceRelevance;
				innovationDef = discoveryCandidates[i];
			}
		}
		if (innovationDef == null || num <= 0f)
		{
			return false;
		}
		if (HasActiveResearchPlan(character.characterId, innovationDef.innovationId))
		{
			return false;
		}
		float num2 = CalculateBreakthroughChance(character, num, deltaDays);
		if (Random.value > num2)
		{
			return false;
		}
		Plan plan = CreateResearchPlan(character, innovationDef, num);
		if (plan == null)
		{
			return false;
		}
		return StartResearchPlan(plan.planId);
	}

	public bool HasActiveResearchPlan(int characterId, int innovationId)
	{
		List<Plan> plans = _plans.GetPlans(PlanType.Research);
		for (int i = 0; i < plans.Count; i++)
		{
			Plan plan = plans[i];
			if (!plan.IsTerminal && _data.TryGetValue(plan.planId, out var value) && value.characterId == characterId && value.innovationId == innovationId)
			{
				return true;
			}
		}
		return false;
	}

	public Plan CreateResearchPlan(CharacterData character, InnovationDef innovation, float relevance = 1f)
	{
		if (character == null || innovation == null || character.realmId < 0)
		{
			return null;
		}
		if (_world.Innovations == null || _world.Innovations.HasInnovation(character.realmId, innovation.innovationId))
		{
			return null;
		}
		if (HasActiveResearchPlan(character.characterId, innovation.innovationId))
		{
			return null;
		}
		Plan plan = _plans.CreatePlan(PlanType.Research, character.characterId, innovation.innovationId, _world.currentDay, "研究：" + innovation.GetName(), "个人突破后的研究、验证与工艺固化。", EstimateResearchDays(character, innovation, relevance));
		plan.ownerId = character.realmId;
		plan.participants.Add(new PlanParticipant(character.characterId, "discoverer"));
		plan.requirements.Add(new PlanRequirement("practice_relevance", Mathf.Max(0f, relevance)));
		_data[plan.planId] = new ResearchPlanData
		{
			planId = plan.planId,
			characterId = character.characterId,
			realmId = character.realmId,
			innovationId = innovation.innovationId,
			relevance = Mathf.Clamp01(relevance),
			discovered = true
		};
		return plan;
	}

	public bool StartResearchPlan(int planId)
	{
		if (!_plans.AcceptPlan(planId))
		{
			return false;
		}
		if (!_plans.BeginPreparation(planId))
		{
			return false;
		}
		return _plans.BeginExecution(planId);
	}

	public bool TryGetResearchData(int planId, out ResearchPlanData data)
	{
		return _data.TryGetValue(planId, out data);
	}

	public bool Learn(int characterId, int innovationId, int targetLevel = 1)
	{
		CharacterData character = GetCharacter(characterId);
		if (character == null || character.realmId < 0 || _world.Innovations == null)
		{
			return false;
		}
		if (!_world.Innovations.HasInnovation(character.realmId, innovationId))
		{
			return false;
		}
		return _knowledge.Learn(characterId, innovationId, targetLevel);
	}

	private float GetPracticeRelevance(int characterId, InnovationDef def)
	{
		float practice = _knowledge.GetPractice(characterId, def.innovationId);
		float num = 0f;
		foreach (int prerequisite in def.GetPrerequisiteIds())
		{
			num = Mathf.Max(num, _knowledge.GetPractice(characterId, prerequisite));
		}
		return Mathf.Clamp01((practice + num * 0.5f) * 0.01f);
	}

	private static float CalculateBreakthroughChance(CharacterData character, float relevance, float deltaDays)
	{
		character.GetEffectiveStats(out _, out _, out _, out _, out _, out float effScholarship, out _);
		float num = Mathf.Clamp01(effScholarship / 100f);
		float num2 = Mathf.Clamp01(((float)character.age - 15f) / 50f);
		return Mathf.Clamp(0.0005f * (0.25f + relevance) * (0.5f + num * 0.008f * 10f) * (0.75f + num2 * 0.005f * 10f) * Mathf.Max(0.1f, deltaDays), 0f, 0.03f);
	}

	private static float EstimateResearchDays(CharacterData character, InnovationDef innovation, float relevance)
	{
		character.GetEffectiveStats(out _, out _, out _, out _, out _, out float effScholarship2, out _);
		float num = Mathf.Clamp(effScholarship2, 5f, 100f);
		float num2 = Mathf.Max(1f, innovation.researchCost);
		float num3 = 0.25f + num / 100f;
		num3 *= 0.75f + Mathf.Clamp01(relevance) * 0.5f;
		return Mathf.Max(5f, num2 / num3);
	}

	internal bool TryFormalizeResearch(ResearchPlanData data)
	{
		if (data == null || _world.Innovations == null)
		{
			return false;
		}
		if (_world.Innovations.HasInnovation(data.realmId, data.innovationId))
		{
			return true;
		}
		if (_world.Innovations.GetCurrentResearch(data.realmId) != null)
		{
			return false;
		}
		if (!_world.Innovations.StartResearch(data.realmId, data.innovationId, _world))
		{
			return false;
		}
		InnovationDef innovation = _world.Innovations.GetInnovation(data.innovationId);
		if (innovation == null)
		{
			return false;
		}
		_world.Innovations.DailyTick(data.realmId, Mathf.Max(1f, innovation.researchCost));
		bool completed = _world.Innovations.HasInnovation(data.realmId, data.innovationId);
		if (completed && data.characterId >= 0)
		{
			Learn(data.characterId, data.innovationId, 1);
		}
		return completed;
	}
}
}
