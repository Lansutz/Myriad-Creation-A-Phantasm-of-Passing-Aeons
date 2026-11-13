using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Actors
{
public interface IMapActorAI
{
	void Think(GameWorld world, MapActor actor);

	void Act(GameWorld world, MapActor actor);
}
}
