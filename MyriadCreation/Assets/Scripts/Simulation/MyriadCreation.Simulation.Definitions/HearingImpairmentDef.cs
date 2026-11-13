using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.Definitions
{
public class HearingImpairmentDef : IImpairmentDef
{
	public string ImpairmentId => "hearing";

	public string ImpairmentName => "听力衰退";

	public ImpairmentStage[] Stages => new ImpairmentStage[4]
	{
		new ImpairmentStage
		{
			stageId = "mild",
			stageName = "轻度耳背",
			description = "偶尔听不清别人说话，需要对方重复。",
			socialMod = -1f,
			conspiracyMod = -0.5f
		},
		new ImpairmentStage
		{
			stageId = "moderate",
			stageName = "中度听力下降",
			description = "听力明显下降，正常对话变得困难。",
			socialMod = -3f,
			conspiracyMod = -1.5f,
			managementMod = -1f
		},
		new ImpairmentStage
		{
			stageId = "severe",
			stageName = "重度听力障碍",
			description = "几乎听不到正常音量的说话，需要大声喊叫才能听到。",
			socialMod = -5f,
			conspiracyMod = -3f,
			managementMod = -2f,
			charmMod = -1f
		},
		new ImpairmentStage
		{
			stageId = "profound",
			stageName = "全聋",
			description = "听力完全丧失。",
			socialMod = -8f,
			conspiracyMod = -4f,
			managementMod = -2f,
			healthMod = -0.1f,
			permanentTraitId = "deafness"
		}
	};
}
}
