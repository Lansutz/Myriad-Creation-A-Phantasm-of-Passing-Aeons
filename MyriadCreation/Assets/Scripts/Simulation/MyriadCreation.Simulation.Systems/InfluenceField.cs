using System.Collections.Generic;

namespace MyriadCreation.Simulation.Systems
{
public sealed class InfluenceField
{
	public byte[] tierByTile;

	public int[] sourceAnchorByTile;

	public Dictionary<int, InfluenceTier> tiersBySettlement = new Dictionary<int, InfluenceTier>();
}
}
