using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Infrastructure.Save
{
    [Serializable]
    public class ExpeditionSaveData
    {
        public int expeditionId;
        public int ownerRealmId;
        public int leaderCharacterId = -1;
        public int homeTileIndex = -1;
        public int currentTileIndex = -1;
        public List<int> route = new List<int>();
        public int routeCursor;
        public float supplies = 100f;
        public int state;
        public int startDay;
        public string failReason = "";

        public static ExpeditionSaveData FromExpedition(Expedition expedition)
        {
            if (expedition == null) return null;
            return new ExpeditionSaveData
            {
                expeditionId = expedition.expeditionId,
                ownerRealmId = expedition.ownerRealmId,
                leaderCharacterId = expedition.leaderCharacterId,
                homeTileIndex = expedition.homeTileIndex,
                currentTileIndex = expedition.currentTileIndex,
                route = new List<int>(expedition.route ?? new List<int>()),
                routeCursor = expedition.routeCursor,
                supplies = expedition.supplies,
                state = (int)expedition.state,
                startDay = expedition.startDay,
                failReason = expedition.failReason ?? ""
            };
        }

        public Expedition ToExpedition()
        {
            return new Expedition
            {
                expeditionId = expeditionId,
                ownerRealmId = ownerRealmId,
                leaderCharacterId = leaderCharacterId,
                homeTileIndex = homeTileIndex,
                currentTileIndex = currentTileIndex,
                route = route != null ? new List<int>(route) : new List<int>(),
                routeCursor = routeCursor,
                supplies = supplies,
                state = (ExpeditionState)state,
                startDay = startDay,
                failReason = failReason ?? ""
            };
        }
    }

    public static class ExpeditionPersistence
    {
        public static List<ExpeditionSaveData> Capture(GameWorld world)
        {
            var result = new List<ExpeditionSaveData>();
            if (world?.expeditions == null) return result;
            foreach (Expedition expedition in world.expeditions.Values)
            {
                ExpeditionSaveData dto = ExpeditionSaveData.FromExpedition(expedition);
                if (dto != null) result.Add(dto);
            }
            return result;
        }

        public static int Restore(GameWorld world, List<ExpeditionSaveData> saved)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.expeditions ??= new Dictionary<int, Expedition>();
            world.expeditions.Clear();
            int nextId = 1;
            if (saved == null) return nextId;
            foreach (ExpeditionSaveData dto in saved)
            {
                if (dto == null) continue;
                Expedition expedition = dto.ToExpedition();
                world.expeditions[expedition.expeditionId] = expedition;
                if (expedition.expeditionId >= nextId) nextId = expedition.expeditionId + 1;
            }
            return nextId;
        }
    }
}
