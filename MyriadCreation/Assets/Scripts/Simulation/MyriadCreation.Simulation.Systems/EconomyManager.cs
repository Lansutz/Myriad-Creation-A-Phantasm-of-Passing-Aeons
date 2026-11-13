using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Units;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.LawHost;

namespace MyriadCreation.Simulation.Systems
{
public class EconomyManager
{
	private readonly TileData[] _tiles;

	private readonly Dictionary<int, TradeCenter> _tradeCenters;

	private readonly Dictionary<int, GoodsDef> _goodsDefs;

	private readonly List<Caravan> _caravans = new List<Caravan>();

	private readonly CurrencySystem _currency;

	private readonly TaxSystem _taxSystem;

	private int _nextCaravanId = 1;

	private int _storageUpdateDay = -1;

	private const int FoodGoodsId = 0;

	private const int SaltGoodsId = 3;

	private const float PersonFoodPerDay = 0.01f;

	private const float SaltPerPersonPerDay = 0.002f;

	private const float CargoEpsilon = 0.001f;

	/// <summary>同时存在的商队上限；达到上限后不再发车（滞留商队占满后贸易不应继续无界发车）。</summary>
	private const int MaxActiveCaravans = 20;

	/// <summary>商队到达后连续多少天仍有货入不了库就退役并处置货物，避免容量不释放导致贸易永久停摆。</summary>
	private const int CaravanStuckRetireDays = 5;

	/// <summary>
	/// 发车前的目的地余量门槛：目的地可用容量低于计划发货量的 10%（或不足 1）就跳过该路线，
	/// 避免"发车 → 5 天滞留退役 → 货物退回"的周期空转，也不占用商队名额。
	/// </summary>
	private const float DispatchDestinationCapacityRatio = 0.1f;

	private const float DispatchDestinationMinCapacity = 1f;

	/// <summary>目的地容量不足导致跳发车的 Warn 节流间隔（按 InitiateTrades 调用次数≈天数计）。</summary>
	private const int TradeCapacityWarnInterval = 30;

	/// <summary>
	/// 缺粮口径选择 (a)：人口惩罚归 GameWorld 的 Famine 事件（GameWorld.HandleFamine：人口 ×0.95、
	/// 满意度 −20、稳定度 −10），本系统只保留轻量的稳定度/秩序日罚，不再扣人口，避免同日双罚。
	/// 理由：EconomyManager 无法感知 Famine 事件是否已触发（其触发条件与本系统的缺粮判定同源），
	/// 方案 (b)"仅在未触发时施加"没有可靠的判定依据，故选 (a)。
	/// </summary>
	private const float FamineStabilityPenalty = 0.2f;

	private const float FamineOrderPenalty = 0.1f;

	/// <summary>商队 id → 到货后连续无法入库的天数（运行时状态，不入档）。</summary>
	private readonly Dictionary<int, int> _caravanStuckDays = new Dictionary<int, int>();

	/// <summary>InitiateTrades 的调用计数（≈已过天数），用于容量不足告警的节流。</summary>
	private int _tradeStepCount;

	private int _lastTradeCapacityWarnStep = -TradeCapacityWarnInterval;

	/// <summary>被节流抑制的容量不足跳发车次数，用于告警汇总。</summary>
	private int _suppressedTradeCapacitySkips;

	public EconomyManager(TileData[] tiles, Dictionary<int, TradeCenter> tradeCenters, Dictionary<int, GoodsDef> goodsDefs, CurrencySystem currency, TaxSystem taxSystem)
	{
		_tiles = tiles;
		_tradeCenters = tradeCenters;
		_goodsDefs = goodsDefs;
		_currency = currency;
		_taxSystem = taxSystem;
	}

	public void UpdateStorageCapacities()
	{
		if (_tiles == null || _tradeCenters == null)
		{
			return;
		}
		foreach (KeyValuePair<int, TradeCenter> tradeCenter in _tradeCenters)
		{
			TradeCenter value = tradeCenter.Value;
			int num = 10000;
			int num2 = 0;
			for (int i = 0; i < _tiles.Length; i++)
			{
				if (_tiles[i].regionId == value.regionId && _tiles[i].buildingLevels != null && _tiles[i].buildingLevels[0] > num2)
				{
					num2 = _tiles[i].buildingLevels[0];
				}
			}
			value.inventoryCapacity = (float)num * (1f + (float)num2 * 0.1f);
		}
	}

	public void DailyTick()
	{
		if (_storageUpdateDay < 0 || _storageUpdateDay >= 30)
		{
			UpdateStorageCapacities();
			_storageUpdateDay = 0;
		}
		else
		{
			_storageUpdateDay++;
		}
		UpdateAllSupplyDemand();
		CalculateAllRouteEfficiency();
		InitiateTrades();
		MoveCaravans();
		ProcessConsumption();
		_currency.DailyTick();
		ProcessShelfLife();
	}

	private void UpdateAllSupplyDemand()
	{
		int num = 16;
		foreach (KeyValuePair<int, TradeCenter> tradeCenter in _tradeCenters)
		{
			int num2 = tradeCenter.Key * num;
			int regionTileEnd = num2 + num;
			tradeCenter.Value.UpdateSupplyDemand(_tiles, _goodsDefs, num2, regionTileEnd);
		}
	}

	private void CalculateAllRouteEfficiency()
	{
		foreach (TradeCenter value in _tradeCenters.Values)
		{
			foreach (TradeRoute tradeRoute in value.tradeRoutes)
			{
				tradeRoute.CalculateEfficiency(_tiles);
			}
		}
	}

	/// <summary>发车步骤（DailyTick 子步骤，公开以便系统集成与测试单独驱动）。</summary>
	public void InitiateTrades()
	{
		_tradeStepCount++;
		if (_caravans.Count >= MaxActiveCaravans)
		{
			return;
		}
		foreach (TradeCenter value2 in _tradeCenters.Values)
		{
			foreach (TradeRoute tradeRoute in value2.tradeRoutes)
			{
				// 发车循环内复查上限：滞留商队不应让贸易继续无界发车
				if (_caravans.Count >= MaxActiveCaravans)
				{
					return;
				}
				if (tradeRoute.isBlocked || tradeRoute.currentEfficiency < 0.1f || !_tradeCenters.TryGetValue(tradeRoute.toRegionId, out var value))
				{
					continue;
				}
				int num = -1;
				float num2 = 0f;
				foreach (KeyValuePair<int, float> item in value2.localSupply)
				{
					int key = item.Key;
					float goodsPrice = value2.GetGoodsPrice(key, _goodsDefs);
					float num3 = (value.GetGoodsPrice(key, _goodsDefs) - goodsPrice) * tradeRoute.currentEfficiency;
					if (num3 > num2 && value2.inventory.GetValueOrDefault(key, 0f) > 10f)
					{
						num2 = num3;
						num = key;
					}
				}
				if (num >= 0 && num2 > 0.5f)
				{
					float num4 = Mathf.Min(100f, value2.inventory[num] * 0.3f);
					float destinationCapacityLeft = value.inventoryCapacity - value.GetTotalInventory();
					// 发车前检查目的地可用容量：不足计划发货量的 10%（或不足 1）就跳过该路线，
					// 避免满仓目的地被反复发车（发车→滞留退役→退货的空转），且不占用商队名额
					if (destinationCapacityLeft < DispatchDestinationMinCapacity
						|| destinationCapacityLeft < num4 * DispatchDestinationCapacityRatio)
					{
						WarnTradeCapacitySkipped(num4, destinationCapacityLeft);
						continue;
					}
					if (value2.RemoveGoods(num, num4))
					{
						Caravan caravan = new Caravan
						{
							caravanId = _nextCaravanId++,
							fromRegionId = tradeRoute.fromRegionId,
							toRegionId = tradeRoute.toRegionId,
							currentNodeIndex = 0,
							isMoving = true
						};
						caravan.cargo[num] = num4;
						_caravans.Add(caravan);
					}
				}
			}
		}
	}

	/// <summary>目的地容量不足导致跳发车的 Warn（按 TradeCapacityWarnInterval 节流，避免每日刷屏）。</summary>
	private void WarnTradeCapacitySkipped(float plannedAmount, float destinationCapacityLeft)
	{
		_suppressedTradeCapacitySkips++;
		if (_tradeStepCount - _lastTradeCapacityWarnStep < TradeCapacityWarnInterval)
		{
			return;
		}
		_lastTradeCapacityWarnStep = _tradeStepCount;
		Debug.LogWarning((object)$"[EconomyManager] 目的地仓库余量不足，跳过发车 {_suppressedTradeCapacitySkips} 次（最近一次计划发货 {plannedAmount:F1}，目的地余量 {destinationCapacityLeft:F1}）");
		_suppressedTradeCapacitySkips = 0;
	}

	/// <summary>商队移动/到货结算步骤（DailyTick 子步骤，公开以便系统集成与测试单独驱动）。</summary>
	public void MoveCaravans()
	{
		for (int num = _caravans.Count - 1; num >= 0; num--)
		{
			Caravan caravan = _caravans[num];
			TradeCenter origin = _tradeCenters.GetValueOrDefault(caravan.fromRegionId);
			if (origin == null)
			{
				RetireCaravan(num, caravan, $"起点区域库 {caravan.fromRegionId} 不存在");
				continue;
			}
			TradeRoute tradeRoute = origin.tradeRoutes.Find((TradeRoute r) => r.toRegionId == caravan.toRegionId);
			if (tradeRoute == null)
			{
				RetireCaravan(num, caravan, $"到区域 {caravan.toRegionId} 的贸易路线不存在");
				continue;
			}
			if (caravan.isMoving && !caravan.MoveTick(tradeRoute, _tiles))
			{
				continue;
			}
			TradeCenter destination = _tradeCenters.GetValueOrDefault(caravan.toRegionId);
			if (destination == null)
			{
				RetireCaravan(num, caravan, $"目的地区域库 {caravan.toRegionId} 不存在");
				continue;
			}
			DeliverOrRetireStuckCaravan(num, caravan, destination);
		}
	}

	/// <summary>
	/// 到货结算：能入多少入多少，剩余留在 cargo；同一商队连续 <see cref="CaravanStuckRetireDays"/> 天仍有货
	/// 入不了库就退役（退回区域库 / 装不下的记损失 + 日志），避免目的地容量不释放时商队永久占位、阻塞发车。
	/// </summary>
	private void DeliverOrRetireStuckCaravan(int index, Caravan caravan, TradeCenter destination)
	{
		float accepted = DeliverCargo(destination, caravan.cargo);
		float left = SumCargo(caravan.cargo);
		if (left <= CargoEpsilon)
		{
			_caravanStuckDays.Remove(caravan.caravanId);
			_caravans.RemoveAt(index);
			return;
		}
		int stuckDays = _caravanStuckDays.GetValueOrDefault(caravan.caravanId, 0) + 1;
		_caravanStuckDays[caravan.caravanId] = stuckDays;
		if (stuckDays >= CaravanStuckRetireDays)
		{
			Debug.LogWarning((object)$"[EconomyManager] 商队#{caravan.caravanId} 已连续 {stuckDays} 天无法入库（剩余 {left:F1}），退役并处置在途货物");
			RetireCaravan(index, caravan, $"连续 {stuckDays} 天无法入库");
		}
		else if (stuckDays == 1)
		{
			Debug.LogWarning((object)$"[EconomyManager] 商队#{caravan.caravanId} 到达区域 {caravan.toRegionId} 但仓库容量不足：本次入库 {accepted:F1}，剩余 {left:F1} 暂存商队等待入库（最多等待 {CaravanStuckRetireDays} 天，不蒸发）");
		}
	}

	/// <summary>登记一支商队到运行时商队列表（读档恢复 / 系统集成 / 测试注入用；商队本身不入档）。</summary>
	public void RegisterCaravan(Caravan caravan)
	{
		if (caravan == null || _caravans.Contains(caravan))
		{
			return;
		}
		_caravans.Add(caravan);
	}

	/// <summary>到货结算：逐项把商队货物入目的地仓储，返回实际入库总量；未入账部分原样留在 cargo（不蒸发）。</summary>
	public static float DeliverCargo(TradeCenter destination, Dictionary<int, float> cargo)
	{
		if (destination == null || cargo == null || cargo.Count == 0)
		{
			return 0f;
		}
		float acceptedTotal = 0f;
		List<int> goodsIds = new List<int>(cargo.Keys);
		foreach (int goodsId in goodsIds)
		{
			float amount = cargo[goodsId];
			float accepted = destination.AddGoodsReturningAccepted(goodsId, amount);
			acceptedTotal += accepted;
			float left = amount - accepted;
			if (left <= CargoEpsilon)
			{
				cargo.Remove(goodsId);
			}
			else
			{
				cargo[goodsId] = left;
			}
		}
		return acceptedTotal;
	}

	/// <summary>
	/// 商队被移除时的在途货物处置：优先退回起点区域库，起点不存在时退回最近可用的区域库；
	/// 仓库仍然装不下的部分**明确记为损失并打日志**。返回损失量。
	/// </summary>
	public float ReturnCaravanCargo(Caravan caravan, string reason)
	{
		if (caravan == null || caravan.cargo == null || caravan.cargo.Count == 0)
		{
			return 0f;
		}
		float before = SumCargo(caravan.cargo);
		TradeCenter home = _tradeCenters.GetValueOrDefault(caravan.fromRegionId);
		if (home == null)
		{
			home = FindNearestTradeCenter(caravan.fromRegionId);
		}
		if (home != null)
		{
			DeliverCargo(home, caravan.cargo);
		}
		float lost = SumCargo(caravan.cargo);
		float returned = before - lost;
		if (lost > CargoEpsilon)
		{
			Debug.LogWarning((object)$"[EconomyManager] 商队#{caravan.caravanId} 被移除（{reason}）：在途货物 {before:F1}，退回区域库 {returned:F1}，损失 {lost:F1}");
		}
		else if (before > CargoEpsilon)
		{
			Debug.Log((object)$"[EconomyManager] 商队#{caravan.caravanId} 被移除（{reason}）：在途货物 {before:F1} 已全部退回区域库");
		}
		caravan.cargo.Clear();
		return lost;
	}

	private void RetireCaravan(int index, Caravan caravan, string reason)
	{
		ReturnCaravanCargo(caravan, reason);
		_caravanStuckDays.Remove(caravan.caravanId);
		_caravans.RemoveAt(index);
	}

	private TradeCenter FindNearestTradeCenter(int regionId)
	{
		TradeCenter nearest = null;
		int bestDistance = int.MaxValue;
		foreach (TradeCenter center in _tradeCenters.Values)
		{
			int distance = Mathf.Abs(center.regionId - regionId);
			if (distance < bestDistance)
			{
				bestDistance = distance;
				nearest = center;
			}
		}
		return nearest;
	}

	private static float SumCargo(Dictionary<int, float> cargo)
	{
		float total = 0f;
		if (cargo == null)
		{
			return total;
		}
		foreach (KeyValuePair<int, float> item in cargo)
		{
			total += item.Value;
		}
		return total;
	}

	private void ProcessConsumption()
	{
		int starvingTileCount = 0;
		float starvingPopulation = 0f;
		for (int i = 0; i < _tiles.Length; i++)
		{
			if (!_tiles[i].exists || !_tiles[i].isLand || _tiles[i].populationBlocks == null)
			{
				continue;
			}
			float num = 0f;
			foreach (PopulationBlock populationBlock in _tiles[i].populationBlocks)
			{
				num += populationBlock.count;
			}
			if (num <= 0f)
			{
				continue;
			}
			int regionId = _tiles[i].regionId;
			if (!_tradeCenters.TryGetValue(regionId, out var value))
			{
				continue;
			}
			float amount = num * PersonFoodPerDay;
			float amount2 = num * SaltPerPersonPerDay;
			float foodAvailable = value.inventory.GetValueOrDefault(FoodGoodsId, 0f);
			if (foodAvailable < amount)
			{
				// 库存不足日需：先把剩余口粮吃光再进入饥荒分支，避免库存永远卡在 0 < stock < need
				if (foodAvailable > 0f)
				{
					value.RemoveGoods(FoodGoodsId, foodAvailable);
				}
				ApplyFaminePenalty(i);
				starvingTileCount++;
				starvingPopulation += num;
			}
			else
			{
				value.RemoveGoods(FoodGoodsId, amount);
			}
			value.RemoveGoods(SaltGoodsId, amount2);
		}
		if (starvingTileCount > 0)
		{
			Debug.LogWarning((object)$"[EconomyManager] 饥荒：{starvingTileCount} 个地块（约 {starvingPopulation:F0} 人）缺粮，稳定度/秩序下降（人口后果由 GameWorld Famine 事件负责）");
		}
	}

	/// <summary>
	/// 缺粮后果（口径选择 a：人口惩罚归 GameWorld Famine 事件，本系统只做轻量稳定度/秩序惩罚）。
	/// GameWorld.PopulationTick 在同一缺粮条件下会派发 Famine 事件，由 HandleFamine 执行人口 ×0.95、
	/// 满意度 −20、稳定度 −10 的重罚；本系统若再扣人口会与事件叠加成双罚，且无法感知事件是否已触发
	/// （Famine 触发条件与本系统的缺粮判定同源），因此这里只保留 −0.2 稳定度 / −0.1 秩序的轻量日罚。
	/// </summary>
	private void ApplyFaminePenalty(int tileIndex)
	{
		_tiles[tileIndex].stability = Mathf.Max(0f, _tiles[tileIndex].stability - FamineStabilityPenalty);
		_tiles[tileIndex].order = Mathf.Max(0f, _tiles[tileIndex].order - FamineOrderPenalty);
	}

	private void ProcessShelfLife()
	{
		foreach (TradeCenter value2 in _tradeCenters.Values)
		{
			foreach (int item in new List<int>(value2.inventory.Keys))
			{
				if (_goodsDefs.TryGetValue(item, out var value) && value.hasShelfLife)
				{
					value2.inventory[item] *= 0.999f;
					if (value2.inventory[item] < 0.01f)
					{
						value2.inventory.Remove(item);
					}
				}
			}
		}
	}

	/// <summary>结算税收；multiplier 为法律/法令的税收乘子（LawEffectRuntime.GetTaxMultiplier）。</summary>
	public float SettleTaxes(int realmId, float multiplier = 1f)
	{
		float num = 0f;
		for (int i = 0; i < _tiles.Length; i++)
		{
			if (_tiles[i].exists && _tiles[i].isLand && _tiles[i].ownerRealmId == realmId)
			{
				float baseOutput = _tiles[i].fertility * _tiles[i].development * 10f;
				GameEnums.SocialClass dominantClass = GetDominantClass(_tiles[i]);
				num += _taxSystem.CalculateTileTax(_tiles[i], baseOutput, dominantClass);
			}
		}
		return num * multiplier;
	}

	private GameEnums.SocialClass GetDominantClass(TileData tile)
	{
		if (tile.populationBlocks == null || tile.populationBlocks.Count == 0)
		{
			return GameEnums.SocialClass.Peasant;
		}
		Dictionary<GameEnums.SocialClass, float> dictionary = new Dictionary<GameEnums.SocialClass, float>();
		foreach (PopulationBlock populationBlock in tile.populationBlocks)
		{
			dictionary[populationBlock.socialClass] = dictionary.GetValueOrDefault(populationBlock.socialClass, 0f) + populationBlock.count;
		}
		GameEnums.SocialClass result = GameEnums.SocialClass.Peasant;
		float num = 0f;
		foreach (KeyValuePair<GameEnums.SocialClass, float> item in dictionary)
		{
			if (item.Value > num)
			{
				num = item.Value;
				result = item.Key;
			}
		}
		return result;
	}

	public IReadOnlyList<Caravan> GetActiveCaravans()
	{
		return _caravans;
	}

	public CurrencySystem GetCurrencySystem()
	{
		return _currency;
	}

	public TaxSystem GetTaxSystem()
	{
		return _taxSystem;
	}

	public TradeCenter GetTradeCenter(int regionId)
	{
		return _tradeCenters.GetValueOrDefault(regionId);
	}

	public TreasuryVault GetNationalVault(RealmData realm)
	{
		return TreasuryLedger.NationalVault(realm);
	}

	public TradeCenter GetRegionalVault(int regionId)
	{
		return GetTradeCenter(regionId);
	}
}
}
