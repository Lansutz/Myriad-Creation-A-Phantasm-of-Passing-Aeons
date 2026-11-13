using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Systems;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class RealmData
{
	public int realmId;

	public string realmName;

	/// <summary>政权主头衔键（运行时；入档待 SaveVersion 批准）。</summary>
	public string primaryTitleKey = "";

	public int stateReligionId = -1;

	[NonSerialized]
	public Dictionary<int, int> officeHolders = new Dictionary<int, int>();

	/// <summary>各槽位的任命绝对日（year×年长+day）；0/缺省视为旧档终身任职。随 RealmOfficeSaveData 入档。</summary>
	[NonSerialized]
	public Dictionary<int, int> officeAppointedDays = new Dictionary<int, int>();

	/// <summary>旗帜类型（P3.4）；None = 无旗合法状态。</summary>
	[NonSerialized]
	public BannerKind bannerKind;

	/// <summary>旗帜布局（如 StateFlag|twoBands）；空 = 无旗。</summary>
	[NonSerialized]
	public string bannerPattern = "";

	/// <summary>旗帜配色（field|charge|accent）；空 = 无旗。</summary>
	[NonSerialized]
	public string bannerColors = "";

	public List<AdminDivision> adminDivisions = new List<AdminDivision>();

	public int statePatronSaintId = -1;

	public float treasury = 1000f;

	public float prestige = 50f;

	public float stability = 50f;

	/// <summary>政权级不稳定度 0-100（法律/法令 unrestPerYear 的持久落点，随 RealmDTO 入档）。</summary>
	public float unrest;

	public float centralization = 0.5f;

	public RealmForm realmForm;

	public NomadicSubtype nomadicSubtype;

	[NonSerialized]
	public NomadicRange nomadicRange;

	public GameEnums.MovementControlLevel movementControl = GameEnums.MovementControlLevel.Loose;

	[NonSerialized]
	public Dictionary<int, GameEnums.MovementControlLevel> tileMovementControlOverrides = new Dictionary<int, GameEnums.MovementControlLevel>();

	[NonSerialized]
	public HashSet<int> militaryAccessGranted = new HashSet<int>();

	public TaxSystem taxSystem = new TaxSystem();

	public CurrencySystem currencySystem = new CurrencySystem();

	[NonSerialized]
	public Dictionary<GameEnums.SocialClass, float> classRelations = new Dictionary<GameEnums.SocialClass, float>();

	[NonSerialized]
	public HashSet<int> coreTiles = new HashSet<int>();

	[NonSerialized]
	public HashSet<int> claimedTiles = new HashSet<int>();

	public int suzerainId = -1;

	public List<int> vassalIds = new List<int>();

	public LawComposition laws = new LawComposition();

	public RealmLawState lawState = new RealmLawState();

	public RealmDecreeState decreeState = new RealmDecreeState();

	public int primaryCultureId = -1;

	public bool successionLawFromCulture = true;

	public int monarchId = -1;

	public int consulId = -1;

	public int heirId = -1;

	// ===== 摄政/空位期（运行时状态；经 RealmDTO 入档，旧档缺失 = 不在摄政）=====
	/// <summary>是否处于摄政/空位期（法理君主已死且无合法继承人）。</summary>
	public bool inInterregnum;

	/// <summary>摄政者角色 id；-1 表示暂无合格摄政者（王国空转）。</summary>
	public int regentCharacterId = -1;

	/// <summary>空位期开始日（currentDay）。</summary>
	public int interregnumStartDay;

	/// <summary>空位期已持续月数（每 30 日 +1，供 UI/编年史）。</summary>
	public int interregnumMonths;

	public int senateSeats;

	public InheritanceLaw SuccessionLaw => GetEffectiveSuccessionLaw();

	public InheritanceLaw GetEffectiveSuccessionLaw()
	{
		if (successionLawFromCulture && primaryCultureId >= 0 && ContentResolvers.Cultures.TryGet(primaryCultureId, out var value))
		{
			return value.data.defaultSuccessionLaw;
		}
		return laws.successionLaw;
	}

	/// <summary>
	/// 最高统治者 id：摄政期返回摄政者（若有），否则返回法理君主/执政官；
	/// 无摄政者时保持死君主（兼容按 id>=0 取角色的既有调用）。
	/// </summary>
	public int GetSupremeRulerId()
	{
		if (inInterregnum && regentCharacterId >= 0)
		{
			return regentCharacterId;
		}
		if (monarchId >= 0)
		{
			return monarchId;
		}
		return consulId;
	}

	/// <summary>实际掌权者 id（摄政期为摄政者；否则同 GetSupremeRulerId）。</summary>
	public int GetEffectiveRulerId()
	{
		return GetSupremeRulerId();
	}

	/// <summary>
	/// 法理统治者 id：摄政期仍是已故法理君主/执政官，不返回摄政者。
	/// 继承/摄政状态机用它判断"法理君主是否仍在世"，避免把摄政者误当君主。
	/// </summary>
	public int GetLegalRulerId()
	{
		if (monarchId >= 0)
		{
			return monarchId;
		}
		return consulId;
	}

	/// <summary>是否处于摄政/空位期。</summary>
	public bool IsInterregnum => inInterregnum;

	public RealmData()
	{
		classRelations[GameEnums.SocialClass.Royalty] = 70f;
		classRelations[GameEnums.SocialClass.NobilityClergy] = 60f;
		classRelations[GameEnums.SocialClass.MerchantFreeman] = 50f;
		classRelations[GameEnums.SocialClass.Peasant] = 50f;
		classRelations[GameEnums.SocialClass.Slave] = 30f;
	}

	public float CalculateManpowerLimit(TileData[] tiles)
	{
		float num = 0f;
		foreach (int coreTile in coreTiles)
		{
			if (coreTile < 0 || coreTile >= tiles.Length || tiles[coreTile].populationBlocks == null)
			{
				continue;
			}
			foreach (PopulationBlock populationBlock in tiles[coreTile].populationBlocks)
			{
				if (populationBlock.socialClass != GameEnums.SocialClass.Slave)
				{
					num += populationBlock.count * 0.1f;
				}
			}
		}
		return num;
	}

	public void AdjustClassRelation(GameEnums.SocialClass socialClass, float delta)
	{
		classRelations[socialClass] = Mathf.Clamp(classRelations.GetValueOrDefault(socialClass, 50f) + delta, 0f, 100f);
	}

	public float CalculateRebellionRisk()
	{
		float num = 0f;
		int num2 = 0;
		foreach (KeyValuePair<GameEnums.SocialClass, float> classRelation in classRelations)
		{
			num += classRelation.Value;
			num2++;
		}
		num = ((num2 > 0) ? (num / (float)num2) : 50f);
		return Mathf.Clamp((100f - num) * 0.5f + (100f - stability) * 0.3f, 0f, 100f);
	}
}
}
