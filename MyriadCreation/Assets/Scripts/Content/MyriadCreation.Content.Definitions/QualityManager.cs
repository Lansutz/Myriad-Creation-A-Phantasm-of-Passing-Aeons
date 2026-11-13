using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MyriadCreation.Content.Definitions
{
public class QualityManager
{
	private readonly Dictionary<int, GoodQuality> _qualities = new Dictionary<int, GoodQuality>();

	public GoodQuality GetQuality(int goodsId)
	{
		if (!_qualities.TryGetValue(goodsId, out var value))
		{
			value = new GoodQuality(goodsId);
			_qualities[goodsId] = value;
		}
		return value;
	}

	public float GetQualityMultiplier(int goodsId)
	{
		if (!_qualities.TryGetValue(goodsId, out var value))
		{
			return 1f;
		}
		return value.QualityMultiplier;
	}

	public void MonthlyTickQuality(int goodsId, float monthlyOutput, bool hasArtisans, bool hasFacility)
	{
		if (monthlyOutput <= 0f)
		{
			return;
		}
		GoodQuality quality = GetQuality(goodsId);
		float num = Mathf.Log(1f + monthlyOutput) * 0.5f;
		if (hasArtisans)
		{
			num *= 1.5f;
		}
		if (hasFacility)
		{
			num *= 1.3f;
		}
		float num2 = 1f / (1f + quality.quality / 5f);
		num *= num2;
		quality.qualityProgress += num;
		while (quality.qualityProgress >= 100f && quality.quality < 10f)
		{
			quality.qualityProgress -= 100f;
			quality.quality = Mathf.Min(10f, quality.quality + 0.5f);
			if (quality.quality > quality.peakQuality)
			{
				quality.peakQuality = quality.quality;
			}
		}
		if (quality.quality >= 10f)
		{
			quality.qualityProgress = 0f;
		}
	}

	public float GetAverageQuality(List<int> goodsIds)
	{
		if (goodsIds == null || goodsIds.Count == 0)
		{
			return 5f;
		}
		float num = 0f;
		foreach (int goodsId in goodsIds)
		{
			num += GetQuality(goodsId).quality;
		}
		return num / (float)goodsIds.Count;
	}

	public float GetCumulativeOutput(int goodsId)
	{
		if (!_qualities.TryGetValue(goodsId, out var value))
		{
			return 0f;
		}
		return value.cumulativeOutput;
	}

	public float GetQualitySpread(int goodsId)
	{
		float cumulativeOutput = GetCumulativeOutput(goodsId);
		return Mathf.Clamp(3f - Mathf.Log(1f + cumulativeOutput) * 0.3f, 1f, 3f);
	}

	public int ProduceQuality(int goodsId, float materialAvailability = 1f)
	{
		GoodQuality quality = GetQuality(goodsId);
		float num = GetQualitySpread(goodsId);
		float num2 = Mathf.Clamp01(materialAvailability);
		float num3 = quality.quality * num2;
		if (num2 < 1f)
		{
			num *= 1f + (1f - num2) * 0.5f;
		}
		float num4 = 1f - Random.value;
		float value = Random.value;
		float num5 = Mathf.Sqrt(-2f * Mathf.Log(num4)) * Mathf.Cos(MathF.PI * 2f * value);
		return Mathf.Clamp(Mathf.RoundToInt(num3 + num5 * num), 0, 10);
	}

	public void RecordProduction(int goodsId, float amount, int actualQuality)
	{
		if (!(amount <= 0f))
		{
			GoodQuality quality = GetQuality(goodsId);
			quality.cumulativeOutput += amount;
			float num = quality.cumulativeOutput + amount;
			float num2 = amount / num;
			quality.quality = quality.quality * (1f - num2) + (float)actualQuality * num2;
			if (quality.quality > quality.peakQuality)
			{
				quality.peakQuality = quality.quality;
			}
		}
	}
}
}
