using System;
using System.Collections.Generic;
using MyriadCreation.Core;

namespace MyriadCreation.Content.Modding
{
[Serializable]
public class EthnicGroupDef
{
	public string groupId;

	public int cultureId;

	public string ethosId;

	public string languageId;

	public List<string> traditionIds = new List<string>();

	public List<int> innovationIds = new List<int>();

	public string GetName()
	{
		return Localization.Get(groupId + "_name");
	}

	public string GetDescription()
	{
		return Localization.Get(groupId + "_desc");
	}

	public string GetPluralName()
	{
		return Localization.Get(groupId + "_plural", GetName());
	}

	public string GetAdjectiveName()
	{
		return Localization.Get(groupId + "_adj", GetName());
	}
}
}
