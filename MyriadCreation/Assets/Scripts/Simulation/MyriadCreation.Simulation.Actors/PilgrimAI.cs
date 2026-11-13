using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Simulation.Actors
{
public class PilgrimAI : IMapActorAI
{
	private readonly int _shrineTile;

	private int _stayDays;

	private bool _atShrine;

	public PilgrimAI(int shrineTile)
	{
		_shrineTile = shrineTile;
	}

	public void Think(GameWorld world, MapActor actor)
	{
		if (_atShrine)
		{
			_stayDays--;
			if (_stayDays <= 0)
			{
				actor.population = 0;
			}
		}
		else if (actor.currentTile != _shrineTile && _shrineTile >= 0)
		{
			actor.SetTarget(_shrineTile);
		}
	}

	public void Act(GameWorld world, MapActor actor)
	{
		if (actor.currentTile == _shrineTile && !_atShrine)
		{
			_atShrine = true;
			_stayDays = Random.Range(10, 30);
			Debug.Log((object)$"[PilgrimAI] 朝圣者#{actor.actorId} 到达圣地地块{_shrineTile}，停留{_stayDays}天");
		}
	}
}
}
