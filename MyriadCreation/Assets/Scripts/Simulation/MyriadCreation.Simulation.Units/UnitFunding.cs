using System;

namespace MyriadCreation.Simulation.Units
{
[Serializable]
public class UnitFunding
{
	public UnitFundingSource source = UnitFundingSource.NationalTreasury;

	public int payerCharacterId = -1;

	public float moneyPaid;

	public float foodPaid;

	public int currencyGoodsId = UnitProvisioningCost.InvalidGoodsId;

	public int foodGoodsId = UnitProvisioningCost.InvalidGoodsId;
}
}
