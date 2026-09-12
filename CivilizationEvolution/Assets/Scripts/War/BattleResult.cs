namespace CivilizationEvolution.War
{
    [System.Serializable]
    public struct BattleResult
    {
        public bool attackerWins;
        public float attackerLosses;
        public float defenderLosses;

 /// <summary>战斗是否因兵力不足（<30%）而结束</summary>
        public bool battleEndedByManpower;
 /// <summary>攻方是否逃跑</summary>
        public bool attackerRetreated;
 /// <summary>守方是否逃跑</summary>
        public bool defenderRetreated;

 /// <summary>攻方溃败踩踏事件（null=未触发）</summary>
        public CivilizationEvolution.Disaster.StampedeEvent attackerStampede;
 /// <summary>守方溃败踩踏事件（null=未触发）</summary>
        public CivilizationEvolution.Disaster.StampedeEvent defenderStampede;
    }
}
