using System;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class GoodQuality
{
	public int goodsId;

	public float quality;

	public float qualityProgress;

	public float peakQuality;

	public float cumulativeOutput;

	public float QualityMultiplier => 1f + (quality - 5f) * 0.1f;

	public bool IsHighQuality => quality >= 7f;

	public bool IsLowQuality => quality <= 3f;

	public GoodQuality(int goodsId)
	{
		this.goodsId = goodsId;
		quality = 3f;
		qualityProgress = 0f;
		peakQuality = 3f;
	}
}
}
