using System;
using System.Collections.Generic;
using MyriadCreation.Core;

namespace MyriadCreation.Content.Modding
{
[Serializable]
public class LanguageDef
{
	public string languageId;

	public string glottocode = string.Empty;

	public string familyId = string.Empty;

	public string parentLanguageId = string.Empty;

	public List<string> sampleWords = new List<string>();

	public List<string> maleNames = new List<string>();

	public List<string> femaleNames = new List<string>();

	public List<string> familyNames = new List<string>();

	public List<string> cityNames = new List<string>();

	public List<PlaceSuffixDef> placeSuffixes = new List<PlaceSuffixDef>();

	public List<PlaceSuffixDef> terrainWords = new List<PlaceSuffixDef>();

	public string GetName()
	{
		return Localization.Get(languageId + "_name");
	}

	public string GetDescription()
	{
		return Localization.Get(languageId + "_desc");
	}

	public string GetScriptType()
	{
		return Localization.Get(languageId + "_script");
	}

	public string GetNamingStyle()
	{
		return Localization.Get(languageId + "_naming");
	}
}
}
