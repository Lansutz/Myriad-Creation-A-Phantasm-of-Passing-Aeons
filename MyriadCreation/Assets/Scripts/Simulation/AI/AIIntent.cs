using CivilizationEvolution.Core.Enums;

namespace CivilizationEvolution.Simulation.AI
{
    /// <summary>
    /// AI 的决策输出。AIController 只负责选择意图，不直接修改外交、经济、地图或角色状态。
    /// 执行阶段由 AI Schedule 的过渡执行器负责，后续再按领域迁移到各领域 Action/Command。
    /// </summary>
    public readonly struct AIIntent
    {
        public readonly AIIntentType Type;
        public readonly int ActorRealmId;
        public readonly int TargetRealmId;
        public readonly int TargetTileIndex;
        public readonly float Amount;
        public readonly GameEnums.RaidType RaidType;
        public readonly AllianceType AllianceType;
        public readonly int InnovationId;

        public AIIntent(
            AIIntentType type,
            int actorRealmId,
            int targetRealmId = -1,
            int targetTileIndex = -1,
            float amount = 0f,
            GameEnums.RaidType raidType = GameEnums.RaidType.VillageRaid,
            AllianceType allianceType = AllianceType.DefensiveAlliance,
            int innovationId = 0)
        {
            Type = type;
            ActorRealmId = actorRealmId;
            TargetRealmId = targetRealmId;
            TargetTileIndex = targetTileIndex;
            Amount = amount;
            RaidType = raidType;
            AllianceType = allianceType;
            InnovationId = innovationId;
        }
    }

    public enum AIIntentType
    {
        StartResearch = 0,
        RaidSettlement = 1,
        DeclareWar = 2,
        ImproveEconomy = 3,
        ProposeAlliance = 4,
        SendGift = 5,
        ConsolidateRealm = 6,
        MilitaryBuildUp = 7
    }
}
