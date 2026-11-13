using System;

namespace MyriadCreation.Simulation.Genetics
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
