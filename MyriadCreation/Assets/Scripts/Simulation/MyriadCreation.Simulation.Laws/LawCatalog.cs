using System;
using System.Collections.Generic;
namespace MyriadCreation.Simulation.Laws
{
public static class LawCatalog
{
	public static List<Law> BuildCorpus(LawComposition composition)
	{
		List<Law> list = new List<Law>();
		if (composition == null)
		{
			return list;
		}
		foreach (LawConstraints.LawGroup value2 in Enum.GetValues(typeof(LawConstraints.LawGroup)))
		{
			LawConstraints.LawDomain domain = LawConstraints.GetDomain(value2);
			Law item = new Law
			{
				lawId = list.Count + 1,
				lawName = LawConstraints.GetGroupName(value2),
				description = string.Empty,
				group = value2,
				enactedDay = 0,
				isActive = true,
				citedCrimes = new List<CrimeType>()
			};
			foreach (KeyValuePair<CrimeType, CrimeDefinition> item2 in CrimeRegistry.All)
			{
				CrimeDefinition value = item2.Value;
				if (CrimeRegistry.GetCitedGroups(value).Contains(value2) || CrimeRegistry.GetCitedDomains(value).Contains(domain))
				{
					item.citedCrimes.Add(item2.Key);
				}
			}
			list.Add(item);
		}
		return list;
	}
}
}
