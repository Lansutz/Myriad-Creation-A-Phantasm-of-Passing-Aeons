using System;
using System.Collections.Generic;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public struct SubStructureData(SubStructureType type)
{
	public SubStructureType type = type;

	public float condition = 0f;

	public float progressionRate = 0f;

	public List<string> causingFactors = new List<string>();
}
}
