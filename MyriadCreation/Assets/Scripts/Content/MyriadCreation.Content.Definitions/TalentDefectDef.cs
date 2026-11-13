using System;
using MyriadCreation.Core;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class TalentDefectDef
{
	public string id;

	public string name;

	public bool isTalent;

	public string stat;

	public float amount;

	public string description;

	public string GetName()
	{
		if (!Localization.Has(id + "_name"))
		{
			return name;
		}
		return Localization.Get(id + "_name");
	}

	public string GetDescription()
	{
		if (!Localization.Has(id + "_desc"))
		{
			return description;
		}
		return Localization.Get(id + "_desc");
	}
}
}
