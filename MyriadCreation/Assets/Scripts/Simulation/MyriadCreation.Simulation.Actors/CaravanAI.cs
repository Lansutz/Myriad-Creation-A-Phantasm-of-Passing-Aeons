using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Simulation.Actors
{
public class CaravanAI : IMapActorAI
{
	private readonly int _originTile;

	private readonly int _destinationTile;

	private bool _returning;

	private int _waitDays;

	public CaravanAI(int originTile, int destinationTile)
	{
		_originTile = originTile;
		_destinationTile = destinationTile;
	}

	public void Think(GameWorld world, MapActor actor)
	{
		if (_waitDays > 0)
		{
			_waitDays--;
			return;
		}
		int num = (_returning ? _originTile : _destinationTile);
		if (num >= 0 && num != actor.currentTile)
		{
			actor.SetTarget(num);
		}
	}

	public void Act(GameWorld world, MapActor actor)
	{
		if (!_returning && actor.currentTile == _destinationTile)
		{
			float num = Mathf.Clamp(actor.ResolveLeader(world)?.greed ?? 0f, -100f, 100f);
			_waitDays = Random.Range(3, 10) + Mathf.RoundToInt(Mathf.Max(0f, num) / 25f);
			_returning = true;
			actor.supplies = Mathf.Min(200f, actor.supplies + 50f);
		}
		else if (_returning && actor.currentTile == _originTile)
		{
			if (actor.leaderCharacterId >= 0)
			{
				_returning = false;
				_waitDays = Random.Range(2, 5);
			}
			else
			{
				actor.population = 0;
			}
		}
	}
}
}
