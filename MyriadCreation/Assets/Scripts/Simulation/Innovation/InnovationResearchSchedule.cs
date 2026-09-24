using System;
using System.Collections.Generic;
using CivilizationEvolution.Core.Simulation;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Simulation.AI;
using CivilizationEvolution.Simulation.WorldState;

namespace CivilizationEvolution.Simulation.Innovation
{
    /// <summary>
    /// 旧式 Realm × Innovation 研究进度的兼容运行时。
    /// AI 只提供研究速率查询与研究选择；革新领域在这里推进既有 Legacy Research 状态。
    /// 这是迁移边界，不代表最终 Innovation 2.0 数据模型。
    /// </summary>
    public static class InnovationResearchSchedule
    {
        public const string ScheduleId = "innovation.legacy_research.daily";

        public static void Register(
            ISimulationScheduler scheduler,
            InnovationTree innovations,
            AIManager ai,
            Func<Dictionary<int, RealmData>> realms,
            Func<TileData[]> tiles)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (innovations == null) throw new ArgumentNullException(nameof(innovations));
            if (ai == null) throw new ArgumentNullException(nameof(ai));
            if (realms == null) throw new ArgumentNullException(nameof(realms));
            if (tiles == null) throw new ArgumentNullException(nameof(tiles));

            scheduler.Register(ScheduleId, SimulationCadence.Daily, 29, _ =>
            {
                var realmTable = realms();
                var mapTiles = tiles();
                if (realmTable == null || mapTiles == null) return;

                foreach (var kv in realmTable)
                {
                    var realm = kv.Value;
                    if (realm == null) continue;
                    float researchRate = ai.GetResearchRate(realm.realmId, realm, mapTiles);
                    if (researchRate > 0f)
                        innovations.DailyTick(realm.realmId, researchRate);
                }
            });
        }
    }
}
