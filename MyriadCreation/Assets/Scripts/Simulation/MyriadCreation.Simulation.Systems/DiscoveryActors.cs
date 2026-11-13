using MyriadCreation.World.Settlement;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class DiscoveryActors
{
	public static DiscoveryActorGroups FromSettlement(SettlementData settlement)
	{
		DiscoveryActorGroups discoveryActorGroups = DiscoveryActorGroups.None;
		if (settlement == null)
		{
			return discoveryActorGroups;
		}
		if (settlement.hasMarket || settlement.tradePower > 0f)
		{
			discoveryActorGroups |= DiscoveryActorGroups.Merchant;
		}
		if (HasSector(settlement, EconomicSector.Artisanal) || HasSector(settlement, EconomicSector.Mining))
		{
			discoveryActorGroups |= DiscoveryActorGroups.Artisan;
		}
		if (settlement.hasUniversity || settlement.hasTemple || HasSector(settlement, EconomicSector.Cultural) || HasSector(settlement, EconomicSector.Religious))
		{
			discoveryActorGroups |= DiscoveryActorGroups.Scholar;
		}
		return discoveryActorGroups;
	}

	public static bool Has(DiscoveryActorGroups groups, string group)
	{
		return group switch
		{
			"merchant" => (groups & DiscoveryActorGroups.Merchant) != 0, 
			"artisan" => (groups & DiscoveryActorGroups.Artisan) != 0, 
			"scholar" => (groups & DiscoveryActorGroups.Scholar) != 0, 
			_ => false, 
		};
	}

	public static int Count(DiscoveryActorGroups groups)
	{
		int num = 0;
		if ((groups & DiscoveryActorGroups.Merchant) != 0)
		{
			num++;
		}
		if ((groups & DiscoveryActorGroups.Artisan) != 0)
		{
			num++;
		}
		if ((groups & DiscoveryActorGroups.Scholar) != 0)
		{
			num++;
		}
		return num;
	}

	private static bool HasSector(SettlementData settlement, EconomicSector sector)
	{
		if (settlement.economicComposition != null && settlement.economicComposition.TryGetValue(sector, out var value))
		{
			return value > 0f;
		}
		return false;
	}
}
}
