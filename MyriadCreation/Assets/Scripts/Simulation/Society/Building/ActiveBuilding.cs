namespace MyriadCreation.Simulation.Society
{
    [System.Serializable]
    public class ActiveBuilding
    {
        public int buildingId;
        public int tileIndex;
        public int realmId;
        public int constructionDays;
        public int remainingDays;
        public bool isComplete;

        // 统一计划系统接管的建造元数据；-1 表示传统直接建造。
        public int constructionPlanId = -1;
        public int builderCharacterId = -1;
        public int innovationId = -1;
        public bool ManagedByPlan => constructionPlanId >= 0;
    }
}
