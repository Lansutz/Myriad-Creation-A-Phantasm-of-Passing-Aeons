using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Events
{
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
}
