using System;
using System.Text;
using System.Collections.Generic;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.UI
{
    /// <summary>
    /// 社会政治面板文本生成（阶层画像/派系力量/政体变迁状态——纯静态可测）
    /// </summary>
    public static class SocietyPanelText
    {
        /// <summary>生成面板全文（阶层区/派系区/政体变迁区）</summary>
        public static string Build(RealmData realm, RealmSociety society,
            FactionManager factions, RegimeChangeDynamics regime, int currentDay,
            IReadOnlyDictionary<int, string> officeDisplay = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"【{realm.realmName}】稳定 {realm.stability:F0} | 集权 {realm.centralization:F2} | 国库 {realm.treasury:F0}");
            sb.AppendLine();

            // ===== 官职体系区（officeHolders 持有者——OfficeTitle 消费） =====
            if (officeDisplay != null && officeDisplay.Count > 0)
            {
                sb.AppendLine("── 官职体系 ──");
                foreach (var kv in officeDisplay)
                    sb.AppendLine(kv.Value);
                sb.AppendLine();
            }

            // ===== 阶层区 =====
            sb.AppendLine("── 阶层画像 ──");
            if (society != null && society.classes != null)
            {
                foreach (var kv in society.classes)
                {
                    var p = kv.Value;
                    sb.AppendLine($"{ClassNames.Get(kv.Key)}：人口 {p.populationShare * 100f:F1}% | 满足 {p.satisfaction:F0} | " +
                                  $"不满 {p.unrest:F0} | 支持 {p.support:F0} | 影响 {p.influence:F0}");
                }
            }
            else sb.AppendLine("（无社会画像数据）");
            sb.AppendLine();

            // ===== 派系区 =====
            sb.AppendLine("── 派系 ──");
            if (factions != null)
            {
                var list = factions.GetFactions(realm.realmId);
                if (list != null && list.Count > 0)
                {
                    foreach (var f in list)
                    {
                        string leader = f.leaderCharacterId >= 0 ? $"（领袖#{f.leaderCharacterId}）" : "（无领袖）";
                        sb.AppendLine($"{FactionNames.Get(f.stance)}：力量 {f.power:F0} | 凝聚 {f.cohesion:F0} {leader}");
                    }
                }
                else sb.AppendLine("（无组织化派系）");
            }
            sb.AppendLine();

            // ===== 政体变迁区 =====
            sb.AppendLine("── 政体变迁 ──");
            if (regime != null)
            {
                var st = regime.GetState(realm.realmId);
                if (st != null)
                {
                    sb.AppendLine($"张力：阶级错配 {st.tension.classMismatch:F0} | 财政军事 {st.tension.fiscalMilitary:F0} | " +
                                  $"合法性侵蚀 {st.tension.legitimacyErosion:F0} | 综合 {st.tension.total:F0}");
                    sb.AppendLine($"制度黏性 {st.institutionalInertia:F0} | 现政体确立 {st.compositionEstablishedDay} 日");
                    if (st.IsWindowOpen && st.activeJuncture != null)
                    {
                        var j = st.activeJuncture;
                        sb.AppendLine($"▶ 关键节点：{JunctureNames.Get(j.type)} | 剩余 {j.remainingDays} 天 | " +
                                      $"烈度 {j.severity:F0} | 结果 {JunctureNames.GetOutcome(j.outcome)}");
                    }
                    else sb.AppendLine("（路径依赖期——无开放窗口）");
                }
                else sb.AppendLine("（无变迁状态）");
            }
            sb.AppendLine();

            // ===== 关键节点历史 =====
            if (regime != null)
            {
                var st = regime.GetState(realm.realmId);
                if (st != null && st.history.Count > 0)
                {
                    sb.AppendLine("── 变迁历史 ──");
                    int from = Math.Max(0, st.history.Count - 5);
                    for (int i = from; i < st.history.Count; i++)
                        sb.AppendLine(st.history[i]);
                }
            }
            return sb.ToString();
        }
    }

    /// <summary>阶层中文名</summary>


    /// <summary>派系中文名</summary>


    /// <summary>关键节点中文名</summary>

}
