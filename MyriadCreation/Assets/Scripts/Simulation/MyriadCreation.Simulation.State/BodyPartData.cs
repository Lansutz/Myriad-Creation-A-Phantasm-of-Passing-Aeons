using System;
using System.Collections.Generic;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public struct BodyPartData(BodyPartType part)
{
	public BodyPartType part = part;

	public float maxHp = 100f;

	public float currentHp = 100f;

	public float functionLevel = 1f;

	public BodyPartState state = BodyPartState.Healthy;

	public List<SubStructureData> subStructures = new List<SubStructureData>();

	public List<DiagnosedCondition> diagnosedConditions = new List<DiagnosedCondition>();
}
}
