namespace MyriadCreation.Simulation.Planning
{
public interface IPlanExecutor
{
	PlanType Type { get; }

	PlanExecutionResult Execute(Plan plan, float deltaDays);
}
}
