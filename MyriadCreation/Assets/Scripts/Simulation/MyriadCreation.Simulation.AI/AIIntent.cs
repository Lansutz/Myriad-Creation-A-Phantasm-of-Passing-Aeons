using MyriadCreation.Core.Enums;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.AI
{
public readonly struct AIIntent(AIIntentType type, int actorRealmId, int targetRealmId = -1, int targetTileIndex = -1, float amount = 0f, GameEnums.RaidType raidType = GameEnums.RaidType.VillageRaid, AllianceType allianceType = AllianceType.DefensiveAlliance, int innovationId = 0, int actorCharacterId = -1)
{
	public readonly AIIntentType Type = type;

	public readonly int ActorRealmId = actorRealmId;

	public readonly int TargetRealmId = targetRealmId;

	public readonly int TargetTileIndex = targetTileIndex;

	public readonly float Amount = amount;

	public readonly GameEnums.RaidType RaidType = raidType;

	public readonly AllianceType AllianceType = allianceType;

	public readonly int InnovationId = innovationId;

	public readonly int ActorCharacterId = actorCharacterId;
}
}
