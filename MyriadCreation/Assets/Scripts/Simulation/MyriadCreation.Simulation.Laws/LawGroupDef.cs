using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Laws
{
[Serializable]
public class LawGroupDef
{
	public string groupId = "";

	public string name = "";

	public int order;

	public string description = "";

	public List<LawDef> laws = new List<LawDef>();

	/// <summary>兜底法律 id；未主动选择时生效。</summary>
	public string fallbackLawId = "";
}
}
