using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Content.Modding;

namespace MyriadCreation.Simulation.CultureHost
{
    /// <summary>
    /// CultureTransition 的 host 片段（B5）：原 `ListTraditionCandidates` 返回 `TraditionDef`（Modding 类型），
    /// 因此从 Culture 程序集移到 host；CultureTransition 本体只保留纯抵抗度公式（依赖 IContentLookup）。
    /// </summary>
    public static class CultureTransitionHost
    {
        public static List<TraditionDef> ListTraditionCandidates(CultureData source, CultureData target, CharacterRole role = CharacterRole.Commoner)
        {
            List<TraditionDef> list = new List<TraditionDef>();
            if (source == null || target == null || target.traditionIds == null)
            {
                return list;
            }
            ContentLookupAdapter content = ContentLookupAdapter.Default;
            foreach (string traditionId in target.traditionIds)
            {
                if (source.HasTradition(traditionId, content))
                {
                    continue;
                }
                if (!content.TryGetTradition(traditionId, out TraditionDef value) || value == null)
                {
                    continue;
                }
                if (!value.RoleGateFulfilled(role))
                {
                    continue;
                }
                if (value.incompatibleWith != null)
                {
                    bool incompatible = false;
                    foreach (string item in value.incompatibleWith)
                    {
                        if (source.HasTradition(item, content))
                        {
                            incompatible = true;
                            break;
                        }
                    }
                    if (incompatible)
                    {
                        continue;
                    }
                }
                list.Add(value);
            }
            return list;
        }
    }
}
