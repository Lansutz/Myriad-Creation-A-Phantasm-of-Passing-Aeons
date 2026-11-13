using System.Collections.Generic;
using System.Text;
using MyriadCreation.Core.Contracts;
using MyriadCreation.UI.Presentation;

namespace MyriadCreation.UI.Panels
{
    /// <summary>
    /// 社会政治面板文本生成（4b4：契约入口；派系/政体变迁细节暂由 SocietyPanelLegacy 的旧重载负责）。
    /// </summary>
    public static partial class SocietyPanelText
    {
        /// <summary>4b4：IWorldReadModel 入口（政权头 + 官职 + 阶层区；派系/变迁无聚合契约）。</summary>
        public static string Build(IWorldReadModel view, int realmId, IReadOnlyDictionary<int, string> officeDisplay = null)
        {
            if (view == null)
            {
                return "";
            }
            RealmSnapshotDto realm = view.GetRealm(realmId);
            if (realm == null)
            {
                return "";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"【{realm.realmName}】稳定 {realm.stability:F0} | 集权 {realm.centralization:F2} | 国库 {realm.treasury:F0}");
            sb.AppendLine();

            if (officeDisplay != null && officeDisplay.Count > 0)
            {
                sb.AppendLine("── 官职体系 ──");
                foreach (var kv in officeDisplay)
                {
                    sb.AppendLine(kv.Value);
                }
                sb.AppendLine();
            }

            // 阶层区：只依赖 ISocietyQuery 的 DTO 文本
            sb.Append(ClassPanelText.Build(view, realmId));
            return sb.ToString();
        }

        /// <summary>4b4：仅社会/阶级快照（无政权头）入口。</summary>
        public static string Build(ISocietyQuery query, int realmId)
        {
            return ClassPanelText.Build(query, realmId);
        }
    }
}
