using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.CultureHost;

namespace MyriadCreation.Simulation.Systems
{
public static class SocialClassAvailability
{
	public static bool IsSubclassAvailable(GameEnums.SocialSubclass subclass, CultureData culture, InnovationTree innovations, int realmId)
	{
		switch (subclass)
		{
		case GameEnums.SocialSubclass.Freeholder:
			if (HasAnyInnovation(innovations, realmId, 100, 916, 917, 918))
			{
				if (culture != null)
				{
					return culture.livelihoodType != 2;
				}
				return true;
			}
			return false;
		case GameEnums.SocialSubclass.Tenant:
			return HasAnyInnovation(innovations, realmId, 501, 952);
		case GameEnums.SocialSubclass.Serf:
			return HasInnovation(innovations, realmId, 952);
		case GameEnums.SocialSubclass.HiredLaborer:
			if (HasAnyInnovation(innovations, realmId, 100, 916, 917, 918))
			{
				return HasAnyInnovation(innovations, realmId, 701);
			}
			return false;
		case GameEnums.SocialSubclass.Citizen:
			return HasAnyInnovation(innovations, realmId, 980, 500);
		case GameEnums.SocialSubclass.Merchant:
			return HasAnyInnovation(innovations, realmId, 701, 700);
		case GameEnums.SocialSubclass.Artisan:
			if (HasAnyInnovation(innovations, realmId, 204, 200))
			{
				return true;
			}
			return HasTradition(culture, "trad_craft_guild");
		case GameEnums.SocialSubclass.Scholar:
			if (HasInnovation(innovations, realmId, 600))
			{
				return HasAnyInnovation(innovations, realmId, 503, 504);
			}
			return false;
		case GameEnums.SocialSubclass.DomesticSlave:
			return HasAnyInnovation(innovations, realmId, 500, 501);
		case GameEnums.SocialSubclass.StateSlave:
			return HasAnyInnovation(innovations, realmId, 503, 502);
		case GameEnums.SocialSubclass.DebtSlave:
			return HasInnovation(innovations, realmId, 505);
		case GameEnums.SocialSubclass.WarCaptiveSlave:
			return HasAnyInnovation(innovations, realmId, 906, 907, 201);
		default:
			return false;
		}
	}

	public static bool IsClassAvailable(GameEnums.SocialClass socialClass, CultureData culture, InnovationTree innovations, int realmId)
	{
		foreach (GameEnums.SocialSubclass subclass in GameEnums.SocialClassHierarchy.GetSubclasses(socialClass))
		{
			if (IsSubclassAvailable(subclass, culture, innovations, realmId))
			{
				return true;
			}
		}
		return socialClass switch
		{
			GameEnums.SocialClass.Royalty => HasAnyInnovation(innovations, realmId, 500, 501, 958), 
			GameEnums.SocialClass.NobilityClergy => HasAnyInnovation(innovations, realmId, 501, 601), 
			_ => false, 
		};
	}

	private static bool HasTradition(CultureData culture, string traditionId)
	{
		return culture != null && culture.HasTradition(traditionId, ContentLookupAdapter.Default);
	}

	private static bool HasInnovation(InnovationTree innovations, int realmId, int id)
	{
		return innovations?.HasInnovation(realmId, id) ?? true;
	}

	private static bool HasAnyInnovation(InnovationTree innovations, int realmId, params int[] ids)
	{
		if (innovations == null)
		{
			return true;
		}
		foreach (int id in ids)
		{
			if (HasInnovation(innovations, realmId, id))
			{
				return true;
			}
		}
		return false;
	}
}
}
