using System.Collections.Generic;

namespace MyriadCreation.Simulation.State
{
/// <summary>D25 社会四轴：阶级 / 阶层 / 身份 / 职业；各出一套报告，不做交叉表。</summary>
public enum SocialAxis
{
	Class,
	Stratum,
	Identity,
	Occupation
}

public sealed class SocialAxisBucket
{
	public string key = "";

	public float population;

	/// <summary>该桶的加权需求满足度 0–100（沿用阶级需求评分，按桶内人口加权）。</summary>
	public float needsSatisfaction = 50f;
}

public sealed class SocialAxisReport
{
	public SocialAxis axis;

	public float totalPopulation;

	/// <summary>该轴整体需求满足度 0–100（按桶人口加权）。</summary>
	public float axisSatisfaction = 50f;

	public List<SocialAxisBucket> buckets = new List<SocialAxisBucket>();
}

/// <summary>四轴在政权 unrest 里的权重（可配；默认四轴均分）。</summary>
[System.Serializable]
public class SocialAxisWeights
{
	public float classWeight = 0.25f;
	public float stratumWeight = 0.25f;
	public float identityWeight = 0.25f;
	public float occupationWeight = 0.25f;

	public float WeightOf(SocialAxis axis)
	{
		return axis switch
		{
			SocialAxis.Class => classWeight,
			SocialAxis.Stratum => stratumWeight,
			SocialAxis.Identity => identityWeight,
			SocialAxis.Occupation => occupationWeight,
			_ => 0f
		};
	}
}

}
