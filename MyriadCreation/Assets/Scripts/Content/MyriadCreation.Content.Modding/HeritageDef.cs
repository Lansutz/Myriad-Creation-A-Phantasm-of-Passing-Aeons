using System;
using System.Collections.Generic;
using MyriadCreation.Core;

namespace MyriadCreation.Content.Modding
{
[Serializable]
public class HeritageDef
{
	public string heritageId;

	public string ethosId = "";

	public List<string> appearanceTags = new List<string>();

	public string affinityGroup = "";

	public List<EffectEntry> effects = new List<EffectEntry>();

	public string GetName()
	{
		return Localization.Get(heritageId + "_name");
	}

	public string GetDescription()
	{
		return Localization.Get(heritageId + "_desc");
	}
}
}
