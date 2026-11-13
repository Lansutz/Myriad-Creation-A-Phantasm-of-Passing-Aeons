using System;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Core.Data
{
[Serializable]
public struct PopulationBlock
{
	public float count;

	public int raceId;

	public int cultureId;

	public int faithId;

	public GameEnums.SocialClass socialClass;

	public int profession;

	public EconomicClass economicClass;

	public SocialStratum stratum;

	public OccupationFlags occupations;

	public float satisfaction;

	public float culturePenetration;
}
}
