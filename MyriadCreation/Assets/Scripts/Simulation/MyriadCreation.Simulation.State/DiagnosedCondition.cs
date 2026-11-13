using System;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public struct DiagnosedCondition
{
	public string conditionId;

	public string displayName;

	public SubStructureType sourceSubStructure;

	public BodyPartType sourcePart;

	public int diagnosedDay;

	public float severityAtDiagnosis;
}
}
