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

	/// <summary>征收前必须给当地人口保留的口粮天数；不足此数的食物不征。</summary>
	public int reserveDays = 90;

	/// <summary>每格运输摩擦 0-1；由地区仓储到首都的距离决定实际损耗，上限 50%。</summary>
	public float transportLossPerTile = 0.002f;

	[NonSerialized]
	public Dictionary<GameEnums.SocialClass, bool> taxExemptions = new Dictionary<GameEnums.SocialClass, bool>();

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
