using System;

namespace MyriadCreation.World.Settlement;

public static class DerivedEconomicProperties
{
	public static float Scarcity(float supply, float demand, float alternativeSupply)
	{
		float num = demand / Math.Max(0.01f, supply);
		float num2 = demand / Math.Max(0.01f, supply + Math.Max(0f, alternativeSupply));
		return Clamp01((0.7f * num + 0.3f * num2 - 0.5f) / 1.5f);
	}

	public static float MoneyScarcityFitness(float localScarcity)
	{
		return Clamp01(1f - 2f * Math.Abs(Clamp01(localScarcity) - 0.5f));
	}

	public static float Divisibility(float ductility)
	{
		return Clamp01(ductility / 10f);
	}

	public static float Portability(float valuePerWeight)
	{
		float num = Math.Max(0f, valuePerWeight);
		return Clamp01(num / (num + 1f));
	}

	public static float RelativeValue(float useValue, float scarcity)
	{
		return useValue * (1f + Math.Max(0f, scarcity));
	}

	public static float CurrencySuitability(float durability, float ductility, float valuePerWeight, float uniformity, float localScarcity, float acceptability, float circulationShare)
	{
		float num = 0.3f * Clamp01(durability) + 0.25f * Divisibility(ductility) + 0.25f * Portability(valuePerWeight) + 0.2f * Clamp01(uniformity);
		float num2 = 0.45f * Clamp01(acceptability) + 0.35f * MoneyScarcityFitness(localScarcity) + 0.2f * Clamp01(circulationShare);
		return Clamp01(0.55f * num + 0.45f * num2);
	}

	public static float ExchangeEquivalent(float moneyUnitValue, float localGoodPrice)
	{
		return Math.Max(0f, moneyUnitValue) / Math.Max(0.01f, localGoodPrice);
	}

	public static float ArbitrageProfit(float buyPrice, float sellPrice, float transportCostPerUnit)
	{
		float val = Math.Max(0f, sellPrice) - Math.Max(0f, buyPrice) - Math.Max(0f, transportCostPerUnit);
		return Math.Max(0f, val);
	}

	private static float Clamp01(float value)
	{
		if (!(value < 0f))
		{
			if (!(value > 1f))
			{
				return value;
			}
			return 1f;
		}
		return 0f;
	}
}
