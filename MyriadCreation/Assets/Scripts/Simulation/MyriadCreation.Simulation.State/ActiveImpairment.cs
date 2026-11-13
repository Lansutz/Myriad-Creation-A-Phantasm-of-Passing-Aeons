using System;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public struct ActiveImpairment
{
	public string impairmentId;

	public int stageIndex;

	public float progressionRate;

	public int daysAtCurrentStage;

	public string cause;

	public bool isReversible;
}
}
