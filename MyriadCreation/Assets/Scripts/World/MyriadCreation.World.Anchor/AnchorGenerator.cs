using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.World.Settlement;
using MyriadCreation.Core.Enums;

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
			int num = value.centerTileIndex;
			if (num < 0 || num >= _tiles.Length)
			{
				num = value.memberTiles[0];
			}
			(AnchorData, SettlementData) tuple = CreatePair(ref nextId, key, num, SiteMode.SedentarySettlement, IsProvinceCenter(value, num) ? SettlementForm.City : SettlementForm.Village);
			AnchorData item = tuple.Item1;
			SettlementData item2 = tuple.Item2;
			item2.hasMarket = true;
			item2.development = 20f + (float)_rng.NextDouble() * 30f;
			SeedPopulation(item2, 500f + (float)_rng.NextDouble() * 1500f);
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
					SeedPopulation(item4, 300f + (float)_rng.NextDouble() * 1000f);
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
			for (int i = 0; i < num4; i++)
			{
				int num5 = value.memberTiles[_rng.Next(value.memberTiles.Count)];
				if (!IsTileOccupied(dictionary, num5) && _tiles[num5].isLand)
				{
					(AnchorData, SettlementData) tuple4 = CreatePair(ref nextId, key, num5, SiteMode.SedentarySettlement);
					AnchorData item7 = tuple4.Item1;
					SettlementData item8 = tuple4.Item2;
					item8.development = 2f + (float)_rng.NextDouble() * 10f;
					SeedPopulation(item8, 50f + (float)_rng.NextDouble() * 300f);
					dictionary[item7.anchorId] = item7;
					dictionary2[item8.anchorId] = item8;
				}
			}
		}
		return (anchors: dictionary, settlements: dictionary2);
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
			constructionTier = 1
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

	private bool IsProvinceCenter(Province p, int tile)
	{
		return p.centerTileIndex == tile;
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
