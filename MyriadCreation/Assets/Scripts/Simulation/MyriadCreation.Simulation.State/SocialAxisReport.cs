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
}

public sealed class SocialAxisReport
{
	public SocialAxis axis;

	public float totalPopulation;

	public List<SocialAxisBucket> buckets = new List<SocialAxisBucket>();
}
}
