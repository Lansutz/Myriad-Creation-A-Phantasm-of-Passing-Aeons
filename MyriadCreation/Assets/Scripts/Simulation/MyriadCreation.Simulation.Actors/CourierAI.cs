using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Actors
{
public class CourierAI : IMapActorAI
{
	private readonly int _targetTile;

	public CourierAI(int targetTile)
	{
		_targetTile = targetTile;
	}

	public void Think(GameWorld world, MapActor actor)
	{
		if (_targetTile >= 0 && actor.targetTile != _targetTile)
		{
			actor.SetTarget(_targetTile);
		}
	}

	public void Act(GameWorld world, MapActor actor)
	{
	}
}
}
