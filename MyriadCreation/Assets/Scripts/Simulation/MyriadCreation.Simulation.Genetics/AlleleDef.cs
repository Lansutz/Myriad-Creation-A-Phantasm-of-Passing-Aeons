using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Genetics
{
[Serializable]
public class AlleleDef
{
	public string alleleId = "";

	public string symbol = "";

	public List<AlleleTraitEffect> effects = new List<AlleleTraitEffect>();

	public bool isLossOfFunction;

	public bool isLethalWhenHomozygous;

	public float defaultFrequency = 0.5f;
}
}
