using System;

namespace MyriadCreation.Simulation.Units
{
[Serializable]
public struct CargoEntry
{
	public int goodsId;

	public float amount;

	public float unitWeight;

	public bool fragile;
}
}
