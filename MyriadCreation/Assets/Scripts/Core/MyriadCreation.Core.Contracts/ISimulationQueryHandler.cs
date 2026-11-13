namespace MyriadCreation.Core.Contracts
{
public interface ISimulationQueryHandler<in TQuery, TResult> where TQuery : ISimulationQuery<TResult>
{
	TResult Handle(TQuery query);
}
}
