using System;

namespace MyriadCreation.Simulation.Units
{
[Serializable]
public struct UnitProvisioningCost
{
	/// <summary>物产 id 的"无效/未指定"哨兵。0 号是粮食，是合法 id，不能当哨兵用。</summary>
	public const int InvalidGoodsId = -1;

	public float food;

	public float money;

	public int horses;

	public int carts;

	public int foodGoodsId;

	public int currencyGoodsId;

	/// <summary>
	/// 传令官补给：口粮按"每日需求 × 预计行程天数 + 缓冲"算，而不是拍脑袋线性式。
	/// 队伍每日消耗 = 1 人 + 1 马 = 2/天（见 TransportUnit.DailyFoodDemand），
	/// 行军保守按 1 格/天估（MapActor 逐格移动），再留 3 天缓冲与 15% 损耗余量。
	/// </summary>
	public static UnitProvisioningCost ForCourier(float distanceTiles, int foodGoodsId = InvalidGoodsId, int currencyGoodsId = InvalidGoodsId)
	{
		float num = Math.Max(0f, distanceTiles);
		float days = num; // 保守：1 格/天
		float demandPerDay = 2f; // 1 人 + 1 马
		return new UnitProvisioningCost
		{
			food = demandPerDay * (days + 3f) * 1.15f,
			money = 4f + num * 0.04f,
			horses = 1,
			carts = 0,
			foodGoodsId = foodGoodsId,
			currencyGoodsId = currencyGoodsId
		};
	}
}
}
