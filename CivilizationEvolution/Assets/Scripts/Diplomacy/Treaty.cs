using CivilizationEvolution.Core;
using System.Collections.Generic;

namespace CivilizationEvolution.Diplomacy
{
    [System.Serializable]
    public class Treaty
    {
        public int treatyId;
        public string treatyName;
        public int signerAId;
        public int signerBId;
        public int signedDay;
        public int expiryDay = -1;

        public List<TreatyClause> clauses = new List<TreatyClause>();
        public bool isActive = true;

        // ===== 和平条约扩展字段（战争三层分离体系）=====
        public float warScoreAtSigning;  // 签订时的战争分数
        public List<GameEnums.WarGoalType> originalWarGoals = new List<GameEnums.WarGoalType>(); // 原战争目标
        public bool goalsFullyAchieved;   // 战争目标是否完全达成
        public int truceUntilDay;          // 停战到期日
        public bool isPeaceTreaty;         // 是否为和平条约（区别于普通外交条约）

        /// <summary>检查条约是否到期</summary>
        public bool IsExpired(int currentDay)
        {
            return expiryDay > 0 && currentDay > expiryDay;
        }
    }
}
