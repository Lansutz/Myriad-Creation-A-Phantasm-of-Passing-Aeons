using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Actors
{
public static class UnitAIFactory
{
	public static IMapActorAI Create(UnitSubcategory subcategory, int targetTile = -1)
	{
		switch (subcategory)
		{
		case UnitSubcategory.Refugee:
			return new RefugeeAI();
		case UnitSubcategory.Nomad:
			return new NomadAI();
		case UnitSubcategory.Courier:
		case UnitSubcategory.Envoy:
			if (targetTile < 0)
			{
				return new NomadAI();
			}
			return new CourierAI(targetTile);
		case UnitSubcategory.TradeCaravan:
		case UnitSubcategory.SupplyCaravan:
			return new NomadAI();
		case UnitSubcategory.Bandit:
			return new BanditAI();
		case UnitSubcategory.Predator:
			return new PredatorAI();
		case UnitSubcategory.LocustSwarm:
		case UnitSubcategory.RodentPlague:
		case UnitSubcategory.InvasiveSpecies:
			return new SwarmAI();
		case UnitSubcategory.Patrol:
			return new NomadAI();
		default:
			return new NomadAI();
		}
	}

	public static IMapActorAI CreateForActor(MapActor actor)
	{
		if (actor == null)
		{
			return null;
		}
		return CreateForLeader(actor.leaderAICategory, actor) ?? Create(actor.subcategory, actor.targetTile);
	}

	public static IMapActorAI CreateForLeader(CharacterAICategory category, MapActor actor)
	{
		if (actor == null)
		{
			return null;
		}
		switch (category)
		{
		case CharacterAICategory.Courier:
			if (actor.targetTile < 0)
			{
				return new NomadAI();
			}
			return new CourierAI(actor.targetTile);
		case CharacterAICategory.Merchant:
			if (actor.targetTile < 0 || actor.targetTile == actor.currentTile)
			{
				return new NomadAI();
			}
			return new CaravanAI(actor.currentTile, actor.targetTile);
		case CharacterAICategory.Migrant:
			return new RefugeeAI();
		case CharacterAICategory.Soldier:
			return new MercenaryAI();
		case CharacterAICategory.Raider:
			return new BanditAI();
		case CharacterAICategory.Ruler:
		case CharacterAICategory.Official:
		case CharacterAICategory.Courtier:
			if (actor.targetTile < 0)
			{
				return new NomadAI();
			}
			return new CourierAI(actor.targetTile);
		default:
			return null;
		}
	}
}
}
