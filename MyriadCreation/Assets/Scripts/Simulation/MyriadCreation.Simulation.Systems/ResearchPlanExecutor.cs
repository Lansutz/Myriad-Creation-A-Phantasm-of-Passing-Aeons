using MyriadCreation.Simulation.Planning;
using MyriadCreation.Simulation.State;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
internal sealed class ResearchPlanExecutor : IPlanExecutor
{
	private readonly ResearchPlanSystem _system;

	public PlanType Type => PlanType.Research;

	public ResearchPlanExecutor(ResearchPlanSystem system)
	{
		_system = system;
	}

	public PlanExecutionResult Execute(Plan plan, float deltaDays)
	{
		if (!_system.TryGetResearchData(plan.planId, out var data))
		{
			return PlanExecutionResult.Fail("research_data_missing");
		}
		CharacterData character = _system.GetCharacter(data.characterId);
		if (character == null || !character.isAlive)
		{
			return PlanExecutionResult.Fail("researcher_unavailable");
		}
		float num = Mathf.Clamp(character.scholarship / 100f, 0.05f, 1f);
		float num2 = (0.004f + num * 0.012f) * (0.75f + data.relevance * 0.5f) * deltaDays;
		float num3 = Mathf.Clamp01(data.verificationProgress + num2);
		if (num3 >= 1f && !_system.TryFormalizeResearch(data))
		{
			num3 = 0.999f;
		}
		else if (num3 >= 1f)
		{
			data.formallyUnlocked = true;
		}
		data.verificationProgress = num3;
		float progressDelta = Mathf.Max(0f, num3 - plan.progress);
		if (num3 >= 1f && data.formallyUnlocked)
		{
			return PlanExecutionResult.Complete("research_verified", "验证与固化");
		}
		return PlanExecutionResult.Continue(progressDelta, "研究与验证");
	}
}
}
