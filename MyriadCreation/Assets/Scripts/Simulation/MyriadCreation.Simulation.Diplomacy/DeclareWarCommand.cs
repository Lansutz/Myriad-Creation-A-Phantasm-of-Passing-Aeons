using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.Diplomacy
{
public readonly struct DeclareWarCommand(int attackerRealmId, int defenderRealmId, string reason, int actorCharacterId = -1) : ISimulationCommand
{
	public readonly int AttackerRealmId = attackerRealmId;

	public readonly int DefenderRealmId = defenderRealmId;

	public readonly string Reason = reason ?? string.Empty;

	public readonly int ActorCharacterId = actorCharacterId;
}
}
