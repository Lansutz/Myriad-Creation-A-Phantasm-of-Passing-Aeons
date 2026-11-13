namespace MyriadCreation.Simulation.Scheduling
{
public sealed class SimulationBudgetGovernor
{
	public float MinBudgetMs = 4f;

	public float MaxBudgetMs = 12f;

	public float TargetFrameMs = 16.7f;

	public float StepUpMs = 1f;

	public float StepDownMs = 0.5f;

	public float CurrentBudgetMs = 8f;

	public float NextBudget(float frameMs, int pendingEntries, int deferredLastTick)
	{
		bool num = pendingEntries > 0 || deferredLastTick > 0;
		bool flag = frameMs > TargetFrameMs * 1.15f;
		bool flag2 = frameMs < TargetFrameMs * 0.8f;
		if (num | flag)
		{
			CurrentBudgetMs += StepUpMs;
		}
		else if (flag2)
		{
			CurrentBudgetMs -= StepDownMs;
		}
		if (CurrentBudgetMs < MinBudgetMs)
		{
			CurrentBudgetMs = MinBudgetMs;
		}
		if (CurrentBudgetMs > MaxBudgetMs)
		{
			CurrentBudgetMs = MaxBudgetMs;
		}
		return CurrentBudgetMs;
	}

	public void Reset()
	{
		CurrentBudgetMs = 8f;
	}
}
}
