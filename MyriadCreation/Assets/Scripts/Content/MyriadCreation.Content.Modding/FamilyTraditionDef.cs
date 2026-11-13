using System;
using System.Collections.Generic;
using MyriadCreation.Core;

namespace MyriadCreation.Content.Modding
{
[Serializable]
public class FamilyTraditionDef
{
	public string traditionId;

	public List<EffectEntry> effects = new List<EffectEntry>();

	public List<string> incompatibleWith = new List<string>();

	public List<int> requiredInnovations = new List<int>();

	public string GetName()
	{
		return Localization.Get(traditionId + "_name");
	}

	public string GetDescription()
	{
		return Localization.Get(traditionId + "_desc");
	}
}
}
