using System;

namespace MyriadCreation.Core.Enums
{
[Flags]
public enum SocialIdentityFlags
{
	None = 0,
	Royalty = 1,
	Noble = 2,
	Clergy = 4,
	Citizen = 8,
	Freedman = 0x10,
	Foreigner = 0x20,
	Outlaw = 0x40
}
}
