using MyriadCreation.Core.Enums;

namespace MyriadCreation.Content.Definitions
{
public static class LegacyIdentityMapping
{
	public static IdentityProfile FromLegacy(GameEnums.SocialClass socialClass, GameEnums.SocialSubclass? subclass = null)
	{
		IdentityProfile identityProfile = new IdentityProfile();
		switch (socialClass)
		{
		case GameEnums.SocialClass.Slave:
			identityProfile.economicClass = EconomicClass.Slave;
			identityProfile.stratum = SocialStratum.Lower;
			break;
		case GameEnums.SocialClass.Peasant:
		{
			IdentityProfile identityProfile2 = identityProfile;
			identityProfile2.economicClass = subclass switch
			{
				GameEnums.SocialSubclass.Tenant => EconomicClass.Tenant, 
				GameEnums.SocialSubclass.Serf => EconomicClass.Serf, 
				GameEnums.SocialSubclass.HiredLaborer => EconomicClass.HiredLaborer, 
				_ => EconomicClass.Freeholder, 
			};
			identityProfile.stratum = SocialStratum.LowerMiddle;
			identityProfile.occupations = OccupationFlags.Farmer;
			break;
		}
		case GameEnums.SocialClass.MerchantFreeman:
			identityProfile.economicClass = EconomicClass.SelfEmployed;
			identityProfile.stratum = SocialStratum.Middle;
			identityProfile.identity |= SocialIdentityFlags.Citizen;
			switch (subclass)
			{
			case GameEnums.SocialSubclass.Merchant:
				identityProfile.occupations = OccupationFlags.Merchant;
				break;
			case GameEnums.SocialSubclass.Artisan:
				identityProfile.occupations = OccupationFlags.Artisan;
				break;
			case GameEnums.SocialSubclass.Scholar:
				identityProfile.occupations = OccupationFlags.Scholar;
				break;
			case GameEnums.SocialSubclass.Citizen:
				identityProfile.identity |= SocialIdentityFlags.Citizen;
				break;
			default:
				identityProfile.occupations = OccupationFlags.None;
				break;
			}
			break;
		case GameEnums.SocialClass.NobilityClergy:
			identityProfile.economicClass = EconomicClass.Lord;
			identityProfile.stratum = SocialStratum.Upper;
			identityProfile.identity |= SocialIdentityFlags.Noble | SocialIdentityFlags.Clergy;
			break;
		case GameEnums.SocialClass.Royalty:
			identityProfile.economicClass = EconomicClass.Lord;
			identityProfile.stratum = SocialStratum.Upper;
			identityProfile.identity |= SocialIdentityFlags.Royalty | SocialIdentityFlags.Noble;
			break;
		default:
			identityProfile.economicClass = EconomicClass.Freeholder;
			break;
		}
		return identityProfile;
	}
}
}
