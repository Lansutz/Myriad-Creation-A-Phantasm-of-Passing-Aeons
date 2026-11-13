using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using UnityEngine;
using MyriadCreation.Core.Enums;
using Random = UnityEngine.Random;

namespace MyriadCreation.World.Settlement
{
public static class EconomicCompositionSystem
{
	public static void InitializeComposition(SettlementData settlement, TileData tile)
	{
		if (settlement != null && !settlement.economicInitialized)
		{
			Dictionary<EconomicSector, float> dictionary = new Dictionary<EconomicSector, float>();
			float num = 10f;
			float num2 = 5f;
			float num3 = 3f;
			float num4 = 0f;
			float num5 = 0f;
			float num6 = 0f;
			float num7 = 0f;
			float num8 = 1f;
			float num9 = 0.5f;
			float num10 = 0f;
			if (tile.isCoast || tile.isRiver)
			{
				num4 += 8f;
				num10 += 5f;
				num2 += 3f;
			}
			if (tile.elevation01 > 0.6f)
			{
				num5 += 10f;
			}
			if (tile.annualPrecipMm < 300f)
			{
				num += 5f;
			}
			if (tile.fertility > 0.6f)
			{
				num += 8f;
			}
			if (settlement.siteMode == SiteMode.FortifiedOutpost)
			{
				num7 += 15f;
				num6 += 3f;
			}
			if (settlement.isCapital)
			{
				num6 += 10f;
				num9 += 5f;
				num8 += 3f;
				num2 += 5f;
			}
			if (settlement.isPort)
			{
				num10 += 10f;
				num2 += 8f;
				num4 += 5f;
			}
			if (settlement.hasMarket)
			{
				num2 += 5f;
			}
			if (settlement.hasTemple)
			{
				num8 += 5f;
			}
			if (settlement.hasUniversity)
			{
				num9 += 8f;
			}
			float num11 = num + num4 + num5 + num2 + num3 + num6 + num7 + num8 + num9 + num10;
			if (num11 > 0f)
			{
				dictionary[EconomicSector.Agriculture] = num / num11;
				dictionary[EconomicSector.Fishery] = num4 / num11;
				dictionary[EconomicSector.Mining] = num5 / num11;
				dictionary[EconomicSector.Commerce] = num2 / num11;
				dictionary[EconomicSector.Artisanal] = num3 / num11;
				dictionary[EconomicSector.Administrative] = num6 / num11;
				dictionary[EconomicSector.Military] = num7 / num11;
				dictionary[EconomicSector.Religious] = num8 / num11;
				dictionary[EconomicSector.Cultural] = num9 / num11;
				dictionary[EconomicSector.Transportation] = num10 / num11;
			}
			else
			{
				dictionary[EconomicSector.Agriculture] = 0.6f;
				dictionary[EconomicSector.Commerce] = 0.2f;
				dictionary[EconomicSector.Artisanal] = 0.1f;
				dictionary[EconomicSector.Religious] = 0.1f;
			}
			settlement.economicComposition = dictionary;
			settlement.primarySector = GetPrimarySector(dictionary);
			settlement.economicInitialized = true;
		}
	}

	public static void UpdateComposition(SettlementData settlement, float deltaTime)
	{
		if (settlement == null || settlement.economicComposition == null || settlement.economicComposition.Count == 0)
		{
			return;
		}
		Dictionary<EconomicSector, float> economicComposition = settlement.economicComposition;
		List<EconomicSector> list = new List<EconomicSector>(economicComposition.Keys);
		float num = 0.0005f * deltaTime;
		float num2 = Mathf.Clamp01(settlement.population / 5000f);
		if (economicComposition.ContainsKey(EconomicSector.Commerce))
		{
			economicComposition[EconomicSector.Commerce] += num2 * num * 2f;
		}
		if (economicComposition.ContainsKey(EconomicSector.Artisanal))
		{
			economicComposition[EconomicSector.Artisanal] += num2 * num * 1.5f;
		}
		if (settlement.hasMarket || settlement.isPort)
		{
			float num3 = (settlement.hasMarket ? 0.5f : 0f);
			num3 += (settlement.isPort ? 0.5f : 0f);
			num3 += settlement.tradePower / 100f;
			num3 = Mathf.Clamp01(num3);
			if (economicComposition.ContainsKey(EconomicSector.Commerce))
			{
				economicComposition[EconomicSector.Commerce] += num3 * num * 3f;
			}
			if (economicComposition.ContainsKey(EconomicSector.Transportation))
			{
				economicComposition[EconomicSector.Transportation] += num3 * num * 2f;
			}
		}
		float num4 = Mathf.Clamp01((float)settlement.garrison / 500f + settlement.fortification / 10f);
		if (num4 > 0.1f && economicComposition.ContainsKey(EconomicSector.Military))
		{
			economicComposition[EconomicSector.Military] += num4 * num * 2f;
		}
		if (settlement.isCapital)
		{
			if (economicComposition.ContainsKey(EconomicSector.Administrative))
			{
				economicComposition[EconomicSector.Administrative] += num * 3f;
			}
			if (economicComposition.ContainsKey(EconomicSector.Cultural))
			{
				economicComposition[EconomicSector.Cultural] += num * 1.5f;
			}
		}
		if (settlement.hasTemple && economicComposition.ContainsKey(EconomicSector.Religious))
		{
			economicComposition[EconomicSector.Religious] += num * 2f;
		}
		if (settlement.hasUniversity && economicComposition.ContainsKey(EconomicSector.Cultural))
		{
			economicComposition[EconomicSector.Cultural] += num * 3f;
		}
		foreach (EconomicSector item in list)
		{
			float num5 = (Random.value - 0.5f) * 2f * num * 0.5f;
			economicComposition[item] = Math.Max(0f, economicComposition[item] + num5);
		}
		foreach (EconomicSector item2 in list)
		{
			economicComposition[item2] = Math.Max(0f, economicComposition[item2]);
		}
		NormalizeComposition(economicComposition);
		settlement.primarySector = GetPrimarySector(economicComposition);
	}

	public static EconomicSector GetPrimarySector(Dictionary<EconomicSector, float> composition)
	{
		if (composition == null || composition.Count == 0)
		{
			return EconomicSector.None;
		}
		EconomicSector result = EconomicSector.None;
		float num = 0f;
		foreach (KeyValuePair<EconomicSector, float> item in composition)
		{
			if (item.Value > num)
			{
				num = item.Value;
				result = item.Key;
			}
		}
		return result;
	}

	private static void NormalizeComposition(Dictionary<EconomicSector, float> composition)
	{
		if (composition == null || composition.Count == 0)
		{
			return;
		}
		float num = 0f;
		foreach (KeyValuePair<EconomicSector, float> item in composition)
		{
			num += item.Value;
		}
		if (!(num > 0f))
		{
			return;
		}
		foreach (EconomicSector item2 in new List<EconomicSector>(composition.Keys))
		{
			composition[item2] /= num;
		}
	}

	public static string GetSectorDescription(EconomicSector sector)
	{
		return sector switch
		{
			EconomicSector.Agriculture => "农业", 
			EconomicSector.Fishery => "渔业", 
			EconomicSector.Mining => "矿业", 
			EconomicSector.Commerce => "商业", 
			EconomicSector.Artisanal => "手工业", 
			EconomicSector.Administrative => "行政", 
			EconomicSector.Military => "军事", 
			EconomicSector.Religious => "宗教", 
			EconomicSector.Cultural => "文化", 
			EconomicSector.Transportation => "交通", 
			_ => "未知", 
		};
	}

	public static string GetCompositionDescription(SettlementData settlement)
	{
		if (settlement == null || settlement.economicComposition == null || settlement.economicComposition.Count == 0)
		{
			return "经济成分未知";
		}
		List<KeyValuePair<EconomicSector, float>> list = new List<KeyValuePair<EconomicSector, float>>(settlement.economicComposition);
		list.Sort((KeyValuePair<EconomicSector, float> a, KeyValuePair<EconomicSector, float> b) => b.Value.CompareTo(a.Value));
		string text = "";
		for (int num = 0; num < Math.Min(3, list.Count); num++)
		{
			if (num > 0)
			{
				text += "、";
			}
			text += $"{GetSectorDescription(list[num].Key)}({list[num].Value * 100f:F0}%)";
		}
		return text;
	}
}
}
