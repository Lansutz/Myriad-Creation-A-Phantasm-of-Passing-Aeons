using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.Definitions
{
public class VisionImpairmentDef : IImpairmentDef
{
	public string ImpairmentId => "vision";

	public string ImpairmentName => "视力衰退";

	public ImpairmentStage[] Stages => new ImpairmentStage[4]
	{
		new ImpairmentStage
		{
			stageId = "mild",
			stageName = "轻度视力模糊",
			description = "看东西略微模糊，对日常生活影响不大。",
			prowessMod = -1f,
			scholarshipMod = -0.5f,
			charmMod = -0.5f
		},
		new ImpairmentStage
		{
			stageId = "moderate",
			stageName = "中度视力下降",
			description = "视力明显下降，阅读和精细工作变得困难。",
			prowessMod = -3f,
			scholarshipMod = -1.5f,
			militaryMod = -1f,
			charmMod = -1f
		},
		new ImpairmentStage
		{
			stageId = "severe",
			stageName = "重度视力障碍",
			description = "视力严重衰退，几乎无法阅读，行动需要辅助。",
			prowessMod = -6f,
			scholarshipMod = -3f,
			militaryMod = -3f,
			socialMod = -1f,
			charmMod = -2f
		},
		new ImpairmentStage
		{
			stageId = "profound",
			stageName = "失明",
			description = "视力完全丧失。",
			prowessMod = -10f,
			scholarshipMod = -5f,
			militaryMod = -5f,
			socialMod = -3f,
			charmMod = -3f,
			healthMod = -0.2f,
			permanentTraitId = "blindness"
		}
	};
}
}
