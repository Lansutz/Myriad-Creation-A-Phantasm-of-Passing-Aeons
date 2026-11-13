using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class GeneticsContentPack
{
	public List<GenomeDef> genomes = new List<GenomeDef>();

	public List<SpeciesDef> species = new List<SpeciesDef>();

	public List<AppearanceStyleDef> appearanceStyles = new List<AppearanceStyleDef>();
}
}
