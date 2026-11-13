using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Units
{
public static class UnitCategoryRules
{
	public static UnitCategory Of(TransportUnitType type)
	{
		switch (type)
		{
		case TransportUnitType.Caravan:
			return UnitCategory.Trade;
		case TransportUnitType.Settlers:
			return UnitCategory.Migration;
		case TransportUnitType.Warband:
		case TransportUnitType.MercenaryCompany:
			return UnitCategory.Military;
		case TransportUnitType.Army:
		case TransportUnitType.Patrol:
			return UnitCategory.Military;
		default:
			return UnitCategory.Military;
		}
	}

	public static bool UsesMilitaryModules(UnitCategory category)
	{
		return category == UnitCategory.Military;
	}

	public static bool UsesTradeModules(UnitCategory category)
	{
		return category == UnitCategory.Trade;
	}

	public static bool UsesMigrationModules(UnitCategory category)
	{
		return category == UnitCategory.Migration;
	}

	public static bool UsesBanditModules(UnitCategory category)
	{
		return category == UnitCategory.Bandit;
	}

	public static bool UsesWildernessModules(UnitCategory category)
	{
		return category == UnitCategory.Wilderness;
	}
}
}
