using System;
using MyriadCreation.Content.Definitions;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Genetics
{
public static class GeneticsCatalog
{
	public const string DefaultGenomeId = "genome_human";

	public const string DefaultSpeciesId = "species_human";

	public const float LegacyEquivalentMutationRate = 0.00375f;

	public const float LegacyEquivalentTalentMutationRate = 0.0075f;

	private static readonly Dictionary<string, GenomeDef> _genomeOverrides = new Dictionary<string, GenomeDef>();

	private static readonly Dictionary<string, SpeciesDef> _speciesOverrides = new Dictionary<string, SpeciesDef>();

	private static GenomeDef _defaultHumanGenome;

	private static SpeciesDef _humanSpecies;

	public static void RegisterGenome(GenomeDef genome)
	{
		if (genome != null && !string.IsNullOrEmpty(genome.genomeId))
		{
			_genomeOverrides[genome.genomeId] = genome;
		}
	}

	public static void RegisterSpecies(SpeciesDef species)
	{
		if (species != null && !string.IsNullOrEmpty(species.speciesId))
		{
			_speciesOverrides[species.speciesId] = species;
		}
	}

	public static void ResetDefaults()
	{
		_genomeOverrides.Clear();
		_speciesOverrides.Clear();
		_defaultHumanGenome = null;
		_humanSpecies = null;
	}

	public static bool TryGetGenome(string genomeId, out GenomeDef genome)
	{
		genome = null;
		if (string.IsNullOrEmpty(genomeId))
		{
			return false;
		}
		if (_genomeOverrides.TryGetValue(genomeId, out genome))
		{
			return true;
		}
		if (genomeId == "genome_human")
		{
			genome = GetDefaultHumanGenome();
			return true;
		}
		return false;
	}

	public static bool TryGetSpecies(string speciesId, out SpeciesDef species)
	{
		species = null;
		if (string.IsNullOrEmpty(speciesId))
		{
			return false;
		}
		if (_speciesOverrides.TryGetValue(speciesId, out species))
		{
			return true;
		}
		if (speciesId == "species_human")
		{
			species = GetDefaultSpecies();
			return true;
		}
		return false;
	}

	public static GenomeDef ResolveGenome(string genomeId)
	{
		if (TryGetGenome(genomeId, out var genome))
		{
			return genome;
		}
		return GetDefaultHumanGenome();
	}

	public static SpeciesDef ResolveSpecies(string speciesId)
	{
		if (TryGetSpecies(speciesId, out var species))
		{
			return species;
		}
		return GetDefaultSpecies();
	}

	public static GenomeDef GetDefaultHumanGenome()
	{
		return _defaultHumanGenome ?? (_defaultHumanGenome = CreateHumanGenome());
	}

	public static SpeciesDef GetDefaultSpecies()
	{
		return _humanSpecies ?? (_humanSpecies = CreateHumanSpecies());
	}

	public static GenomeDef CreateHumanGenome()
	{
		GenomeDef genomeDef = new GenomeDef
		{
			genomeId = "genome_human",
			speciesId = "species_human",
			chromosomeCount = 3,
			loci = new List<GenomeLocusDef>
			{
				Locus("longevity", 0, 0f, "lifespan", 1f, 0f, 0.00375f),
				Locus("intelligence", 0, 30f, "intelligence", 1f, 0f, 0.00375f),
				Locus("martial", 1, 0f, "martial", 1f, 0f, 0.00375f),
				Locus("resistance", 1, 25f, "resistance", 1f, 0f, 0.00375f),
				Locus("appearance", 2, 0f, "appearance", 1f, 0f, 0.00375f),
				Locus("talent_defect", 2, 20f, "talent", 1f, 0f, 0.0075f),
				DiscreteLocus("body_plan_wing", 2, 40f, "wingType", "none", "none", 1E-06f),
				DiscreteLocus("body_plan_horn", 2, 45f, "hornType", "none", "none", 1E-06f),
				DiscreteLocus("body_plan_tail", 2, 50f, "tailType", "none", "none", 1E-06f),
				DiscreteLocus("body_plan_posture", 2, 55f, "posture", "biped", "biped", 1E-06f),
				DiscreteLocus("integument_skin", 2, 60f, "skinType", "skin", "skin", 1E-06f),
				Locus("integument_insulation", 1, 55f, "insulation", 0.2f, 0f, 1E-06f),
				Locus("integument_armor", 1, 65f, "naturalArmor", 0.1f, 0f, 1E-06f),
				Locus("metabolism_size", 0, 60f, "size", 0.3f, 0f, 1E-06f),
				Locus("metabolism_diet", 0, 70f, "dietCarnivore", 0.2f, 0f, 1E-06f),
				Locus("metabolism_temp", 1, 75f, "tempToleranceC", 5f, 0f, 1E-06f)
			}
		};
		GenomeLocusDef genomeLocusDef = genomeDef.loci.Find((GenomeLocusDef l) => l.locusId == "appearance");
		if (genomeLocusDef != null)
		{
			genomeLocusDef.alleles[0].effects.Add(new AlleleTraitEffect
			{
				traitId = "appearance",
				appearanceChannel = AppearanceChannel.SkinPigment,
				appearanceAmount = 0.5f
			});
			genomeLocusDef.alleles[0].effects.Add(new AlleleTraitEffect
			{
				traitId = "appearance",
				appearanceChannel = AppearanceChannel.HairPigment,
				appearanceAmount = 0.5f
			});
			genomeLocusDef.alleles[0].effects.Add(new AlleleTraitEffect
			{
				traitId = "appearance",
				appearanceChannel = AppearanceChannel.EyePigment,
				appearanceAmount = 0.25f
			});
		}
		return genomeDef;
	}

	public static SpeciesDef CreateHumanSpecies()
	{
		return new SpeciesDef
		{
			speciesId = "species_human",
			name = "人类",
			category = SpeciesCategory.Sapient,
			genomeId = "genome_human",
			bodySize = 1f,
			lifespanYears = 75f,
			reproductionRate = 1f,
			dietHerbivore = 0.8f,
			dietCarnivore = 0.2f,
			habitatTempMinC = -20f,
			habitatTempMaxC = 45f,
			habitatHumidityMinPct = 0f,
			habitatHumidityMaxPct = 100f,
			behaviorTags = new List<string> { "social", "tool_user" }
		};
	}

	public static List<SpeciesDef> CreateDefaultSpecies()
	{
		return new List<SpeciesDef>
		{
			CreateHumanSpecies(),
			new SpeciesDef
			{
				speciesId = "species_deer",
				name = "鹿",
				category = SpeciesCategory.Animal,
				genomeId = "genome_deer",
				bodySize = 0.8f,
				lifespanYears = 12f,
				reproductionRate = 1.2f,
				dietHerbivore = 1f,
				dietCarnivore = 0f,
				behaviorTags = new List<string> { "herbivore", "herd" }
			},
			new SpeciesDef
			{
				speciesId = "species_wolf",
				name = "狼",
				category = SpeciesCategory.Animal,
				genomeId = "genome_wolf",
				bodySize = 0.6f,
				lifespanYears = 10f,
				reproductionRate = 0.8f,
				dietHerbivore = 0f,
				dietCarnivore = 1f,
				behaviorTags = new List<string> { "carnivore", "pack" }
			},
			new SpeciesDef
			{
				speciesId = "species_oak",
				name = "橡树",
				category = SpeciesCategory.Plant,
				genomeId = "genome_oak",
				bodySize = 5f,
				lifespanYears = 300f,
				reproductionRate = 0.3f,
				dietHerbivore = 0f,
				dietCarnivore = 0f,
				behaviorTags = new List<string> { "tree", "habitat" }
			}
		};
	}

	public static int FindDominantAlleleIndex(GenomeLocusDef locus)
	{
		if (locus?.alleles == null || locus.alleles.Count == 0)
		{
			return -1;
		}
		for (int i = 0; i < locus.alleles.Count; i++)
		{
			if (locus.alleles[i] != null && locus.alleles[i].symbol == "A")
			{
				return i;
			}
		}
		for (int j = 0; j < locus.alleles.Count; j++)
		{
			string text = locus.alleles[j]?.alleleId;
			if (!string.IsNullOrEmpty(text) && text.EndsWith("_plus", StringComparison.Ordinal))
			{
				return j;
			}
		}
		return 0;
	}

	public static bool IsDominantAllele(GenomeLocusDef locus, int alleleIndex)
	{
		if (locus?.alleles == null || locus.alleles.Count == 0 || alleleIndex < 0)
		{
			return false;
		}
		int num = FindDominantAlleleIndex(locus);
		if (num >= 0)
		{
			return alleleIndex == num;
		}
		return false;
	}

	public static GenomeDef CloneWithDominantFrequencies(GenomeDef template, IReadOnlyList<float> dominantFrequencies)
	{
		if (template == null)
		{
			return CreateHumanGenome();
		}
		if (dominantFrequencies == null || dominantFrequencies.Count == 0)
		{
			return template;
		}
		GenomeDef genomeDef = new GenomeDef
		{
			genomeId = template.genomeId,
			speciesId = template.speciesId,
			chromosomeCount = template.chromosomeCount,
			loci = new List<GenomeLocusDef>(template.loci.Count)
		};
		for (int i = 0; i < template.loci.Count; i++)
		{
			GenomeLocusDef genomeLocusDef = template.loci[i];
			GenomeLocusDef genomeLocusDef2 = new GenomeLocusDef
			{
				locusId = genomeLocusDef.locusId,
				chromosome = genomeLocusDef.chromosome,
				mapPositionCm = genomeLocusDef.mapPositionCm,
				inheritance = genomeLocusDef.inheritance,
				mutationRatePerGeneration = genomeLocusDef.mutationRatePerGeneration,
				alleles = new List<AlleleDef>(genomeLocusDef.alleles.Count)
			};
			int num = FindDominantAlleleIndex(genomeLocusDef);
			bool flag = i < dominantFrequencies.Count;
			float num2 = (flag ? Clamp01(dominantFrequencies[i]) : 0.5f);
			for (int j = 0; j < genomeLocusDef.alleles.Count; j++)
			{
				AlleleDef alleleDef = genomeLocusDef.alleles[j];
				AlleleDef alleleDef2 = new AlleleDef
				{
					alleleId = alleleDef.alleleId,
					symbol = alleleDef.symbol,
					isLossOfFunction = alleleDef.isLossOfFunction,
					isLethalWhenHomozygous = alleleDef.isLethalWhenHomozygous,
					defaultFrequency = alleleDef.defaultFrequency,
					effects = new List<AlleleTraitEffect>(alleleDef.effects.Count)
				};
				foreach (AlleleTraitEffect effect in alleleDef.effects)
				{
					alleleDef2.effects.Add(new AlleleTraitEffect
					{
						traitId = effect.traitId,
						additiveEffect = effect.additiveEffect,
						dominanceDeviation = effect.dominanceDeviation,
						environmentalSensitivity = effect.environmentalSensitivity
					});
				}
				if (flag)
				{
					if (j == num)
					{
						alleleDef2.defaultFrequency = num2;
					}
					else if (genomeLocusDef.alleles.Count == 2)
					{
						alleleDef2.defaultFrequency = 1f - num2;
					}
				}
				genomeLocusDef2.alleles.Add(alleleDef2);
			}
			genomeDef.loci.Add(genomeLocusDef2);
		}
		return genomeDef;
	}

	private static GenomeLocusDef DiscreteLocus(string id, int chromosome, float cM, string discreteTraitId, string plusValue, string minusValue, float mutationRate)
	{
		return new GenomeLocusDef
		{
			locusId = id,
			chromosome = chromosome,
			mapPositionCm = cM,
			mutationRatePerGeneration = mutationRate,
			alleles = new List<AlleleDef>
			{
				new AlleleDef
				{
					alleleId = id + "_plus",
					symbol = "A",
					defaultFrequency = 0.5f,
					effects = new List<AlleleTraitEffect>
					{
						new AlleleTraitEffect { discreteTraitId = discreteTraitId, discreteValue = plusValue }
					}
				},
				new AlleleDef
				{
					alleleId = id + "_minus",
					symbol = "a",
					defaultFrequency = 0.5f,
					effects = new List<AlleleTraitEffect>
					{
						new AlleleTraitEffect { discreteTraitId = discreteTraitId, discreteValue = minusValue }
					}
				}
			}
		};
	}

	private static GenomeLocusDef Locus(string id, int chromosome, float cM, string traitId, float effect, float dominanceDeviation, float mutationRate)
	{
		return new GenomeLocusDef
		{
			locusId = id,
			chromosome = chromosome,
			mapPositionCm = cM,
			mutationRatePerGeneration = mutationRate,
			alleles = new List<AlleleDef>
			{
				new AlleleDef
				{
					alleleId = id + "_plus",
					symbol = "A",
					defaultFrequency = 0.5f,
					effects = new List<AlleleTraitEffect>
					{
						new AlleleTraitEffect
						{
							traitId = traitId,
							additiveEffect = effect,
							dominanceDeviation = dominanceDeviation
						}
					}
				},
				new AlleleDef
				{
					alleleId = id + "_minus",
					symbol = "a",
					defaultFrequency = 0.5f,
					effects = new List<AlleleTraitEffect>
					{
						new AlleleTraitEffect
						{
							traitId = traitId,
							additiveEffect = 0f
						}
					}
				}
			}
		};
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