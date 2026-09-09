namespace CivilizationEvolution.Diplomacy
{
    [System.Serializable]
    public class Alliance
    {
        public AllianceType type;
        public int realmAId;
        public int realmBId;
        public int signedDay;
        public int durationDays; // -1表示永久
        public bool isActive = true;

 // 盟约条款        public float tradeEfficiencyBonus = 0f;
        public float tariffReduction = 0f;
        public bool mutualDefense = false;
        public bool jointOffensive = false;
        public bool militaryAccess = false;
        public float relationRequirement = 0f;

 /// <summary>检查盟约是否到期</summary>        public bool IsExpired(int currentDay)
        {
            return durationDays > 0 && currentDay - signedDay > durationDays;
        }

 /// <summary>检查盟约条件是否满足</summary>        public bool CheckConditions(DiplomaticRelation relation)
        {
            return relation.relation >= relationRequirement && !relation.isAtWar;
        }
    }
}
