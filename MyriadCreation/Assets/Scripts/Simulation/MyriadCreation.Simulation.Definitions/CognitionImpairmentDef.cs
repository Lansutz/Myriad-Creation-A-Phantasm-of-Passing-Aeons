using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.Definitions
{
public class CognitionImpairmentDef : IImpairmentDef
{
	public string ImpairmentId => "cognition";

	public string ImpairmentName => "认知衰退";

	public ImpairmentStage[] Stages => new ImpairmentStage[4]
	{
		new ImpairmentStage
		{
			stageId = "mild",
			stageName = "健忘",
			description = "偶尔忘记事情，对日常生活影响不大。",
			scholarshipMod = -1f,
			managementMod = -0.5f
		},
		new ImpairmentStage
		{
			stageId = "moderate",
			stageName = "轻度认知障碍",
			description = "记忆力和思考能力明显下降，处理复杂事务变得困难。",
			scholarshipMod = -2.5f,
			managementMod = -1.5f,
			conspiracyMod = -1f,
			socialMod = -1f
		},
		new ImpairmentStage
		{
			stageId = "severe",
			stageName = "痴呆",
			description = "认知能力严重衰退，无法正常处理复杂事务，需要他人照顾。",
			scholarshipMod = -5f,
			managementMod = -4f,
			conspiracyMod = -3f,
			socialMod = -3f
		},
		new ImpairmentStage
		{
			stageId = "profound",
			stageName = "失能",
			description = "认知能力严重衰退，无法正常执政和处理事务。对应CK3的Incapable特质。",
			scholarshipMod = -10f,
			managementMod = -8f,
			conspiracyMod = -6f,
			socialMod = -5f,
			healthMod = -0.3f,
			permanentTraitId = "dementia"
		}
	};
}
}
