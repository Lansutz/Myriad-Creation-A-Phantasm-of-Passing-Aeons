namespace MyriadCreation.Simulation.Units
{
public static class UnitTypeRules
{
	public static bool IsArmyType(TransportUnitType type)
	{
		if (type != TransportUnitType.Army && type != TransportUnitType.Warband && type != TransportUnitType.MercenaryCompany)
		{
			return type == TransportUnitType.Patrol;
		}
		return true;
	}

	public static bool CanConvert(TransportUnitType from, TransportUnitType to)
	{
		if (from == to)
		{
			return true;
		}
		if (IsArmyType(from) && IsArmyType(to))
		{
			return true;
		}
		if (from == TransportUnitType.Caravan && to == TransportUnitType.Settlers)
		{
			return true;
		}
		if (from == TransportUnitType.Settlers && IsArmyType(to))
		{
			return true;
		}
		return false;
	}
}
}
