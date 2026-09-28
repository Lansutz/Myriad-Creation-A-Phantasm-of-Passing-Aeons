namespace MyriadCreation.World.Settlement
{
    public enum SiteTransitionFailure
    {
        None,
        MissingSite,
        InvalidTargetMode,
        ForbiddenTransition
    }

    /// <summary>
    /// 栖居点模式转换规则。
    /// 正常发展只能向上：营地 → 据点 / 定居点，据点 → 定居点。
    /// 定居点是终态，只有毁坏才走显式的降级路径。
    /// </summary>
    public static class HabitationSiteTransitionSystem
    {
        public static bool TryTransition(
            SettlementData site,
            SiteMode targetMode,
            out SiteTransitionFailure failure)
        {
            if (!TryTransition((HabitationSite)site, targetMode, out failure))
                return false;

            site.SetLegacyCategoryFromSiteMode();
            return true;
        }

        public static bool TryTransition(
            HabitationSite site,
            SiteMode targetMode,
            out SiteTransitionFailure failure)
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

        /// <summary>
        /// 毁坏导致的降级。只有明确的损毁流程可以调用，
        /// 与正常发展的向上转换分开。
        /// </summary>
        public static bool TryDowngradeDueToDestruction(
            HabitationSite site,
            SiteMode targetMode,
            out SiteTransitionFailure failure)
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

        public static bool TrySetPendingMode(
            HabitationSite site,
            SiteMode targetMode,
            out SiteTransitionFailure failure)
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
            return site != null && site.siteMode == SiteMode.TemporaryEncampment;
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
            return mode == SiteMode.SedentarySettlement
                || mode == SiteMode.FortifiedOutpost
                || mode == SiteMode.TemporaryEncampment;
        }

        // 正常发展：只能向上。
        private static bool IsAllowedUpgrade(SiteMode source, SiteMode target)
        {
            switch (source)
            {
                case SiteMode.TemporaryEncampment:
                    return target == SiteMode.FortifiedOutpost
                        || target == SiteMode.SedentarySettlement;
                case SiteMode.FortifiedOutpost:
                    return target == SiteMode.SedentarySettlement;
                default:
                    return false;
            }
        }

        // 毁坏降级：定居点 / 据点被摧毁后才允许向下。
        private static bool IsAllowedDestructionDowngrade(SiteMode source, SiteMode target)
        {
            switch (source)
            {
                case SiteMode.SedentarySettlement:
                    return target == SiteMode.FortifiedOutpost
                        || target == SiteMode.TemporaryEncampment;
                case SiteMode.FortifiedOutpost:
                    return target == SiteMode.TemporaryEncampment;
                default:
                    return false;
            }
        }
    }
}
