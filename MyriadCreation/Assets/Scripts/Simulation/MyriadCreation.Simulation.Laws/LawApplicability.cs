using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.Laws
{
/// <summary>
/// 法律的人格效力（P2.5/M5 合并项）：同一条法对不同身份给出不同适用结果。
/// 豁免优先于适用范围；`appliesTo=None` 表示不限制身份。
/// </summary>
public static class LawApplicability
{
	public static bool AppliesTo(LawDef law, SocialIdentityFlags identity)
	{
		if (law == null)
		{
			return false;
		}
		if (law.exemptIdentities != SocialIdentityFlags.None && (law.exemptIdentities & identity) != 0)
		{
			return false;
		}
		if (law.appliesTo == SocialIdentityFlags.None)
		{
			return true;
		}
		return (law.appliesTo & identity) != 0;
	}
}
}
