namespace MyriadCreation.Simulation.Faith
{
/// <summary>
/// 同一 religion 下诸 faith 的局势阶段（派生，不入档）：
/// 合一 → 脆弱的合一 → 妥协 → 敌意 → 分裂。阈值是内容参数前的占位（TODO_M05）。
/// </summary>
public enum ReligionStage
{
	Unity,
	FragileUnity,
	Compromise,
	Hostility,
	Schism
}

public static class ReligionStageRules
{
	public const float FragileDivergence = 20f;

	public const float CompromiseDivergence = 40f;

	public const float HostilityDivergence = 60f;

	public const float SchismDivergence = 80f;

	public static ReligionStage StageFor(float divergence)
	{
		if (divergence >= SchismDivergence)
		{
			return ReligionStage.Schism;
		}
		if (divergence >= HostilityDivergence)
		{
			return ReligionStage.Hostility;
		}
		if (divergence >= CompromiseDivergence)
		{
			return ReligionStage.Compromise;
		}
		if (divergence >= FragileDivergence)
		{
			return ReligionStage.FragileUnity;
		}
		return ReligionStage.Unity;
	}
}
}
