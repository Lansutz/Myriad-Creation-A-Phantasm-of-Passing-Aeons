using System.Collections.Generic;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Simulation.Units
{
public static class MapUnitRegistry
{
	private static readonly Dictionary<int, MapUnit> _units = new Dictionary<int, MapUnit>();

	private static int _nextId = 1;

	public static IReadOnlyCollection<MapUnit> All => _units.Values;

	public static int Count => _units.Count;

	public static void Clear()
	{
		_units.Clear();
		_nextId = 1;
	}

	/// <summary>
	/// 读档失败回滚：整表还原快照。按对象自身 unitId 重新登记，并把自增游标推进到最大值之后。
	/// </summary>
	public static void Restore(IReadOnlyCollection<MapUnit> snapshot)
	{
		Clear();
		if (snapshot == null) return;
		int nextId = 1;
		foreach (var unit in snapshot)
		{
			if (unit == null) continue;
			Add(unit);
			if (unit.unitId >= nextId) nextId = unit.unitId + 1;
		}
		AdoptNextId(nextId);
	}

	/// <summary>
	/// 读档后把自增 ID 推进到存档最大值之后：否则随后的 Add（军队视图等）会从 1 重新分配，
	/// 覆盖已按存档 unitId 还原的地图单位条目。只前进不后退。
	/// </summary>
	public static void AdoptNextId(int nextId)
	{
		if (nextId > _nextId)
		{
			_nextId = nextId;
		}
	}

	public static MapUnit Add(MapUnit unit)
	{
		if (unit == null)
		{
			return null;
		}
		if (unit.unitId <= 0)
		{
			unit.unitId = _nextId++;
		}
		_units[unit.unitId] = unit;
		return unit;
	}

	public static bool TryGet(int unitId, out MapUnit unit)
	{
		return _units.TryGetValue(unitId, out unit);
	}

	public static bool Remove(int unitId)
	{
		return _units.Remove(unitId);
	}

	public static MapUnit RegisterArmy(Army army)
	{
		MapUnit mapUnit = MapUnit.FromArmy(army);
		if (mapUnit == null)
		{
			return null;
		}
		RemoveBySource(UnitSourceKind.Army, mapUnit.sourceId);
		return Add(mapUnit);
	}

	public static MapUnit RegisterActor(MapActor actor)
	{
		MapUnit mapUnit = MapUnit.FromMapActor(actor);
		if (mapUnit == null)
		{
			return null;
		}
		RemoveBySource(UnitSourceKind.MapActor, mapUnit.sourceId);
		return Add(mapUnit);
	}

	public static bool RemoveBySource(UnitSourceKind source, int sourceId)
	{
		int num = -1;
		foreach (KeyValuePair<int, MapUnit> unit in _units)
		{
			if (unit.Value.source == source && unit.Value.sourceId == sourceId)
			{
				num = unit.Key;
				break;
			}
		}
		if (num > 0)
		{
			return _units.Remove(num);
		}
		return false;
	}

	public static void SyncFromWorld(GameWorld world)
	{
		Clear();
		if ((Object)(object)world == (Object)null)
		{
			return;
		}
		if (world.armies != null)
		{
			foreach (Army value in world.armies.Values)
			{
				Add(MapUnit.FromArmy(value));
			}
		}
		if (world.MapActors == null)
		{
			return;
		}
		foreach (MapActor allActor in world.MapActors.AllActors)
		{
			Add(MapUnit.FromMapActor(allActor));
		}
	}
}
}
