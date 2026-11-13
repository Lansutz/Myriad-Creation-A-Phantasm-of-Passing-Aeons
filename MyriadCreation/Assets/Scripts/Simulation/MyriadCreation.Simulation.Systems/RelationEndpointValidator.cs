using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class RelationEndpointValidator
{
	public static List<EntityRelation> Filter(GameWorld world, IEnumerable<EntityRelation> relations, out List<EntityRelation> dropped)
	{
		dropped = new List<EntityRelation>();
		List<EntityRelation> list = new List<EntityRelation>();
		if (relations == null)
		{
			return list;
		}
		foreach (EntityRelation relation in relations)
		{
			if (relation != null)
			{
				if (Exists(world, relation.from) && Exists(world, relation.to))
				{
					list.Add(relation);
				}
				else
				{
					dropped.Add(relation);
				}
			}
		}
		return list;
	}

	public static bool Exists(GameWorld world, EntityRef entity)
	{
		if ((Object)(object)world == (Object)null || entity.id < 0)
		{
			return false;
		}
		switch (entity.kind)
		{
		case EntityKind.Realm:
			if (world.realms != null)
			{
				return world.realms.ContainsKey(entity.id);
			}
			return false;
		case EntityKind.Anchor:
			if (world.anchors != null)
			{
				return world.anchors.ContainsKey(entity.id);
			}
			return false;
		case EntityKind.Province:
			if (world.provinces != null)
			{
				return world.provinces.ContainsKey(entity.id);
			}
			return false;
		case EntityKind.Unit:
			if (world.armies != null && world.armies.ContainsKey(entity.id))
			{
				return true;
			}
			if (world.MapActors != null)
			{
				return world.MapActors.GetActor(entity.id) != null;
			}
			return false;
		case EntityKind.Character:
		{
			CharacterManager characterManager = world.GetCharacterManager();
			if (characterManager != null)
			{
				return characterManager.GetCharacter(entity.id) != null;
			}
			return false;
		}
		default:
			return false;
		}
	}
}
}
