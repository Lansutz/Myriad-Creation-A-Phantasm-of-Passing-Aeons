using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
public static class PopulationIdentity
{
	public static IdentityProfile Resolve(in PopulationBlock block)
	{
		if (block.economicClass != EconomicClass.Freeholder || block.stratum != SocialStratum.Middle || block.occupations != OccupationFlags.None)
		{
			return new IdentityProfile
			{
				economicClass = block.economicClass,
				stratum = block.stratum,
				occupations = block.occupations
			};
		}
		return LegacyIdentityMapping.FromLegacy(block.socialClass);
	}

	public static void Add(in PopulationBlock block, Dictionary<EconomicClass, float> byClass, Dictionary<SocialStratum, float> byStratum)
	{
		IdentityProfile identityProfile = Resolve(in block);
		if (byClass != null)
		{
			byClass[identityProfile.economicClass] = byClass.GetValueOrDefault(identityProfile.economicClass, 0f) + block.count;
		}
		if (byStratum != null)
		{
			byStratum[identityProfile.stratum] = byStratum.GetValueOrDefault(identityProfile.stratum, 0f) + block.count;
		}
	}

	public static void ApplyLegacy(GameEnums.SocialClass legacyClass, ref PopulationBlock block, GameEnums.SocialSubclass? subclass = null)
	{
		IdentityProfile identityProfile = LegacyIdentityMapping.FromLegacy(legacyClass, subclass);
		block.economicClass = identityProfile.economicClass;
		block.stratum = identityProfile.stratum;
		block.occupations = identityProfile.occupations;
	}
}
}
