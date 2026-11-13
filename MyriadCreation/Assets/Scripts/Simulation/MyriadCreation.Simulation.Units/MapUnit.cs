using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.State;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Units
{
public class MapUnit
{
	public int unitId;

	public UnitSourceKind source;

	public int sourceId;

	public TransportUnitType type;

	public string unitName = "";

	public int leaderCharacterId = -1;

	public int ownerRealmId = -1;

	public int currentTile = -1;

	public int currentSettlementAnchorId = -1;

	public TransportUnit transport = new TransportUnit();

	public UnitCategory category;

	public UnitSubcategory subcategory;

	public bool IsArmy
	{
		get
		{
			if (type != TransportUnitType.Army && type != TransportUnitType.Warband && type != TransportUnitType.Patrol)
			{
				return type == TransportUnitType.MercenaryCompany;
			}
			return true;
		}
	}

	public bool ConvertTo(TransportUnitType target)
	{
		if (!UnitTypeRules.CanConvert(type, target))
		{
			return false;
		}
		type = target;
		transport.type = target;
		return true;
	}

	public static MapUnit FromMapActor(MapActor actor)
	{
		if (actor == null)
		{
			return null;
		}
		TransportUnitType transportUnitType = MapActorTypeToUnit(actor.type);
		return new MapUnit
		{
			source = UnitSourceKind.MapActor,
			sourceId = actor.actorId,
			type = transportUnitType,
			category = CategoryForActor(actor.type),
			subcategory = actor.subcategory,
			unitName = actor.actorName,
			leaderCharacterId = actor.leaderCharacterId,
			ownerRealmId = actor.ownerRealmId,
			currentTile = actor.currentTile,
			currentSettlementAnchorId = actor.currentSettlementAnchorId,
			transport = (actor.transport ?? new TransportUnit
			{
				type = transportUnitType
			})
		};
	}

	public static MapUnit FromArmy(Army army)
	{
		if (army == null)
		{
			return null;
		}
		return new MapUnit
		{
			source = UnitSourceKind.Army,
			sourceId = army.armyId,
			type = TransportUnitType.Army,
			category = UnitCategory.Military,
			subcategory = UnitSubcategory.None,
			unitName = army.armyName,
			leaderCharacterId = army.commanderId,
			ownerRealmId = army.ownerRealmId,
			currentTile = army.currentTileIndex,
			transport = (army.transport ?? new TransportUnit
			{
				type = TransportUnitType.Army
			})
		};
	}

	public static UnitCategory CategoryForActor(MapActorType type)
	{
		switch (type)
		{
		case MapActorType.Caravan:
			return UnitCategory.Trade;
		case MapActorType.Bandit:
			return UnitCategory.Bandit;
		case MapActorType.Refugee:
		case MapActorType.Nomad:
			return UnitCategory.Migration;
		case MapActorType.Wildlife:
			return UnitCategory.Wilderness;
		case MapActorType.Mercenary:
		case MapActorType.Pilgrim:
			return UnitCategory.Military;
		case MapActorType.Courier:
			return UnitCategory.Migration;
		default:
			return UnitCategory.Military;
		}
	}

	public static TransportUnitType MapActorTypeToUnit(MapActorType type)
	{
		switch (type)
		{
		case MapActorType.Caravan:
			return TransportUnitType.Caravan;
		case MapActorType.Mercenary:
			return TransportUnitType.MercenaryCompany;
		case MapActorType.Bandit:
			return TransportUnitType.Warband;
		case MapActorType.Pilgrim:
			return TransportUnitType.Army;
		case MapActorType.Courier:
			return TransportUnitType.Settlers;
		case MapActorType.Refugee:
		case MapActorType.Nomad:
			return TransportUnitType.Settlers;
		case MapActorType.Wildlife:
			return TransportUnitType.Patrol;
		default:
			return TransportUnitType.Army;
		}
	}
}
}
