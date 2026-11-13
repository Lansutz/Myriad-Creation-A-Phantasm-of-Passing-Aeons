using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Simulation.LawHost
{
public static class PolityComponentInnovations
{
	public enum PolityDimension
	{
		SupremeSuccession,
		SupremeScope,
		CentralSuccession,
		CentralInstitution,
		LocalSuccession,
		LocalScope,
		SpatialStructure
	}

	private static readonly List<int> A1Hereditary = new List<int> { 500 };

	private static readonly List<int> A1ElectiveDirect = new List<int> { 980 };

	private static readonly List<int> A1ElectiveRepresentative = new List<int> { 603, 980 };

	private static readonly List<int> A1Usurpation = new List<int>();

	private static readonly List<int> A1Rotation = new List<int> { 500 };

	private static readonly List<int> A2Absolute = new List<int>();

	private static readonly List<int> A2LegallyBound = new List<int> { 505 };

	private static readonly List<int> A2CustomBound = new List<int> { 505 };

	private static readonly List<int> A2Consensual = new List<int> { 980 };

	private static readonly List<int> A2DivinelyBound = new List<int> { 601 };

	private static readonly List<int> B1Appointed = new List<int> { 503 };

	private static readonly List<int> B1Elected = new List<int> { 980, 500 };

	private static readonly List<int> B1Examination = new List<int> { 504 };

	private static readonly List<int> B1Hereditary = new List<int> { 501 };

	private static readonly List<int> B2None = new List<int> { 500 };

	private static readonly List<int> B2Court = new List<int>();

	private static readonly List<int> B2Assembly = new List<int> { 980 };

	private static readonly List<int> B2EldersCouncil = new List<int> { 500 };

	private static readonly List<int> B2BureaucraticCore = new List<int> { 503 };

	private static readonly List<int> B2ReligiousCouncil = new List<int> { 601 };

	private static readonly List<int> B2MilitaryCouncil = new List<int> { 302 };

	private static readonly List<int> C1Appointed = new List<int> { 959, 960 };

	private static readonly List<int> C1Elected = new List<int> { 980, 500 };

	private static readonly List<int> C1Hereditary = new List<int> { 501, 952 };

	private static readonly List<int> C1CityCharter = new List<int> { 994 };

	private static readonly List<int> C2FullAutonomy = new List<int> { 500 };

	private static readonly List<int> C2FiscalJudicial = new List<int> { 502 };

	private static readonly List<int> C2MilitaryOnly = new List<int> { 302 };

	private static readonly List<int> C2None = new List<int> { 502, 959 };

	private static readonly List<int> DUnitary = new List<int> { 502 };

	private static readonly List<int> DFederal = new List<int> { 501 };

	private static readonly List<int> DConfederal = new List<int> { 500 };

	private static readonly Dictionary<PolityDimension, Dictionary<int, List<int>>> Table = new Dictionary<PolityDimension, Dictionary<int, List<int>>>
	{
		[PolityDimension.SupremeSuccession] = new Dictionary<int, List<int>>
		{
			[0] = A1Hereditary,
			[1] = A1ElectiveDirect,
			[2] = A1ElectiveRepresentative,
			[3] = A1Usurpation,
			[4] = A1Rotation
		},
		[PolityDimension.SupremeScope] = new Dictionary<int, List<int>>
		{
			[0] = A2Absolute,
			[1] = A2LegallyBound,
			[2] = A2CustomBound,
			[3] = A2Consensual,
			[4] = A2DivinelyBound
		},
		[PolityDimension.CentralSuccession] = new Dictionary<int, List<int>>
		{
			[0] = B1Appointed,
			[1] = B1Elected,
			[2] = B1Examination,
			[3] = B1Hereditary
		},
		[PolityDimension.CentralInstitution] = new Dictionary<int, List<int>>
		{
			[0] = B2None,
			[1] = B2Court,
			[2] = B2Assembly,
			[3] = B2EldersCouncil,
			[4] = B2BureaucraticCore,
			[5] = B2ReligiousCouncil,
			[6] = B2MilitaryCouncil
		},
		[PolityDimension.LocalSuccession] = new Dictionary<int, List<int>>
		{
			[0] = C1Appointed,
			[1] = C1Elected,
			[3] = C1Hereditary,
			[4] = C1CityCharter
		},
		[PolityDimension.LocalScope] = new Dictionary<int, List<int>>
		{
			[0] = C2FullAutonomy,
			[1] = C2FiscalJudicial,
			[2] = C2MilitaryOnly,
			[3] = C2None
		},
		[PolityDimension.SpatialStructure] = new Dictionary<int, List<int>>
		{
			[0] = DUnitary,
			[1] = DFederal,
			[2] = DConfederal
		}
	};

	public static List<int> GetRequiredInnovations(PolityDimension dimension, int component)
	{
		if (Table.TryGetValue(dimension, out var value) && value.TryGetValue(component, out var value2))
		{
			return value2;
		}
		return new List<int>();
	}

	public static bool IsComponentAvailable(PolityDimension dimension, int component, InnovationTree innovations, int realmId)
	{
		if (innovations == null)
		{
			return true;
		}
		List<int> requiredInnovations = GetRequiredInnovations(dimension, component);
		if (requiredInnovations.Count == 0)
		{
			return true;
		}
		foreach (int item in requiredInnovations)
		{
			if (innovations.HasInnovation(realmId, item))
			{
				return true;
			}
		}
		return false;
	}
}
}
