namespace MyriadCreation.Core.Contracts
{
public interface ISimulationCondition
{
	string ConditionId { get; }

	bool Evaluate(in SimulationConditionContext context);
}
}
