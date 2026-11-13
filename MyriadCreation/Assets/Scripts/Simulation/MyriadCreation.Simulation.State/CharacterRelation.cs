using System;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public struct CharacterRelation
{
	public int otherCharacterId;

	[Range(-200f, 200f)]
	public float opinion;

	public RelationshipType type;

	public List<string> history;

	public float trust;

	public float fear;

	public float romanticAttraction;
}
}
