namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// 城墙材质：与城防等级（FortificationLevel）分开，避免把"多高"和"什么材料"混成一个枚举。
    /// </summary>
    public enum WallMaterial
    {
        Palisade = 0,
        Earth = 1,
        Stone = 2,
        Brick = 3,
        Composite = 4,
    }
}
