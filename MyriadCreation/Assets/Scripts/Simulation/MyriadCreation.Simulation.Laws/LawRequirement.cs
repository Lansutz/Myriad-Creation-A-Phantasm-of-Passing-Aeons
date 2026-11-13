using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Laws
{
[Serializable]
public class LawRequirement
{
	public float minTreasury;

	public float minLegitimacy;

	public float minCentralization;

	public List<string> requiredLawIds = new List<string>();

	public List<int> requiredInnovationIds = new List<int>();

	/// <summary>必须先生效的法令 id。</summary>
	public List<string> requiredDecreeIds = new List<string>();

	/// <summary>政体形式条件（feudal/centralized/city_state/tribal）；空=不限。</summary>
	public string requiredPolityForm = "";
}
}
