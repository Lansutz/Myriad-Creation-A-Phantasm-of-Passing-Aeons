using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Culture
{
public static class CultureSimilarity
{
	public const float W_LIVELIHOOD = 0.25f;

	public const float W_MOBILITY = 0.15f;

	public const float W_BURIAL = 0.15f;

	public const float W_WORSHIP = 0.2f;

	public const float W_MATERIAL = 0.1f;

	public const float W_SYMBOLIC = 0.1f;

	public const float W_ENVIRONMENT = 0.05f;

	public static float CalculateSim(CultureData a, CultureData b)
	{
		return Mathf.Clamp01(0f + 0.25f * CategoricalSim(a.livelihoodType, b.livelihoodType) + 0.15f * CategoricalSim(a.mobilityType, b.mobilityType) + 0.15f * SetOrSingleSim(a.burialTypes, b.burialTypes, a.burialType, b.burialType) + 0.2f * CosineSimilarity(a.worshipVector, b.worshipVector) + 0.1f * CategoricalSim(a.materialStyle, b.materialStyle) + 0.1f * SetOrSingleSim(a.symbolicFoci, b.symbolicFoci, a.symbolicFocus, b.symbolicFocus) + 0.05f * SetOrSingleSim(a.environmentAdapts, b.environmentAdapts, a.environmentAdapt, b.environmentAdapt));
	}

	public static float CalculateTraditionOverlap(CultureData a, CultureData b)
	{
		if (a?.traditionIds == null || b?.traditionIds == null)
		{
			return 0.5f;
		}
		if (a.traditionIds.Count == 0 && b.traditionIds.Count == 0)
		{
			return 0.5f;
		}
		if (a.traditionIds.Count == 0 || b.traditionIds.Count == 0)
		{
			return 0f;
		}
		int num = a.traditionIds.Intersect(b.traditionIds).Count();
		int num2 = a.traditionIds.Union(b.traditionIds).Count();
		if (num2 != 0)
		{
			return (float)num / (float)num2;
		}
		return 0f;
	}

	public static float CalculateFactorSimilarity(CultureData a, CultureData b)
	{
		if (a == null || b == null)
		{
			return 0.5f;
		}
		TraditionFactorKey[] array = new TraditionFactorKey[4]
		{
			TraditionFactorKey.SettlementDegree,
			TraditionFactorKey.SeparationTendency,
			TraditionFactorKey.FusionTendency,
			TraditionFactorKey.ResourcePreference
		};
		float num = 0f;
		TraditionFactorKey[] array2 = array;
		foreach (TraditionFactorKey key in array2)
		{
			float factorSum = a.GetFactorSum(key);
			float factorSum2 = b.GetFactorSum(key);
			float num2 = Mathf.Abs(Mathf.Clamp(factorSum, -1f, 1f) - Mathf.Clamp(factorSum2, -1f, 1f));
			num += 1f - num2 / 2f;
		}
		return Mathf.Clamp01(num / (float)array.Length);
	}

	public static float CalculateTraditionSim(CultureData a, CultureData b)
	{
		return Mathf.Clamp01(0.5f * CalculateTraditionOverlap(a, b) + 0.5f * CalculateFactorSimilarity(a, b));
	}

	public static float CalculateFullSimilarity(CultureData a, CultureData b)
	{
		if (a == null || b == null)
		{
			return 0f;
		}
		bool num = a.traditionIds != null && a.traditionIds.Count > 0;
		bool flag = b.traditionIds != null && b.traditionIds.Count > 0;
		if (num & flag)
		{
			return Mathf.Clamp01(0.6f * CalculateTraditionSim(a, b) + 0.4f * CalculateSim(a, b));
		}
		return CalculateSim(a, b);
	}

	public static float SettlementGap(CultureData a, CultureData b)
	{
		if (a == null || b == null)
		{
			return 0f;
		}
		return Mathf.Abs(a.GetSettlementDegree() - b.GetSettlementDegree());
	}

	private static float CategoricalSim(int a, int b)
	{
		if (a == b)
		{
			return 1f;
		}
		if (Mathf.Abs(a - b) == 1)
		{
			return 0.5f;
		}
		return 0f;
	}

	private static float SetOrSingleSim(List<int> setA, List<int> setB, int singleA, int singleB)
	{
		bool flag = setA == null || setA.Count == 0;
		bool flag2 = setB == null || setB.Count == 0;
		if (flag & flag2)
		{
			return CategoricalSim(singleA, singleB);
		}
		if (flag | flag2)
		{
			return 0.5f;
		}
		int num = 0;
		foreach (int setum in setA)
		{
			if (setB.Contains(setum))
			{
				num++;
			}
		}
		int num2 = setA.Count + setB.Count - num;
		if (num2 <= 0)
		{
			return 1f;
		}
		return (float)num / (float)num2;
	}

	private static float CosineSimilarity(float[] a, float[] b)
	{
		if (a == null || b == null || a.Length != b.Length)
		{
			return 0.5f;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		for (int i = 0; i < a.Length; i++)
		{
			num += a[i] * b[i];
			num2 += a[i] * a[i];
			num3 += b[i] * b[i];
		}
		float num4 = Mathf.Sqrt(num2) * Mathf.Sqrt(num3);
		if (!(num4 > 0f))
		{
			return 0.5f;
		}
		return num / num4;
	}

	public static float CalculateSeparationResistance(CultureData culture, float transformativity)
	{
		float num = 0.3f + culture.maturity / 100f * 0.5f;
		float num2 = 1f - transformativity / 200f;
		return Mathf.Clamp(num * num2, 0.1f, 1f);
	}

	public static float CalculateFusionResistance(CultureData dominant, CultureData minority, float transDominant, float transMinority)
	{
		float num = CalculateSim(dominant, minority);
		float num2 = 1f - num * 0.7f;
		float num3 = (transDominant + transMinority) / 2f;
		float num4 = 1f - num3 / 150f;
		return Mathf.Clamp(num2 * num4, 0.1f, 1f);
	}

	public static float CalculateSpreadRate(CultureData culture, float transformativity, float contactIntensity)
	{
		float num = 0.5f + culture.maturity / 100f;
		float num2 = 0.5f + transformativity / 200f;
		return culture.spreadPower * num * num2 * contactIntensity * 0.01f;
	}
}
}
