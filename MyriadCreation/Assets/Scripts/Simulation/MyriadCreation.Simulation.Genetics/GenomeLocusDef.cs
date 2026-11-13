using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Genetics
{
[Serializable]
public class GenomeLocusDef
{
	public string locusId = "";

	public int chromosome;

	public float mapPositionCm;

	public GeneticInheritance inheritance;

	public float mutationRatePerGeneration = 1E-06f;

	public List<AlleleDef> alleles = new List<AlleleDef>();
}
}
