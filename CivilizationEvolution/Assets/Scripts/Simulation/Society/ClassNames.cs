using CivilizationEvolution.Politics;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.UI
{
    public static class ClassNames
    {
        public static string Get(GameEnums.SocialClass c) => c switch
        {
            GameEnums.SocialClass.Royalty => "王室",
            GameEnums.SocialClass.NobilityClergy => "贵族教士",
            GameEnums.SocialClass.MerchantFreeman => "市民商人",
            GameEnums.SocialClass.Peasant => "农民",
            GameEnums.SocialClass.Slave => "奴隶",
            _ => c.ToString()
        };
    }
}
