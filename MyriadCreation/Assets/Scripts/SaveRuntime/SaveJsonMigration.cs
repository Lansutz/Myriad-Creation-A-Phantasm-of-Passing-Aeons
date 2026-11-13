using System;
using MyriadCreation.Save.Contracts;
using UnityEngine;

namespace MyriadCreation.Infrastructure.Save
{
    /// <summary>
    /// 旧存档 JSON 的字段名迁移入口。
    ///
    /// 背景：<c>[FormerlySerializedAs]</c> 只对 Unity 原生序列化（场景/资源/.meta）生效，
    /// <c>JsonUtility.FromJson</c> 不识别它——实测把旧键 <c>settlements</c> 喂给带
    /// <c>[FormerlySerializedAs("settlements")]</c> 的字段，结果字段仍是 null。
    /// 因此旧键必须在读档入口显式迁移，不能靠特性。
    ///
    /// 旧档形态（SaveVersion &lt; 11）：
    /// <code>
    /// { "settlements": [ { "anchorId": 42, "settlementName": "Old camp",
    ///                      "settlementCategory": 2, "x": 0.2, "y": 0.3 } ] }
    /// </code>
    /// 其中 <c>burgs</c> 是更早的一层旧键，<c>settlements</c> 是它之后的旧键；
    /// 新键为 <c>habitation_sites</c> + <c>site_id/site_mode/site_name</c>。
    /// </summary>
    public static class SaveJsonMigration
    {
        [Serializable]
        public sealed class LegacySettlementEntry
        {
            public int anchorId;
            public string settlementName;
            public int settlementCategory;
            public int settlementForm;
            public float x;
            public float y;
            public float population;
        }

        [Serializable]
        public sealed class LegacySettlementKeys
        {
            // 旧键：burgs（最早）→ settlements（次早）。
            public LegacySettlementEntry[] burgs;
            public LegacySettlementEntry[] settlements;
        }

        /// <summary>反序列化地图存档，并把旧键聚落数据迁移成规范字段。</summary>
        public static MapSaveData FromMapSaveJson(string json)
        {
            var data = JsonUtility.FromJson<MapSaveData>(json);
            if (data == null) return null;

            if (data.habitation_sites == null || data.habitation_sites.Length == 0)
            {
                var migrated = ReadLegacySettlements(json);
                if (migrated != null) data.habitation_sites = migrated;
            }

            return data;
        }

        private static HabitationSiteSaveData[] ReadLegacySettlements(string json)
        {
            var keys = JsonUtility.FromJson<LegacySettlementKeys>(json);
            var entries = (keys?.settlements != null && keys.settlements.Length > 0)
                ? keys.settlements
                : keys?.burgs;

            if (entries == null || entries.Length == 0) return null;

            var sites = new HabitationSiteSaveData[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                var legacy = entries[i];
                sites[i] = new HabitationSiteSaveData
                {
                    anchor_id = legacy.anchorId,
                    site_name = legacy.settlementName ?? string.Empty,
                    legacy_site_category = legacy.settlementCategory,
                    legacy_settlement_form = legacy.settlementForm,
                    x = legacy.x,
                    y = legacy.y,
                    population = legacy.population
                };
            }

            return sites;
        }
    }
}
