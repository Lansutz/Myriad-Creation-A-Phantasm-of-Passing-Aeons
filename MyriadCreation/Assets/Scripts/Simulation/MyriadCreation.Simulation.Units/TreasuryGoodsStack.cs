using System;

namespace MyriadCreation.Simulation.Units
{
[Serializable]
public struct TreasuryGoodsStack(int goodsId, float amount)
{
	public int goodsId = goodsId;

	public float amount = amount;
}
}
