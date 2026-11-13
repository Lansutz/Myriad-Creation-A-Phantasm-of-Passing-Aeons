using MyriadCreation.Core.Enums;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.UI.Presentation
{
public static class ClassNames
{
	public static string Get(GameEnums.SocialClass c)
	{
		return c switch
		{
			GameEnums.SocialClass.Royalty => "王室", 
			GameEnums.SocialClass.NobilityClergy => "贵族教士", 
			GameEnums.SocialClass.MerchantFreeman => "市民商人", 
			GameEnums.SocialClass.Peasant => "农民", 
			GameEnums.SocialClass.Slave => "奴隶", 
			_ => c.ToString(), 
		};
	}
}
}
