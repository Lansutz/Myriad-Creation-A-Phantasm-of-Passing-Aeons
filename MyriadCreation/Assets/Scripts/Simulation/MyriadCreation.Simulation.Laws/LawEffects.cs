using System;
using System.Collections.Generic;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Laws
{
public static class LawEffects
{
	[Serializable]
	public class LawEffectData
	{
		public float expansionism;

		public float centralization = 0.5f;

		public float stability;

		public float administrativeCapacity = 0.5f;

		public float taxEfficiency = 1f;

		public float tradeEfficiency = 1f;

		public float developmentSpeed = 1f;

		public float inflationResistance = 1f;

		public float manpowerRecruitment = 1f;

		public float armyMaintenanceCost = 1f;

		public float armyMorale = 1f;

		public float fortificationBonus;

		public float diplomaticReputation;

		public float allianceDesirability;

		public float aggressiveDiplomacy;

		public float vassalizationEfficiency = 1f;

		public List<string> requiredInnovations = new List<string>();

		public List<string> unlockedBuildings = new List<string>();

		public List<string> unlockedSettlementTypes = new List<string>();
	}

	public static LawEffectData CalculateEffects(LawComposition comp)
	{
		LawEffectData lawEffectData = new LawEffectData();
		CalcSupreme(comp, lawEffectData);
		CalcCentral(comp, lawEffectData);
		CalcLocal(comp, lawEffectData);
		CalcSpatial(comp, lawEffectData);
		CalcFaith(comp, lawEffectData);
		CalcInnovations(comp, lawEffectData);
		CalcUnlocked(comp, lawEffectData);
		return lawEffectData;
	}

	private static void CalcSupreme(LawComposition comp, LawEffectData e)
	{
		LawConstraints.SupremeSovereignty supremeSovereignty = comp.supremeSovereignty;
		SupremeSuccession primary = (SupremeSuccession)comp.supremeSuccession.primary;
		SupremeScope primary2 = (SupremeScope)comp.supremeScope.primary;
		if (supremeSovereignty == LawConstraints.SupremeSovereignty.Monarchy)
		{
			e.centralization += 0.1f;
			e.armyMorale += 0.05f;
			e.diplomaticReputation += 5f;
		}
		else
		{
			e.tradeEfficiency += 0.15f;
			e.developmentSpeed += 0.1f;
			e.allianceDesirability += 5f;
			e.armyMaintenanceCost -= 0.1f;
		}
		switch (primary)
		{
		case SupremeSuccession.Hereditary:
			e.stability += 10f;
			e.centralization += 0.1f;
			e.expansionism += 0.1f;
			break;
		case SupremeSuccession.ElectiveDirect:
			e.stability -= 5f;
			e.tradeEfficiency += 0.1f;
			e.manpowerRecruitment += 0.1f;
			break;
		case SupremeSuccession.ElectiveRepresentative:
			e.stability += 5f;
			e.diplomaticReputation += 5f;
			e.taxEfficiency += 0.05f;
			break;
		case SupremeSuccession.Usurpation:
			e.expansionism += 0.2f;
			e.stability -= 15f;
			e.armyMorale += 0.1f;
			e.aggressiveDiplomacy += 10f;
			break;
		case SupremeSuccession.Rotation:
			e.stability -= 10f;
			e.centralization -= 0.15f;
			e.allianceDesirability += 5f;
			break;
		}
		switch (primary2)
		{
		case SupremeScope.Absolute:
			e.centralization += 0.2f;
			e.taxEfficiency += 0.1f;
			e.expansionism += 0.1f;
			e.stability -= 5f;
			break;
		case SupremeScope.LegallyBound:
			e.stability += 5f;
			e.tradeEfficiency += 0.05f;
			e.diplomaticReputation += 5f;
			break;
		case SupremeScope.CustomBound:
			e.centralization -= 0.1f;
			e.stability += 10f;
			e.armyMaintenanceCost -= 0.05f;
			break;
		case SupremeScope.Consensual:
			e.stability += 10f;
			e.allianceDesirability += 10f;
			e.manpowerRecruitment += 0.1f;
			break;
		case SupremeScope.DivinelyBound:
			e.stability += 15f;
			e.centralization += 0.1f;
			e.inflationResistance += 0.15f;
			break;
		}
		foreach (int item in comp.supremeSuccession.secondary)
		{
			switch ((SupremeSuccession)item)
			{
			case SupremeSuccession.Usurpation:
				e.expansionism += 0.05f;
				e.stability -= 3f;
				break;
			case SupremeSuccession.ElectiveRepresentative:
				e.diplomaticReputation += 3f;
				e.stability += 3f;
				break;
			}
		}
		if (comp.titleDistribution == LawConstraints.TitleDistribution.FamilyShared)
		{
			e.centralization -= 0.15f;
			e.stability -= 5f;
			e.expansionism += 0.05f;
		}
		else if (comp.titleDistribution == LawConstraints.TitleDistribution.NoTitle)
		{
			e.centralization -= 0.1f;
			e.tradeEfficiency += 0.1f;
		}
		if (comp.domainDistribution == LawConstraints.DomainDistribution.Partible)
		{
			e.centralization -= 0.2f;
			e.stability -= 10f;
			e.expansionism += 0.1f;
		}
		else if (comp.domainDistribution == LawConstraints.DomainDistribution.Appanage)
		{
			e.centralization -= 0.1f;
			e.stability += 5f;
		}
	}

	private static void CalcCentral(LawComposition comp, LawEffectData e)
	{
		if (comp.centralExistence == LawConstraints.CentralExistence.None)
		{
			e.centralization -= 0.3f;
			e.taxEfficiency -= 0.2f;
			e.administrativeCapacity -= 0.2f;
			e.stability -= 5f;
			return;
		}
		switch ((CentralSuccession)comp.centralSuccession.primary)
		{
		case CentralSuccession.Appointed:
			e.centralization += 0.1f;
			e.administrativeCapacity += 0.1f;
			break;
		case CentralSuccession.Elected:
			e.stability += 5f;
			e.tradeEfficiency += 0.05f;
			break;
		case CentralSuccession.Examination:
			e.administrativeCapacity += 0.2f;
			e.taxEfficiency += 0.1f;
			e.developmentSpeed += 0.1f;
			break;
		case CentralSuccession.Hereditary:
			e.centralization -= 0.1f;
			e.stability += 5f;
			e.administrativeCapacity -= 0.1f;
			break;
		}
		switch ((CentralInstitution)comp.centralInstitution.primary)
		{
		case CentralInstitution.None:
			e.administrativeCapacity -= 0.2f;
			e.taxEfficiency -= 0.15f;
			break;
		case CentralInstitution.Court:
			e.centralization += 0.05f;
			e.diplomaticReputation += 3f;
			break;
		case CentralInstitution.Assembly:
			e.stability += 10f;
			e.tradeEfficiency += 0.1f;
			e.allianceDesirability += 5f;
			break;
		case CentralInstitution.EldersCouncil:
			e.stability += 15f;
			e.centralization -= 0.05f;
			break;
		case CentralInstitution.BureaucraticCore:
			e.administrativeCapacity += 0.2f;
			e.taxEfficiency += 0.15f;
			e.developmentSpeed += 0.1f;
			e.centralization += 0.1f;
			break;
		case CentralInstitution.ReligiousCouncil:
			e.stability += 10f;
			e.inflationResistance += 0.1f;
			e.diplomaticReputation -= 3f;
			break;
		case CentralInstitution.MilitaryCouncil:
			e.armyMorale += 0.15f;
			e.expansionism += 0.15f;
			e.aggressiveDiplomacy += 10f;
			e.stability -= 5f;
			break;
		}
	}

	private static void CalcLocal(LawComposition comp, LawEffectData e)
	{
		LocalSuccession primary = (LocalSuccession)comp.localSuccession.primary;
		LocalScope primary2 = (LocalScope)comp.localScope.primary;
		switch (primary)
		{
		case LocalSuccession.Appointed:
			e.centralization += 0.1f;
			e.administrativeCapacity += 0.05f;
			break;
		case LocalSuccession.Elected:
			e.stability += 5f;
			e.developmentSpeed += 0.05f;
			break;
		case LocalSuccession.Examination:
			e.administrativeCapacity += 0.15f;
			e.taxEfficiency += 0.1f;
			break;
		case LocalSuccession.Hereditary:
			e.centralization -= 0.15f;
			e.stability += 5f;
			e.armyMaintenanceCost -= 0.1f;
			e.expansionism += 0.05f;
			break;
		case LocalSuccession.CityCharter:
			e.tradeEfficiency += 0.2f;
			e.developmentSpeed += 0.15f;
			e.centralization -= 0.1f;
			break;
		}
		switch (primary2)
		{
		case LocalScope.FullAutonomy:
			e.centralization -= 0.2f;
			e.stability += 10f;
			e.taxEfficiency -= 0.15f;
			e.developmentSpeed += 0.1f;
			break;
		case LocalScope.FiscalJudicial:
			e.centralization -= 0.05f;
			e.taxEfficiency += 0.05f;
			break;
		case LocalScope.MilitaryOnly:
			e.centralization += 0.05f;
			e.fortificationBonus += 0.1f;
			e.developmentSpeed -= 0.1f;
			break;
		case LocalScope.None:
			e.centralization += 0.2f;
			e.taxEfficiency += 0.1f;
			e.administrativeCapacity += 0.1f;
			e.stability -= 5f;
			break;
		}
	}

	private static void CalcSpatial(LawComposition comp, LawEffectData e)
	{
		switch ((SpatialStructure)comp.spatialStructure.primary)
		{
		case SpatialStructure.Unitary:
			e.centralization += 0.2f;
			e.taxEfficiency += 0.1f;
			e.administrativeCapacity += 0.1f;
			e.stability += 5f;
			break;
		case SpatialStructure.Federal:
			e.centralization -= 0.1f;
			e.stability += 10f;
			e.developmentSpeed += 0.1f;
			e.allianceDesirability += 5f;
			break;
		case SpatialStructure.Confederal:
			e.centralization -= 0.25f;
			e.stability -= 10f;
			e.expansionism -= 0.1f;
			e.tradeEfficiency += 0.1f;
			break;
		}
	}

	private static void CalcFaith(LawComposition comp, LawEffectData e)
	{
		if (comp.establishedFaith == LawConstraints.FaithStatus.Established)
		{
			e.stability += 5f;
			e.administrativeCapacity -= 0.05f;
			switch (comp.faithLawPriority)
			{
			case LawConstraints.FaithLawPriority.ReligiousFirst:
				e.inflationResistance += 0.1f;
				e.centralization -= 0.05f;
				break;
			case LawConstraints.FaithLawPriority.SecularFirst:
				e.centralization += 0.05f;
				e.administrativeCapacity += 0.05f;
				break;
			}
			switch (comp.heterodoxPolicy)
			{
			case LawConstraints.HeterodoxPolicy.Tolerant:
				e.tradeEfficiency += 0.05f;
				e.allianceDesirability += 3f;
				e.stability += 2f;
				break;
			case LawConstraints.HeterodoxPolicy.Restricted:
				e.stability += 3f;
				e.diplomaticReputation -= 3f;
				break;
			case LawConstraints.HeterodoxPolicy.Prohibited:
				e.stability -= 8f;
				e.expansionism += 0.05f;
				break;
			}
			if (comp.faithTitle == LawConstraints.FaithTitle.Granted)
			{
				e.diplomaticReputation -= 3f;
				e.stability += 3f;
			}
		}
		switch (comp.legalSource)
		{
		case LawSource.Customary:
			e.stability += 5f;
			break;
		case LawSource.Statutory:
			e.administrativeCapacity += 0.05f;
			e.taxEfficiency += 0.05f;
			break;
		case LawSource.Religious:
			e.inflationResistance += 0.05f;
			e.centralization -= 0.05f;
			break;
		case LawSource.Common:
			e.tradeEfficiency += 0.05f;
			e.developmentSpeed += 0.05f;
			break;
		case LawSource.Civil:
			e.administrativeCapacity += 0.05f;
			break;
		case LawSource.Mixed:
			break;
		}
	}

	private static void CalcInnovations(LawComposition comp, LawEffectData e)
	{
		int primary = comp.centralInstitution.primary;
		if (primary == 4)
		{
			e.requiredInnovations.Add("bureaucracy");
			e.requiredInnovations.Add("writing");
		}
		if (primary == 2)
		{
			e.requiredInnovations.Add("republican_traditions");
		}
		if (primary == 5)
		{
			e.requiredInnovations.Add("organized_religion");
		}
		if (primary == 6)
		{
			e.requiredInnovations.Add("military_reform");
		}
		int primary2 = comp.localSuccession.primary;
		if (primary2 == 2)
		{
			e.requiredInnovations.Add("civil_service_exam");
			e.requiredInnovations.Add("bureaucracy");
		}
		if (primary2 == 4)
		{
			e.requiredInnovations.Add("urban_charter");
			e.requiredInnovations.Add("merchant_guilds");
		}
		int primary3 = comp.spatialStructure.primary;
		if (primary3 == 1)
		{
			e.requiredInnovations.Add("federalism");
		}
		if (primary3 == 2)
		{
			e.requiredInnovations.Add("confederation");
		}
		if (comp.supremeSovereignty == LawConstraints.SupremeSovereignty.Republic)
		{
			e.requiredInnovations.Add("republican_government");
		}
	}

	private static void CalcUnlocked(LawComposition comp, LawEffectData e)
	{
		int primary = comp.centralInstitution.primary;
		if (primary == 4)
		{
			e.unlockedBuildings.Add("tax_office");
			e.unlockedBuildings.Add("courthouse");
			e.unlockedBuildings.Add("archive");
		}
		if (primary == 2)
		{
			e.unlockedBuildings.Add("senate_house");
			e.unlockedBuildings.Add("forum");
		}
		if (primary == 5)
		{
			e.unlockedBuildings.Add("temple_complex");
			e.unlockedBuildings.Add("monastery");
		}
		if (primary == 6)
		{
			e.unlockedBuildings.Add("barracks");
			e.unlockedBuildings.Add("military_academy");
			e.unlockedBuildings.Add("fortress");
		}
		int primary2 = comp.localSuccession.primary;
		if (primary2 == 4)
		{
			e.unlockedSettlementTypes.Add("free_city");
			e.unlockedSettlementTypes.Add("trade_hub");
		}
		if (primary2 == 3)
		{
			e.unlockedSettlementTypes.Add("manor");
			e.unlockedSettlementTypes.Add("castle_town");
		}
		if (primary2 == 2)
		{
			e.unlockedSettlementTypes.Add("administrative_center");
			e.unlockedSettlementTypes.Add("scholar_town");
		}
	}

	public static bool IsHighlyExpansionist(LawComposition comp)
	{
		return CalculateEffects(comp).expansionism > 0.3f;
	}

	public static bool IsHighlyCentralized(LawComposition comp)
	{
		return CalculateEffects(comp).centralization > 0.7f;
	}

	public static bool IsStable(LawComposition comp)
	{
		return CalculateEffects(comp).stability > 0f;
	}

	public static string GetExpansionismDescription(LawComposition comp)
	{
		float expansionism = CalculateEffects(comp).expansionism;
		if (expansionism > 0.3f)
		{
			return "高度扩张";
		}
		if (expansionism > 0.1f)
		{
			return "倾向扩张";
		}
		if (expansionism > -0.1f)
		{
			return "平衡";
		}
		if (expansionism > -0.3f)
		{
			return "倾向内敛";
		}
		return "高度内敛";
	}
}
}
