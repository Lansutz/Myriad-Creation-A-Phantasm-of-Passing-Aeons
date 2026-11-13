namespace MyriadCreation.Content.Definitions
{
    /// <summary>
    /// 成形/加工工艺大类。注意：工艺不是材料名；同一工艺可跨材料，同一材料也可有多种工艺。
    /// </summary>
    public enum CraftProcess
    {
        None = 0,
        Smelting = 1,       // 冶炼
        Casting = 2,        // 铸造/浇铸
        Forging = 3,        // 锻造/锻打
        HeatTreatment = 4,  // 热处理
        Weaving = 5,        // 纺织
        Papermaking = 6,    // 造纸/制浆
        Leatherworking = 7, // 鞣制
        Woodworking = 8,    // 木作
        Masonry = 9,        // 石作
    }
}
