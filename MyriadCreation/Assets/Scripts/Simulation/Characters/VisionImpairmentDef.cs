namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 视力衰退——4个阶段
    /// 可由白内障、青光眼、糖尿病、衰老、外伤等多种原因导致
    /// </summary>
    public class VisionImpairmentDef : IImpairmentDef
    {
        public string ImpairmentId => "vision";
        public string ImpairmentName => "视力衰退";

        public ImpairmentStage[] Stages => new[]
        {
            new ImpairmentStage
            {
                stageId = "mild",
                stageName = "轻度视力模糊",
                description = "看东西略微模糊，对日常生活影响不大。",
                prowessMod = -1, scholarshipMod = -0.5f, charmMod = -0.5f
            },
            new ImpairmentStage
            {
                stageId = "moderate",
                stageName = "中度视力下降",
                description = "视力明显下降，阅读和精细工作变得困难。",
                prowessMod = -3, scholarshipMod = -1.5f, militaryMod = -1, charmMod = -1
            },
            new ImpairmentStage
            {
                stageId = "severe",
                stageName = "重度视力障碍",
                description = "视力严重衰退，几乎无法阅读，行动需要辅助。",
                prowessMod = -6, scholarshipMod = -3, militaryMod = -3, socialMod = -1, charmMod = -2
            },
            new ImpairmentStage
            {
                stageId = "profound",
                stageName = "失明",
                description = "视力完全丧失。",
                prowessMod = -10, scholarshipMod = -5, militaryMod = -5, socialMod = -3, charmMod = -3,
                healthMod = -0.2f,
                permanentTraitId = "blindness"
            }
        };
    }
}
