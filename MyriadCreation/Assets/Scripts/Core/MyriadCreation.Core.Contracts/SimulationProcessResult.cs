using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
public readonly struct SimulationProcessResult(SimulationProcessOutcome outcome, float progressDelta = 0f, string resultCode = "", string resultSummary = "", IReadOnlyList<object> facts = null)
{
	public readonly SimulationProcessOutcome Outcome = outcome;

	public readonly float ProgressDelta = progressDelta;

	public readonly string ResultCode = resultCode ?? string.Empty;

	public readonly string ResultSummary = resultSummary ?? string.Empty;

	public readonly IReadOnlyList<object> Facts = facts;

	public static SimulationProcessResult Continue(float progressDelta = 0f)
	{
		return new SimulationProcessResult(SimulationProcessOutcome.Continue, progressDelta);
	}

	public static SimulationProcessResult Complete(string resultCode = "", string resultSummary = "")
	{
		return new SimulationProcessResult(SimulationProcessOutcome.Complete, 0f, resultCode, resultSummary);
	}

	public static SimulationProcessResult Wait(string resultCode = "", string resultSummary = "")
	{
		return new SimulationProcessResult(SimulationProcessOutcome.Wait, 0f, resultCode, resultSummary);
	}

	public static SimulationProcessResult Fail(string resultCode = "", string resultSummary = "")
	{
		return new SimulationProcessResult(SimulationProcessOutcome.Fail, 0f, resultCode, resultSummary);
	}

	public static SimulationProcessResult Cancel(string resultCode = "", string resultSummary = "")
	{
		return new SimulationProcessResult(SimulationProcessOutcome.Cancel, 0f, resultCode, resultSummary);
	}
}
}
