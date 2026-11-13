using System;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public struct ImpairmentStage
{
	public string stageId;

	public string stageName;

	public string description;

	public float healthMod;

	public float prowessMod;

	public float socialMod;

	public float militaryMod;

	public float managementMod;

	public float conspiracyMod;

	public float scholarshipMod;

	public float charmMod;

	public float fertilityMod;

	public string permanentTraitId;
}
}
