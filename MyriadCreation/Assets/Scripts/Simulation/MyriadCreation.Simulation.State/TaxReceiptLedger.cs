using System.Collections.Generic;

namespace MyriadCreation.Simulation.State
{
/// <summary>一条实物税收凭据：哪个政权、哪天、从哪个地区、征了什么、实际入库多少。</summary>
public readonly struct TaxReceipt
{
	public readonly int realmId;

	public readonly int absoluteDay;

	public readonly int regionId;

	public readonly int goodsId;

	public readonly float delivered;

	public TaxReceipt(int realmId, int absoluteDay, int regionId, int goodsId, float delivered)
	{
		this.realmId = realmId;
		this.absoluteDay = absoluteDay;
		this.regionId = regionId;
		this.goodsId = goodsId;
		this.delivered = delivered;
	}
}

/// <summary>
/// 实物税流水（运行时，不落盘）：用于 UI/长跑断言“税真的从地方仓储征到了国家仓”。
/// 货币流水仍走 <see cref="MyriadCreation.Simulation.Units.TreasuryLedger"/>；这里只记实物，不参与结算。
/// </summary>
public static class TaxReceiptLedger
{
	private static readonly Dictionary<int, List<TaxReceipt>> _history = new Dictionary<int, List<TaxReceipt>>();

	private const int HistoryLimit = 4000;

	public static void Record(int realmId, int absoluteDay, int regionId, int goodsId, float delivered)
	{
		if (realmId < 0 || delivered <= 0.0001f) return;
		if (!_history.TryGetValue(realmId, out List<TaxReceipt> list))
		{
			list = new List<TaxReceipt>();
			_history[realmId] = list;
		}
		list.Add(new TaxReceipt(realmId, absoluteDay, regionId, goodsId, delivered));
		if (list.Count > HistoryLimit) list.RemoveRange(0, list.Count - HistoryLimit);
	}

	public static List<TaxReceipt> GetHistory(int realmId, int fromAbsoluteDay)
	{
		List<TaxReceipt> result = new List<TaxReceipt>();
		if (!_history.TryGetValue(realmId, out List<TaxReceipt> list)) return result;
		foreach (TaxReceipt receipt in list)
		{
			if (receipt.absoluteDay >= fromAbsoluteDay) result.Add(receipt);
		}
		return result;
	}

	public static void Clear()
	{
		_history.Clear();
	}
}
}
