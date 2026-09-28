using System.Collections.Generic;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Simulation.WorldState
{
    /// <summary>
    /// 栖居点统一查询入口。业务代码通过这里按 siteId / anchorId / tile / siteMode 查询，
    /// 不直接从 Anchor 推导位点模式。
    /// </summary>
    public partial class GameWorld
    {
        /// <summary>按 anchorId 查询栖居点（定居点/据点在 settlements，营地在 CampManager）。</summary>
        public bool TryGetSiteByAnchor(int anchorId, out HabitationSite site)
        {
            if (settlements.TryGetValue(anchorId, out var settlement))
            {
                site = settlement;
                return true;
            }

            if (_campManager != null && _campManager.TryGetActiveSiteByAnchor(anchorId, out var campSite))
            {
                site = campSite;
                return true;
            }

            site = null;
            return false;
        }

        /// <summary>按地块查询栖居点。</summary>
        public bool TryGetSiteAtTile(int tileIndex, out HabitationSite site)
        {
            foreach (var habitationSite in HabitationSites)
            {
                if (habitationSite.anchorId >= 0 &&
                    anchors.TryGetValue(habitationSite.anchorId, out var anchor) &&
                    anchor.tileIndex == tileIndex)
                {
                    site = habitationSite;
                    return true;
                }
            }

            site = null;
            return false;
        }

        /// <summary>查询某一模式的全部栖居点。</summary>
        public IEnumerable<HabitationSite> GetSitesByMode(SiteMode mode)
        {
            foreach (var site in HabitationSites)
                if (site.siteMode == mode)
                    yield return site;
        }
    }
}
