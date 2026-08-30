using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Core
{
    /// <summary>
    /// 编年史（借鉴《地图上发生的事》Chronicle/Chronicle.Entry：
    /// event_id/tick/participants/major——世界大事的时序记录）
    /// 供 UI 历史视图/存档回溯使用
    /// </summary>
    [Serializable]
    public class ChronicleEntry
    {
        public int entryId;
        public int tick;          // 游戏日
        public int year;          // 游戏年
        public string eventType;  // 事件类型键（war/peace/alliance/innovation/...）
        public string description;
        public bool major;        // 重大事件（篡位/废立/称王/大战）
        public List<int> participants = new List<int>(); // 参与政权/角色
    }

    /// <summary>编年史（世界大事日志，上限保留）</summary>
    [Serializable]
    public class Chronicle
    {
        private readonly List<ChronicleEntry> _entries = new List<ChronicleEntry>();
        private int _nextEntryId = 1;
        private const int MaxEntries = 500;

        public int CurrentTick { get; set; } = 0;
        public int CurrentYear { get; set; } = 1;

        /// <summary>记录一条编年史</summary>
        public ChronicleEntry Add(string eventType, string description, bool major = false, params int[] participants)
        {
            var entry = new ChronicleEntry
            {
                entryId = _nextEntryId++,
                tick = CurrentTick,
                year = CurrentYear,
                eventType = eventType,
                description = description,
                major = major
            };
            if (participants != null)
                entry.participants.AddRange(participants);

            _entries.Add(entry);
            if (_entries.Count > MaxEntries)
                _entries.RemoveAt(0); // 滚动保留

            return entry;
        }

        /// <summary>全部条目（新→旧）</summary>
        public List<ChronicleEntry> GetEntries() => _entries;

        /// <summary>重大事件（新→旧）</summary>
        public List<ChronicleEntry> GetMajorEntries()
        {
            var result = new List<ChronicleEntry>();
            for (int i = _entries.Count - 1; i >= 0; i--)
                if (_entries[i].major) result.Add(_entries[i]);
            return result;
        }

        /// <summary>按类型过滤</summary>
        public List<ChronicleEntry> GetEntriesByType(string eventType)
        {
            var result = new List<ChronicleEntry>();
            for (int i = _entries.Count - 1; i >= 0; i--)
                if (_entries[i].eventType == eventType) result.Add(_entries[i]);
            return result;
        }

        public int Count => _entries.Count;
    }
}
