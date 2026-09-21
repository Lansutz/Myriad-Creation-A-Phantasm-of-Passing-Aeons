namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 认知衰退——4个阶段
    /// 可由衰老、脑损伤、精神疾病等多种原因导致
    /// 对应CK3的Withering Mind，最终导致Incapable
    /// </summary>
    public class CognitionImpairmentDef : IImpairmentDef
    {
        public string ImpairmentId => "cognition";
        public string ImpairmentName => "认知衰退";

        public ImpairmentStage[] Stages => new[]
        {
            new ImpairmentStage
            {
                stageId = "mild",
                stageName = "健忘",
                description = "偶尔忘记事情，对日常生活影响不大。",
                scholarshipMod = -1, managementMod = -0.5f
            },
            new ImpairmentStage
            {
                stageId = "moderate",
                stageName = "轻度认知障碍",
                description = "记忆力和思考能力明显下降，处理复杂事务变得困难。",
                scholarshipMod = -2.5f, managementMod = -1.5f, conspiracyMod = -1, socialMod = -1
            },
            new ImpairmentStage
            {
                stageId = "severe",
                stageName = "痴呆",
                description = "认知能力严重衰退，无法正常处理复杂事务，需要他人照顾。",
                scholarshipMod = -5, managementMod = -4, conspiracyMod = -3, socialMod = -3
            },
            new ImpairmentStage
            {
                stageId = "profound",
                stageName = "失能",
                description = "认知能力严重衰退，无法正常执政和处理事务。对应CK3的Incapable特质。",
                scholarshipMod = -10, managementMod = -8, conspiracyMod = -6, socialMod = -5,
                healthMod = -0.3f,
                permanentTraitId = "dementia"
            }
        };
    }
}
