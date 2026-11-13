using System.Collections.Generic;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.LawHost
{
/// <summary>
/// P1.3 法律生命周期写入方：Active → Consolidated（20 年成习）→ Fundamentalized（50 年 + 合法性 + 无推行中）。
/// 只写阶段字段，不改法律效果；换法时由 RealmLawState.SetActiveLaw 记废除并重置。
/// </summary>
public static class LawLifecycleSystem
{
	public const int ConsolidationYears = 20;

	public const int FundamentalizationYears = 50;

	public const float FundamentalizationLegitimacy = 70f;

	public static int YearlyTick(IEnumerable<RealmData> realms)
	{
		int changed = 0;
		if (realms == null)
		{
			return changed;
		}
		foreach (RealmData realm in realms)
		{
			if (realm?.lawState?.groups == null)
			{
				continue;
			}
			foreach (LawGroupState group in realm.lawState.groups)
			{
				if (group == null || string.IsNullOrEmpty(group.activeLawId))
				{
					continue;
				}
				if (group.stage != LegalNormStage.Active && group.stage != LegalNormStage.Consolidated)
				{
					continue;
				}
				group.yearsActive++;
				if (group.stage == LegalNormStage.Active && group.yearsActive >= ConsolidationYears)
				{
					group.stage = LegalNormStage.Consolidated;
					changed++;
				}
				else if (group.stage == LegalNormStage.Consolidated
					&& group.yearsActive >= FundamentalizationYears
					&& realm.lawState.legitimacy >= FundamentalizationLegitimacy
					&& realm.lawState.GetPendingLaw(group.groupId) == null)
				{
					group.stage = LegalNormStage.Fundamentalized;
					realm.lawState.fundamentalizedLawIds ??= new List<string>();
					if (!realm.lawState.fundamentalizedLawIds.Contains(group.activeLawId))
					{
						realm.lawState.fundamentalizedLawIds.Add(group.activeLawId);
					}
					changed++;
				}
			}
		}
		return changed;
	}
}
}
