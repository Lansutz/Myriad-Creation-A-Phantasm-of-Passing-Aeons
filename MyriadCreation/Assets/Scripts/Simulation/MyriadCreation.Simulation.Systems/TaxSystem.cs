using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
[Serializable]
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

	public float wartimeSpecialTax;

	[NonSerialized]
	public Dictionary<GameEnums.SocialClass, bool> taxExemptions = new Dictionary<GameEnums.SocialClass, bool>();

	public float CalculateTileTax(TileData tile, float baseOutput, GameEnums.SocialClass dominantClass)
	{
		if (taxExemptions.GetValueOrDefault(dominantClass, defaultValue: false))
		{
			return 0f;
		}
		float num = 0.3f + tile.stability / 100f * 0.7f;
		float num2 = agriculturalTax * 0.4f + headTax * 0.2f + tradeTax * 0.2f + craftTax * 0.1f + wartimeSpecialTax * 0.1f;
		float num3 = ((num2 < 0.3f) ? num2 : (0.3f + (num2 - 0.3f) * 0.5f));
		return baseOutput * num3 * num;
	}

	public float GetTaxSatisfactionImpact(GameEnums.SocialClass socialClass)
	{
		return Mathf.Clamp(socialClass switch
		{
			GameEnums.SocialClass.Peasant => (0f - (agriculturalTax + headTax)) * 50f, 
			GameEnums.SocialClass.MerchantFreeman => (0f - (tradeTax + craftTax)) * 40f, 
			GameEnums.SocialClass.NobilityClergy => (0f - (luxuryTax + livestockTax)) * 30f, 
			GameEnums.SocialClass.Slave => (0f - headTax) * 20f, 
			_ => 0f, 
		} - wartimeSpecialTax * 60f, -50f, 10f);
	}
}
}
