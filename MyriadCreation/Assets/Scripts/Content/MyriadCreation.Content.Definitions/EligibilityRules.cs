using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class EligibilityRules
{
	public InheritanceGender gender;

	public EligibilityScope scope = EligibilityScope.FreePeople;

	public bool IsGenderEligible(bool isMale)
	{
		if (gender == InheritanceGender.MaleOnly)
		{
			return isMale;
		}
		if (gender == InheritanceGender.FemaleOnly)
		{
			return !isMale;
		}
		return true;
	}


	public string GetName()
	{
		return gender switch
		{
			InheritanceGender.MalePreference => "男子优先", 
			InheritanceGender.MaleOnly => "男子专属", 
			InheritanceGender.Equal => "男女平等", 
			InheritanceGender.FemalePreference => "女子优先", 
			InheritanceGender.FemaleOnly => "女子专属", 
			_ => "男子优先", 
		} + "·" + scope switch
		{
			EligibilityScope.ClanOnly => "限本族", 
			EligibilityScope.Citizens => "限公民", 
			EligibilityScope.Nobility => "限贵族", 
			EligibilityScope.Clergy => "限教阶", 
			EligibilityScope.FreePeople => "限自由民", 
			EligibilityScope.All => "全体", 
			_ => "限自由民", 
		};
	}

	public static List<GameEnums.SocialClass> ScopeToSocialClasses(EligibilityScope scope)
	{
		List<GameEnums.SocialClass> list = new List<GameEnums.SocialClass>();
		switch (scope)
		{
		case EligibilityScope.Nobility:
			list.Add(GameEnums.SocialClass.Royalty);
			list.Add(GameEnums.SocialClass.NobilityClergy);
			break;
		case EligibilityScope.Citizens:
			list.Add(GameEnums.SocialClass.MerchantFreeman);
			break;
		case EligibilityScope.Clergy:
			list.Add(GameEnums.SocialClass.NobilityClergy);
			break;
		case EligibilityScope.FreePeople:
			list.Add(GameEnums.SocialClass.Royalty);
			list.Add(GameEnums.SocialClass.NobilityClergy);
			list.Add(GameEnums.SocialClass.MerchantFreeman);
			list.Add(GameEnums.SocialClass.Peasant);
			break;
		case EligibilityScope.All:
			list.Add(GameEnums.SocialClass.Royalty);
			list.Add(GameEnums.SocialClass.NobilityClergy);
			list.Add(GameEnums.SocialClass.MerchantFreeman);
			list.Add(GameEnums.SocialClass.Peasant);
			list.Add(GameEnums.SocialClass.Slave);
			break;
		}
		return list;
	}

	public bool IsScopeEligible(GameEnums.SocialClass socialClass)
	{
		if (scope == EligibilityScope.ClanOnly)
		{
			return true;
		}
		return ScopeToSocialClasses(scope).Contains(socialClass);
	}
}
}
