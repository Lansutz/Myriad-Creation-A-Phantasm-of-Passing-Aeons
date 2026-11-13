using MyriadCreation.Simulation.State;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Genetics
{
public static class LegacyDnaAdapter
{
	public static readonly string[] LegacyLocusIds = new string[6] { "longevity", "intelligence", "martial", "resistance", "appearance", "talent_defect" };

	public const int LegacyLocusCount = 6;

	public static int ToLocusIndex(DnaLocus locus)
	{
		return (int)locus;
	}

	public static DnaLocus ToDnaLocus(int index)
	{
		if (index < 0 || index >= 6)
		{
			return DnaLocus.Longevity;
		}
		return (DnaLocus)index;
	}

	public static GenomeInstance ToGenomeInstance(DnaData legacy, GenomeDef genome, bool isMale = false)
	{
		if (genome == null)
		{
			genome = GeneticsCatalog.GetDefaultHumanGenome();
		}
		int num = genome.loci?.Count ?? 0;
		GenomeInstance genomeInstance = new GenomeInstance
		{
			genomeId = genome.genomeId,
			sex = (isMale ? GeneticSex.Male : GeneticSex.Female),
			paternalAlleles = new int[num],
			maternalAlleles = new int[num],
			mutationCount = (legacy?.mutationCount ?? 0),
			inbreedingCoefficient = (legacy?.inbreedingCoefficient ?? 0f)
		};
		for (int i = 0; i < num; i++)
		{
			genomeInstance.paternalAlleles[i] = -1;
			genomeInstance.maternalAlleles[i] = -1;
		}
		for (int j = 0; j < num && j < 6; j++)
		{
			GenomeLocusDef genomeLocusDef = genome.loci[j];
			LocusPair locusPair = legacy?.GetLocus(ToDnaLocus(j)) ?? new LocusPair(Allele.Dominant, Allele.Dominant);
			int paternal = ResolveAlleleIndex(genomeLocusDef, locusPair.paternal);
			int maternal = ResolveAlleleIndex(genomeLocusDef, locusPair.maternal);
			ApplyInheritanceRules(genomeLocusDef.inheritance, genomeInstance.sex, ref paternal, ref maternal);
			genomeInstance.paternalAlleles[j] = paternal;
			genomeInstance.maternalAlleles[j] = maternal;
		}
		return genomeInstance;
	}

	public static DnaData ToLegacyData(GenomeInstance instance, GenomeDef genome)
	{
		DnaData dnaData = new DnaData();
		if (instance == null)
		{
			return dnaData;
		}
		if (genome == null)
		{
			genome = GeneticsCatalog.ResolveGenome(instance.genomeId);
		}
		dnaData.genome = instance;
		dnaData.mutationCount = instance.mutationCount;
		dnaData.inbreedingCoefficient = instance.inbreedingCoefficient;
		int num = genome.loci?.Count ?? 0;
		for (int i = 0; i < num && i < 6; i++)
		{
			GenomeLocusDef locus = genome.loci[i];
			int num2 = instance.PaternalAt(i);
			int num3 = instance.MaternalAt(i);
			if (num2 < 0)
			{
				num2 = num3;
			}
			if (num3 < 0)
			{
				num3 = num2;
			}
			if (num2 < 0)
			{
				num2 = (num3 = 0);
			}
			dnaData.SetLocus(ToDnaLocus(i), new LocusPair(ToLegacyAllele(GeneticsCatalog.IsDominantAllele(locus, num2)), ToLegacyAllele(GeneticsCatalog.IsDominantAllele(locus, num3))));
		}
		return dnaData;
	}

	public static int ResolveAlleleIndex(GenomeLocusDef locus, Allele allele)
	{
		if (locus?.alleles == null || locus.alleles.Count == 0)
		{
			return -1;
		}
		int num = GeneticsCatalog.FindDominantAlleleIndex(locus);
		if (num < 0)
		{
			num = 0;
		}
		if (allele == Allele.Dominant)
		{
			return num;
		}
		for (int i = 0; i < locus.alleles.Count; i++)
		{
			if (i != num)
			{
				return i;
			}
		}
		return num;
	}

	public static Allele ToLegacyAllele(bool isDominant)
	{
		if (!isDominant)
		{
			return Allele.Recessive;
		}
		return Allele.Dominant;
	}

	public static void ApplyInheritanceRules(GeneticInheritance inheritance, GeneticSex sex, ref int paternal, ref int maternal)
	{
		switch (inheritance)
		{
		case GeneticInheritance.XLinked:
			if (sex == GeneticSex.Male)
			{
				paternal = -1;
			}
			break;
		case GeneticInheritance.YLinked:
			if (sex == GeneticSex.Male)
			{
				maternal = -1;
			}
			else
			{
				paternal = (maternal = -1);
			}
			break;
		case GeneticInheritance.Mitochondrial:
			paternal = -1;
			break;
		}
	}
}
}
