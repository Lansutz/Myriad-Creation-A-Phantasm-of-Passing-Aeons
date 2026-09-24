using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Modding;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Population;
using MyriadCreation.Simulation.WorldState;




namespace MyriadCreation.Simulation.Society
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
