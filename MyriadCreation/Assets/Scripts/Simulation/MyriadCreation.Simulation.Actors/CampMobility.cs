namespace MyriadCreation.Simulation.Actors
{
    /// <summary>
    /// 营地移动状态：扎营（固定锚点）/ 收起（随主人移动，无地面占用）/ 迁移中（锚点随队移动）。
    /// 归属以 ownerActorId 为单一真源；ownerRealmId 是管辖快照。
    /// </summary>
    public enum CampMobility
    {
        Pitched = 0,
        Packed = 1,
        Moving = 2
    }
}
