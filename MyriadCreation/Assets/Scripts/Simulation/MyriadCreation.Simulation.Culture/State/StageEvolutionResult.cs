using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.Culture
{
public struct StageEvolutionResult
{
	public bool canEvolve;

	public GameEnums.CultureStage targetStage;

	public List<string> unmetConditions;

	public float progress;
}
}
