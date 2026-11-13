using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Infrastructure.Save
{
    /// <summary>海军实体存档 DTO（SaveVersion 17 起）；舰船数量字典拍平成数组。</summary>
    [Serializable]
    public class NavySaveData
    {
        public int navyId;
        public string navyName = "";
        public int ownerRealmId = -1;
        public int commanderId = -1;
        public int homePortTile = -1;
        public int currentTileIndex = -1;

        public int[] shipEquipmentIds = new int[0];
        public float[] shipCounts = new float[0];

        public GameEnums.CombatState state;
        public float organization = 100f;
        public float morale = 100f;
        public float supply = 100f;
        public float training = 50f;

        public int[] movePath = new int[0];
        public int moveTargetTile = -1;
        public float moveProgress;

        public static NavySaveData FromNavy(Navy navy)
        {
            if (navy == null) return null;
            var dto = new NavySaveData
            {
                navyId = navy.navyId,
                navyName = navy.navyName ?? "",
                ownerRealmId = navy.ownerRealmId,
                commanderId = navy.commanderId,
                homePortTile = navy.homePortTile,
                currentTileIndex = navy.currentTileIndex,
                state = navy.state,
                organization = navy.organization,
                morale = navy.morale,
                supply = navy.supply,
                training = navy.training,
                movePath = navy.movePath?.ToArray() ?? new int[0],
                moveTargetTile = navy.moveTargetTile,
                moveProgress = navy.moveProgress,
            };
            if (navy.shipCounts != null && navy.shipCounts.Count > 0)
            {
                int n = navy.shipCounts.Count;
                dto.shipEquipmentIds = new int[n];
                dto.shipCounts = new float[n];
                int i = 0;
                foreach (var pair in navy.shipCounts)
                {
                    dto.shipEquipmentIds[i] = pair.Key;
                    dto.shipCounts[i] = pair.Value;
                    i++;
                }
            }
            return dto;
        }

        public Navy ToNavy()
        {
            var navy = new Navy
            {
                navyId = navyId,
                navyName = navyName ?? "",
                ownerRealmId = ownerRealmId,
                commanderId = commanderId,
                homePortTile = homePortTile,
                currentTileIndex = currentTileIndex,
                state = state,
                organization = organization,
                morale = morale,
                supply = supply,
                training = training,
                movePath = movePath != null ? new List<int>(movePath) : new List<int>(),
                moveTargetTile = moveTargetTile,
                moveProgress = moveProgress,
            };
            if (shipEquipmentIds != null && shipCounts != null && shipEquipmentIds.Length == shipCounts.Length)
            {
                for (int i = 0; i < shipEquipmentIds.Length; i++)
                    navy.shipCounts[shipEquipmentIds[i]] = shipCounts[i];
            }
            return navy;
        }
    }

    public static class NavyPersistence
    {
        public static List<NavySaveData> CaptureNavies(GameWorld world)
        {
            var result = new List<NavySaveData>();
            if (world?.navies == null) return result;
            foreach (var navy in world.navies.Values)
            {
                var saved = NavySaveData.FromNavy(navy);
                if (saved != null) result.Add(saved);
            }
            return result;
        }

        public static int RestoreNavies(GameWorld world, List<NavySaveData> saved)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.navies ??= new Dictionary<int, Navy>();
            world.navies.Clear();
            int nextNavyId = 1;
            if (saved == null) return nextNavyId;
            foreach (var entry in saved)
            {
                if (entry == null) continue;
                var navy = entry.ToNavy();
                if (navy.navyId <= 0)
                    throw new InvalidOperationException($"Navy '{entry.navyName}' has an invalid navyId {navy.navyId}.");
                if (world.navies.ContainsKey(navy.navyId))
                    throw new InvalidOperationException($"Duplicate navyId {navy.navyId} in save.");
                world.navies[navy.navyId] = navy;
                if (navy.navyId >= nextNavyId) nextNavyId = navy.navyId + 1;
            }
            return nextNavyId;
        }
    }
}
