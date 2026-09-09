using System;
using UnityEngine;
using System.Collections.Generic;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Politics
{
    [Serializable]
    public class Faction
    {
        public int factionId;
        public int realmId;
        public string factionName = "";
        public FactionStance stance;

 /// <summary>阶层基础：各社会阶层对本派系的支持权重（0~1，可跨阶层结盟）</summary>
        [System.NonSerialized]
        public Dictionary<GameEnums.SocialClass, float> classBacking = new Dictionary<GameEnums.SocialClass, float>();
 /// <summary>主要代表阶层（backing 最高者）</summary>
        public GameEnums.SocialClass primaryClass = GameEnums.SocialClass.Peasant;

 /// <summary>领袖角色ID（-1=暂无有名领袖的底层运动）</summary>
        public int leaderCharacterId = -1;
 /// <summary>派系成员（廷臣/贵族/官员等有名角色）</summary>
        public List<int> memberCharacterIds = new List<int>();

        public FactionPlatform platform;

        [UnityEngine.Range(0f, 100f)] public float power;       // 政治力量（由阶层能量+领袖+成员聚合）
        [UnityEngine.Range(0f, 100f)] public float cohesion = 60f; // 凝聚力（低则分裂）
        public bool isInGovernment;                 // 是否当前执政/参与执政（保守派通常是）

        public Faction() { }
        public Faction(int id, int realmId, FactionStance stance)
        {
            factionId = id; this.realmId = realmId; this.stance = stance;
        }

 /// <summary>派系是否拥有某阶层的显著支持（&gt;阈值）</summary>
        public bool BackedBy(GameEnums.SocialClass cls, float threshold = 0.2f)
            => classBacking.GetValueOrDefault(cls, 0f) >= threshold;
    }
}
