using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.Definitions
{
public class SpeechImpairmentDef : IImpairmentDef
{
	public string ImpairmentId => "speech";

	public string ImpairmentName => "语言能力衰退";

	public ImpairmentStage[] Stages => new ImpairmentStage[4]
	{
		new ImpairmentStage
		{
			stageId = "mild",
			stageName = "表达困难",
			description = "偶尔找不到合适的词，说话略微迟缓。",
			socialMod = -1f,
			conspiracyMod = -0.5f
		},
		new ImpairmentStage
		{
			stageId = "moderate",
			stageName = "理解困难",
			description = "表达和理解都变得困难，复杂对话无法参与。",
			socialMod = -3f,
			conspiracyMod = -1.5f,
			managementMod = -1f
		},
		new ImpairmentStage
		{
			stageId = "severe",
			stageName = "严重失语",
			description = "几乎无法用语言表达和理解，只能通过简单手势交流。",
			socialMod = -6f,
			conspiracyMod = -3f,
			managementMod = -2f,
			charmMod = -1f
		},
		new ImpairmentStage
		{
			stageId = "profound",
			stageName = "完全失语",
			description = "语言能力完全丧失。",
			socialMod = -10f,
			conspiracyMod = -5f,
			managementMod = -3f,
			healthMod = -0.1f,
			permanentTraitId = "aphasia"
		}
	};
}
}
