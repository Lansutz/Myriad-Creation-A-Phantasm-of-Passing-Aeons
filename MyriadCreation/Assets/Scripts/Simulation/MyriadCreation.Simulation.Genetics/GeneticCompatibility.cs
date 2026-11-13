using System;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Genetics
{
public static class GeneticCompatibility
{
	public static HybridizationResult Evaluate(SpeciesDef father, SpeciesDef mother)
	{
		if (father == null || mother == null)
		{
			return new HybridizationResult
			{
				allowed = true,
				introgressionRate = 1f,
				paternalIntrogressionRate = 1f,
				maternalIntrogressionRate = 1f,
				reason = "缺少物种定义，按可杂交处理"
			};
		}
		if (father.speciesId == mother.speciesId)
		{
			return new HybridizationResult
			{
				allowed = true,
				introgressionRate = 1f,
				paternalIntrogressionRate = 1f,
				maternalIntrogressionRate = 1f,
				useMotherSpecies = false,
				reason = "同种"
			};
		}
		if (father.reproductiveIsolation != ReproductiveIsolation.None || mother.reproductiveIsolation != ReproductiveIsolation.None)
		{
			return new HybridizationResult
			{
				allowed = false,
				introgressionRate = 0f,
				reason = "存在生殖隔离"
			};
		}
		float num = Math.Max(0f, Math.Min(1f, mother.paternalIntrogressionRate));
		float num2 = Math.Max(0f, Math.Min(1f, mother.maternalIntrogressionRate));
		float num3 = Math.Min(num, num2);
		return new HybridizationResult
		{
			allowed = true,
			introgressionRate = num3,
			paternalIntrogressionRate = num,
			maternalIntrogressionRate = num2,
			useMotherSpecies = mother.offspringUsesMotherSpecies,
			reason = ((num3 <= 0f) ? "可交配但双方基因几乎不互渗" : "可杂交，双方基因按各自比例渗入")
		};
	}
}
}