using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Settlement;
using UnityEngine;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.Actors
{
[Serializable]
public class CampData : HabitationSite
{
	public int campId;

	private CampType _type;

	public int tileIndex;

	public int ownerRealmId = -1;

	public int ownerActorId = -1;

	[NonSerialized]
	public AnchorData anchor;

	public float defense = 10f;

	public float supplies = 100f;

	public float morale = 80f;

	public int establishedDay;

	public float permanence;

	/// <summary>P11.6 移动状态；默认扎营。归属只认 ownerActorId，realm 只是管辖快照。</summary>
	public CampMobility mobility = CampMobility.Pitched;

	/// <summary>迁移目标地块（-1=无）。Moving 状态下每 tick 向目标推进一格。</summary>
	public int targetTileIndex = -1;

	public bool isAbandoned;

	public Dictionary<string, float> attributes = new Dictionary<string, float>();

	public string campName
	{
		get
		{
			return siteName;
		}
		set
		{
			siteName = value;
		}
	}

	public CampType type
	{
		get
		{
			return _type;
		}
		set
		{
			_type = value;
			InitializeModeData(siteMode);
			if (siteMode == SiteMode.TemporaryEncampment)
			{
				temporaryEncampmentData.encampmentKind = value.ToString();
			}
			else if (siteMode == SiteMode.FortifiedOutpost)
			{
				fortifiedOutpostData.purpose = value.ToString();
			}
		}
	}

	public bool CanEvolveToSettlement
	{
		get
		{
			if (permanence >= 100f && type != CampType.Nomad && type != CampType.Temporary)
			{
				return population >= 50f;
			}
			return false;
		}
	}

	public CampData()
	{
		siteMode = SiteMode.TemporaryEncampment;
		InitializeModeData(siteMode);
	}

	/// <summary>营地口粮按人口口径：1 人 1 日 0.01 食物单位（与 EconomyManager.PersonFoodPerDay 一致）。</summary>
	public const float FoodPerPersonPerDay = 0.01f;

	public void Tick(GameWorld world, float deltaDays)
	{
		if (isAbandoned)
		{
			return;
		}
		// 收起状态没有 hearth/固定营地，不产生本地采集；迁移中按当天所在地块照常采集（沿途放牧）。
		if (mobility != CampMobility.Packed
			&& world?.tiles != null && tileIndex >= 0 && tileIndex < world.tiles.Length && population > 0f)
		{
			ref TileData tile = ref world.tiles[tileIndex];
			float farm = AgricultureYield.ComputeForLabor(tile, population) * deltaDays;
			float meat = EcosystemSystem.HarvestForPeople(ref tile, population, deltaDays);
			float fish = EcosystemSystem.HarvestFishForPeople(ref tile, population, deltaDays);
			supplies += farm + meat + fish;
		}
		supplies -= population * FoodPerPersonPerDay * deltaDays;
		if (supplies <= 0f)
		{
			morale -= deltaDays * 2f;
			population = Mathf.Max(0f, population - (float)Mathf.RoundToInt(deltaDays));
			if (population <= 0f || morale <= 0f)
			{
				isAbandoned = true;
				population = 0f;
				Debug.Log((object)$"[Camp] 营寨#{campId}({type}) 因补给耗尽被废弃，地块={tileIndex}");
				return;
			}
		}
		if (supplies > 50f && population > 20f)
		{
			permanence += deltaDays * 0.05f;
			if (permanence >= 100f)
			{
				permanence = 100f;
			}
		}
		if (type == CampType.Nomad)
		{
			permanence = 0f;
		}
	}
}
}
