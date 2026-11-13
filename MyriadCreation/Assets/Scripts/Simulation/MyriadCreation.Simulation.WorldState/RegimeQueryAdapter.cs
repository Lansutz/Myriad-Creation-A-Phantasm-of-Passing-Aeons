using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.WorldState
{
/// <summary>
/// UI 4b5a：IRegimeQuery 的 Simulation host 适配器。
/// 转调 GameWorld.RegimeDynamics.GetState + Factions.GetChangeVsStatusQuo；
/// coupRisk 用 tension.total 与改革/守旧力量差推导（0..100）；null/无状态安全。
/// </summary>
public sealed class RegimeQueryAdapter : IRegimeQuery
{
	private readonly GameWorld _world;

	public RegimeQueryAdapter(GameWorld world)
	{
		_world = world;
	}

	public RegimeSnapshotDto GetRegime(int realmId)
	{
		RegimeChangeDynamics dynamics = _world?.RegimeDynamics;
		RegimeChangeState state = dynamics?.GetState(realmId);
		if (state == null)
		{
			return null;
		}

		float changePower = 0f;
		float statusQuoPower = 0f;
		_world?.Factions?.GetChangeVsStatusQuo(realmId, out changePower, out statusQuoPower);

		float tension = state.tension?.total ?? 0f;
		float coupRisk = tension * 0.6f;
		if (changePower > statusQuoPower)
		{
			coupRisk += (changePower - statusQuoPower) * 0.4f;
		}
		coupRisk = UnityEngine.Mathf.Clamp(coupRisk, 0f, 100f);

		RegimeSnapshotDto dto = new RegimeSnapshotDto
		{
			realmId = realmId,
			coupRisk = coupRisk,
			hasOpenWindow = state.IsWindowOpen,
			institutionalInertia = state.institutionalInertia,
			tensionTotal = tension,
			tensionClassMismatch = state.tension?.classMismatch ?? 0f,
			tensionFiscalMilitary = state.tension?.fiscalMilitary ?? 0f,
			tensionLegitimacyErosion = state.tension?.legitimacyErosion ?? 0f,
			activeEvents = (state.history != null)
				? new List<string>(state.history)
				: new List<string>()
		};

		ActiveJuncture juncture = state.activeJuncture;
		if (juncture != null)
		{
			dto.junctureType = (int)juncture.type;
			dto.junctureName = JunctureName(juncture.type);
			dto.junctureOutcome = (int)juncture.outcome;
			dto.junctureOutcomeName = OutcomeName(juncture.outcome);
			dto.junctureSeverity = juncture.severity;
			dto.junctureRemainingDays = juncture.remainingDays;
		}
		return dto;
	}

	private static string JunctureName(CriticalJunctureType type)
	{
		switch (type)
		{
		case CriticalJunctureType.SuccessionCrisis: return "继承危机";
		case CriticalJunctureType.WarDefeat: return "战败";
		case CriticalJunctureType.FiscalCollapse: return "财政破产";
		case CriticalJunctureType.EliteSplit: return "精英分裂";
		case CriticalJunctureType.PopularUprising: return "民众起义";
		case CriticalJunctureType.ForeignConquest: return "外敌征服";
		case CriticalJunctureType.StrongReformer: return "强势改革者";
		default: return type.ToString();
		}
	}

	private static string OutcomeName(JunctureOutcomeType outcome)
	{
		switch (outcome)
		{
		case JunctureOutcomeType.Reform: return "改革";
		case JunctureOutcomeType.Compromise: return "妥协";
		case JunctureOutcomeType.Reaction: return "复辟";
		case JunctureOutcomeType.Stalemate: return "僵持";
		case JunctureOutcomeType.Collapse: return "崩溃";
		default: return "未决";
		}
	}
}
}
