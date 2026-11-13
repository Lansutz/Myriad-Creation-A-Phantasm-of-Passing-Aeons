using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Content.Modding;
using MyriadCreation.Simulation.ContentHost;

namespace MyriadCreation.Simulation.CultureHost
{
    /// <summary>
    /// <see cref="IContentLookup"/> 的 host 实现（B5）：把 Modding 的 ContentResolvers 静态目录
    /// 收敛到 Culture 程序集可注入的接口后面；Culture 不直接引用 Modding。
    /// 默认实例懒加载，供 SocialClassAvailability 等 host 调用点使用。
    /// </summary>
    public sealed class ContentLookupAdapter : IContentLookup
    {
        private static ContentLookupAdapter _default;

        public static ContentLookupAdapter Default => _default ??= new ContentLookupAdapter();

        public bool TryGetTraditionFactor(string traditionId, TraditionFactorKey key, out float value)
        {
            value = 0f;
            if (string.IsNullOrEmpty(traditionId))
            {
                return false;
            }
            if (!ContentResolvers.Traditions.TryGet(traditionId, out TraditionDef def) || def == null)
            {
                return false;
            }
            value = def.GetFactor(key);
            return true;
        }

        public bool TryGetEthnicGroupTraditionIds(int cultureId, out string[] traditionIds)
        {
            List<string> list = new List<string>();
            foreach (EthnicGroupDef group in ContentResolvers.EthnicGroups.All)
            {
                if (group == null || group.cultureId != cultureId || group.traditionIds == null)
                {
                    continue;
                }
                foreach (string traditionId in group.traditionIds)
                {
                    if (!string.IsNullOrEmpty(traditionId) && !list.Contains(traditionId))
                    {
                        list.Add(traditionId);
                    }
                }
            }
            traditionIds = list.ToArray();
            return list.Count > 0;
        }

        /// <summary>host 内部用：直接取传统定义（CultureTransitionHost 用）。</summary>
        public bool TryGetTradition(string traditionId, out TraditionDef def)
        {
            def = null;
            if (string.IsNullOrEmpty(traditionId))
            {
                return false;
            }
            return ContentResolvers.Traditions.TryGet(traditionId, out def) && def != null;
        }
    }
}
