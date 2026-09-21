namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 运动能力衰退——4个阶段
    /// 可由关节炎、衰老、外伤、中风等多种原因导致
    /// 对应CK3的Fragile Bones/Infirm
    /// </summary>
    public class MobilityImpairmentDef : IImpairmentDef
    {
        public string ImpairmentId => "mobility";
        public string ImpairmentName => "运动能力衰退";

        public ImpairmentStage[] Stages => new[]
        {
            new ImpairmentStage
            {
                stageId = "mild",
                stageName = "关节疼痛",
                description = "关节偶尔疼痛，剧烈运动时加重。",
                prowessMod = -2, healthMod = -0.05f
            },
            new ImpairmentStage
            {
                stageId = "moderate",
                stageName = "行动不便",
                description = "行动明显受限，长时间行走或站立变得困难。",
                prowessMod = -5, militaryMod = -2, healthMod = -0.1f
            },
            new ImpairmentStage
            {
                stageId = "severe",
                stageName = "需要辅助",
                description = "行走需要拐杖或他人搀扶，几乎无法参加战斗。",
                prowessMod = -10, militaryMod = -4, healthMod = -0.15f, charmMod = -1
            },
            new ImpairmentStage
            {
                stageId = "profound",
                stageName = "瘫痪",
                description = "运动能力严重丧失，无法正常行动。",
                prowessMod = -15, militaryMod = -5,
                healthMod = -0.2f,
                permanentTraitId = "paralysis"
            }
        };
    }
}
