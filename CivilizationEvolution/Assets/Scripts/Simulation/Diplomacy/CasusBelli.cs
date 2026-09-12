using System;
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
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;



namespace CivilizationEvolution.Simulation.Diplomacy
{
    [Serializable]
    public class CasusBelli
    {
        public int cbId;
        public GameEnums.CasusBelliType type;
        public int holderRealmId;      // 借口持有方（可以用这个借口宣战的一方）
        public int targetRealmId;      // 借口针对方
        public int generatedDay;       // 生成日期
        public int expiryDay;           // 过期日期（-1=永不过期）
        public string description;       // 借口描述
        public float justificationStrength; // 正当性强度（0-100，影响宣战惩罚和战争目标选择）
        public int relatedTileIndex;    // 相关地块（领土争端/劫掠地点等，-1=无）
        public bool isUsed;             // 是否已被使用（宣战后标记为已用）

 /// <summary>是否有效（未过期、未使用）</summary>
        public bool IsValid(int currentDay)
        {
            return !isUsed && (expiryDay < 0 || currentDay < expiryDay);
        }
    }
}
