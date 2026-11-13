using System;
using System.Collections.Generic;
namespace MyriadCreation.Simulation.Laws
{
[Serializable]
public struct Law
{
	public int lawId;

	public string lawName;

	public string description;

	public LawConstraints.LawGroup group;

	public List<CrimeType> citedCrimes;

	public int enactedDay;

	public bool isActive;

	public LawConstraints.LawFamily Family => LawConstraints.GetFamily(group);

	public LawConstraints.LawDomain Domain => LawConstraints.GetDomain(group);
}
}
