namespace MyriadCreation.Content.Definitions
{
    /// <summary>配方输出类型；当前运行时只消耗 Goods，其余类型先由数据/系统接。</summary>
    public enum RecipeOutputKind
    {
        Good = 0,
        Equipment = 1,
        Unit = 2,
        Building = 3,
    }
}
