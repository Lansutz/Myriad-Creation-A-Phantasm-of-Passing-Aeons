using System.Collections.Generic;



using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Modding;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Society;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Hydrology;
using MyriadCreation.World.Settlement;
using MyriadCreation.World.Terrain;
using MyriadCreation.World.Anchor;


namespace MyriadCreation.Simulation.Systems
{
    /// <summary>
    /// 据点演化系统：统一管理 营寨(Camp) → 坞堡(FortifiedCamp) → 聚落(Settlement) 的生命周期转化。
    ///
    /// 升级路径（拟真）：
    /// - 行军营地 → 驻屯营（驻扎时间长）
    /// - 驻屯营/蛮族营地 → 坞堡（修筑防御工事，defense达到阈值）
    /// - 营寨/坞堡 → 村镇（人口积累+永久化进度满+定居化）
    /// - 坞堡 → 堡垒据点（军事化升级为Outpost类别）
    /// - 游牧营地不永久化（逐水草而居，只能通过"定居化改革"转化）
    ///
    /// 反向路径：
    /// - 聚落被摧毁 → 废墟（SettlementDestructionSystem处理）
    /// - 城国被游牧征服 → 聚落降级（定居点→贡赋据点）
    /// </summary>
    public sealed class SettlementEvolutionRuntime
    {
        private readonly GameWorld _world;

        public SettlementEvolutionRuntime(GameWorld world)
        {
            _world = world ?? throw new System.ArgumentNullException(nameof(world));
        }

        public void DailyTick()
        {
            SettlementEvolutionSystem.DailyTick(_world);
        }

        public SettlementData TryEvolveCampToSettlement(CampData camp)
        {
            return SettlementEvolutionSystem.TryEvolveCampToSettlement(_world, camp);
        }

        public SettlementData SettleNomadCamp(int campId)
        {
            return SettlementEvolutionSystem.SettleNomadCamp(_world, campId);
        }

        public CampData DegradeSettlementToCamp(int anchorId, CampType campType = CampType.Military, int ownerRealmId = -1)
        {
            return SettlementEvolutionSystem.DegradeSettlementToCamp(_world, anchorId, campType, ownerRealmId);
        }
    }

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
                    TryEvolveCampToSettlement(world, camp);
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
                if (!HabitationSiteTransitionSystem.TryTransition(
                    (HabitationSite)camp, SiteMode.FortifiedOutpost, out var transitionFailure))
                {
                    Debug.LogError($"[SettlementEvolution] 营地#{camp.campId}无法升级为据点：{transitionFailure}");
                    return;
                }

                camp.fortifiedOutpostData.purpose = camp.type.ToString();
                camp.attributes["isFortified"] = 1f;
                Debug.Log($"[SettlementEvolution] 营地#{camp.campId}升级为据点（防御={camp.defense:F0}）");
            }
        }

        /// <summary>
        /// 营寨/坞堡转化为永久聚落（Settlement）。
        /// </summary>
        public static SettlementData TryEvolveCampToSettlement(GameWorld world, CampData camp)
        {
            if (world == null || camp == null) return null;
            if (camp.type == CampType.Nomad) return null; // 游牧营地不能直接转化

            bool isFortified = camp.attributes != null &&
                camp.attributes.TryGetValue("isFortified", out float fortified) && fortified > 0f;

            // 判定转化形态
            SettlementForm targetForm;
            SettlementLevel targetLevel;
            SiteMode targetMode;

            if (isFortified && camp.defense >= FORT_SETTLEMENT_DEFENSE_THRESHOLD &&
                camp.population >= FORTIFIED_CAMP_TO_FORT_POP)
            {
                // 坞堡 → 堡垒
                targetMode = SiteMode.FortifiedOutpost;
                targetLevel = SettlementLevel.LevelII;
                targetForm = SettlementForm.Village;
            }
            else if (camp.population >= CAMP_TO_VILLAGE_POP)
            {
                // 营寨 → 村镇
                targetMode = SiteMode.SedentarySettlement;
                targetLevel = SettlementLevel.LevelI;
                targetForm = SettlementForm.Village;
            }
            else
            {
                return null; // 条件不足
            }

            if (camp.siteMode == SiteMode.TemporaryEncampment &&
                !HabitationSiteTransitionSystem.TryTransition(
                    (HabitationSite)camp, SiteMode.FortifiedOutpost, out var outpostFailure))
            {
                Debug.LogError($"[SettlementEvolution] 营地#{camp.campId}无法先转为据点：{outpostFailure}");
                return null;
            }
            if (camp.siteMode != SiteMode.FortifiedOutpost)
            {
                Debug.LogError($"[SettlementEvolution] 营地#{camp.campId}不处于可演化模式：{SiteModeKeys.ToKey(camp.siteMode)}");
                return null;
            }
            camp.fortifiedOutpostData.purpose = camp.type.ToString();

            world.anchors ??= new Dictionary<int, AnchorData>();
            int newAnchorId = camp.anchorId >= 0 ? camp.anchorId : NextAnchorId(world);
            var anchor = camp.anchor ?? new AnchorData
            {
                anchorId = newAnchorId,
                provinceId = world.tiles[camp.tileIndex].provinceId,
                tileIndex = camp.tileIndex,
                x = camp.location.x,
                y = camp.location.y
            };
            anchor.anchorId = newAnchorId;
            anchor.provinceId = world.tiles[camp.tileIndex].provinceId;
            anchor.tileIndex = camp.tileIndex;

            var settlement = new SettlementData
            {
                siteId = camp.siteId,
                previousSiteMode = camp.previousSiteMode,
                hasPreviousSiteMode = camp.hasPreviousSiteMode,
                anchorId = newAnchorId,
                anchor = anchor,
                settlementName = camp.campName,
                population = camp.population,
                development = camp.permanence * 0.1f,
                wealth = camp.supplies * 0.5f,
                fortification = camp.defense * 0.1f,
                garrison = Mathf.RoundToInt(camp.population / 5f),
                siteMode = SiteMode.FortifiedOutpost,
                constructionProgress = 0f,
                constructionTier = 1,
                settlementEvolution = 100f,
                settlementStability = 50f,
                foundingTick = world.currentDay,
                wallLevel = isFortified ? WallLevel.Palisade : WallLevel.None
            };
            settlement.fortifiedOutpostData.subtype = camp.fortifiedOutpostData.subtype;
            settlement.fortifiedOutpostData.purpose = camp.fortifiedOutpostData.purpose;
            settlement.SetLegacyCategoryFromSiteMode();

            if (targetMode == SiteMode.SedentarySettlement
                && !HabitationSiteTransitionSystem.TryTransition(
                    settlement, SiteMode.SedentarySettlement, out var transitionFailure))
            {
                Debug.LogError($"[SettlementEvolution] 无法将栖居点#{settlement.siteId}从据点转为定居点：{transitionFailure}");
                return null;
            }

            settlement.settlementForm = targetForm;
            settlement.settlementLevel = targetLevel;
            world.anchors[newAnchorId] = anchor;
            world.settlements[newAnchorId] = settlement;

            // 清除营寨
            world.Camps.RemoveTransferredCamp(camp.campId, settlement.siteId);
            var tile = world.tiles[camp.tileIndex];
            tile.campId = -1;
            world.tiles[camp.tileIndex] = tile;

            Debug.Log($"[SettlementEvolution] 营寨#{camp.campId}({camp.type}) 演化为聚落#{newAnchorId}({targetForm})");
            return settlement;
        }

        /// <summary>
        /// 游牧营地通过"定居化改革"转化（需要革新条件，由文化/革新系统调用）。
        /// </summary>
        public static SettlementData SettleNomadCamp(GameWorld world, int campId)
        {
            var camp = world?.Camps?.GetCamp(campId);
            if (camp == null || camp.type != CampType.Nomad) return null;
            if (camp.population < CAMP_TO_VILLAGE_POP) return null;

            // 游牧定居化：直接走营寨→村镇路径
            camp.type = CampType.Military; // 转为军事驻屯类型以通过Nomad检查
            camp.permanence = 100f;
            return TryEvolveCampToSettlement(world, camp);
        }

        /// <summary>
        /// 聚落反向降级（城国被游牧征服/严重衰退）：Settlement → 营寨。
        /// </summary>
        public static CampData DegradeSettlementToCamp(GameWorld world, int anchorId,
            CampType campType = CampType.Military, int ownerRealmId = -1)
        {
            if (world == null || !world.settlements.TryGetValue(anchorId, out var settlement)) return null;
            if (settlement.IsRuined) return null;

            var camp = world.Camps.EstablishCamp(
                campType, settlement.tileIndex, Mathf.RoundToInt(settlement.population),
                ownerRealmId, -1, settlement.settlementName);
            if (camp != null)
            {
                if (!HabitationSiteTransitionSystem.TryTransition(
                    settlement, SiteMode.TemporaryEncampment, out var transitionFailure))
                {
                    world.Camps.AbandonCamp(camp.campId);
                    Debug.LogError($"[SettlementEvolution] 无法将栖居点#{settlement.siteId}降为营地：{transitionFailure}");
                    return null;
                }

                camp.siteId = settlement.siteId;
                camp.anchorId = settlement.anchorId;
                camp.anchor = settlement.anchor;
                camp.location.x = settlement.x;
                camp.location.y = settlement.y;
                camp.defense = settlement.fortification * 10f;
                camp.supplies = settlement.wealth;
                world.settlements.Remove(anchorId);
                Debug.Log($"[SettlementEvolution] 聚落#{anchorId}降级为营寨#{camp.campId}");
            }
            return camp;
        }

        private static int NextAnchorId(GameWorld world)
        {
            int max = 0;
            foreach (var id in world.anchors.Keys)
                if (id > max) max = id;
            return max + 1;
        }
    }
}
