using System;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Genetics
{
/// <summary>
/// 基因组结构迁移（D6）：新位点加入后，旧 GenomeInstance 的等位基因数组可能短于当前基因组；
/// 读档时按该位点最常见等位基因补齐一次，之后读写往返稳定。只做结构补齐，不改已有位点。
/// </summary>
public static class GenomeMigration
{
	public const int CurrentVersion = 2;

	/// <summary>就地补齐并返回同一实例；空实例/未知基因组只打版本号，不臆造数据。</summary>
	public static GenomeInstance Upgrade(GenomeInstance instance)
	{
		if (instance == null) return null;
		GenomeDef genome = GeneticsCatalog.ResolveGenome(instance.genomeId);
		if (genome?.loci == null || genome.loci.Count == 0)
		{
			instance.genomeVersion = CurrentVersion;
			return instance;
		}
		int count = genome.loci.Count;
		int paternalLength = instance.paternalAlleles?.Length ?? 0;
		int maternalLength = instance.maternalAlleles?.Length ?? 0;
		if (paternalLength >= count && maternalLength >= count)
		{
			instance.genomeVersion = CurrentVersion;
			return instance;
		}
		int[] paternal = new int[count];
		int[] maternal = new int[count];
		for (int i = 0; i < count; i++)
		{
			int defaultAllele = DefaultAlleleIndex(genome.loci[i]);
			paternal[i] = i < paternalLength ? instance.paternalAlleles[i] : defaultAllele;
			maternal[i] = i < maternalLength ? instance.maternalAlleles[i] : defaultAllele;
		}
		instance.paternalAlleles = paternal;
		instance.maternalAlleles = maternal;
		instance.genomeVersion = CurrentVersion;
		return instance;
	}

	/// <summary>
	/// 补齐用的等位基因：优先“中性”等位基因（加性 0、离散值 none/skin/biped/short），
	/// 保证旧档新位点不凭空获得体型/耐温；没有中性项时才取 defaultFrequency 最高者。并列取索引最小。
	/// </summary>
	private static int DefaultAlleleIndex(GenomeLocusDef locus)
	{
		if (locus?.alleles == null || locus.alleles.Count == 0) return -1;
		int neutralBest = -1;
		float neutralFrequency = -1f;
		int best = 0;
		float bestFrequency = -1f;
		for (int i = 0; i < locus.alleles.Count; i++)
		{
			AlleleDef allele = locus.alleles[i];
			float frequency = allele != null ? Math.Max(0f, allele.defaultFrequency) : 0f;
			if (frequency > bestFrequency)
			{
				bestFrequency = frequency;
				best = i;
			}
			if (IsNeutral(allele) && frequency > neutralFrequency)
			{
				neutralFrequency = frequency;
				neutralBest = i;
			}
		}
		return neutralBest >= 0 ? neutralBest : best;
	}

	private static bool IsNeutral(AlleleDef allele)
	{
		if (allele?.effects == null) return true;
		foreach (AlleleTraitEffect effect in allele.effects)
		{
			if (effect == null) continue;
			if (Math.Abs(effect.additiveEffect) > 1e-6f) return false;
			if (!string.IsNullOrEmpty(effect.discreteValue))
			{
				switch (effect.discreteValue)
				{
					case "none":
					case "skin":
					case "biped":
					case "short":
						break;
					default:
						return false;
				}
			}
		}
		return true;
	}
}
}
