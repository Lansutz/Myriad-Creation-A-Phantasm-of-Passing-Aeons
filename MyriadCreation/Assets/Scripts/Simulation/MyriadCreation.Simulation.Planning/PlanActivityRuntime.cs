using System;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.Planning
{
[Serializable]
public sealed class PlanActivityRuntime : ISimulationActivity
{
	public string activityId = string.Empty;

	public string definitionId = string.Empty;

	public string stateCode = string.Empty;

	public string ActivityId => activityId;

	public string DefinitionId => definitionId;

	public string StateCode => stateCode;

	public void Set(string id, string definition, string state)
	{
		activityId = id ?? string.Empty;
		definitionId = definition ?? string.Empty;
		stateCode = state ?? string.Empty;
	}
}
}
