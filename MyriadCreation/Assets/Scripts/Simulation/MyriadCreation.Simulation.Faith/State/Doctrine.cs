using System;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Faith
{
[Serializable]
public struct Doctrine
{
	public string name;

	public string description;

	public DoctrineCategory category;

	public float strictness;
}
}
