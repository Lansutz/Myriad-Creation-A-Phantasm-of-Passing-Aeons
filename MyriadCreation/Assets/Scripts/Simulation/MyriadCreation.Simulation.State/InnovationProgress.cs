using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class InnovationProgress
{
	public int innovationId;

	public float progress;

	public float monthlyGain;

	public bool isAvailable;

	public string lockedReason;

	public Dictionary<string, float> gainBreakdown = new Dictionary<string, float>();

	public float cumulativeOutput;

	public float averageQuality;

	public int oldMethodPracticeCount;
}
}
