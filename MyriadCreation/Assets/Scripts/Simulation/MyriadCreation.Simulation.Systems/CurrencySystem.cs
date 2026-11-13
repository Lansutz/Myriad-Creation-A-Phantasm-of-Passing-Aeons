using System;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Units;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
[Serializable]
public class CurrencySystem
{
	public GameEnums.CurrencyStage currentStage;

	public string currencyName = "";

	public float goldReserve;

	public float silverReserve;

	public float coinPurity = 1f;

	public float paperMoneyInCirculation;

	public float inflationRate;

	public TreasuryVault vault = new TreasuryVault();

	public float GetCurrencyValue()
	{
		return currentStage switch
		{
			GameEnums.CurrencyStage.Barter => 1f, 
			GameEnums.CurrencyStage.Bullion => 1f, 
			GameEnums.CurrencyStage.MintedCoin => coinPurity, 
			GameEnums.CurrencyStage.PaperMoney => Mathf.Clamp((goldReserve + silverReserve * 0.1f) / Mathf.Max(1f, paperMoneyInCirculation), 0.01f, 2f), 
			_ => 1f, 
		};
	}

	public void DebaseCoin(float purityReduction, float amountMinted)
	{
		coinPurity = Mathf.Max(0.1f, coinPurity - purityReduction);
		inflationRate += purityReduction * 0.5f;
	}

	public bool IssuePaperMoney(float amount)
	{
		float num = goldReserve + silverReserve * 0.1f;
		float num2 = num * 3f;
		if (paperMoneyInCirculation + amount > num2)
		{
			return false;
		}
		paperMoneyInCirculation += amount;
		if (paperMoneyInCirculation > num * 1.5f)
		{
			inflationRate += (paperMoneyInCirculation / num - 1.5f) * 0.1f;
		}
		return true;
	}

	public void DailyTick()
	{
		inflationRate = Mathf.Max(0f, inflationRate - 0.001f);
	}
}
}
