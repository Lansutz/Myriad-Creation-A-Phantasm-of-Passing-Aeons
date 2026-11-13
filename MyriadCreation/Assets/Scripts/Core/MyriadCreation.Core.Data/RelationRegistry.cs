using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Core.Data
{
public static class RelationRegistry
{
	private static readonly Dictionary<(EntityKind, int), List<EntityRelation>> _byTarget = new Dictionary<(EntityKind, int), List<EntityRelation>>();

	public static void Clear()
	{
		_byTarget.Clear();
	}

	public static bool Add(EntityRelation relation)
	{
		if (relation == null)
		{
			return false;
		}
		if (!RelationRules.IsAllowed(relation.from.kind, relation.to.kind, relation.type))
		{
			return false;
		}
		(EntityKind, int) key = (relation.to.kind, relation.to.id);
		if (!_byTarget.TryGetValue(key, out var value))
		{
			value = (_byTarget[key] = new List<EntityRelation>());
		}
		if (relation.isPrimary)
		{
			foreach (EntityRelation item in value)
			{
				item.isPrimary = false;
			}
		}
		value.Add(relation);
		return true;
	}

	public static bool TryGetPrimary(EntityKind kind, int id, out EntityRelation relation)
	{
		relation = null;
		if (!_byTarget.TryGetValue((kind, id), out var value))
		{
			return false;
		}
		foreach (EntityRelation item in value)
		{
			if (item.isPrimary)
			{
				relation = item;
				return true;
			}
		}
		return false;
	}

	public static IReadOnlyList<EntityRelation> GetRelations(EntityKind kind, int id)
	{
		if (!_byTarget.TryGetValue((kind, id), out var value))
		{
			return Array.Empty<EntityRelation>();
		}
		return value.ToArray();
	}

	public static List<EntityRelation> GetBySource(EntityKind kind, int id)
	{
		List<EntityRelation> list = new List<EntityRelation>();
		foreach (List<EntityRelation> value in _byTarget.Values)
		{
			foreach (EntityRelation item in value)
			{
				if (item.from.kind == kind && item.from.id == id)
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	public static bool Remove(EntityKind kind, int id, RelationType type)
	{
		if (!_byTarget.TryGetValue((kind, id), out var value))
		{
			return false;
		}
		int num = value.RemoveAll((EntityRelation r) => r.type == type);
		if (num > 0 && value.Count == 0)
		{
			_byTarget.Remove((kind, id));
		}
		return num > 0;
	}

	public static bool CanSetPrimary(EntityKind kind, int id, EntityRef from)
	{
		if (!TryGetPrimary(kind, id, out var relation))
		{
			return true;
		}
		if (relation.from.kind == from.kind)
		{
			return relation.from.id == from.id;
		}
		return false;
	}

	public static void Merge(IEnumerable<EntityRelation> relations)
	{
		if (relations == null)
		{
			return;
		}
		foreach (EntityRelation relation in relations)
		{
			Add(relation);
		}
	}

	public static int RemoveByTargetKind(EntityKind kind)
	{
		List<(EntityKind, int)> list = new List<(EntityKind, int)>();
		foreach (KeyValuePair<(EntityKind, int), List<EntityRelation>> item in _byTarget)
		{
			if (item.Key.Item1 == kind)
			{
				list.Add(item.Key);
			}
		}
		foreach (var item2 in list)
		{
			_byTarget.Remove(item2);
		}
		return list.Count;
	}

	public static List<EntityRelation> Capture()
	{
		List<EntityRelation> list = new List<EntityRelation>();
		foreach (List<EntityRelation> value in _byTarget.Values)
		{
			foreach (EntityRelation item in value)
			{
				if (!item.isProjection)
				{
					list.Add(item.Clone());
				}
			}
		}
		return list;
	}

	public static void Restore(IEnumerable<EntityRelation> relations)
	{
		Clear();
		if (relations == null)
		{
			return;
		}
		foreach (EntityRelation relation in relations)
		{
			Add(relation);
		}
	}
}
}
