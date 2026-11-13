using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.Genetics;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class DnaData
{
	[NonSerialized]
	public Dictionary<DnaLocus, LocusPair> loci = new Dictionary<DnaLocus, LocusPair>();

	public int mutationCount;

	public float inbreedingCoefficient;

	[NonSerialized]
	public GenomeInstance genome;

	public LocusPair GetLocus(DnaLocus locus)
	{
		if (loci != null && loci.TryGetValue(locus, out var value))
		{
			return value;
		}
		return new LocusPair(Allele.Dominant, Allele.Dominant);
	}

	public void SetLocus(DnaLocus locus, LocusPair pair)
	{
		if (loci == null)
		{
			loci = new Dictionary<DnaLocus, LocusPair>();
		}
		loci[locus] = pair;
	}

	public GenomeInstance ToGenomeInstance(RaceData race = null, bool isMale = false)
	{
		GenomeDef genomeDef = GeneticsCatalog.ResolveGenome(race?.GetGenomeId());
		return LegacyDnaAdapter.ToGenomeInstance(this, genomeDef, isMale);
	}

	public GenomeInstance GetGenome(RaceData race = null, bool isMale = false)
	{
		if (genome == null || genome.paternalAlleles == null || genome.maternalAlleles == null)
		{
			genome = ToGenomeInstance(race, isMale);
		}
		return genome;
	}
}
}
