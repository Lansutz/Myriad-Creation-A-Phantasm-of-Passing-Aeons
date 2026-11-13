namespace MyriadCreation.Simulation.Systems
{
public sealed class InfluenceSettings
{
	public float baseBudget = 8f;

	public float levelBudget = 10f;

	public float populationBudgetFactor = 0.8f;

	public float garrisonBudgetFactor = 0.005f;

	public float maxBudget = 120f;

	public float roadCostMultiplier = 0.55f;

	public float riverCrossingCostMultiplier = 2.2f;

	public float slopeCostFactor = 1f / 30f;

	public float coreFraction = 0.25f;

	public float secureFraction = 0.5f;

	public float contestedFraction = 0.75f;

	public static InfluenceSettings Default => new InfluenceSettings();
}
}
