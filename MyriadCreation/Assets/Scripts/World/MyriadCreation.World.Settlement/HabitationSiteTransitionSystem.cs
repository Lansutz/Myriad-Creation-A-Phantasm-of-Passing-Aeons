using MyriadCreation.Core.Enums;

namespace MyriadCreation.World.Settlement
{
public static class HabitationSiteTransitionSystem
{
	public static bool TryTransition(SettlementData site, SiteMode targetMode, out SiteTransitionFailure failure)
	{
		if (!TryTransition((HabitationSite)site, targetMode, out failure))
		{
			return false;
		}
		site.SetLegacyCategoryFromSiteMode();
		return true;
	}

	public static bool TryTransition(HabitationSite site, SiteMode targetMode, out SiteTransitionFailure failure)
	{
		if (site == null)
		{
			failure = SiteTransitionFailure.MissingSite;
			return false;
		}
		if (!IsKnownMode(targetMode))
		{
			failure = SiteTransitionFailure.InvalidTargetMode;
			return false;
		}
		if (site.siteMode == targetMode)
		{
			failure = SiteTransitionFailure.None;
			return true;
		}
		if (!IsAllowedUpgrade(site.siteMode, targetMode))
		{
			failure = SiteTransitionFailure.ForbiddenTransition;
			return false;
		}
		ApplyMode(site, targetMode);
		failure = SiteTransitionFailure.None;
		return true;
	}

	public static bool TryDowngradeDueToDestruction(HabitationSite site, SiteMode targetMode, out SiteTransitionFailure failure)
	{
		if (site == null)
		{
			failure = SiteTransitionFailure.MissingSite;
			return false;
		}
		if (!IsKnownMode(targetMode))
		{
			failure = SiteTransitionFailure.InvalidTargetMode;
			return false;
		}
		if (site.siteMode == targetMode)
		{
			failure = SiteTransitionFailure.None;
			return true;
		}
		if (!IsAllowedDestructionDowngrade(site.siteMode, targetMode))
		{
			failure = SiteTransitionFailure.ForbiddenTransition;
			return false;
		}
		ApplyMode(site, targetMode);
		failure = SiteTransitionFailure.None;
		return true;
	}

	public static bool TrySetPendingMode(HabitationSite site, SiteMode targetMode, out SiteTransitionFailure failure)
	{
		if (site == null)
		{
			failure = SiteTransitionFailure.MissingSite;
			return false;
		}
		if (!IsKnownMode(targetMode))
		{
			failure = SiteTransitionFailure.InvalidTargetMode;
			return false;
		}
		if (site.siteMode != targetMode && !IsAllowedUpgrade(site.siteMode, targetMode))
		{
			failure = SiteTransitionFailure.ForbiddenTransition;
			return false;
		}
		site.pendingSiteMode = targetMode;
		site.hasPendingSiteMode = true;
		failure = SiteTransitionFailure.None;
		return true;
	}

	public static bool CanDissolve(HabitationSite site)
	{
		if (site != null)
		{
			return site.siteMode == SiteMode.TemporaryEncampment;
		}
		return false;
	}

	public static bool CanDelete(HabitationSite site)
	{
		return CanDissolve(site);
	}

	private static void ApplyMode(HabitationSite site, SiteMode targetMode)
	{
		site.previousSiteMode = site.siteMode;
		site.hasPreviousSiteMode = true;
		site.InitializeModeData(targetMode);
		site.siteMode = targetMode;
		site.hasPendingSiteMode = false;
	}

	private static bool IsKnownMode(SiteMode mode)
	{
		if (mode != SiteMode.SedentarySettlement && mode != SiteMode.FortifiedOutpost)
		{
			return mode == SiteMode.TemporaryEncampment;
		}
		return true;
	}

	private static bool IsAllowedUpgrade(SiteMode source, SiteMode target)
	{
		switch (source)
		{
		case SiteMode.TemporaryEncampment:
			if (target != SiteMode.FortifiedOutpost)
			{
				return target == SiteMode.SedentarySettlement;
			}
			return true;
		case SiteMode.FortifiedOutpost:
			return target == SiteMode.SedentarySettlement;
		default:
			return false;
		}
	}

	private static bool IsAllowedDestructionDowngrade(SiteMode source, SiteMode target)
	{
		switch (source)
		{
		case SiteMode.SedentarySettlement:
			if (target != SiteMode.FortifiedOutpost)
			{
				return target == SiteMode.TemporaryEncampment;
			}
			return true;
		case SiteMode.FortifiedOutpost:
			return target == SiteMode.TemporaryEncampment;
		default:
			return false;
		}
	}
}
}
