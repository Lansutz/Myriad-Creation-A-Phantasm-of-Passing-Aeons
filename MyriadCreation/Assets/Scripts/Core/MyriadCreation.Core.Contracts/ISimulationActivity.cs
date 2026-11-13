namespace MyriadCreation.Core.Contracts
{
public interface ISimulationActivity
{
	string ActivityId { get; }

	string DefinitionId { get; }

	string StateCode { get; }
}
}
