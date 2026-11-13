namespace MyriadCreation.Simulation.State
{
public interface IImpairmentDef
{
	string ImpairmentId { get; }

	string ImpairmentName { get; }

	ImpairmentStage[] Stages { get; }
}
}
