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

	/// <summary>目标升级（要求更高战争分）的威望成本；降级免费。</summary>
	public float warGoalEscalationPrestigeCost = 25f;

	public int GetTruceUntilDay(int currentDay, int years)
	{
		return currentDay + years * 365;
	}

	public static WarRules Default()
	{
		return new WarRules();
	}
}
}
