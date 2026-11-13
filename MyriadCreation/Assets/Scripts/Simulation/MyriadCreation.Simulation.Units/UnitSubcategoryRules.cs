using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Units
{
public static class UnitSubcategoryRules
{
	public static UnitInteraction Interaction(UnitSubcategory subcategory)
	{
		return subcategory switch
		{
			UnitSubcategory.Refugee => UnitInteraction.Travel, 
			UnitSubcategory.Nomad => UnitInteraction.Graze, 
			UnitSubcategory.Courier => UnitInteraction.DeliverOrders, 
			UnitSubcategory.Envoy => UnitInteraction.DeliverOrders, 
			UnitSubcategory.OfficialTravel => UnitInteraction.Travel, 
			UnitSubcategory.TradeCaravan => UnitInteraction.Trade, 
			UnitSubcategory.SupplyCaravan => UnitInteraction.Trade, 
			UnitSubcategory.Bandit => UnitInteraction.Raid, 
			UnitSubcategory.Predator => UnitInteraction.Predation, 
			UnitSubcategory.LocustSwarm => UnitInteraction.CropDamage, 
			UnitSubcategory.RodentPlague => UnitInteraction.CropDamage, 
			UnitSubcategory.InvasiveSpecies => UnitInteraction.CropDamage, 
			UnitSubcategory.Patrol => UnitInteraction.Travel, 
			_ => UnitInteraction.Travel, 
		};
	}
}
}
