using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.Definitions
{
public class MobilityImpairmentDef : IImpairmentDef
{
	public string ImpairmentId => "mobility";

	public string ImpairmentName => "运动能力衰退";

	public ImpairmentStage[] Stages => new ImpairmentStage[4]
	{
		new ImpairmentStage
		{
			stageId = "mild",
			stageName = "关节疼痛",
			description = "关节偶尔疼痛，剧烈运动时加重。",
			prowessMod = -2f,
			healthMod = -0.05f
		},
		new ImpairmentStage
		{
			stageId = "moderate",
			stageName = "行动不便",
			description = "行动明显受限，长时间行走或站立变得困难。",
			prowessMod = -5f,
			militaryMod = -2f,
			healthMod = -0.1f
		},
		new ImpairmentStage
		{
			stageId = "severe",
			stageName = "需要辅助",
			description = "行走需要拐杖或他人搀扶，几乎无法参加战斗。",
			prowessMod = -10f,
			militaryMod = -4f,
			healthMod = -0.15f,
			charmMod = -1f
		},
		new ImpairmentStage
		{
			stageId = "profound",
			stageName = "瘫痪",
			description = "运动能力严重丧失，无法正常行动。",
			prowessMod = -15f,
			militaryMod = -5f,
			healthMod = -0.2f,
			permanentTraitId = "paralysis"
		}
	};
}
}
