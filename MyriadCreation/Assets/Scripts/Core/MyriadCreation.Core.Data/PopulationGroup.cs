using System;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Core.Data
{
[Serializable]
public struct PopulationGroup
{
	public float count;

	public int raceId;

	public int cultureId;

	public int faithId;

	public int religionTraditionId;

	public EconomicClass economicClass;

	public SocialStratum stratum;

	public OccupationFlags occupations;
}
}
