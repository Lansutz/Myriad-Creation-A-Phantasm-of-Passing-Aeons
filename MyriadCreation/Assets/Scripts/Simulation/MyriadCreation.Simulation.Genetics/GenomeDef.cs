using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Genetics
{
[Serializable]
public class GenomeDef
{
	public string genomeId = "";

	public string speciesId = "";

	public int chromosomeCount = 1;

	public SexChromosomeSystemDef sexSystem = new SexChromosomeSystemDef();

	public AppearanceStyleDef appearance = new AppearanceStyleDef();

	public List<GenomeLocusDef> loci = new List<GenomeLocusDef>();
}
}
