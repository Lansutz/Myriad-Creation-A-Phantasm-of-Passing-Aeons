using System;
using System.Collections.Generic;
using MyriadCreation.Content.Modding;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Culture;

namespace MyriadCreation.Simulation.CultureHost
{
public static class CultureLineage
{
	public static int Apply(IReadOnlyDictionary<int, CultureData> cultures, Func<string, LanguageDef> languageLookup = null)
	{
		if (cultures == null || cultures.Count == 0)
		{
			return 0;
		}
		if (languageLookup == null)
		{
			languageLookup = LookupLanguage;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (KeyValuePair<int, CultureData> culture in cultures)
		{
			string text = culture.Value?.languageId;
			if (!string.IsNullOrEmpty(text))
			{
				dictionary[text] = culture.Key;
			}
		}
		int num = 0;
		foreach (KeyValuePair<int, CultureData> culture2 in cultures)
		{
			CultureData value = culture2.Value;
			if (value == null || string.IsNullOrEmpty(value.languageId))
			{
				continue;
			}
			LanguageDef languageDef = languageLookup(value.languageId);
			if (languageDef != null)
			{
				if (string.IsNullOrEmpty(value.languageFamilyId) && !string.IsNullOrEmpty(languageDef.familyId))
				{
					value.languageFamilyId = languageDef.familyId;
					num++;
				}
				if (value.parentCultureId < 0 && !string.IsNullOrEmpty(languageDef.parentLanguageId) && dictionary.TryGetValue(languageDef.parentLanguageId, out var value2) && value2 != value.cultureId)
				{
					value.parentCultureId = value2;
					num++;
				}
			}
		}
		return num;
	}

	private static LanguageDef LookupLanguage(string languageId)
	{
		// TODO(Content Lookup): Content 尚未提供 IContentLookup；暂用 ContentRegistry 静态目录，
		// 后续按 docs/architecture/ASMDEF_SPLIT_FAITH_CULTURE_LAWS.md §Phase 0 替换。
		if (!ContentRegistry.TryGetLanguage(languageId, out var def))
		{
			return null;
		}
		return def;
	}
}
}
