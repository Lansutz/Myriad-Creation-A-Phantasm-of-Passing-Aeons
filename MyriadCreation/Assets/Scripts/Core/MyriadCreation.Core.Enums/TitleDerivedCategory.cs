namespace MyriadCreation.Core.Enums
{
    /// <summary>
    /// 头衔派生分类（只查询、不落库）：由绑定 + 权利 + 模块推导，用于 UI/筛选，不参与门禁判定。
    /// 与内容层的 TitleKinds 字符串常量（monarch/bureaucratic/noble/honorific）不同：那是内容分类，本枚举是派生视图。
    /// B2 需要提供 "标题 kind 字符串 → 本枚举" 的显式映射，旧字段标 legacy。
    /// </summary>
    public enum TitleDerivedCategory
    {
        Landed = 0,
        Office = 1,
        Honor = 2,
        NobleRank = 3,
        Claim = 4
    }
}
