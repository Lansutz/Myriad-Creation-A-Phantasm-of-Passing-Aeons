using System.Collections.Generic;
using System;

namespace CivilizationEvolution.AI
{
        [Serializable]
        public class AIState
        {
            public int realmId;
            public float aggression = 0.5f;       // 侵略性 0~1
            public float expansionism = 0.5f;     // 扩张性 0~1
            public float diplomacyFocus = 0.5f;   // 外交倾向 0~1
            public float economyFocus = 0.5f;      // 经济倾向 0~1
            public float militaryFocus = 0.5f;     // 军事倾向 0~1
            public List<int> rivals = new List<int>();      // 竞争对手
            public List<int> allies = new List<int>();      // 盟友
            public List<int> threatenedBy = new List<int>(); // 威胁来源
            public AIStance currentStance = AIStance.Peaceful;
            public int targetRealmId = -1;        // 当前目标政权
            public float warReadiness = 0f;        // 战争准备度 0~1
            public float stabilityConcern = 0f;    // 稳定度担忧
        }
}
