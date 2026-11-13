using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Units;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>一次税收结算的结果：只记录实际发生的实物/货币，不产生“抽象税额”。</summary>
public struct TaxCollectionResult
{
	public float goodsCollected;

	public float moneyCollected;

	public int regions;

	public int receipts;
}

/// <summary>
/// 机制化税收（P5.3）：地方仓储 → 有管辖权的行政区划持有人 → 运输损耗 → 国家仓；
/// 只有政权进入铸币阶段、且当地市场有真实需求时，才把非食用货物卖出换成国库货币。
/// 没有行政区划、没有持有人、没有库存、没有市场，就没有税；不再用 fertility×development 造税基。
/// </summary>
public static class TaxCollectionSystem
{
	public const int SaltGoodsId = 3;

	private const float PersonFoodPerDay = 0.01f;

	public static TaxCollectionResult Collect(
		RealmData realm,
		CharacterManager characters,
		TileData[] tiles,
		Dictionary<int, TradeCenter> centers,
		Dictionary<int, GoodsDef> goodsDefs,
		int absoluteDay,
		float multiplier = 1f,
		int mapWidth = 128)
	{
		TaxCollectionResult result = default;
		if (realm == null || characters == null || tiles == null || centers == null || goodsDefs == null)
		{
			return result;
		}
		TaxSystem tax = realm.taxSystem ?? new TaxSystem();
		HashSet<int> authorizedRegions = ResolveAuthorizedRegions(realm, characters, tiles);
		if (authorizedRegions.Count == 0) return result;

		Dictionary<int, List<int>> ownedByRegion = new Dictionary<int, List<int>>();
		Dictionary<int, int> landByRegion = new Dictionary<int, int>();
		for (int i = 0; i < tiles.Length; i++)
		{
			if (!tiles[i].exists || !tiles[i].isLand) continue;
			int regionId = tiles[i].regionId;
			landByRegion.TryGetValue(regionId, out int land);
			landByRegion[regionId] = land + 1;
			if (tiles[i].ownerRealmId != realm.realmId) continue;
			if (!ownedByRegion.TryGetValue(regionId, out List<int> owned))
			{
				owned = new List<int>();
				ownedByRegion[regionId] = owned;
			}
			owned.Add(i);
		}

		int capitalTile = ResolveCapitalTile(realm, characters);
		foreach (int regionId in authorizedRegions)
		{
			if (!ownedByRegion.TryGetValue(regionId, out List<int> ownedTiles) || ownedTiles.Count == 0) continue;
			if (!centers.TryGetValue(regionId, out TradeCenter center) || center?.inventory == null) continue;
			int landCount = Mathf.Max(1, landByRegion.GetValueOrDefault(regionId, ownedTiles.Count));
			float ownedFraction = Mathf.Clamp01((float)ownedTiles.Count / landCount);
			if (ownedFraction <= 0f) continue;

			float population = 0f;
			foreach (int tileIndex in ownedTiles) population += PopulationLedger.Total(tiles[tileIndex]);
			float edibleStock = EdibleStock(center, goodsDefs);
			float edibleReserve = Mathf.Min(edibleStock, population * PersonFoodPerDay * Mathf.Max(1, tax.reserveDays));
			float taxableEdibleShare = edibleStock <= 0f ? 0f : Mathf.Clamp01((edibleStock - edibleReserve) / edibleStock);

			float loss = ResolveTransportLoss(center.centerTileIndex, capitalTile, mapWidth, tax.transportLossPerTile);
			float kept = 1f - loss;
			bool collectedAny = false;

			List<int> goodsIds = new List<int>(center.inventory.Keys);
			foreach (int goodsId in goodsIds)
			{
				float amount = center.inventory.GetValueOrDefault(goodsId, 0f);
				if (amount <= 0f) continue;
				if (!goodsDefs.TryGetValue(goodsId, out GoodsDef def)) continue;
				float rate = RateFor(def, goodsId, tax) * ownedFraction * Mathf.Max(0f, multiplier);
				if (rate <= 0f) continue;
				bool edible = IsEdible(def);
				float taxable = edible ? amount * taxableEdibleShare : amount;
				float take = Mathf.Min(amount, taxable * Mathf.Clamp(rate, 0f, 0.9f));
				if (take <= 0.0001f) continue;

				center.RemoveGoods(goodsId, take);
				float delivered = take * kept;
				if (delivered <= 0f) continue;
				TreasuryLedger.NationalVault(realm).Add(goodsId, delivered);
				TaxReceiptLedger.Record(realm.realmId, absoluteDay, regionId, goodsId, delivered);
				result.goodsCollected += delivered;
				result.receipts++;
				collectedAny = true;

				// 货币化：只有铸币/纸币阶段，且只卖非食用货物；销售量受当地市场日需求限制。
				if (realm.currencySystem != null
					&& realm.currencySystem.currentStage >= GameEnums.CurrencyStage.MintedCoin
					&& !edible)
				{
					float price = center.GetGoodsPrice(goodsId, goodsDefs);
					float demand = center.localDemand != null ? center.localDemand.GetValueOrDefault(goodsId, 0f) : 0f;
					float sellable = Mathf.Min(delivered, demand);
					if (sellable > 0.0001f && TreasuryLedger.NationalVault(realm).TrySpend(goodsId, sellable))
					{
						float money = sellable * price;
						RealmTreasury.Add(realm, money);
						TreasuryLedger.Record(realm.realmId, absoluteDay, money, "tax");
						result.moneyCollected += money;
					}
				}
			}
			if (collectedAny) result.regions++;
		}
		return result;
	}

	/// <summary>有管辖权的地区：行政区划有在世持有人；首都级区划允许君主本人直接征收。</summary>
	public static HashSet<int> ResolveAuthorizedRegions(RealmData realm, CharacterManager characters, TileData[] tiles)
	{
		HashSet<int> regions = new HashSet<int>();
		if (realm?.adminDivisions == null || characters == null || tiles == null) return regions;
		int supremeRulerId = realm.GetSupremeRulerId();
		int capitalTile = ResolveCapitalTile(realm, characters);
		foreach (AdminDivision division in realm.adminDivisions)
		{
			if (division == null || division.tiles == null) continue;
			bool authorized = false;
			if (division.holderCharacterId >= 0)
			{
				CharacterData holder = characters.GetCharacter(division.holderCharacterId);
				authorized = holder != null && holder.isAlive;
			}
			// 首都级区划允许君主直领：但他只能直接管到首都所在地区，不能隔空征全国。
			if (authorized && division.level <= 1 && capitalTile >= 0)
			{
				regions.Add(tiles[capitalTile].regionId);
				continue;
			}
			if (!authorized && division.level <= 1 && supremeRulerId >= 0 && capitalTile >= 0)
			{
				CharacterData ruler = characters.GetCharacter(supremeRulerId);
				if (ruler != null && ruler.isAlive)
				{
					regions.Add(tiles[capitalTile].regionId);
				}
				continue;
			}
			if (!authorized) continue;
			foreach (int tileIndex in division.tiles)
			{
				if (tileIndex < 0 || tileIndex >= tiles.Length) continue;
				regions.Add(tiles[tileIndex].regionId);
			}
		}
		return regions;
	}

	public static float EdibleStock(TradeCenter center, Dictionary<int, GoodsDef> goodsDefs)
	{
		if (center?.inventory == null || goodsDefs == null) return 0f;
		float total = 0f;
		foreach (KeyValuePair<int, float> pair in center.inventory)
		{
			if (pair.Value <= 0f) continue;
			if (!goodsDefs.TryGetValue(pair.Key, out GoodsDef def) || !IsEdible(def)) continue;
			total += pair.Value * (def.nutrition <= 0f ? 1f : def.nutrition);
		}
		return total;
	}

	private static bool IsEdible(GoodsDef def)
	{
		// 盐也归在 Food 类，但不是口粮；只有内容表标记 edible 或谷物/牲畜才算。
		return def.edible || def.category == GameEnums.GoodsCategory.Crop
			|| def.category == GameEnums.GoodsCategory.Livestock;
	}

	private static float RateFor(GoodsDef def, int goodsId, TaxSystem tax)
	{
		if (goodsId == SaltGoodsId && tax.saltMonopolyTax > 0f) return tax.saltMonopolyTax;
		switch (def.category)
		{
			case GameEnums.GoodsCategory.Food:
			case GameEnums.GoodsCategory.Crop:
				return tax.agriculturalTax;
			case GameEnums.GoodsCategory.Livestock:
				return tax.livestockTax;
			case GameEnums.GoodsCategory.MetalOre:
			case GameEnums.GoodsCategory.Metal:
			case GameEnums.GoodsCategory.Gem:
			case GameEnums.GoodsCategory.Stone:
				return tax.miningTax;
			case GameEnums.GoodsCategory.Equipment:
			case GameEnums.GoodsCategory.Ceramic:
			case GameEnums.GoodsCategory.Cloth:
			case GameEnums.GoodsCategory.Leather:
			case GameEnums.GoodsCategory.Glass:
				return tax.craftTax;
			case GameEnums.GoodsCategory.Luxury:
				return tax.luxuryTax;
			default:
				return 0f;
		}
	}

	private static int ResolveCapitalTile(RealmData realm, CharacterManager characters)
	{
		CharacterData ruler = characters?.GetCharacter(realm.GetSupremeRulerId());
		if (ruler != null && ruler.currentTile >= 0) return ruler.currentTile;
		foreach (int tileIndex in realm.coreTiles)
		{
			if (tileIndex >= 0) return tileIndex;
		}
		return -1;
	}

	private static float ResolveTransportLoss(int fromTile, int toTile, int mapWidth, float lossPerTile)
	{
		if (fromTile < 0 || toTile < 0 || lossPerTile <= 0f) return 0f;
		int width = Mathf.Max(1, mapWidth);
		int dx = Mathf.Abs(fromTile % width - toTile % width);
		int dy = Mathf.Abs(fromTile / width - toTile / width);
		float distance = dx + dy;
		return Mathf.Clamp(distance * lossPerTile, 0f, 0.5f);
	}
}
}
