using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Economy
{
    [System.Serializable]
    public class TaxSystem
    {
        public float agriculturalTax = 0.1f;
        public float headTax = 0.05f;
        public float tradeTax = 0.1f;
        public float miningTax = 0.15f;
        public float craftTax = 0.1f;
        public float livestockTax = 0.08f;
        public float luxuryTax = 0.3f;
        public float saltMonopolyTax = 0.5f;
        public float wartimeSpecialTax = 0f;

        [System.NonSerialized]
        public Dictionary<GameEnums.SocialClass, bool> taxExemptions = new Dictionary<GameEnums.SocialClass, bool>();

 /// <summary>计算地块实际税收</summary>
        public float CalculateTileTax(TileData tile, float baseOutput, GameEnums.SocialClass dominantClass)
        {
            if (taxExemptions.GetValueOrDefault(dominantClass, false)) return 0f;

            float controlEfficiency = 0.3f + tile.stability / 100f * 0.7f;
            float combinedRate = agriculturalTax * 0.4f + headTax * 0.2f + tradeTax * 0.2f + craftTax * 0.1f + wartimeSpecialTax * 0.1f;

 // 最优税率区间：超过30%后边际收益递减
            float effectiveRate = combinedRate < 0.3f
                ? combinedRate
                : 0.3f + (combinedRate - 0.3f) * 0.5f;

            return baseOutput * effectiveRate * controlEfficiency;
        }

 /// <summary>计算税率对阶层好感的影响</summary>
        public float GetTaxSatisfactionImpact(GameEnums.SocialClass socialClass)
        {
            float impact = socialClass switch
            {
                GameEnums.SocialClass.Peasant => -(agriculturalTax + headTax) * 50f,
                GameEnums.SocialClass.MerchantFreeman => -(tradeTax + craftTax) * 40f,
                GameEnums.SocialClass.NobilityClergy => -(luxuryTax + livestockTax) * 30f,
                GameEnums.SocialClass.Slave => -headTax * 20f,
                _ => 0f
            };
            impact -= wartimeSpecialTax * 60f;
            return Mathf.Clamp(impact, -50f, 10f);
        }
    }
}
