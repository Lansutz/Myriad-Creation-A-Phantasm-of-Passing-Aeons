namespace MyriadCreation.Core.Enums
{
public static class RelationRules
{
	public static bool IsAllowed(EntityKind from, EntityKind to, RelationType type)
	{
		switch (type)
		{
		case RelationType.DirectRule:
			if (from == EntityKind.Realm)
			{
				return to == EntityKind.Anchor;
			}
			return false;
		case RelationType.MilitaryOccupation:
			if (from == EntityKind.Realm || from == EntityKind.Unit)
			{
				if (to != EntityKind.Realm)
				{
					return to == EntityKind.Anchor;
				}
				return true;
			}
			return false;
		case RelationType.Vassalage:
		case RelationType.PersonalUnion:
		case RelationType.Protectorate:
		case RelationType.Tributary:
			if (from == EntityKind.Realm)
			{
				return to == EntityKind.Realm;
			}
			return false;
		case RelationType.DelegatedRule:
			if (from == EntityKind.Realm)
			{
				if (to != EntityKind.Anchor && to != EntityKind.Province)
				{
					return to == EntityKind.Character;
				}
				return true;
			}
			return false;
		case RelationType.Lease:
		case RelationType.Condominium:
			if (from == EntityKind.Realm)
			{
				return to == EntityKind.Anchor;
			}
			return false;
		case RelationType.Commission:
			if (from == EntityKind.Realm)
			{
				return to == EntityKind.Unit;
			}
			return false;
		case RelationType.Employment:
			if (from == EntityKind.Realm || from == EntityKind.Character || from == EntityKind.Unit)
			{
				return to == EntityKind.Unit;
			}
			return false;
		case RelationType.Command:
			if (from == EntityKind.Character)
			{
				return to == EntityKind.Unit;
			}
			return false;
		case RelationType.Subordination:
			if (from == EntityKind.Unit)
			{
				return to == EntityKind.Unit;
			}
			return false;
		case RelationType.Garrison:
			if (from == EntityKind.Unit)
			{
				return to == EntityKind.Anchor;
			}
			return false;
		case RelationType.SpatialSubordination:
			if (from == EntityKind.Anchor)
			{
				return to == EntityKind.Anchor;
			}
			return false;
		case RelationType.Administrative:
			if (from == EntityKind.Realm)
			{
				return to == EntityKind.Province;
			}
			return false;
		default:
			return false;
		}
	}
}
}
