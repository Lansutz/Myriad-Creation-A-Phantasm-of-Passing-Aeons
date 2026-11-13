using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.Genetics;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using Random = System.Random;

namespace MyriadCreation.Simulation.Systems
{
public static class DnaSystem
{
	public const float MutationChance = 0.0075f;

	public const float TalentDefectMutationChance = 0.015f;

	public const float TalentChanceAA = 0.25f;

	public const float DefectChanceAA = 0.9f;

	public const float OffsetRange = 15f;

	public static readonly DnaLocus[] ActiveLoci = new DnaLocus[6]
	{
		DnaLocus.Longevity,
		DnaLocus.Intelligence,
		DnaLocus.Martial,
		DnaLocus.Resistance,
		DnaLocus.Appearance,
		DnaLocus.TalentDefect
	};

	private static readonly List<TalentDefectDef> _talentDefs = new List<TalentDefectDef>
	{
		new TalentDefectDef
		{
			id = "talent_photographic",
			name = "过目不忘",
			isTalent = true,
			stat = "learning",
			amount = 5f,
			description = "记忆超群，见闻过目成诵"
		},
		new TalentDefectDef
		{
			id = "talent_divine_strength",
			name = "神力",
			isTalent = true,
			stat = "martial",
			amount = 5f,
			description = "天生神力，万夫莫当"
		},
		new TalentDefectDef
		{
			id = "talent_iron_body",
			name = "铁躯",
			isTalent = true,
			stat = "lifespan",
			amount = 10f,
			description = "体格强韧，寿数绵长"
		},
		new TalentDefectDef
		{
			id = "talent_keen_mind",
			name = "慧心",
			isTalent = true,
			stat = "learning",
			amount = 3f,
			description = "颖悟绝伦，触类旁通"
		}
	};

	private static readonly List<TalentDefectDef> _defectDefs = new List<TalentDefectDef>
	{
		new TalentDefectDef
		{
			id = "defect_frail",
			name = "先天体弱",
			isTalent = false,
			stat = "lifespan",
			amount = -15f,
			description = "胎里带的孱弱，寿元受损"
		},
		new TalentDefectDef
		{
			id = "defect_feeblemind",
			name = "痴愚",
			isTalent = false,
			stat = "learning",
			amount = -20f,
			description = "心智蒙昧，难以开化"
		},
		new TalentDefectDef
		{
			id = "defect_hemophilia",
			name = "血友病",
			isTalent = false,
			stat = "lifespan",
			amount = -10f,
			description = "凝血障碍，伤病易危"
		},
		new TalentDefectDef
		{
			id = "defect_pale",
			name = "白化",
			isTalent = false,
			stat = "appearance",
			amount = 0f,
			description = "肤发无色，畏光避日"
		}
	};

	private static readonly string[] _appearanceAA = new string[4] { "肤色深邃", "体魄强健", "轮廓鲜明", "目光如炬" };

	private static readonly string[] _appearanceAa = new string[4] { "肤色匀称", "体型中等", "轮廓分明", "眉目清朗" };

	private static readonly string[] _appearanceaa = new string[4] { "肤色浅淡", "身形纤细", "轮廓柔和", "形容清减" };

	public static IReadOnlyList<TalentDefectDef> GetTalentDefs()
	{
		if (ContentRegistry.IsInitialized)
		{
			List<TalentDefectDef> list = new List<TalentDefectDef>();
			{
				foreach (TalentDefectDef item in ContentResolvers.TalentDefects.All)
				{
					if (item.isTalent)
					{
						list.Add(item);
					}
				}
				return list;
			}
		}
		return _talentDefs;
	}

	public static IReadOnlyList<TalentDefectDef> GetDefectDefs()
	{
		if (ContentRegistry.IsInitialized)
		{
			List<TalentDefectDef> list = new List<TalentDefectDef>();
			{
				foreach (TalentDefectDef item in ContentResolvers.TalentDefects.All)
				{
					if (!item.isTalent)
					{
						list.Add(item);
					}
				}
				return list;
			}
		}
		return _defectDefs;
	}

	public static TalentDefectDef FindDef(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		if (ContentRegistry.IsInitialized && ContentResolvers.TalentDefects.TryGet(id, out var value))
		{
			return value;
		}
		foreach (TalentDefectDef talentDef in _talentDefs)
		{
			if (talentDef.id == id)
			{
				return talentDef;
			}
		}
		foreach (TalentDefectDef defectDef in _defectDefs)
		{
			if (defectDef.id == id)
			{
				return defectDef;
			}
		}
		return null;
	}

	public static GenomeInstance GenerateGenome(RaceData race, bool isMale, Random rng = null)
	{
		return GenerateGenome(race, isMale ? GeneticSex.Male : GeneticSex.Female, rng);
	}

	public static GenomeInstance GenerateGenome(RaceData race, GeneticSex sex, Random rng = null)
	{
		return GeneticsEngine.CreateFounder(GeneticsCatalog.ResolveGenome(race?.GetGenomeId()), sex, rng ?? new Random());
	}

	public static GenomeInstance InheritGenome(GenomeInstance father, GenomeInstance mother, RaceData race, bool childIsMale, float inbreeding, bool allowMutation = true, Random rng = null)
	{
		return InheritGenome(father, mother, race, childIsMale ? GeneticSex.Male : GeneticSex.Female, inbreeding, allowMutation, rng);
	}

	public static GenomeInstance InheritGenome(GenomeInstance father, GenomeInstance mother, RaceData race, GeneticSex childSex, float inbreeding, bool allowMutation = true, Random rng = null)
	{
		GenomeInstance genomeInstance = GeneticsEngine.Reproduce(GeneticsCatalog.ResolveGenome(race?.GetGenomeId()), father, mother, childSex, rng ?? new Random(), allowMutation);
		genomeInstance.inbreedingCoefficient = Mathf.Clamp01(inbreeding);
		return genomeInstance;
	}

	public static DnaData GenerateRandom(RaceData race)
	{
		DnaData dnaData = new DnaData();
		DnaLocus[] activeLoci = ActiveLoci;
		foreach (DnaLocus locus in activeLoci)
		{
			float num = race?.GetLocusAFrequency(locus) ?? 0.5f;
			LocusPair pair = new LocusPair((!(UnityEngine.Random.value < num)) ? Allele.Recessive : Allele.Dominant, (!(UnityEngine.Random.value < num)) ? Allele.Recessive : Allele.Dominant);
			dnaData.SetLocus(locus, pair);
		}
		return dnaData;
	}

	public static DnaData Inherit(DnaData father, DnaData mother, RaceData race, float inbreeding, bool allowMutation = true)
	{
		DnaData dnaData = new DnaData
		{
			inbreedingCoefficient = Mathf.Clamp01(inbreeding)
		};
		DnaLocus[] activeLoci = ActiveLoci;
		foreach (DnaLocus dnaLocus in activeLoci)
		{
			Allele allele = ((father != null) ? PickAllele(father.GetLocus(dnaLocus)) : RandomAllele(race, dnaLocus));
			Allele allele2 = ((mother != null) ? PickAllele(mother.GetLocus(dnaLocus)) : RandomAllele(race, dnaLocus));
			if (dnaData.inbreedingCoefficient > 0f && UnityEngine.Random.value < dnaData.inbreedingCoefficient)
			{
				if (UnityEngine.Random.value < 0.5f)
				{
					allele = allele2;
				}
				else
				{
					allele2 = allele;
				}
			}
			if (allowMutation)
			{
				float num = ((dnaLocus == DnaLocus.TalentDefect) ? 0.015f : 0.0075f);
				if (UnityEngine.Random.value < num)
				{
					if (UnityEngine.Random.value < 0.5f)
					{
						allele = Flip(allele);
					}
					else
					{
						allele2 = Flip(allele2);
					}
					dnaData.mutationCount++;
				}
			}
			dnaData.SetLocus(dnaLocus, new LocusPair(allele, allele2));
		}
		return dnaData;
	}

	public static DnaExpression ComputeExpression(DnaData dna, RaceData race)
	{
		DnaExpression expr = default;
		if (dna == null)
		{
			return expr;
		}
		expr.longevityOffsetYears = ComputeOffset(dna, DnaLocus.Longevity, race?.lifespanRangeYears ?? 15f);
		expr.intelligenceOffset = ComputeOffset(dna, DnaLocus.Intelligence, 15f);
		expr.martialOffset = ComputeOffset(dna, DnaLocus.Martial, 15f);
		expr.resistanceOffset = ComputeOffset(dna, DnaLocus.Resistance, 15f);
		expr.appearanceTag = ComputeAppearance(dna);
		ApplyTalentDefect(dna, ref expr);
		return expr;
	}

	private static float ComputeOffset(DnaData dna, DnaLocus locus, float range)
	{
		LocusPair locus2 = dna.GetLocus(locus);
		if (locus2.IsHomozygousDominant)
		{
			return range * UnityEngine.Random.Range(0.8f, 1f);
		}
		if (locus2.IsHeterozygous)
		{
			return range * UnityEngine.Random.Range(0.3f, 0.6f);
		}
		return (0f - range) * UnityEngine.Random.Range(0.5f, 1f);
	}

	private static string ComputeAppearance(DnaData dna)
	{
		LocusPair locus = dna.GetLocus(DnaLocus.Appearance);
		string[] array;
		if (locus.IsHomozygousDominant)
		{
			array = _appearanceAA;
		}
		else
		{
			array = (locus.IsHeterozygous ? _appearanceAa : _appearanceaa);
		}
		return array[UnityEngine.Random.Range(0, array.Length)];
	}

	private static void ApplyTalentDefect(DnaData dna, ref DnaExpression expr)
	{
		LocusPair locus = dna.GetLocus(DnaLocus.TalentDefect);
		IReadOnlyList<TalentDefectDef> talentDefs = GetTalentDefs();
		IReadOnlyList<TalentDefectDef> defectDefs = GetDefectDefs();
		if (locus.IsHomozygousDominant)
		{
			if (UnityEngine.Random.value < 0.25f && talentDefs.Count > 0)
			{
				expr.talentId = talentDefs[UnityEngine.Random.Range(0, talentDefs.Count)].id;
			}
		}
		else if (locus.IsHeterozygous)
		{
			expr.carriesDefect = true;
		}
		else if (defectDefs.Count > 0 && UnityEngine.Random.value < 0.9f)
		{
			expr.defectId = defectDefs[UnityEngine.Random.Range(0, defectDefs.Count)].id;
		}
		else if (talentDefs.Count > 0)
		{
			expr.talentId = talentDefs[UnityEngine.Random.Range(0, talentDefs.Count)].id;
		}
	}

	public static float CalculateInbreeding(CharacterData a, CharacterData b, Dictionary<int, CharacterData> characters)
	{
		if (a == null || b == null || a.characterId == b.characterId)
		{
			return 0f;
		}
		Dictionary<int, int> ancestry = GetAncestry(a, characters);
		Dictionary<int, int> ancestry2 = GetAncestry(b, characters);
		float num = 0f;
		foreach (KeyValuePair<int, int> item in ancestry)
		{
			if (ancestry2.TryGetValue(item.Key, out var value))
			{
				int value2 = item.Value;
				int num2 = value2 + value;
				float num3 = ((num2 <= 1) ? 0.25f : (num2 switch
				{
					2 => (value2 == 1 && value == 1 && HasSameParents(a, b)) ? 0.25f : 0.125f, 
					3 => 0.125f, 
					4 => 0.0625f, 
					_ => 1f / 32f, 
				}));
				if (num3 > num)
				{
					num = num3;
				}
			}
		}
		return num;
	}

	private static Dictionary<int, int> GetAncestry(CharacterData c, Dictionary<int, CharacterData> characters)
	{
		Dictionary<int, int> result = new Dictionary<int, int>();
		CollectAncestors(c, characters, 0, result);
		return result;
	}

	private static void CollectAncestors(CharacterData c, Dictionary<int, CharacterData> characters, int depth, Dictionary<int, int> result)
	{
		if (c != null && depth <= 2 && characters != null)
		{
			if (!result.TryGetValue(c.characterId, out var value) || depth < value)
			{
				result[c.characterId] = depth;
			}
			if (c.fatherId >= 0 && characters.TryGetValue(c.fatherId, out var value2))
			{
				CollectAncestors(value2, characters, depth + 1, result);
			}
			if (c.motherId >= 0 && characters.TryGetValue(c.motherId, out var value3))
			{
				CollectAncestors(value3, characters, depth + 1, result);
			}
		}
	}

	private static bool HasSameParents(CharacterData a, CharacterData b)
	{
		if (a.fatherId >= 0 && a.fatherId == b.fatherId && a.motherId >= 0)
		{
			return a.motherId == b.motherId;
		}
		return false;
	}

	private static Allele PickAllele(LocusPair pair)
	{
		if (!(UnityEngine.Random.value < 0.5f))
		{
			return pair.maternal;
		}
		return pair.paternal;
	}

	private static Allele RandomAllele(RaceData race, DnaLocus locus)
	{
		float num = race?.GetLocusAFrequency(locus) ?? 0.5f;
		if (!(UnityEngine.Random.value < num))
		{
			return Allele.Recessive;
		}
		return Allele.Dominant;
	}

	private static Allele Flip(Allele a)
	{
		if (a != Allele.Dominant)
		{
			return Allele.Dominant;
		}
		return Allele.Recessive;
	}
}
}
