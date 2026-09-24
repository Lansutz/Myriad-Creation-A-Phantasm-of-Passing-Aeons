using System;
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
using MyriadCreation.Simulation.WorldState;



namespace MyriadCreation.Simulation.Society
{
    [Serializable]
    public class ClassNeedWeightTable
    {
 // 顺序对应 ClassNeedDimension 枚举：Subsistence/Security/TaxBurden/PoliticalAccess/ // EconomicOpportunity/InstitutionalRecognition/Legitimacy/Privilege
        public float[] royalty =        { 0.10f, 0.20f, 0.00f, 0.00f, 0.15f, 0.00f, 0.30f, 0.25f };
        public float[] nobilityClergy = { 0.10f, 0.15f, 0.00f, 0.25f, 0.00f, 0.00f, 0.20f, 0.30f };
        public float[] merchantFreeman ={ 0.15f, 0.10f, 0.20f, 0.25f, 0.30f, 0.00f, 0.00f, 0.00f };
        public float[] peasant =        { 0.35f, 0.25f, 0.25f, 0.05f, 0.00f, 0.00f, 0.10f, 0.00f };
        public float[] slave =          { 0.70f, 0.30f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f };

        public float[] GetWeights(GameEnums.SocialClass cls) => cls switch
        {
            GameEnums.SocialClass.Royalty => royalty,
            GameEnums.SocialClass.NobilityClergy => nobilityClergy,
            GameEnums.SocialClass.MerchantFreeman => merchantFreeman,
            GameEnums.SocialClass.Peasant => peasant,
            GameEnums.SocialClass.Slave => slave,
            _ => peasant
        };
    }
}
