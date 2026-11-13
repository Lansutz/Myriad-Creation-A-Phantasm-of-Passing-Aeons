using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Systems
{
public sealed class LandAbandonmentRuntime
{
	private readonly GameWorld _world;

	public LandAbandonmentRuntime(GameWorld world)
	{
		_world = world ?? throw new ArgumentNullException("world");
	}

	public LandAbandonmentSystem.AbandonmentRecord? AbandonTile(int tileIndex, AbandonmentType type, int realmId)
	{
		return LandAbandonmentSystem.AbandonTile(_world, tileIndex, type, realmId);
	}

	public List<LandAbandonmentSystem.AbandonmentRecord> AbandonTiles(IEnumerable<int> tileIndices, AbandonmentType type, int realmId)
	{
		return LandAbandonmentSystem.AbandonTiles(_world, tileIndices, type, realmId);
	}

	public bool ResettleTile(int tileIndex, int newRealmId, int settlerCount)
	{
		return LandAbandonmentSystem.ResettleTile(_world, tileIndex, newRealmId, settlerCount);
	}

	public void DailyCheckBanditSpawn()
	{
		LandAbandonmentSystem.DailyCheckBanditSpawn(_world);
	}
}
}
