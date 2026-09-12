using System;
using System.Collections.Generic;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.WorldState;



namespace CivilizationEvolution.Simulation.Society
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
