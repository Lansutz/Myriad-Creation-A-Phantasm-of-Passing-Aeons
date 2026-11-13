using MyriadCreation.Simulation.Planning;

namespace MyriadCreation.Simulation.Systems
{
internal sealed class ConstructionPlanExecutor : IPlanExecutor
{
	private readonly ConstructionPlanSystem _system;

	public PlanType Type => PlanType.Construction;

	public ConstructionPlanExecutor(ConstructionPlanSystem system)
	{
		_system = system;
	}

	public PlanExecutionResult Execute(Plan plan, float deltaDays)
	{
		return _system.Execute(plan, deltaDays);
	}
}
}
