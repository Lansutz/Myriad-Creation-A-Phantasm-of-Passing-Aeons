using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public struct UnitDef
{
	public int unitId;

	public string unitName;

	public GameEnums.UnitArmType category;

	public int tier;

	public float meleeAttack;

	public float rangedAttack;

	public float defense;

	public float morale;

	public float speed;

	public float supplyConsumption;

	[NonSerialized]
	public Dictionary<int, float> recruitCost;

	public float manpowerCost;

	[NonSerialized]
	public Dictionary<GameEnums.TerrainTacticType, float> terrainModifiers;

	public List<int> requiredInnovations;
}
}
