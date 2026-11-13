using System;
using System.Collections.Generic;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Laws
{
[Serializable]
public class LawComposition
{
	public LawConstraints.SupremeSovereignty supremeSovereignty;

	public ComponentChoice supremeSuccession = new ComponentChoice(0);

	public ComponentChoice supremeScope = new ComponentChoice(0);

	public LawConstraints.TitleDistribution titleDistribution;

	public LawConstraints.DomainDistribution domainDistribution;

	public LawConstraints.CentralExistence centralExistence = LawConstraints.CentralExistence.Established;

	public ComponentChoice centralSuccession = new ComponentChoice(0);

	public ComponentChoice centralInstitution = new ComponentChoice(4);

	public ComponentChoice localSuccession = new ComponentChoice(0);

	public ComponentChoice localScope = new ComponentChoice(1);

	public ComponentChoice spatialStructure = new ComponentChoice(0);

	public ComponentChoice militaryRecruitment = new ComponentChoice(-1);

	public ComponentChoice militaryOrganization = new ComponentChoice(-1);

	public ComponentChoice militaryManagement = new ComponentChoice(-1);

	public LawConstraints.FaithStatus establishedFaith;

	public LawConstraints.FaithLawPriority faithLawPriority = LawConstraints.FaithLawPriority.Parallel;

	public LawConstraints.HeterodoxPolicy heterodoxPolicy;

	public LawConstraints.FaithTitle faithTitle;

	public ComponentChoice household = new ComponentChoice(-1);

	public ComponentChoice landTenure = new ComponentChoice(-1);

	public ComponentChoice marriageStatus = new ComponentChoice(-1);

	public ComponentChoice classCitizenship = new ComponentChoice(-1);

	public ComponentChoice nameRegistration = new ComponentChoice(-1);

	public ComponentChoice measures = new ComponentChoice(-1);

	public ComponentChoice tradeControl = new ComponentChoice(-1);

	public ComponentChoice nationalTax = new ComponentChoice(-1);

	public ComponentChoice localTax = new ComponentChoice(-1);

	public ComponentChoice passage = new ComponentChoice(-1);

	public ComponentChoice borderMarket = new ComponentChoice(-1);

	public LawSource legalSource;

	public EligibilityRules supremeEligibility = new EligibilityRules();

	public EligibilityRules centralEligibility = new EligibilityRules();

	public EligibilityRules localEligibility = new EligibilityRules();

	public AssemblyComposition assemblyComposition;

	public LocalAppointAuthority localAppointAuthority;

	public LocalLordship localLordship;

	public InheritanceLaw successionLaw = InheritanceLaw.Primogeniture();

	public static LawComposition BureaucraticMonarchy()
	{
		return new LawComposition
		{
			supremeSuccession = new ComponentChoice(0),
			supremeScope = new ComponentChoice(0),
			centralSuccession = new ComponentChoice(0),
			centralInstitution = new ComponentChoice(4),
			localSuccession = new ComponentChoice(0),
			localAppointAuthority = LocalAppointAuthority.Central,
			localScope = new ComponentChoice(3),
			spatialStructure = new ComponentChoice(0),
			supremeEligibility = new EligibilityRules
			{
				gender = InheritanceGender.MalePreference,
				scope = EligibilityScope.FreePeople
			},
			successionLaw = InheritanceLaw.ChinesePartible(),
			legalSource = LawSource.Statutory
		};
	}

	public static LawComposition FeudalFiefdom()
	{
		return new LawComposition
		{
			supremeSuccession = new ComponentChoice(0),
			supremeScope = new ComponentChoice(0),
			centralSuccession = new ComponentChoice(3),
			centralInstitution = new ComponentChoice(1),
			localSuccession = new ComponentChoice(3),
			localLordship = LocalLordship.Appanage,
			localScope = new ComponentChoice(0),
			spatialStructure = new ComponentChoice(2),
			supremeEligibility = new EligibilityRules
			{
				gender = InheritanceGender.MalePreference,
				scope = EligibilityScope.ClanOnly
			},
			successionLaw = InheritanceLaw.Primogeniture(),
			legalSource = LawSource.Customary
		};
	}

	public static LawComposition AthenianDemocracy()
	{
		return new LawComposition
		{
			supremeSuccession = new ComponentChoice(1),
			supremeScope = new ComponentChoice(3),
			centralSuccession = new ComponentChoice(1),
			centralInstitution = new ComponentChoice(2),
			localSuccession = new ComponentChoice(1),
			assemblyComposition = AssemblyComposition.Unicameral,
			localScope = new ComponentChoice(0),
			spatialStructure = new ComponentChoice(0),
			supremeEligibility = new EligibilityRules
			{
				gender = InheritanceGender.MaleOnly,
				scope = EligibilityScope.Citizens
			},
			legalSource = LawSource.Common
		};
	}

	public static LawComposition SenatorialRepublic()
	{
		return new LawComposition
		{
			supremeSuccession = new ComponentChoice(1),
			supremeScope = new ComponentChoice(1),
			centralSuccession = new ComponentChoice(1),
			centralInstitution = new ComponentChoice(2),
			localSuccession = new ComponentChoice(0),
			assemblyComposition = AssemblyComposition.Unicameral,
			localScope = new ComponentChoice(1),
			spatialStructure = new ComponentChoice(1),
			supremeEligibility = new EligibilityRules
			{
				gender = InheritanceGender.MaleOnly,
				scope = EligibilityScope.Citizens
			},
			legalSource = LawSource.Common
		};
	}

	public static LawComposition Theocracy()
	{
		return new LawComposition
		{
			supremeSuccession = new ComponentChoice(5),
			supremeScope = new ComponentChoice(4),
			centralSuccession = new ComponentChoice(0),
			centralInstitution = new ComponentChoice(5),
			localSuccession = new ComponentChoice(0),
			localAppointAuthority = LocalAppointAuthority.Religious,
			localScope = new ComponentChoice(0),
			spatialStructure = new ComponentChoice(0),
			supremeEligibility = new EligibilityRules
			{
				gender = InheritanceGender.MaleOnly,
				scope = EligibilityScope.Clergy
			},
			establishedFaith = LawConstraints.FaithStatus.Established,
			faithLawPriority = LawConstraints.FaithLawPriority.ReligiousFirst,
			heterodoxPolicy = LawConstraints.HeterodoxPolicy.Restricted,
			faithTitle = LawConstraints.FaithTitle.Granted,
			legalSource = LawSource.Religious
		};
	}

	public static LawComposition MongolHorde()
	{
		LawComposition lawComposition = new LawComposition();
		lawComposition.supremeSuccession = new ComponentChoice(1, default(int));
		lawComposition.supremeScope = new ComponentChoice(0);
		lawComposition.centralSuccession = new ComponentChoice(0);
		lawComposition.centralInstitution = new ComponentChoice(6);
		lawComposition.localSuccession = new ComponentChoice(3);
		lawComposition.localLordship = LocalLordship.Vassal;
		lawComposition.localScope = new ComponentChoice(0);
		lawComposition.spatialStructure = new ComponentChoice(2);
		lawComposition.supremeEligibility = new EligibilityRules
		{
			gender = InheritanceGender.MaleOnly,
			scope = EligibilityScope.Nobility
		};
		lawComposition.successionLaw = InheritanceLaw.Tanistry();
		lawComposition.legalSource = LawSource.Customary;
		return lawComposition;
	}

	public static LawComposition ConstitutionalMonarchy()
	{
		return new LawComposition
		{
			supremeSuccession = new ComponentChoice(2),
			supremeScope = new ComponentChoice(3),
			centralSuccession = new ComponentChoice(1),
			centralInstitution = new ComponentChoice(2),
			localSuccession = new ComponentChoice(0),
			assemblyComposition = AssemblyComposition.Bicameral,
			localScope = new ComponentChoice(1),
			spatialStructure = new ComponentChoice(0),
			supremeEligibility = new EligibilityRules
			{
				gender = InheritanceGender.MalePreference,
				scope = EligibilityScope.FreePeople
			},
			legalSource = LawSource.Common
		};
	}

	public static LawComposition ImperialSenate()
	{
		LawComposition lawComposition = new LawComposition();
		lawComposition.supremeSuccession = new ComponentChoice(0, 3);
		lawComposition.supremeScope = new ComponentChoice(0);
		lawComposition.centralSuccession = new ComponentChoice(1);
		lawComposition.centralInstitution = new ComponentChoice(2);
		lawComposition.localSuccession = new ComponentChoice(0);
		lawComposition.assemblyComposition = AssemblyComposition.Unicameral;
		lawComposition.localScope = new ComponentChoice(1);
		lawComposition.spatialStructure = new ComponentChoice(0);
		lawComposition.supremeEligibility = new EligibilityRules
		{
			gender = InheritanceGender.MalePreference,
			scope = EligibilityScope.FreePeople
		};
		lawComposition.legalSource = LawSource.Common;
		return lawComposition;
	}

	public static LawComposition HolyRomanEmpire()
	{
		return new LawComposition
		{
			supremeSuccession = new ComponentChoice(2),
			supremeScope = new ComponentChoice(0),
			centralSuccession = new ComponentChoice(1),
			centralInstitution = new ComponentChoice(2),
			localSuccession = new ComponentChoice(3),
			assemblyComposition = AssemblyComposition.Estate,
			localLordship = LocalLordship.Vassal,
			localScope = new ComponentChoice(0),
			spatialStructure = new ComponentChoice(2),
			supremeEligibility = new EligibilityRules
			{
				gender = InheritanceGender.MaleOnly,
				scope = EligibilityScope.Nobility
			},
			legalSource = LawSource.Customary
		};
	}

	public static LawComposition VenetianRepublic()
	{
		return new LawComposition
		{
			supremeSuccession = new ComponentChoice(1),
			supremeScope = new ComponentChoice(3),
			centralSuccession = new ComponentChoice(1),
			centralInstitution = new ComponentChoice(2),
			localSuccession = new ComponentChoice(4),
			assemblyComposition = AssemblyComposition.Bicameral,
			localScope = new ComponentChoice(0),
			spatialStructure = new ComponentChoice(0),
			supremeEligibility = new EligibilityRules
			{
				gender = InheritanceGender.MaleOnly,
				scope = EligibilityScope.Nobility
			},
			legalSource = LawSource.Common
		};
	}

	public string GetName()
	{
		return LawComponentNames.NameSupremeSuccession(supremeSuccession.primary) + "·" + LawComponentNames.NameCentralInstitution(centralInstitution.primary) + "·" + LawComponentNames.NameLocalSuccession(localSuccession.primary) + "·" + LawComponentNames.NameSpatialStructure(spatialStructure.primary);
	}

	public bool IsGroupActive(LawConstraints.LawGroup group)
	{
		switch (group)
		{
		case LawConstraints.LawGroup.SupremeSovereigntyGroup:
		case LawConstraints.LawGroup.SupremeSuccessionGroup:
		case LawConstraints.LawGroup.SupremeScopeGroup:
		case LawConstraints.LawGroup.TitleDistributionGroup:
		case LawConstraints.LawGroup.DomainDistributionGroup:
		case LawConstraints.LawGroup.CentralExistenceGroup:
		case LawConstraints.LawGroup.CentralSuccessionGroup:
		case LawConstraints.LawGroup.CentralInstitutionGroup:
		case LawConstraints.LawGroup.LocalSuccessionGroup:
		case LawConstraints.LawGroup.LocalScopeGroup:
		case LawConstraints.LawGroup.RealmStructureGroup:
		case LawConstraints.LawGroup.EstablishedFaithGroup:
		case LawConstraints.LawGroup.FaithLawPriorityGroup:
		case LawConstraints.LawGroup.HeterodoxPolicyGroup:
		case LawConstraints.LawGroup.FaithTitleGroup:
		case LawConstraints.LawGroup.LegalSourceGroup:
			return true;
		default:
			return false;
		}
	}

	public bool IsGroupEnabled(LawConstraints.LawGroup group)
	{
		if ((uint)(group - 14) <= 2u)
		{
			return establishedFaith == LawConstraints.FaithStatus.Established;
		}
		return IsGroupActive(group);
	}

	public int GetGroupPrimary(LawConstraints.LawGroup group)
	{
		return group switch
		{
			LawConstraints.LawGroup.SupremeSovereigntyGroup => (int)supremeSovereignty, 
			LawConstraints.LawGroup.SupremeSuccessionGroup => supremeSuccession.primary, 
			LawConstraints.LawGroup.SupremeScopeGroup => supremeScope.primary, 
			LawConstraints.LawGroup.TitleDistributionGroup => (int)titleDistribution, 
			LawConstraints.LawGroup.DomainDistributionGroup => (int)domainDistribution, 
			LawConstraints.LawGroup.CentralExistenceGroup => (int)centralExistence, 
			LawConstraints.LawGroup.CentralSuccessionGroup => centralSuccession.primary, 
			LawConstraints.LawGroup.CentralInstitutionGroup => centralInstitution.primary, 
			LawConstraints.LawGroup.LocalSuccessionGroup => localSuccession.primary, 
			LawConstraints.LawGroup.LocalScopeGroup => localScope.primary, 
			LawConstraints.LawGroup.RealmStructureGroup => spatialStructure.primary, 
			LawConstraints.LawGroup.EstablishedFaithGroup => (int)establishedFaith, 
			LawConstraints.LawGroup.FaithLawPriorityGroup => (int)faithLawPriority, 
			LawConstraints.LawGroup.HeterodoxPolicyGroup => (int)heterodoxPolicy, 
			LawConstraints.LawGroup.FaithTitleGroup => (int)faithTitle, 
			LawConstraints.LawGroup.LegalSourceGroup => (int)legalSource, 
			LawConstraints.LawGroup.MilitaryRecruitmentGroup => militaryRecruitment.primary, 
			LawConstraints.LawGroup.MilitaryOrganizationGroup => militaryOrganization.primary, 
			LawConstraints.LawGroup.MilitaryManagementGroup => militaryManagement.primary, 
			LawConstraints.LawGroup.HouseholdGroup => household.primary, 
			LawConstraints.LawGroup.LandTenureGroup => landTenure.primary, 
			LawConstraints.LawGroup.MarriageStatusGroup => marriageStatus.primary, 
			LawConstraints.LawGroup.ClassCitizenshipGroup => classCitizenship.primary, 
			LawConstraints.LawGroup.NameRegistrationGroup => nameRegistration.primary, 
			LawConstraints.LawGroup.MeasuresGroup => measures.primary, 
			LawConstraints.LawGroup.TradeControlGroup => tradeControl.primary, 
			LawConstraints.LawGroup.NationalTaxGroup => nationalTax.primary, 
			LawConstraints.LawGroup.LocalTaxGroup => localTax.primary, 
			LawConstraints.LawGroup.PassageGroup => passage.primary, 
			LawConstraints.LawGroup.BorderMarketGroup => borderMarket.primary, 
			_ => -1, 
		};
	}

	public IReadOnlyList<int> GetGroupSecondary(LawConstraints.LawGroup group)
	{
		return group switch
		{
			LawConstraints.LawGroup.SupremeSuccessionGroup => (IReadOnlyList<int>)supremeSuccession.secondary, 
			LawConstraints.LawGroup.SupremeScopeGroup => supremeScope.secondary, 
			LawConstraints.LawGroup.CentralSuccessionGroup => centralSuccession.secondary, 
			LawConstraints.LawGroup.CentralInstitutionGroup => centralInstitution.secondary, 
			LawConstraints.LawGroup.LocalSuccessionGroup => localSuccession.secondary, 
			LawConstraints.LawGroup.LocalScopeGroup => localScope.secondary, 
			LawConstraints.LawGroup.RealmStructureGroup => spatialStructure.secondary, 
			LawConstraints.LawGroup.MilitaryRecruitmentGroup => militaryRecruitment.secondary, 
			LawConstraints.LawGroup.MilitaryOrganizationGroup => militaryOrganization.secondary, 
			LawConstraints.LawGroup.MilitaryManagementGroup => militaryManagement.secondary, 
			LawConstraints.LawGroup.HouseholdGroup => household.secondary, 
			LawConstraints.LawGroup.LandTenureGroup => landTenure.secondary, 
			LawConstraints.LawGroup.MarriageStatusGroup => marriageStatus.secondary, 
			LawConstraints.LawGroup.ClassCitizenshipGroup => classCitizenship.secondary, 
			LawConstraints.LawGroup.NameRegistrationGroup => nameRegistration.secondary, 
			LawConstraints.LawGroup.MeasuresGroup => measures.secondary, 
			LawConstraints.LawGroup.TradeControlGroup => tradeControl.secondary, 
			LawConstraints.LawGroup.NationalTaxGroup => nationalTax.secondary, 
			LawConstraints.LawGroup.LocalTaxGroup => localTax.secondary, 
			LawConstraints.LawGroup.PassageGroup => passage.secondary, 
			LawConstraints.LawGroup.BorderMarketGroup => borderMarket.secondary, 
			_ => Array.Empty<int>(), 
		};
	}
}
}
