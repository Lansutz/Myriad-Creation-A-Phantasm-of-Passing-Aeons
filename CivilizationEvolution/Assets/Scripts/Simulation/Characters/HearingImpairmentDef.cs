namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 听力衰退——4个阶段
    /// 可由衰老、疾病、外伤、长期噪音等多种原因导致
    /// </summary>
    public class HearingImpairmentDef : IImpairmentDef
    {
        public string ImpairmentId => "hearing";
        public string ImpairmentName => "听力衰退";

        public ImpairmentStage[] Stages => new[]
        {
            new ImpairmentStage
            {
                stageId = "mild",
                stageName = "轻度耳背",
                description = "偶尔听不清别人说话，需要对方重复。",
                socialMod = -1, conspiracyMod = -0.5f
            },
            new ImpairmentStage
            {
                stageId = "moderate",
                stageName = "中度听力下降",
                description = "听力明显下降，正常对话变得困难。",
                socialMod = -3, conspiracyMod = -1.5f, managementMod = -1
            },
            new ImpairmentStage
            {
                stageId = "severe",
                stageName = "重度听力障碍",
                description = "几乎听不到正常音量的说话，需要大声喊叫才能听到。",
                socialMod = -5, conspiracyMod = -3, managementMod = -2, charmMod = -1
            },
            new ImpairmentStage
            {
                stageId = "profound",
                stageName = "全聋",
                description = "听力完全丧失。",
                socialMod = -8, conspiracyMod = -4, managementMod = -2,
                healthMod = -0.1f,
                permanentTraitId = "deafness"
            }
        };
    }
}
