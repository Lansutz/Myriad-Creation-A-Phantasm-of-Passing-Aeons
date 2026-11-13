using System;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class SexChromosomeSystemDef
{
	public SexDeterminationSystem system;

	public string heterogameticChromosome = "Y";

	public string homogameticChromosome = "X";

	public float environmentalFemaleBias = 0.5f;

	public string description = "";
}
}
