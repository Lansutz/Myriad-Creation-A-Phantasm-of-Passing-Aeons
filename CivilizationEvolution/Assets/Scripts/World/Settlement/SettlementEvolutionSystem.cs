using System.Collections.Generic;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;
using CivilizationEvolution.World;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 据点演化系统：统一管理 营寨(Camp) → 坞堡(FortifiedCamp) → 聚落(Burg) 的生命周期转化。
    ///
    /// 升级路径（拟真）：
    /// - 行军营地 → 驻屯营（驻扎时间长）
    /// - 驻屯营/蛮族营地 → 坞堡（修筑防御工事，defense达到阈值）
    /// - 营寨/坞堡 → 村镇（人口积累+永久化进度满+定居化）
    /// - 坞堡 → 堡垒（军事化升级，成为Burg的Fort形态）
    /// - 游牧营地不永久化（逐水草而居，只能通过"定居化改革"转化）
    ///
    /// 反向路径：
    /// - 聚落被摧毁 → 废墟（SettlementDestructionSystem处理）
    /// - 城国被游牧征服 → 聚落降级（定居点→贡赋据点）
    /// </summary>
    public static class SettlementEvolutionSystem
    {
        // ===== 升级阈值 =====
        /// <summary>营寨升级为坞堡的防御值阈值</summary>
        public const float FORTIFIED_CAMP_DEFENSE_THRESHOLD = 40f;
        /// <summary>坞堡升级为堡垒聚落的防御值阈值</summary>
        public const float FORT_SETTLEMENT_DEFENSE_THRESHOLD = 70f;
        /// <summary>营寨转化为村镇的最低人口</summary>
        public const int CAMP_TO_VILLAGE_POP = 50;
        /// <summary>坞堡转化为堡垒聚落的最低人口</summary>
        public const int FORTIFIED_CAMP_TO_FORT_POP = 100;

        /// <summary>
        /// 由传统 BurgType 推断聚落形态（村镇/城/堡）。
        /// Village/Town→Village；City/Port/Capital→City；Fortress→Fort。
        /// </summary>
        public static SettlementType InferFromBurgType(BurgType burgType)
        {
            switch (burgType)
            {
                case BurgType.Fortress:
                    return SettlementType.Fort;
                case BurgType.City:
                case BurgType.Port:
                case BurgType.Capital:
                    return SettlementType.City;
                default:
                    return SettlementType.Village;
            }
        }


        /// <summary>
        /// 每日检查所有营寨的演化。
        /// </summary>
        public static void DailyTick(GameWorld world)
        {
            if (world?.Camps == null) return;
            var camps = world.Camps.AllCamps;
            for (int i = camps.Count - 1; i >= 0; i--)
            {
                var camp = camps[i];
                if (camp.isAbandoned) continue;
                if (camp.type == CampType.Nomad) continue; // 游牧营地不自动演化

                // 永久化进度在CampData.Tick里已增长，这里检查转化
                if (camp.CanEvolveToSettlement)
                {
                    TryEvolveCampToBurg(world, camp);
                }
            }
        }

        /// <summary>
        /// 营寨修筑防御工事（向坞堡演化）。
        /// 由军队/政权主动调用，消耗物资（简化：直接增加defense和permanence）。
        /// </summary>
        public static void FortifyCamp(GameWorld world, int campId, float materialInput)
        {
            var camp = world?.Camps?.GetCamp(campId);
            if (camp == null || camp.isAbandoned) return;

            // 投入物资转化为防御和永久化
            camp.defense = Mathf.Min(100f, camp.defense + materialInput * 0.5f);
            camp.permanence = Mathf.Min(100f, camp.permanence + materialInput * 0.2f);

            if (camp.defense >= FORTIFIED_CAMP_DEFENSE_THRESHOLD &&
                camp.attributes != null && !camp.attributes.ContainsKey("isFortified"))
            {
                camp.attributes["isFortified"] = 1f;
                Debug.Log($"[SettlementEvolution] 营寨#{camp.campId}升级为坞堡（防御={camp.defense:F0}）");
            }
        }

        /// <summary>
        /// 营寨/坞堡转化为永久聚落（Burg）。
        /// </summary>
        public static BurgData TryEvolveCampToBurg(GameWorld world, CampData camp)
        {
            if (world == null || camp == null) return null;
            if (camp.type == CampType.Nomad) return null; // 游牧营地不能直接转化

            bool isFortified = camp.attributes != null &&
                camp.attributes.TryGetValue("isFortified", out float fortified) && fortified > 0f;

            // 判定转化形态
            SettlementType targetType;
            SettlementLevel targetLevel;
            BurgType burgType;

            if (isFortified && camp.defense >= FORT_SETTLEMENT_DEFENSE_THRESHOLD &&
                camp.population >= FORTIFIED_CAMP_TO_FORT_POP)
            {
                // 坞堡 → 堡垒
                targetType = SettlementType.Fort;
                targetLevel = SettlementLevel.LevelII;
                burgType = BurgType.Fortress;
            }
            else if (camp.population >= CAMP_TO_VILLAGE_POP)
            {
                // 营寨 → 村镇
                targetType = SettlementType.Village;
                targetLevel = SettlementLevel.LevelI;
                burgType = BurgType.Village;
            }
            else
            {
                return null; // 条件不足
            }

            // 创建BurgData
            int newBurgId = world.burgs.Count > 0 ? NextBurgId(world) : 1;
            var burg = new BurgData
            {
                burgId = newBurgId,
                burgName = camp.campName,
                type = burgType,
                tileIndex = camp.tileIndex,
                provinceId = world.tiles[camp.tileIndex].provinceId,
                population = camp.population,
                development = camp.permanence * 0.1f,
                wealth = camp.supplies * 0.5f,
                fortification = camp.defense * 0.1f,
                garrison = camp.population / 5,
                buildLevel = (int)targetLevel,
                settlementType = targetType,
                settlementLevel = targetLevel,
                settlementEvolution = 100f,
                settlementStability = 50f,
                foundingTick = world.currentDay,
                wallLevel = isFortified ? WallLevel.Palisade : WallLevel.None
            };

            world.burgs[newBurgId] = burg;

            // 清除营寨
            world.Camps.AbandonCamp(camp.campId);
            var tile = world.tiles[camp.tileIndex];
            tile.campId = -1;
            world.tiles[camp.tileIndex] = tile;

            Debug.Log($"[SettlementEvolution] 营寨#{camp.campId}({camp.type}) 演化为聚落#{newBurgId}({targetType})");
            return burg;
        }

        /// <summary>
        /// 游牧营地通过"定居化改革"转化（需要革新条件，由文化/革新系统调用）。
        /// </summary>
        public static BurgData SettleNomadCamp(GameWorld world, int campId)
        {
            var camp = world?.Camps?.GetCamp(campId);
            if (camp == null || camp.type != CampType.Nomad) return null;
            if (camp.population < CAMP_TO_VILLAGE_POP) return null;

            // 游牧定居化：直接走营寨→村镇路径
            camp.type = CampType.Military; // 转为军事驻屯类型以通过Nomad检查
            camp.permanence = 100f;
            return TryEvolveCampToBurg(world, camp);
        }

        /// <summary>
        /// 聚落反向降级（城国被游牧征服/严重衰退）：Burg → 营寨。
        /// </summary>
        public static CampData DegradeBurgToCamp(GameWorld world, int burgId,
            CampType campType = CampType.Military, int ownerRealmId = -1)
        {
            if (world == null || !world.burgs.TryGetValue(burgId, out var burg)) return null;
            if (burg.IsRuined) return null;

            var camp = world.Camps.EstablishCamp(
                campType, burg.tileIndex, Mathf.RoundToInt(burg.population),
                ownerRealmId, -1, burg.burgName + "（降级）");
            if (camp != null)
            {
                camp.defense = burg.fortification * 10f;
                camp.supplies = burg.wealth;
                // 移除聚落（但不删除数据，标记为降级）
                burg.ruinLevel = 2;
                burg.ruinedDay = world.currentDay;
                world.burgs[burgId] = burg;
                Debug.Log($"[SettlementEvolution] 聚落#{burgId}降级为营寨#{camp.campId}");
            }
            return camp;
        }

        private static int NextBurgId(GameWorld world)
        {
            int max = 0;
            foreach (var id in world.burgs.Keys)
                if (id > max) max = id;
            return max + 1;
        }
    }
}
