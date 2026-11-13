using System;
using System.Collections.Generic;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.LawHost;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class RealmDTO
{
	public int realmId;

	public string realmName;

	public LawComposition laws = new LawComposition();

	public RealmLawState lawState = new RealmLawState();

	public RealmDecreeState decreeState = new RealmDecreeState();

	[Obsolete("Legacy save field: use laws; NormalizeLegacyFields migrates it on load.")]
	public LawComposition composition;

	public float treasury;

	public float prestige;

	public float stability;

	/// <summary>政权级不稳定度（旧档缺失默认 0；不升 SaveVersion）。</summary>
	public float unrest;

	public float centralization;

	public TaxSystemDTO taxSystem = new TaxSystemDTO();

	public CurrencySystem currencySystem = new CurrencySystem();

	public List<IntFloatEntry> classRelations = new List<IntFloatEntry>();

	public List<int> coreTiles = new List<int>();

	public List<int> claimedTiles = new List<int>();

	public int suzerainId = -1;

	public List<int> vassalIds = new List<int>();

	/// <summary>行政区划树（SaveVersion 12 起；旧档为空表，读档后按政权/地块重建）。</summary>
	public List<AdminDivisionSaveData> adminDivisions = new List<AdminDivisionSaveData>();

	// ===== 摄政/空位期状态（旧档字段缺失 = 不在摄政；不升 SaveVersion，随 realms 列表自动入档）=====
	public bool inInterregnum;

	public int regentCharacterId = -1;

	public int interregnumStartDay;

	public int interregnumMonths;

	public void NormalizeLegacyFields()
	{
		if (composition != null)
		{
			laws = composition;
			composition = null;
		}
	}

	public static RealmDTO FromRealmData(RealmData r)
	{
		RealmDTO realmDTO = new RealmDTO
		{
			realmId = r.realmId,
			realmName = r.realmName,
			laws = r.laws,
			lawState = r.lawState,
			decreeState = r.decreeState,
			treasury = r.treasury,
			prestige = r.prestige,
			stability = r.stability,
			unrest = r.unrest,
			centralization = r.centralization,
			taxSystem = TaxSystemDTO.FromTaxSystem(r.taxSystem),
			currencySystem = r.currencySystem,
			suzerainId = r.suzerainId,
			inInterregnum = r.inInterregnum,
			regentCharacterId = r.regentCharacterId,
			interregnumStartDay = r.interregnumStartDay,
			interregnumMonths = r.interregnumMonths
		};
		if (r.classRelations != null)
		{
			foreach (KeyValuePair<GameEnums.SocialClass, float> classRelation in r.classRelations)
			{
				realmDTO.classRelations.Add(new IntFloatEntry((int)classRelation.Key, classRelation.Value));
			}
		}
		if (r.coreTiles != null)
		{
			foreach (int coreTile in r.coreTiles)
			{
				realmDTO.coreTiles.Add(coreTile);
			}
		}
		if (r.claimedTiles != null)
		{
			foreach (int claimedTile in r.claimedTiles)
			{
				realmDTO.claimedTiles.Add(claimedTile);
			}
		}
		if (r.vassalIds != null)
		{
			realmDTO.vassalIds.AddRange(r.vassalIds);
		}
		if (r.adminDivisions != null)
		{
			foreach (AdminDivision division in r.adminDivisions)
			{
				if (division != null) realmDTO.adminDivisions.Add(AdminDivisionSaveData.From(division));
			}
		}
		return realmDTO;
	}

	public RealmData ToRealmData()
	{
		RealmData realmData = new RealmData
		{
			realmId = realmId,
			realmName = realmName,
			laws = laws,
			lawState = lawState,
			decreeState = decreeState,
			treasury = treasury,
			prestige = prestige,
			stability = stability,
			unrest = unrest,
			centralization = centralization,
			taxSystem = taxSystem.ToTaxSystem(),
			currencySystem = currencySystem,
			suzerainId = suzerainId,
			inInterregnum = inInterregnum,
			regentCharacterId = regentCharacterId,
			interregnumStartDay = interregnumStartDay,
			interregnumMonths = interregnumMonths
		};
		realmData.classRelations.Clear();
		foreach (IntFloatEntry classRelation in classRelations)
		{
			realmData.classRelations[(GameEnums.SocialClass)classRelation.key] = classRelation.value;
		}
		realmData.coreTiles.Clear();
		foreach (int coreTile in coreTiles)
		{
			realmData.coreTiles.Add(coreTile);
		}
		realmData.claimedTiles.Clear();
		foreach (int claimedTile in claimedTiles)
		{
			realmData.claimedTiles.Add(claimedTile);
		}
		realmData.vassalIds.Clear();
		realmData.vassalIds.AddRange(vassalIds);
		realmData.adminDivisions.Clear();
		if (adminDivisions != null)
		{
			foreach (AdminDivisionSaveData saved in adminDivisions)
			{
				if (saved != null) realmData.adminDivisions.Add(saved.ToDivision());
			}
		}
		LawCompositionMigration.MigrateIfEmpty(realmData);
		return realmData;
	}
}
}
