using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Infrastructure.Save
{
    /// <summary>
    /// 军队实体入档与还原（SaveVersion 13）。
    ///
    /// 为什么单独一层：`world.armies` 是 `Dictionary&lt;int, Army&gt;`，军队又是
    /// PoliticsSimulationSystem 的构造参数——还原必须**就地清空并填充**同一个字典实例，
    /// 换实例会让政治/战斗系统指向旧容器。
    ///
    /// 地图单位（MapUnitRegistry）里的军队视图条目不入档：视图由 `Army` 派生，
    /// 读档后按军队实体重新登记，避免两份数据打架。
    /// </summary>
    public static class ArmyPersistence
    {
        public static List<ArmySaveData> CaptureArmies(GameWorld world)
        {
            var result = new List<ArmySaveData>();
            if (world?.armies == null) return result;

            foreach (var army in world.armies.Values)
            {
                var saved = ArmySaveData.FromArmy(army);
                if (saved != null) result.Add(saved);
            }

            return result;
        }

        /// <summary>就地还原军队字典；返回下一个可用 armyId。</summary>
        public static int RestoreArmies(GameWorld world, List<ArmySaveData> saved)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.armies ??= new Dictionary<int, Army>();
            world.armies.Clear();

            int nextArmyId = 1;
            if (saved == null) return nextArmyId;

            foreach (var entry in saved)
            {
                if (entry == null) continue;
                var army = entry.ToArmy();
                if (army.armyId <= 0)
                    throw new InvalidOperationException($"Army '{entry.armyName}' has an invalid armyId {army.armyId}.");
                if (world.armies.ContainsKey(army.armyId))
                    throw new InvalidOperationException($"Duplicate armyId {army.armyId} in save.");

                world.armies[army.armyId] = army;
                if (army.armyId >= nextArmyId) nextArmyId = army.armyId + 1;
            }

            return nextArmyId;
        }

        /// <summary>读档后按军队实体重新登记地图单位视图（幂等：先按 source 移除旧视图）。</summary>
        public static void RegisterArmyViews(GameWorld world)
        {
            if (world?.armies == null) return;
            foreach (var army in world.armies.Values)
                if (army != null) MapUnitRegistry.RegisterArmy(army);
        }
    }
}
