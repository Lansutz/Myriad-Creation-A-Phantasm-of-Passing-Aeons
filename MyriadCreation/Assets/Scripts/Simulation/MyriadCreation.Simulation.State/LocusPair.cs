using System;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public struct LocusPair(Allele p, Allele m)
{
	public Allele paternal = p;

	public Allele maternal = m;

	public bool IsHomozygousDominant
	{
		get
		{
			if (paternal == Allele.Dominant)
			{
				return maternal == Allele.Dominant;
			}
			return false;
		}
	}

	public bool IsHeterozygous => paternal != maternal;

	public bool IsHomozygousRecessive
	{
		get
		{
			if (paternal == Allele.Recessive)
			{
				return maternal == Allele.Recessive;
			}
			return false;
		}
	}
}
}
