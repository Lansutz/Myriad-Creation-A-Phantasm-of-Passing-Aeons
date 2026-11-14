using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Infrastructure.Save
{
    [Serializable]
    public class ChronicleSaveData
    {
        public int currentTick;
        public int currentYear = 1;
        public List<ChronicleEntrySaveData> entries = new List<ChronicleEntrySaveData>();
    }

    /// <summary>编年史运行态入档；旧档无 chronicle 段时清空静态/上一局残留。</summary>
    public static class ChroniclePersistence
    {
        public static ChronicleSaveData Capture(GameWorld world)
        {
            Chronicle chronicle = world?.GetChronicle();
            if (chronicle == null) return null;

            var data = new ChronicleSaveData
            {
                currentTick = chronicle.CurrentTick,
                currentYear = chronicle.CurrentYear
            };
            List<ChronicleEntry> entries = chronicle.GetEntries();
            if (entries == null) return data;
            foreach (ChronicleEntry entry in entries)
            {
                if (entry == null) continue;
                data.entries.Add(new ChronicleEntrySaveData
                {
                    entryId = entry.entryId,
                    tick = entry.tick,
                    year = entry.year,
                    eventType = entry.eventType,
                    description = entry.description,
                    major = entry.major,
                    participants = entry.participants != null ? entry.participants.ToArray() : new int[0]
                });
            }
            return data;
        }

        public static void Restore(GameWorld world, ChronicleSaveData saved)
        {
            Chronicle chronicle = world?.GetChronicle();
            if (chronicle == null) return;

            var entries = new List<ChronicleEntry>();
            if (saved?.entries != null)
            {
                foreach (ChronicleEntrySaveData dto in saved.entries)
                {
                    if (dto == null) continue;
                    var entry = new ChronicleEntry
                    {
                        entryId = dto.entryId,
                        tick = dto.tick,
                        year = dto.year,
                        eventType = dto.eventType,
                        description = dto.description,
                        major = dto.major
                    };
                    if (dto.participants != null) entry.participants.AddRange(dto.participants);
                    entries.Add(entry);
                }
            }
            chronicle.RestoreEntries(entries, saved?.currentTick ?? 0, saved?.currentYear ?? 1);
        }
    }
}
