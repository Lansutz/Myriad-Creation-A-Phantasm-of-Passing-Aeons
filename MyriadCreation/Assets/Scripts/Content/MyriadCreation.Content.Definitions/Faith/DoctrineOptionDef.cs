using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class DoctrineOptionDef
{
	public string optionId;

	public string pillar = "doctrine";

	public string optionName;

	public string description = "";

	public List<int> exclusiveReligionIds = new List<int>();

	public string enhancedFrom = "";
}
}
