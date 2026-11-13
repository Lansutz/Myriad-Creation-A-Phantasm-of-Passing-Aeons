using System;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Systems
{
public sealed class SettlementDestructionRuntime
{
	private readonly GameWorld _world;

	public SettlementDestructionRuntime(GameWorld world)
	{
		_world = world ?? throw new ArgumentNullException("world");
	}

	public string DestroySettlement(int anchorId, DestructionMethod method, int attackerRealmId = -1)
	{
		return SettlementDestructionSystem.DestroySettlement(_world, anchorId, method, attackerRealmId);
	}

	public void DailyTickRecovery()
	{
		SettlementDestructionSystem.DailyTickRecovery(_world);
	}
}
}
