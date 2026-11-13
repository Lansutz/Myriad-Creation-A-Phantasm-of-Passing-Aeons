using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.Settlement
{
public readonly struct DestroySettlementCommand(int anchorId, DestructionMethod method, int attackerRealmId = -1) : ISimulationCommand
{
	public readonly int SettlementAnchorId = anchorId;

	public readonly DestructionMethod Method = method;

	public readonly int AttackerRealmId = attackerRealmId;
}
}
