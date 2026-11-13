using System;

namespace MyriadCreation.Simulation.Genetics
{
[Serializable]
public class GenomeInstance
{
	public string genomeId = "";

	public GeneticSex sex;

	public int[] paternalAlleles = Array.Empty<int>();

	public int[] maternalAlleles = Array.Empty<int>();

	public float inbreedingCoefficient;

	public int mutationCount;

	public int generation;

	public int ploidy = 2;

	public int PaternalAt(int locus)
	{
		if (locus < 0 || locus >= paternalAlleles.Length)
		{
			return -1;
		}
		return paternalAlleles[locus];
	}

	public int MaternalAt(int locus)
	{
		if (locus < 0 || locus >= maternalAlleles.Length)
		{
			return -1;
		}
		return maternalAlleles[locus];
	}
}
}
