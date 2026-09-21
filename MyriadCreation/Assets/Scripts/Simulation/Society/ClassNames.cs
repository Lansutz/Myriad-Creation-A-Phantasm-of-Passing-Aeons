using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.WorldState;




namespace CivilizationEvolution.Simulation.Society
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
