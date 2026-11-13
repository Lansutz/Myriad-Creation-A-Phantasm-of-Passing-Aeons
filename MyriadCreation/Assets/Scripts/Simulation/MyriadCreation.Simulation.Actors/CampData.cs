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
		// 先收本地食物：营地人口在所在地块耕作/狩猎/捕鱼，产出从地块植被、野生、鱼群真实扣减。
		if (world?.tiles != null && tileIndex >= 0 && tileIndex < world.tiles.Length && population > 0f)
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
