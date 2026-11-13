using System;
using System.Collections.Generic;
using MyriadCreation.Core;

namespace MyriadCreation.Content.Modding
{
[Serializable]
public class EthosDef
{
	public string ethosId;

	public List<EffectEntry> effects = new List<EffectEntry>();

	public string GetName()
	{
		return Localization.Get(ethosId + "_name");
	}

	public string GetDescription()
	{
		return Localization.Get(ethosId + "_desc");
	}
}
}
