using System;

namespace MyriadCreation.Core.Data
{
[Serializable]
public struct TileResource
{
	public int goodsId;

	public float abundance;

	public bool discovered;

	public bool developed;

	public float developmentLevel;

	public float depletion;

	public ResourceAccessDepth accessDepth;

	public bool IsDepleted => depletion >= 1f;

	public float EffectiveOutput => abundance * Math.Max(developmentLevel, 0.1f) * (1f - depletion);

	public static TileResource Create(int goodsId, float abundance)
	{
		return Create(goodsId, abundance, ResourceAccessDepth.Surface);
	}

	public static TileResource Create(int goodsId, float abundance, ResourceAccessDepth depth)
	{
		return new TileResource
		{
			goodsId = goodsId,
			abundance = abundance,
			discovered = false,
			developed = false,
			developmentLevel = 0f,
			depletion = 0f,
			accessDepth = depth
		};
	}
}
}
