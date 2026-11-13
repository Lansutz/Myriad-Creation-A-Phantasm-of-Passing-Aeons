using System;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class WarRules
{
	public int truceYears = 5;

	public bool allowWhitePeace = true;

	public float peaceWhiteScore = 0.15f;

	public int peaceMinYears = 2;

	public float annualDrift = 0.05f;

	public float annualDriftLimit = 0.6f;

	public bool allowAllianceIntervention = true;

	public bool allowVassalObligation = true;

	public int settlementMode;

	public float scoreBattle = 10f;

	public float scoreBattleMenPerPoint = 100f;

	public float scoreBattleMin = 2f;

	public float scoreCapital = 30f;

	public float scoreCity = 15f;

	public float scoreProvince = 5f;

	public float scoreSmallCity = 3f;

	public float scoreOccupationCity = 12f;

	public float scoreOccupationProvince = 4f;

	public float scoreOccupationCapital = 25f;

	public float peaceForceScore = 0.1f;

	public int pocketSurrenderMaxProvinces = 3;

	/// <summary>战争目标可改期：两次变更之间的最短间隔（天）。理由固定，目标不是随便改的。</summary>
	public int warGoalRevisionCooldownDays = 90;

	/// <summary>改目标的基础政治代价：稳定度下降与不安上升（不是扣威望点）。</summary>
	public float warGoalRevisionStabilityCost = 2f;

	public float warGoalRevisionUnrestCost = 2f;

	/// <summary>威望等级不足却强推目标时，每差一级额外付出的稳定度/不安。</summary>
	public float warGoalForceStabilityCostPerTier = 6f;

	public float warGoalForceUnrestCostPerTier = 4f;

	/// <summary>强推目标时每差一级的廷臣好感损失（对统治者）。</summary>
	public float warGoalForceCourtOpinionPerTier = 5f;

	/// <summary>威望等级不足时，能否强推：需要政权仍稳、中央仍有权威。</summary>
	public float warGoalForceMinStability = 50f;

	public float warGoalForceMinCentralization = 0.5f;

	/// <summary>绕过议事（不协商）的政治代价：廷臣好感、稳定度、不安。</summary>
	public float warCouncilSkipOpinionPenalty = 6f;

	public float warCouncilSkipStabilityCost = 2f;

	public float warCouncilSkipUnrestCost = 3f;

	/// <summary>议事多数反对仍强推的代价（比不协商更重：既没共识又压过反对）。</summary>
	public float warCouncilOverrideOpinionPenalty = 9f;

	public float warCouncilOverrideStabilityCost = 4f;

	public float warCouncilOverrideUnrestCost = 5f;

	public int GetTruceUntilDay(int currentDay, int years)
	{
		return currentDay + years * MyriadCreation.Core.Data.GameCalendar.DefaultDaysPerYear;
	}

	public static WarRules Default()
	{
		return new WarRules();
	}
}
}
