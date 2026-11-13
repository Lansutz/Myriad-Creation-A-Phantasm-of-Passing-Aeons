using System;

namespace MyriadCreation.Content.Definitions
{
public static class PersonalityDimensions
{
	public static readonly PersonalityDimension[] All = new PersonalityDimension[8]
	{
		PersonalityDimension.Boldness,
		PersonalityDimension.Sociability,
		PersonalityDimension.Compassion,
		PersonalityDimension.Greed,
		PersonalityDimension.Honor,
		PersonalityDimension.Rationality,
		PersonalityDimension.Vengefulness,
		PersonalityDimension.Piety
	};

	public static string Key(this PersonalityDimension dim)
	{
		return dim switch
		{
			PersonalityDimension.Boldness => "boldness", 
			PersonalityDimension.Sociability => "sociability", 
			PersonalityDimension.Compassion => "compassion", 
			PersonalityDimension.Greed => "greed", 
			PersonalityDimension.Honor => "honor", 
			PersonalityDimension.Rationality => "rationality", 
			PersonalityDimension.Vengefulness => "vengefulness", 
			PersonalityDimension.Piety => "piety", 
			_ => "", 
		};
	}

	public static string DisplayName(this PersonalityDimension dim)
	{
		return dim switch
		{
			PersonalityDimension.Boldness => "大胆", 
			PersonalityDimension.Sociability => "社交性", 
			PersonalityDimension.Compassion => "悲悯", 
			PersonalityDimension.Greed => "贪婪", 
			PersonalityDimension.Honor => "荣誉", 
			PersonalityDimension.Rationality => "理性", 
			PersonalityDimension.Vengefulness => "报复", 
			PersonalityDimension.Piety => "虔信", 
			_ => "?", 
		};
	}

	public static bool TryParse(string key, out PersonalityDimension dim)
	{
		if (!string.IsNullOrEmpty(key))
		{
			PersonalityDimension[] all = All;
			foreach (PersonalityDimension personalityDimension in all)
			{
				if (string.Equals(personalityDimension.Key(), key, StringComparison.OrdinalIgnoreCase))
				{
					dim = personalityDimension;
					return true;
				}
			}
		}
		dim = PersonalityDimension.Boldness;
		return false;
	}
}
}
