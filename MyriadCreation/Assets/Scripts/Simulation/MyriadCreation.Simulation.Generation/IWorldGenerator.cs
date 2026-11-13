using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Generation
{
public interface IWorldGenerator
{
	GenerationStage Stage { get; }

	void Generate(GameWorld world);
}
}
