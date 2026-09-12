using System.Collections.Generic;
using System;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Diplomacy
{
    [Serializable]
    public class WarGoal
    {
        public int goalId;
        public GameEnums.WarGoalType type;
        public int attackerRealmId;
        public int defenderRealmId;
        public int targetTileIndex;     // 目标地块（夺取领土/边境调整等，-1=无）
        public int targetRegionId;      // 目标地区（-1=无）
        public float targetWarScore;    // 达成目标所需战争分数（0-100）
        public string description;
        public bool isPrimaryGoal;      // 是否为主要战争目标（一场战争可有多个目标）

 /// <summary>获取该战争目标支持的条约条款类型</summary>
        public List<TreatyClauseType> GetSupportedClauses()
        {
            return type switch
            {
                GameEnums.WarGoalType.ConquerTerritory => new List<TreatyClauseType>
                    { TreatyClauseType.TerritoryCession, TreatyClauseType.WarReparations, TreatyClauseType.Truce },
                GameEnums.WarGoalType.ConquerRegion => new List<TreatyClauseType>
                    { TreatyClauseType.TerritoryCession, TreatyClauseType.WarReparations, TreatyClauseType.Humiliation, TreatyClauseType.Truce },
                GameEnums.WarGoalType.Vassalization => new List<TreatyClauseType>
                    { TreatyClauseType.Vassalage, TreatyClauseType.WarReparations, TreatyClauseType.Truce },
                GameEnums.WarGoalType.Indemnity => new List<TreatyClauseType>
                    { TreatyClauseType.WarReparations, TreatyClauseType.TradePrivileges, TreatyClauseType.Truce },
                GameEnums.WarGoalType.Humiliation => new List<TreatyClauseType>
                    { TreatyClauseType.Humiliation, TreatyClauseType.WarReparations, TreatyClauseType.Truce },
                GameEnums.WarGoalType.Annihilation => new List<TreatyClauseType>
                    { TreatyClauseType.Annexation, TreatyClauseType.TerritoryCession, TreatyClauseType.WarCrimesTrial },
                GameEnums.WarGoalType.BorderAdjustment => new List<TreatyClauseType>
                    { TreatyClauseType.TerritoryCession, TreatyClauseType.BorderDemilitarization, TreatyClauseType.Truce },
                GameEnums.WarGoalType.ConvertReligion => new List<TreatyClauseType>
                    { TreatyClauseType.ReligiousFreedom, TreatyClauseType.WarReparations, TreatyClauseType.Truce },
                _ => new List<TreatyClauseType> { TreatyClauseType.WarReparations, TreatyClauseType.Truce }
            };
        }
    }
}
