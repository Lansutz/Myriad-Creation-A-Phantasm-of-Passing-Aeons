using System;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Politics
{
    [Serializable]
    public class ClassNeedWeightTable
    {
        // 顺序对应 ClassNeedDimension 枚举：Subsistence/Security/TaxBurden/PoliticalAccess/
        // EconomicOpportunity/InstitutionalRecognition/Legitimacy/Privilege
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
