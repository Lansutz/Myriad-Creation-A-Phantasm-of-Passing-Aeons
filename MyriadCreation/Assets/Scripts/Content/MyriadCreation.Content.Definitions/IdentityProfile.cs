using System;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class IdentityProfile
{
	public EconomicClass economicClass;

	public SocialStratum stratum = SocialStratum.Middle;

	public SocialIdentityFlags identity;

	public OccupationFlags occupations;

	public bool ownsSlaves;

	public bool HasIdentity(SocialIdentityFlags flag)
	{
		return (identity & flag) != 0;
	}

	public bool HasOccupation(OccupationFlags flag)
	{
		return (occupations & flag) != 0;
	}

	public void AddIdentity(SocialIdentityFlags flag)
	{
		identity |= flag;
	}

	public void RemoveIdentity(SocialIdentityFlags flag)
	{
		identity &= ~flag;
	}

	public void AddOccupation(OccupationFlags flag)
	{
		occupations |= flag;
	}

	public void RemoveOccupation(OccupationFlags flag)
	{
		occupations &= ~flag;
	}

	public IdentityProfile Clone()
	{
		return new IdentityProfile
		{
			economicClass = economicClass,
			stratum = stratum,
			identity = identity,
			occupations = occupations,
			ownsSlaves = ownsSlaves
		};
	}
}
}
