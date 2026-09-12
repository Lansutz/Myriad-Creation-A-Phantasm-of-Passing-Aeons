namespace CivilizationEvolution.Simulation.Diplomacy
{
    [System.Serializable]
    public struct TreatyClause
    {
        public TreatyClauseType type;
        public string description;
        public float value;
        public int targetRealmId;
        public int fromRealmId;         // 条款执行方（付出代价的一方）
        public int toRealmId;           // 条款受益方
        public int tileIndex;            // 相关地块（-1=无）
        public int regionId;             // 相关地区（-1=无）
        public int durationDays;         // 条款持续时间（-1=永久）
    }
}
