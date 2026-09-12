using System;
using System.Collections.Generic;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Politics
{
    [Serializable]
    public class RealmSociety
    {
        public int realmId;
        [System.NonSerialized]
        public Dictionary<GameEnums.SocialClass, ClassProfile> classes
            = new Dictionary<GameEnums.SocialClass, ClassProfile>();
        public float totalPopulation;
        [UnityEngine.Range(0f, 100f)] public float unrestScore;    // 整体社会动荡（影响力加权怨气）
        public GameEnums.SocialClass dominantClass = GameEnums.SocialClass.Peasant;     // 人口最多
        public GameEnums.SocialClass mostRestlessClass = GameEnums.SocialClass.Peasant; // 动荡能量最高

        public ClassProfile Get(GameEnums.SocialClass c) =>
            classes.TryGetValue(c, out var p) ? p : null;
    }
}
