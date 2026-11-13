using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.Settlement
{
public readonly struct SettlementDestroyedEvent(int anchorId, DestructionMethod method, int attackerRealmId)
{
	public readonly int SettlementAnchorId = anchorId;

	public readonly DestructionMethod Method = method;

	public readonly int AttackerRealmId = attackerRealmId;
}
}
