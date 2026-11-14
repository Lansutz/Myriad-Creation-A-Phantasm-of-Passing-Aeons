using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public struct BuildingDef
{
	public int buildingId;

	public string buildingName;

	public BuildingCategory category;

	public int tier;

	public float buildCost;

	[NonSerialized]
	public Dictionary<int, float> materialCost;

	public int buildDays;

	public float maintenanceCost;

	/// <summary>建造门槛：政权须已掌握该革新；0 表示无门槛。</summary>
	public int requiredInnovation;
}
}
