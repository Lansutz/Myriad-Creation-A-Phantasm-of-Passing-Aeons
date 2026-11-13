using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.World.Settlement;
using MyriadCreation.Core.Enums;
using UnityEngine;
using Random = System.Random;

namespace MyriadCreation.World.Anchor
{
public class AnchorGenerator
{
	private readonly TileData[] _tiles;

	private readonly int _width;

	private readonly int _height;

	private readonly Dictionary<int, Province> _provinces;

	private readonly Random _rng;

	public const int MinSettlementsPerProvince = 1;

	public const int MaxSettlementsPerProvince = 6;

	public const float PortSpawnChance = 0.6f;

	public const float CitySpawnChance = 0.35f;

	public const float FortressSpawnChance = 0.25f;

	public AnchorGenerator(TileData[] tiles, int width, int height, Dictionary<int, Province> provinces, int seed)
	{
		_tiles = tiles;
		_width = width;
		_height = height;
		_provinces = provinces;
		_rng = new Random(seed + 999);
	}

	public (Dictionary<int, AnchorData> anchors, Dictionary<int, SettlementData> settlements) Generate()
	{
		Dictionary<int, AnchorData> dictionary = new Dictionary<int, AnchorData>();
		Dictionary<int, SettlementData> dictionary2 = new Dictionary<int, SettlementData>();
		int nextId = 0;
		foreach (KeyValuePair<int, Province> province in _provinces)
		{
			int key = province.Key;
			Province value = province.Value;
			if (value.memberTiles.Count == 0)
			{
				continue;
			}
			int num = PickProvinceCenterSite(value);
			if (num < 0 || num >= _tiles.Length)
			{
				num = value.centerTileIndex;
			}
			(AnchorData, SettlementData) tuple = CreatePair(ref nextId, key, num, SiteMode.SedentarySettlement, SettlementForm.City);
			AnchorData item = tuple.Item1;
			SettlementData item2 = tuple.Item2;
			item2.hasMarket = true;
			item2.development = 20f + (float)_rng.NextDouble() * 30f;
			// 初始人口按选址的土地潜力给：干旱海岸不能凭空塞进两千人，否则开局就过载、几十年后饿死。
			SeedPopulation(item2, SeedFromScore(SiteScore(num), 500f));
			dictionary[item.anchorId] = item;
			dictionary2[item2.anchorId] = item2;
			if (HasCoastalTile(value))
			{
				int num2 = FindCoastalTile(value);
				if (num2 >= 0 && _rng.NextDouble() < 0.6000000238418579)
				{
					(AnchorData, SettlementData) tuple2 = CreatePair(ref nextId, key, num2, SiteMode.SedentarySettlement, SettlementForm.City);
					AnchorData item3 = tuple2.Item1;
					SettlementData item4 = tuple2.Item2;
					item4.isPort = true;
					item4.isCoastal = true;
					item4.hasMarket = true;
					item4.tradePower = 30f + (float)_rng.NextDouble() * 50f;
					item4.development = 15f + (float)_rng.NextDouble() * 25f;
					SeedPopulation(item4, SeedFromScore(SiteScore(num2), 350f));
					dictionary[item3.anchorId] = item3;
					dictionary2[item4.anchorId] = item4;
				}
			}
			if (IsBorderProvince(value) && _rng.NextDouble() < 0.25)
			{
				int num3 = FindBorderTile(value);
				if (num3 >= 0)
				{
					(AnchorData, SettlementData) tuple3 = CreatePair(ref nextId, key, num3, SiteMode.FortifiedOutpost);
					AnchorData item5 = tuple3.Item1;
					SettlementData item6 = tuple3.Item2;
					item6.fortification = 3f + (float)_rng.NextDouble() * 5f;
					item6.garrison = 100 + _rng.Next(200);
					item6.development = 5f + (float)_rng.NextDouble() * 15f;
					dictionary[item5.anchorId] = item5;
					dictionary2[item6.anchorId] = item6;
				}
			}
			int num4 = Math.Min(3, value.memberTiles.Count / 40);
			List<int> extraSites = PickExtraSites(value, dictionary, num4);
			for (int i = 0; i < extraSites.Count; i++)
			{
				int num5 = extraSites[i];
				(AnchorData, SettlementData) tuple4 = CreatePair(ref nextId, key, num5, SiteMode.SedentarySettlement);
				AnchorData item7 = tuple4.Item1;
				SettlementData item8 = tuple4.Item2;
				item8.development = 2f + (float)_rng.NextDouble() * 10f;
				SeedPopulation(item8, SeedFromScore(SiteScore(num5), 250f));
				dictionary[item7.anchorId] = item7;
				dictionary2[item8.anchorId] = item8;
			}
		}
		return (anchors: dictionary, settlements: dictionary2);
	}

	/// <summary>
	/// 选址质量：与农业产出同一组乘子（肥力 × NPP × 水），再加河流/海岸的灌溉与运输便利。
	/// 聚落不该被均匀撒在省域里——人群会优先落在能养活自己的土地上。
	/// </summary>
	private float SiteScore(int tileIndex)
	{
		if (tileIndex < 0 || tileIndex >= _tiles.Length) return -1f;
		TileData tile = _tiles[tileIndex];
		if (!tile.exists || !tile.isLand) return -1f;
		float fertility = Mathf.Clamp01(tile.fertility);
		float npp = tile.npp > 0f ? Mathf.Clamp01(tile.npp) : 0.5f;
		float water = tile.isRiver ? 1.25f : (tile.isCoast ? 1.1f : 1f);
		return (0.2f + 0.8f * fertility) * (0.5f + 0.5f * npp) * water;
	}

	/// <summary>省治聚落落在省域内地力最好的地块；同分取小索引，保证同 seed 同结果。</summary>
	private int PickProvinceCenterSite(Province province)
	{
		int best = -1;
		float bestScore = -1f;
		foreach (int tileIndex in province.memberTiles)
		{
			if (!_tiles[tileIndex].exists || !_tiles[tileIndex].isLand) continue;
			float score = SiteScore(tileIndex);
			if (score > bestScore || (score == bestScore && tileIndex < best))
			{
				bestScore = score;
				best = tileIndex;
			}
		}
		return best;
	}

	/// <summary>额外村落从“未占用且地力靠前”的省域地块中抽取，而不是全省均匀随机。</summary>
	private List<int> PickExtraSites(Province province, Dictionary<int, AnchorData> anchors, int count)
	{
		List<int> candidates = new List<int>();
		if (count <= 0) return candidates;
		foreach (int tileIndex in province.memberTiles)
		{
			if (!_tiles[tileIndex].exists || !_tiles[tileIndex].isLand) continue;
			if (IsTileOccupied(anchors, tileIndex)) continue;
			candidates.Add(tileIndex);
		}
		candidates.Sort((a, b) =>
		{
			int byScore = SiteScore(b).CompareTo(SiteScore(a));
			return byScore != 0 ? byScore : a.CompareTo(b);
		});
		int pool = Mathf.Max(count, candidates.Count / 3);
		List<int> picked = new List<int>();
		while (picked.Count < count && candidates.Count > 0)
		{
			int pick = _rng.Next(Mathf.Min(pool, candidates.Count));
			picked.Add(candidates[pick]);
			candidates.RemoveAt(pick);
		}
		return picked;
	}


	/// <summary>
	/// 初始人口按选址得分的平方给：好地开村就有几百人，贫瘠地只有几十人。
	/// 线性给人口会让干旱海岸也拿到几百人的初始规模，开局就过载、几十年后被饥荒打回原形。
	/// </summary>
	private static float SeedFromScore(float score, float maxPopulation)
	{
		float s = Mathf.Clamp01(score);
		return 40f + maxPopulation * s * s;
	}

	/// <summary>生成期把聚落初始人口写进地块人口块（D40：地块块是唯一权威）。</summary>
	private void SeedPopulation(SettlementData settlement, float amount)
	{
		if (settlement == null || amount <= 0f)
		{
			return;
		}
		int tileIndex = settlement.tileIndex;
		if (tileIndex < 0 || tileIndex >= _tiles.Length)
		{
			return;
		}
		TileData tile = _tiles[tileIndex];
		// 聚落意味着土地已被开垦：把聚落开发度折算成地块开发度（0-1），
		// 否则人口 500+ 的城市仍按未开发土地产粮，会因产出低于口粮而被饥荒拖死。
		tile.development = Mathf.Max(tile.development, Mathf.Clamp01(settlement.development / 50f));
		tile.populationBlocks ??= new List<PopulationBlock>();
		tile.populationBlocks.Add(new PopulationBlock
		{
			count = amount,
			raceId = 0,
			cultureId = -1,
			faithId = -1,
			socialClass = GameEnums.SocialClass.Peasant,
			satisfaction = 60f
		});
		_tiles[tileIndex] = tile;
	}

	private (AnchorData, SettlementData) CreatePair(ref int nextId, int provinceId, int tileIndex, SiteMode mode, SettlementForm type = SettlementForm.Village)
	{
		ref TileData reference = ref _tiles[tileIndex];
		int anchorId = nextId++;
		AnchorData item = new AnchorData
		{
			anchorId = anchorId,
			provinceId = provinceId,
			tileIndex = tileIndex,
			x = 0.5f,
			y = 0.5f
		};
		SettlementData settlementData = new SettlementData
		{
			anchorId = anchorId,
			settlementName = GenerateName(reference, mode, type),
			isCoastal = reference.isCoast,
			siteMode = mode,
			constructionProgress = 0f,
			constructionTier = 1,
			// 初始社会秩序是“无人扰动”的中性值，不是加成；后续由事件/政治 tick 改变。
			settlementStability = 50f,
			// 生成期 SeedPopulation 需要 settlement.tileIndex；它由 anchor 派生，
			// 不能等 GameWorld.AdoptSettlements 之后再补，否则初始人口会写进 -1 号地块。
			anchor = item
		};
		SettlementTypologySystem.DeriveInitialType(settlementData, reference, _width, _height);
		EconomicCompositionSystem.InitializeComposition(settlementData, reference);
		settlementData.EnsureBlocksFromLegacy();
		settlementData.settlementForm = type;
		SettlementData settlementData2 = settlementData;
		// 用派生后的 siteMode（军事隘口会被定成据点），而不是入参 mode——
		// 否则被提升为据点的聚落会拿到城市的 LevelIII，等级与模式不一致。
		settlementData2.settlementLevel = settlementData.siteMode switch
		{
			SiteMode.FortifiedOutpost => SettlementLevel.LevelII, 
			SiteMode.SedentarySettlement => (settlementData.settlementForm != SettlementForm.City) ? SettlementLevel.LevelII : SettlementLevel.LevelIII, 
			_ => SettlementLevel.LevelI, 
		};
		settlementData.SetLegacyCategoryFromSiteMode();
		return (item, settlementData);
	}

	private bool HasCoastalTile(Province p)
	{
		foreach (int memberTile in p.memberTiles)
		{
			if (_tiles[memberTile].isCoast)
			{
				return true;
			}
		}
		return false;
	}

	private int FindCoastalTile(Province p)
	{
		foreach (int memberTile in p.memberTiles)
		{
			if (_tiles[memberTile].isCoast)
			{
				return memberTile;
			}
		}
		return -1;
	}

	private bool IsBorderProvince(Province p)
	{
		foreach (int memberTile in p.memberTiles)
		{
			if (Province.IsBorder(_tiles, _width, _height, memberTile))
			{
				return true;
			}
		}
		return false;
	}

	private int FindBorderTile(Province p)
	{
		foreach (int memberTile in p.memberTiles)
		{
			if (Province.IsBorder(_tiles, _width, _height, memberTile))
			{
				return memberTile;
			}
		}
		return -1;
	}

	private bool IsTileOccupied(Dictionary<int, AnchorData> anchors, int tile)
	{
		foreach (AnchorData value in anchors.Values)
		{
			if (value.tileIndex == tile)
			{
				return true;
			}
		}
		return false;
	}

	private string GenerateName(TileData tile, SiteMode mode, SettlementForm type)
	{
		string text;
		if (tile.elevation01 > 0.55f)
		{
			text = "山";
		}
		else
		{
			text = (tile.isCoast ? "海" : "原");
		}
		string text2;
		if (tile.annualPrecipMm > 900f)
		{
			text2 = "润";
		}
		else
		{
			text2 = ((tile.annualPrecipMm < 300f) ? "干" : "丰");
		}
		return text + text2 + mode switch
		{
			SiteMode.FortifiedOutpost => "寨", 
			SiteMode.TemporaryEncampment => "营", 
			_ => (type == SettlementForm.City) ? "城" : "村", 
		};
	}
}
}
