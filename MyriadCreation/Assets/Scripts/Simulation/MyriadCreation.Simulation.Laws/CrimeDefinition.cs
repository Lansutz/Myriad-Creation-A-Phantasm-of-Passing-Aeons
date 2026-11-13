using System;
using System.Collections.Generic;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Laws
{
[Serializable]
public struct CrimeDefinition
{
	public CrimeType type;

	public string name;

	public float baseSeverity;

	public PunishmentType defaultPunishment;

	public List<CrimeBehavior> behaviors;

	public List<CrimeTargetClass> targetClasses;

	public List<string> contexts;

	public List<int> citedDomainIds;

	public List<int> citedFamilyIds;

	public List<int> citedGroupIds;
}
}
