namespace MyriadCreation.Core.Contracts
{
public interface ISimulationCommandHandler<in TCommand> where TCommand : ISimulationCommand
{
	CommandResult Handle(TCommand command);
}
}
