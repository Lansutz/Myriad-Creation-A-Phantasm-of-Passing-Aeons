using System.Collections.Generic;
using MyriadCreation.Content.Modding;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Generation
{
public static class PlaceNameGenerator
{
	private static readonly Dictionary<string, HashSet<string>> AllowedSuffixes = new Dictionary<string, HashSet<string>>
	{
		{
			"mountain",
			new HashSet<string> { "city", "fort", "region", "home" }
		},
		{
			"plain",
			new HashSet<string> { "city", "region", "home" }
		},
		{
			"cliff",
			new HashSet<string> { "city", "fort", "region" }
		},
		{
			"valley",
			new HashSet<string> { "city", "region", "home" }
		},
		{
			"coast",
			new HashSet<string> { "city", "port", "region" }
		},
		{
			"river",
			new HashSet<string> { "city", "port", "region", "home" }
		},
		{
			"sea",
			new HashSet<string> { "port", "region" }
		},
		{
			"hill",
			new HashSet<string> { "city", "fort", "region", "home" }
		},
		{
			"desert",
			new HashSet<string> { "city", "region" }
		},
		{
			"highland",
			new HashSet<string> { "fort", "region", "home" }
		}
	};

	public static string FindWord(List<PlaceSuffixDef> words, string semantic)
	{
		if (words == null)
		{
			return "";
		}
		foreach (PlaceSuffixDef word in words)
		{
			if (word.semantic == semantic)
			{
				return word.word;
			}
		}
		return "";
	}

	public static bool CanCombine(string stemSemantic, string suffixSemantic)
	{
		switch (suffixSemantic)
		{
		case "region":
		case "home":
			return true;
		case "founded":
			return true;
		default:
		{
			if (!AllowedSuffixes.TryGetValue(stemSemantic, out var value))
			{
				return false;
			}
			return value.Contains(suffixSemantic);
		}
		}
	}

	public static string Combine(string stemWord, string suffixWord, string stemSemantic, string suffixSemantic)
	{
		if (string.IsNullOrEmpty(stemWord) || string.IsNullOrEmpty(suffixWord))
		{
			return "";
		}
		if (!CanCombine(stemSemantic, suffixSemantic))
		{
			return "";
		}
		return stemWord + suffixWord;
	}

	public static string Generate(string stemSemantic, string suffixSemantic, LanguageDef lang)
	{
		if (lang == null)
		{
			return "";
		}
		string text = FindWord(lang.terrainWords, stemSemantic);
		string text2 = FindWord(lang.placeSuffixes, suffixSemantic);
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text2))
		{
			return "";
		}
		if (!CanCombine(stemSemantic, suffixSemantic))
		{
			return "";
		}
		return text + text2;
	}

	public static string FounderCity(string founderName, LanguageDef lang)
	{
		if (string.IsNullOrEmpty(founderName) || lang == null)
		{
			return "";
		}
		string text = FindWord(lang.placeSuffixes, "city");
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		return founderName + text;
	}
}
}
