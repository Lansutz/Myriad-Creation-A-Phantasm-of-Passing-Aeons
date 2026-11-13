using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Anchor;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
public static class RelationProjection
{
	public static List<EntityRelation> Build(GameWorld world)
	{
		List<EntityRelation> list = new List<EntityRelation>();
		if ((Object)(object)world == (Object)null)
		{
			return list;
		}
		if (world.realms != null)
		{
			foreach (RealmData value2 in world.realms.Values)
			{
				if (value2.suzerainId >= 0 && world.realms.ContainsKey(value2.suzerainId))
				{
					list.Add(new EntityRelation
					{
						from = new EntityRef(EntityKind.Realm, value2.suzerainId),
						to = new EntityRef(EntityKind.Realm, value2.realmId),
						type = RelationType.Vassalage,
						isProjection = true
					});
				}
			}
		}
		if (world.tiles != null && world.anchors != null)
		{
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			foreach (AnchorData value3 in world.anchors.Values)
			{
				if (value3 != null)
				{
					dictionary[value3.tileIndex] = value3.anchorId;
				}
			}
			for (int i = 0; i < world.tiles.Length; i++)
			{
				TileData tileData = world.tiles[i];
				if (tileData.exists && dictionary.TryGetValue(i, out var value))
				{
					if (tileData.ownerRealmId >= 0)
					{
						list.Add(new EntityRelation
						{
							from = new EntityRef(EntityKind.Realm, tileData.ownerRealmId),
							to = new EntityRef(EntityKind.Anchor, value),
							type = RelationType.DirectRule,
							isPrimary = true,
							isProjection = true
						});
					}
					if (tileData.occupyingRealmId >= 0 && tileData.occupyingRealmId != tileData.ownerRealmId)
					{
						list.Add(new EntityRelation
						{
							from = new EntityRef(EntityKind.Realm, tileData.occupyingRealmId),
							to = new EntityRef(EntityKind.Anchor, value),
							type = RelationType.MilitaryOccupation,
							isProjection = true
						});
					}
				}
			}
		}
		if (world.Camps != null && world.anchors != null)
		{
			foreach (CampData allCamp in world.Camps.AllCamps)
			{
				if (allCamp != null && allCamp.anchorId >= 0 && allCamp.ownerRealmId >= 0)
				{
					list.Add(new EntityRelation
					{
						from = new EntityRef(EntityKind.Realm, allCamp.ownerRealmId),
						to = new EntityRef(EntityKind.Anchor, allCamp.anchorId),
						type = RelationType.DirectRule,
						isPrimary = true,
						isProjection = true
					});
				}
			}
		}
		if (world.armies != null)
		{
			foreach (Army value4 in world.armies.Values)
			{
				if (value4 != null && value4.ownerRealmId >= 0)
				{
					list.Add(new EntityRelation
					{
						from = new EntityRef(EntityKind.Realm, value4.ownerRealmId),
						to = new EntityRef(EntityKind.Unit, value4.armyId),
						type = RelationType.Commission,
						isPrimary = true,
						isProjection = true
					});
				}
			}
		}
		if (world.MapActors != null)
		{
			foreach (MapActor allActor in world.MapActors.AllActors)
			{
				if (allActor != null && allActor.ownerRealmId >= 0)
				{
					list.Add(new EntityRelation
					{
						from = new EntityRef(EntityKind.Realm, allActor.ownerRealmId),
						to = new EntityRef(EntityKind.Unit, allActor.actorId),
						type = RelationType.Employment,
						isProjection = true
					});
				}
			}
		}
		return list;
	}
}
}
