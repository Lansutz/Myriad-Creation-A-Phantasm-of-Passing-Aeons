using System;
using System.Collections.Generic;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Genetics
{
public static class GeneticsEngine
{
	public static float RecombinationFraction(float mapDistanceCm)
	{
		if (mapDistanceCm <= 0f)
		{
			return 0f;
		}
		return 0.5f * (1f - (float)Math.Exp(-2f * mapDistanceCm / 100f));
	}

	public static float ExpectedHeterozygosity(float alleleFrequency)
	{
		float num = Clamp01(alleleFrequency);
		return 2f * num * (1f - num);
	}

	public static GenomeInstance CreateFounder(GenomeDef genome, GeneticSex sex, Random rng)
	{
		if (genome == null)
		{
			throw new ArgumentNullException("genome");
		}
		if (rng == null)
		{
			rng = new Random(1);
		}
		int count = genome.loci.Count;
		SexDeterminationSystem sexDeterminationSystem = genome.sexSystem?.system ?? SexDeterminationSystem.XY;
		bool flag = sexDeterminationSystem == SexDeterminationSystem.Haplodiploid && IsMale(sex);
		GenomeInstance genomeInstance = new GenomeInstance
		{
			genomeId = genome.genomeId,
			sex = sex,
			ploidy = (flag ? 1 : 2),
			paternalAlleles = new int[count],
			maternalAlleles = new int[count]
		};
		for (int i = 0; i < count; i++)
		{
			GenomeLocusDef genomeLocusDef = genome.loci[i];
			int paternal = PickAllele(genomeLocusDef, rng);
			int maternal = PickAllele(genomeLocusDef, rng);
			ApplyFounderSlots(sexDeterminationSystem, sex, genomeLocusDef.inheritance, ref paternal, ref maternal, rng);
			if (flag)
			{
				genomeInstance.paternalAlleles[i] = -1;
				genomeInstance.maternalAlleles[i] = ((paternal >= 0) ? paternal : maternal);
			}
			else
			{
				genomeInstance.paternalAlleles[i] = paternal;
				genomeInstance.maternalAlleles[i] = maternal;
			}
		}
		return genomeInstance;
	}

	public static GenomeInstance CreateFounder(GenomeDef genome, Random rng)
	{
		return CreateFounder(genome, DetermineSex(genome, rng), rng);
	}

	public static GeneticSex DetermineSex(GenomeDef genome, Random rng, float? environmentalBias = null)
	{
		if (rng == null)
		{
			rng = new Random(1);
		}
		switch ((genome?.sexSystem?.system).GetValueOrDefault())
		{
		case SexDeterminationSystem.Hermaphroditic:
			return GeneticSex.Hermaphrodite;
		case SexDeterminationSystem.Environmental:
		{
			float num = environmentalBias ?? genome?.sexSystem?.environmentalFemaleBias ?? 0.5f;
			if (!(rng.NextDouble() < (double)num))
			{
				return GeneticSex.Male;
			}
			return GeneticSex.Female;
		}
		case SexDeterminationSystem.Haplodiploid:
			if (rng.Next(2) != 0)
			{
				return GeneticSex.Male;
			}
			return GeneticSex.Female;
		default:
			if (rng.Next(2) != 0)
			{
				return GeneticSex.Male;
			}
			return GeneticSex.Female;
		}
	}

	private static void ApplyFounderSlots(SexDeterminationSystem system, GeneticSex sex, GeneticInheritance inheritance, ref int paternal, ref int maternal, Random rng)
	{
		switch (inheritance)
		{
		case GeneticInheritance.XLinked:
			switch (system)
			{
			case SexDeterminationSystem.XY:
			case SexDeterminationSystem.XO:
				if (IsMale(sex))
				{
					paternal = -1;
				}
				break;
			case SexDeterminationSystem.ZW:
				if (IsFemale(sex))
				{
					maternal = -1;
				}
				break;
			}
			break;
		case GeneticInheritance.YLinked:
			switch (system)
			{
			case SexDeterminationSystem.XY:
				if (IsMale(sex))
				{
					maternal = -1;
					break;
				}
				paternal = -1;
				maternal = -1;
				break;
			case SexDeterminationSystem.ZW:
				if (IsFemale(sex))
				{
					paternal = -1;
					break;
				}
				paternal = -1;
				maternal = -1;
				break;
			}
			break;
		case GeneticInheritance.Mitochondrial:
			paternal = -1;
			break;
		}
	}

	public static GenomeInstance Reproduce(GenomeDef genome, GenomeInstance father, GenomeInstance mother, Random rng, bool allowMutation = true)
	{
		return Reproduce(genome, father, mother, DetermineSex(genome, rng), rng, allowMutation);
	}

	public static GenomeInstance Reproduce(GenomeDef genome, GenomeInstance father, GenomeInstance mother, GeneticSex childSex, Random rng, bool allowMutation = true)
	{
		if (genome == null)
		{
			throw new ArgumentNullException("genome");
		}
		if (father == null || mother == null)
		{
			throw new ArgumentNullException("Parent genome is required.");
		}
		if (rng == null)
		{
			rng = new Random(1);
		}
		int count = genome.loci.Count;
		bool flag = (genome.sexSystem?.system ?? SexDeterminationSystem.XY) == SexDeterminationSystem.Haplodiploid && IsMale(childSex);
		int[] paternalAlleles = (flag ? Empty(count) : BuildGamete(genome, father, isFather: true, childSex, rng));
		int[] maternalAlleles = BuildGamete(genome, mother, isFather: false, childSex, rng);
		GenomeInstance genomeInstance = new GenomeInstance
		{
			genomeId = genome.genomeId,
			sex = childSex,
			ploidy = (flag ? 1 : 2),
			paternalAlleles = paternalAlleles,
			maternalAlleles = maternalAlleles,
			inbreedingCoefficient = Clamp01((father.inbreedingCoefficient + mother.inbreedingCoefficient) * 0.5f),
			generation = Math.Max(father.generation, mother.generation) + 1
		};
		if (allowMutation)
		{
			for (int i = 0; i < count; i++)
			{
				GenomeLocusDef genomeLocusDef = genome.loci[i];
				float num = Math.Max(0f, genomeLocusDef.mutationRatePerGeneration);
				if (rng.NextDouble() < (double)num && genomeInstance.paternalAlleles[i] >= 0)
				{
					genomeInstance.paternalAlleles[i] = MutateAllele(genomeLocusDef, genomeInstance.paternalAlleles[i], rng, ref genomeInstance.mutationCount);
				}
				if (rng.NextDouble() < (double)num && genomeInstance.maternalAlleles[i] >= 0)
				{
					genomeInstance.maternalAlleles[i] = MutateAllele(genomeLocusDef, genomeInstance.maternalAlleles[i], rng, ref genomeInstance.mutationCount);
				}
			}
		}
		return genomeInstance;
	}

	public static GenomeInstance ReproduceHybrid(GenomeDef childGenome, GenomeInstance father, GenomeInstance mother, GeneticSex childSex, float introgressionRate, Random rng, bool allowMutation = true)
	{
		return ReproduceHybrid(childGenome, father, mother, childSex, introgressionRate, introgressionRate, rng, allowMutation);
	}

	public static GenomeInstance ReproduceHybrid(GenomeDef childGenome, GenomeInstance father, GenomeInstance mother, GeneticSex childSex, float paternalRate, float maternalRate, Random rng, bool allowMutation = true)
	{
		GenomeInstance genomeInstance = Reproduce(childGenome, father, mother, childSex, rng, allowMutation);
		float num = Math.Max(0f, Math.Min(1f, paternalRate));
		float num2 = Math.Max(0f, Math.Min(1f, maternalRate));
		for (int i = 0; i < genomeInstance.paternalAlleles.Length; i++)
		{
			if (rng.NextDouble() >= (double)num)
			{
				genomeInstance.paternalAlleles[i] = genomeInstance.maternalAlleles[i];
			}
			if (rng.NextDouble() >= (double)num2)
			{
				genomeInstance.maternalAlleles[i] = genomeInstance.paternalAlleles[i];
			}
		}
		return genomeInstance;
	}

	private static int[] Empty(int n)
	{
		int[] array = new int[n];
		for (int i = 0; i < n; i++)
		{
			array[i] = -1;
		}
		return array;
	}

	public static int[] BuildGamete(GenomeDef genome, GenomeInstance parent, bool isFather, GeneticSex childSex, Random rng)
	{
		int count = genome.loci.Count;
		int[] array = new int[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = -1;
		}
		SexDeterminationSystem system = genome.sexSystem?.system ?? SexDeterminationSystem.XY;
		Dictionary<int, List<int>> dictionary = new Dictionary<int, List<int>>();
		for (int j = 0; j < count; j++)
		{
			GenomeLocusDef genomeLocusDef = genome.loci[j];
			if (genomeLocusDef.inheritance == GeneticInheritance.Autosomal)
			{
				if (!dictionary.TryGetValue(genomeLocusDef.chromosome, out var value))
				{
					value = new List<int>();
					dictionary[genomeLocusDef.chromosome] = value;
				}
				value.Add(j);
			}
		}
		foreach (KeyValuePair<int, List<int>> item in dictionary)
		{
			List<int> value2 = item.Value;
			value2.Sort((int a, int b) => genome.loci[a].mapPositionCm.CompareTo(genome.loci[b].mapPositionCm));
			int num = rng.Next(2);
			for (int num2 = 0; num2 < value2.Count; num2++)
			{
				int num3 = value2[num2];
				if (num2 > 0)
				{
					float mapDistanceCm = genome.loci[value2[num2]].mapPositionCm - genome.loci[value2[num2 - 1]].mapPositionCm;
					if (rng.NextDouble() < (double)RecombinationFraction(mapDistanceCm))
					{
						num = 1 - num;
					}
				}
				array[num3] = ((num == 0) ? parent.PaternalAt(num3) : parent.MaternalAt(num3));
			}
		}
		for (int num4 = 0; num4 < count; num4++)
		{
			switch (genome.loci[num4].inheritance)
			{
			case GeneticInheritance.XLinked:
				array[num4] = PassSexLinked(genome, parent, isFather, childSex, num4, rng);
				break;
			case GeneticInheritance.YLinked:
				array[num4] = PassHeterogameticLinked(system, parent, isFather, childSex, num4);
				break;
			case GeneticInheritance.Mitochondrial:
				array[num4] = (isFather ? (-1) : FirstPresent(parent.MaternalAt(num4), parent.PaternalAt(num4)));
				break;
			}
		}
		return array;
	}

	private static int PassSexLinked(GenomeDef genome, GenomeInstance parent, bool isFather, GeneticSex childSex, int locus, Random rng)
	{
		switch (genome.sexSystem?.system ?? SexDeterminationSystem.XY)
		{
		case SexDeterminationSystem.XY:
		case SexDeterminationSystem.XO:
			if (isFather)
			{
				if (!IsFemale(childSex))
				{
					return -1;
				}
				return PickHomolog(parent, locus, rng);
			}
			return PickHomolog(parent, locus, rng);
		case SexDeterminationSystem.ZW:
			if (isFather)
			{
				return PickHomolog(parent, locus, rng);
			}
			if (!IsMale(childSex))
			{
				return -1;
			}
			return FirstPresent(parent.PaternalAt(locus), parent.MaternalAt(locus));
		case SexDeterminationSystem.Haplodiploid:
			if (isFather)
			{
				if (!IsFemale(childSex))
				{
					return -1;
				}
				return PickHomolog(parent, locus, rng);
			}
			return FirstPresent(parent.PaternalAt(locus), parent.MaternalAt(locus));
		default:
			return PickHomolog(parent, locus, rng);
		}
	}

	private static int PassHeterogameticLinked(SexDeterminationSystem system, GenomeInstance parent, bool isFather, GeneticSex childSex, int locus)
	{
		switch (system)
		{
		case SexDeterminationSystem.XY:
			if (!isFather || !IsMale(childSex))
			{
				return -1;
			}
			return FirstPresent(parent.PaternalAt(locus), parent.MaternalAt(locus));
		case SexDeterminationSystem.ZW:
			if (isFather || !IsFemale(childSex))
			{
				return -1;
			}
			return FirstPresent(parent.MaternalAt(locus), parent.PaternalAt(locus));
		default:
			return -1;
		}
	}

	private static int PickHomolog(GenomeInstance parent, int locus, Random rng)
	{
		int num = parent.PaternalAt(locus);
		int num2 = parent.MaternalAt(locus);
		if (num < 0)
		{
			return num2;
		}
		if (num2 < 0)
		{
			return num;
		}
		if (rng.Next(2) != 0)
		{
			return num2;
		}
		return num;
	}

	private static bool IsMale(GeneticSex sex)
	{
		if (sex != GeneticSex.Male)
		{
			return sex == GeneticSex.Drone;
		}
		return true;
	}

	private static bool IsFemale(GeneticSex sex)
	{
		if (sex != GeneticSex.Female && sex != GeneticSex.Worker)
		{
			return sex == GeneticSex.Queen;
		}
		return true;
	}

	public static PhenotypeResult ComputePhenotype(GenomeDef genome, GenomeInstance instance, PhenotypeEnvironment environment = default(PhenotypeEnvironment))
	{
		if (genome == null || instance == null)
		{
			return new PhenotypeResult();
		}
		if (environment.nutrition01 <= 0f && environment.stress01 <= 0f)
		{
			environment = PhenotypeEnvironment.Neutral;
		}
		List<string> list = new List<string>();
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		for (int i = 0; i < genome.loci.Count; i++)
		{
			foreach (AlleleDef allele in genome.loci[i].alleles)
			{
				foreach (AlleleTraitEffect effect in allele.effects)
				{
					if (!string.IsNullOrEmpty(effect.traitId) && !dictionary.ContainsKey(effect.traitId))
					{
						dictionary[effect.traitId] = list.Count;
						list.Add(effect.traitId);
					}
				}
			}
		}
		float[] array = new float[list.Count];
		PhenotypeResult phenotypeResult = new PhenotypeResult
		{
			traitIds = list.ToArray(),
			traitValues = array,
			viable = true
		};
		float envShift = (environment.nutrition01 - 0.5f) * 2f - (environment.stress01 - 0.5f) * 2f;
		for (int j = 0; j < genome.loci.Count; j++)
		{
			GenomeLocusDef genomeLocusDef = genome.loci[j];
			int num = instance.PaternalAt(j);
			int num2 = instance.MaternalAt(j);
			if (num < 0 && num2 < 0)
			{
				continue;
			}
			AlleleDef alleleDef = ((num >= 0 && num < genomeLocusDef.alleles.Count) ? genomeLocusDef.alleles[num] : null);
			AlleleDef alleleDef2 = ((num2 >= 0 && num2 < genomeLocusDef.alleles.Count) ? genomeLocusDef.alleles[num2] : null);
			if (alleleDef != null && alleleDef.isLethalWhenHomozygous && num == num2 && num2 >= 0)
			{
				phenotypeResult.viable = false;
			}
			if (alleleDef != null && alleleDef2 != null && num == num2)
			{
				AddAlleleEffects(alleleDef, 2, dominance: false, envShift, dictionary, array);
				continue;
			}
			if (alleleDef != null && alleleDef2 != null)
			{
				AddAlleleEffects(alleleDef, 1, dominance: false, envShift, dictionary, array);
				AddAlleleEffects(alleleDef2, 1, dominance: false, envShift, dictionary, array);
				AddHeterozygousDominance(alleleDef, alleleDef2, envShift, dictionary, array);
				continue;
			}
			AlleleDef alleleDef3 = alleleDef ?? alleleDef2;
			if (alleleDef3 != null)
			{
				AddAlleleEffects(alleleDef3, 1, dominance: false, envShift, dictionary, array);
			}
		}
		Dictionary<AppearanceChannel, float> dictionary2 = new Dictionary<AppearanceChannel, float>();
		for (int k = 0; k < genome.loci.Count; k++)
		{
			GenomeLocusDef locus = genome.loci[k];
			int num3 = instance.PaternalAt(k);
			int num4 = instance.MaternalAt(k);
			CountAppearance(locus, num3, dictionary2);
			if (num4 != num3)
			{
				CountAppearance(locus, num4, dictionary2);
			}
		}
		List<string> list2 = new List<string>();
		List<float> list3 = new List<float>();
		foreach (KeyValuePair<AppearanceChannel, float> item in dictionary2)
		{
			list2.Add(item.Key.ToString());
			list3.Add(item.Value);
		}
		phenotypeResult.appearanceIds = list2.ToArray();
		phenotypeResult.appearanceValues = list3.ToArray();
		float num5 = 0f;
		for (int l = 0; l < array.Length; l++)
		{
			num5 += array[l];
		}
		phenotypeResult.polygenicScore = ((array.Length != 0) ? (num5 / (float)array.Length) : 0f);
		phenotypeResult.inbreedingDepression = Clamp01(instance.inbreedingCoefficient) * 0.25f;
		phenotypeResult.fitness = (phenotypeResult.viable ? Clamp01(1f - phenotypeResult.inbreedingDepression - Math.Abs(environment.stress01 - 0.5f) * 0.2f) : 0f);
		return phenotypeResult;
	}

	private static void CountAppearance(GenomeLocusDef locus, int alleleIndex, Dictionary<AppearanceChannel, float> appearance)
	{
		if (alleleIndex < 0 || alleleIndex >= locus.alleles.Count)
		{
			return;
		}
		foreach (AlleleTraitEffect effect in locus.alleles[alleleIndex].effects)
		{
			if (effect.appearanceChannel != AppearanceChannel.None && effect.appearanceAmount != 0f)
			{
				appearance.TryGetValue(effect.appearanceChannel, out var value);
				appearance[effect.appearanceChannel] = value + effect.appearanceAmount;
			}
		}
	}

	private static void AddAlleleEffects(AlleleDef allele, int alleleCopies, bool dominance, float envShift, Dictionary<string, int> traitIndex, float[] values)
	{
		if (allele?.effects == null)
		{
			return;
		}
		foreach (AlleleTraitEffect effect in allele.effects)
		{
			if (traitIndex.TryGetValue(effect.traitId, out var value))
			{
				float num = effect.additiveEffect * (float)alleleCopies;
				num += effect.environmentalSensitivity * envShift;
				values[value] += num;
			}
		}
	}

	private static void AddHeterozygousDominance(AlleleDef paternal, AlleleDef maternal, float envShift, Dictionary<string, int> traitIndex, float[] values)
	{
		Dictionary<string, float> dictionary = new Dictionary<string, float>();
		foreach (AlleleTraitEffect effect in paternal.effects)
		{
			dictionary[effect.traitId] = effect.dominanceDeviation;
		}
		foreach (AlleleTraitEffect effect2 in maternal.effects)
		{
			dictionary.TryGetValue(effect2.traitId, out var value);
			dictionary[effect2.traitId] = value + effect2.dominanceDeviation;
		}
		foreach (KeyValuePair<string, float> item in dictionary)
		{
			if (traitIndex.TryGetValue(item.Key, out var value2))
			{
				values[value2] += item.Value * 0.5f;
			}
		}
	}

	private static int MutateAllele(GenomeLocusDef locus, int current, Random rng, ref int mutationCount)
	{
		if (locus.alleles.Count <= 1)
		{
			return current;
		}
		int num = rng.Next(locus.alleles.Count - 1);
		if (num >= current)
		{
			num++;
		}
		mutationCount++;
		return num;
	}

	private static int PickAllele(GenomeLocusDef locus, Random rng)
	{
		if (locus.alleles.Count == 0)
		{
			return -1;
		}
		float num = 0f;
		foreach (AlleleDef allele in locus.alleles)
		{
			num += Math.Max(0f, allele.defaultFrequency);
		}
		if (num <= 0f)
		{
			return rng.Next(locus.alleles.Count);
		}
		double num2 = rng.NextDouble() * (double)num;
		for (int i = 0; i < locus.alleles.Count; i++)
		{
			num2 -= (double)Math.Max(0f, locus.alleles[i].defaultFrequency);
			if (num2 <= 0.0)
			{
				return i;
			}
		}
		return locus.alleles.Count - 1;
	}

	private static int FirstPresent(int a, int b)
	{
		if (a < 0)
		{
			return b;
		}
		return a;
	}

	private static float Clamp01(float value)
	{
		if (!(value < 0f))
		{
			if (!(value > 1f))
			{
				return value;
			}
			return 1f;
		}
		return 0f;
	}
}
}
