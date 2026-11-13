using System;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Simulation.Systems
{
public sealed class SettlementEvolutionRuntime
{
	private readonly GameWorld _world;

	public SettlementEvolutionRuntime(GameWorld world)
	{
		_world = world ?? throw new ArgumentNullException("world");
	}

	public void DailyTick()
	{
		SettlementEvolutionSystem.DailyTick(_world);
	}

	public SettlementData TryEvolveCampToSettlement(CampData camp)
	{
		return SettlementEvolutionSystem.TryEvolveCampToSettlement(_world, camp);
	}

	public SettlementData SettleNomadCamp(int campId)
	{
		return SettlementEvolutionSystem.SettleNomadCamp(_world, campId);
	}

	public CampData DegradeSettlementToCamp(int anchorId, CampType campType = CampType.Military, int ownerRealmId = -1)
	{
		return SettlementEvolutionSystem.DegradeSettlementToCamp(_world, anchorId, campType, ownerRealmId);
	}
}
}
