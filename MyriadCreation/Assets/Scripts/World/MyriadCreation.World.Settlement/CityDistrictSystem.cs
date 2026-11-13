using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.World.Settlement
{
public static class CityDistrictSystem
{
	public static void GenerateDistricts(SettlementData settlement)
	{
		if (settlement == null || settlement.districtsGenerated || settlement.settlementLevel < SettlementLevel.LevelIV)
		{
			return;
		}
		List<CityDistrict> list = new List<CityDistrict>();
		if (settlement.economicComposition != null && settlement.economicComposition.Count > 0)
		{
			List<KeyValuePair<EconomicSector, float>> list2 = new List<KeyValuePair<EconomicSector, float>>(settlement.economicComposition);
			list2.Sort((KeyValuePair<EconomicSector, float> a, KeyValuePair<EconomicSector, float> b) => b.Value.CompareTo(a.Value));
			int num = Math.Min(3, list2.Count);
			float num2 = 1f;
			for (int num3 = 0; num3 < num; num3++)
			{
				EconomicSector key = list2[num3].Key;
				float value = list2[num3].Value;
				float num4 = value * ((num3 == 0) ? 0.6f : 0.3f);
				if (num4 > num2)
				{
					num4 = num2;
				}
				num2 -= num4;
				CityDistrictType cityDistrictType = MapSectorToDistrictType(key);
				if (cityDistrictType != CityDistrictType.None)
				{
					CityDistrict item = new CityDistrict(cityDistrictType, GetDistrictName(cityDistrictType, num3), num4, value * 0.8f, 50f + value * 30f, value * settlement.wealth, 0f);
					list.Add(item);
				}
			}
			if (num2 > 0.1f)
			{
				CityDistrict item2 = new CityDistrict(CityDistrictType.Slum, "平民区", num2, num2 * 1.2f, 30f, num2 * settlement.wealth * 0.3f, 0f);
				list.Add(item2);
			}
		}
		else
		{
			list.Add(new CityDistrict(CityDistrictType.Commercial, "商业区", 0.3f, 0.25f, 60f, settlement.wealth * 0.3f, 0f));
			list.Add(new CityDistrict(CityDistrictType.Administrative, "行政区", 0.2f, 0.15f, 70f, settlement.wealth * 0.2f, 0f));
			list.Add(new CityDistrict(CityDistrictType.Slum, "平民区", 0.5f, 0.6f, 30f, settlement.wealth * 0.2f, 0f));
		}
		settlement.districts = list;
		settlement.districtsGenerated = true;
	}

	public static void UpdateDistricts(SettlementData settlement, float deltaTime)
	{
		if (settlement == null || settlement.districts == null || settlement.districts.Count == 0)
		{
			return;
		}
		foreach (CityDistrict district in settlement.districts)
		{
			district.development = Math.Min(100f, district.development + 0.01f * deltaTime);
			district.wealth += district.areaRatio * settlement.wealth * 0.001f * deltaTime;
		}
	}

	public static void CheckDistrictGeneration(SettlementData settlement)
	{
		if (settlement != null && !settlement.districtsGenerated && settlement.settlementLevel >= SettlementLevel.LevelIV)
		{
			GenerateDistricts(settlement);
		}
	}

	private static CityDistrictType MapSectorToDistrictType(EconomicSector sector)
	{
		return sector switch
		{
			EconomicSector.Commerce => CityDistrictType.Commercial, 
			EconomicSector.Artisanal => CityDistrictType.Artisanal, 
			EconomicSector.Military => CityDistrictType.Military, 
			EconomicSector.Administrative => CityDistrictType.Administrative, 
			EconomicSector.Religious => CityDistrictType.Religious, 
			EconomicSector.Cultural => CityDistrictType.Cultural, 
			EconomicSector.Transportation => CityDistrictType.Port, 
			EconomicSector.Agriculture => CityDistrictType.Agricultural, 
			EconomicSector.Fishery => CityDistrictType.Port, 
			EconomicSector.Mining => CityDistrictType.Artisanal, 
			_ => CityDistrictType.None, 
		};
	}

	private static string GetDistrictName(CityDistrictType type, int index)
	{
		string text = type switch
		{
			CityDistrictType.Commercial => "商业区", 
			CityDistrictType.Artisanal => "手工业区", 
			CityDistrictType.Military => "军事区", 
			CityDistrictType.Noble => "贵族区", 
			CityDistrictType.Administrative => "行政区", 
			CityDistrictType.Religious => "宗教区", 
			CityDistrictType.Port => "港口区", 
			CityDistrictType.Slum => "平民区", 
			CityDistrictType.Cultural => "文教区", 
			CityDistrictType.Agricultural => "农业区", 
			_ => "未知区", 
		};
		if (index > 0)
		{
			return $"{text}{index + 1}";
		}
		return text;
	}

	public static DistrictEffects GetDistrictEffects(SettlementData settlement)
	{
		DistrictEffects districtEffects = new DistrictEffects();
		if (settlement == null)
		{
			return districtEffects;
		}
		if (settlement.districtsGenerated && settlement.districts != null && settlement.districts.Count > 0)
		{
			GetDistrictEconomicEffects(settlement, districtEffects);
			return districtEffects;
		}
		if (settlement.settlementLevel == SettlementLevel.LevelIII)
		{
			GetCityEconomicEffects(settlement, districtEffects);
			return districtEffects;
		}
		GetVillageEconomicEffects(settlement, districtEffects);
		return districtEffects;
	}

	private static void GetDistrictEconomicEffects(SettlementData settlement, DistrictEffects effects)
	{
		foreach (CityDistrict district in settlement.districts)
		{
			float num = district.development / 100f;
			float areaRatio = district.areaRatio;
			float num2 = num * areaRatio;
			switch (district.districtType)
			{
			case CityDistrictType.Commercial:
				effects.tradeIncome += num2 * 0.5f;
				effects.commerceTax += num2 * 0.4f;
				effects.marketAttraction += num2 * 0.3f;
				effects.populationCapacity += num2 * 0.2f;
				break;
			case CityDistrictType.Artisanal:
				effects.artisanalOutput += num2 * 0.5f;
				effects.artisanalTax += num2 * 0.3f;
				effects.employment += num2 * 0.4f;
				effects.populationCapacity += num2 * 0.3f;
				break;
			case CityDistrictType.Military:
				effects.garrisonCapacity += num2 * 0.6f;
				effects.defenseBonus += num2 * 0.4f;
				effects.trainingEfficiency += num2 * 0.3f;
				effects.unrest += num2 * 0.1f;
				break;
			case CityDistrictType.Noble:
				effects.nobleApproval += num2 * 0.5f;
				effects.nobleTax += num2 * 0.4f;
				effects.culturalOutput += num2 * 0.2f;
				effects.populationCapacity += num2 * 0.1f;
				break;
			case CityDistrictType.Administrative:
				effects.administrativeEfficiency += num2 * 0.5f;
				effects.taxEfficiency += num2 * 0.4f;
				effects.order += num2 * 0.3f;
				effects.unrest -= num2 * 0.2f;
				break;
			case CityDistrictType.Religious:
				effects.religiousApproval += num2 * 0.5f;
				effects.religiousTax += num2 * 0.3f;
				effects.stability += num2 * 0.3f;
				effects.unrest -= num2 * 0.15f;
				break;
			case CityDistrictType.Port:
				effects.tradeIncome += num2 * 0.6f;
				effects.transportEfficiency += num2 * 0.5f;
				effects.fisheryOutput += num2 * 0.3f;
				effects.populationCapacity += num2 * 0.2f;
				break;
			case CityDistrictType.Slum:
				effects.populationCapacity += num2 * 0.8f;
				effects.unrest += num2 * 0.3f;
				effects.publicHealth -= num2 * 0.2f;
				effects.taxEfficiency -= num2 * 0.1f;
				break;
			case CityDistrictType.Cultural:
				effects.culturalOutput += num2 * 0.6f;
				effects.innovationBonus += num2 * 0.4f;
				effects.education += num2 * 0.5f;
				effects.populationCapacity += num2 * 0.15f;
				break;
			case CityDistrictType.Agricultural:
				effects.agriculturalOutput += num2 * 0.6f;
				effects.foodStorage += num2 * 0.4f;
				effects.populationCapacity += num2 * 0.2f;
				break;
			}
		}
	}

	private static void GetCityEconomicEffects(SettlementData settlement, DistrictEffects effects)
	{
		if (settlement.economicComposition != null && settlement.economicComposition.Count != 0)
		{
			Dictionary<EconomicSector, float> economicComposition = settlement.economicComposition;
			if (economicComposition.ContainsKey(EconomicSector.Agriculture))
			{
				float num = economicComposition[EconomicSector.Agriculture];
				effects.agriculturalOutput += num * 0.5f;
				effects.foodStorage += num * 0.3f;
				effects.populationCapacity += num * 0.2f;
			}
			if (economicComposition.ContainsKey(EconomicSector.Fishery))
			{
				float num2 = economicComposition[EconomicSector.Fishery];
				effects.fisheryOutput += num2 * 0.4f;
				effects.foodStorage += num2 * 0.2f;
			}
			if (economicComposition.ContainsKey(EconomicSector.Mining))
			{
				float num3 = economicComposition[EconomicSector.Mining];
				effects.artisanalOutput += num3 * 0.3f;
				effects.employment += num3 * 0.3f;
			}
			if (economicComposition.ContainsKey(EconomicSector.Artisanal))
			{
				float num4 = economicComposition[EconomicSector.Artisanal];
				effects.artisanalOutput += num4 * 0.5f;
				effects.artisanalTax += num4 * 0.35f;
				effects.employment += num4 * 0.4f;
				effects.populationCapacity += num4 * 0.25f;
			}
			if (economicComposition.ContainsKey(EconomicSector.Commerce))
			{
				float num5 = economicComposition[EconomicSector.Commerce];
				effects.tradeIncome += num5 * 0.5f;
				effects.commerceTax += num5 * 0.4f;
				effects.marketAttraction += num5 * 0.3f;
				effects.populationCapacity += num5 * 0.2f;
			}
			if (economicComposition.ContainsKey(EconomicSector.Transportation))
			{
				float num6 = economicComposition[EconomicSector.Transportation];
				effects.transportEfficiency += num6 * 0.4f;
				effects.tradeIncome += num6 * 0.1f;
			}
			if (economicComposition.ContainsKey(EconomicSector.Administrative))
			{
				float num7 = economicComposition[EconomicSector.Administrative];
				effects.administrativeEfficiency += num7 * 0.35f;
				effects.taxEfficiency += num7 * 0.3f;
				effects.order += num7 * 0.2f;
			}
			if (economicComposition.ContainsKey(EconomicSector.Military))
			{
				float num8 = economicComposition[EconomicSector.Military];
				effects.garrisonCapacity += num8 * 0.4f;
				effects.defenseBonus += num8 * 0.3f;
				effects.trainingEfficiency += num8 * 0.2f;
			}
			if (economicComposition.ContainsKey(EconomicSector.Religious))
			{
				float num9 = economicComposition[EconomicSector.Religious];
				effects.religiousApproval += num9 * 0.3f;
				effects.religiousTax += num9 * 0.25f;
				effects.stability += num9 * 0.2f;
			}
			if (economicComposition.ContainsKey(EconomicSector.Cultural))
			{
				float num10 = economicComposition[EconomicSector.Cultural];
				effects.culturalOutput += num10 * 0.35f;
				effects.education += num10 * 0.3f;
				effects.innovationBonus += num10 * 0.2f;
			}
		}
	}

	private static void GetVillageEconomicEffects(SettlementData settlement, DistrictEffects effects)
	{
		if (settlement.economicComposition != null && settlement.economicComposition.Count != 0)
		{
			Dictionary<EconomicSector, float> economicComposition = settlement.economicComposition;
			if (economicComposition.ContainsKey(EconomicSector.Agriculture))
			{
				float num = economicComposition[EconomicSector.Agriculture];
				effects.agriculturalOutput += num * 0.7f;
				effects.foodStorage += num * 0.5f;
				effects.populationCapacity += num * 0.3f;
			}
			if (economicComposition.ContainsKey(EconomicSector.Fishery))
			{
				float num2 = economicComposition[EconomicSector.Fishery];
				effects.fisheryOutput += num2 * 0.6f;
				effects.foodStorage += num2 * 0.3f;
			}
			if (economicComposition.ContainsKey(EconomicSector.Mining))
			{
				float num3 = economicComposition[EconomicSector.Mining];
				effects.artisanalOutput += num3 * 0.2f;
				effects.employment += num3 * 0.2f;
			}
			if (economicComposition.ContainsKey(EconomicSector.Artisanal))
			{
				float num4 = economicComposition[EconomicSector.Artisanal];
				effects.artisanalOutput += num4 * 0.3f;
				effects.employment += num4 * 0.2f;
			}
			if (economicComposition.ContainsKey(EconomicSector.Commerce))
			{
				float num5 = economicComposition[EconomicSector.Commerce];
				effects.tradeIncome += num5 * 0.15f;
			}
		}
	}
}
}
