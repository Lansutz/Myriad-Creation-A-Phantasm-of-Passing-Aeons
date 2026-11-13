using System;
using MyriadCreation.Core;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class MentalDisorderDef
{
	public string id;

	public string name;

	public string description;

	public bool reversible = true;

	public float martialMod;

	public float diplomacyMod;

	public float warfareMod;

	public float stewardshipMod;

	public float intrigueMod;

	public float learningMod;

	public float charmMod;

	public float stressDecayMult = 1f;

	public float opinionDecayMult = 1f;

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
