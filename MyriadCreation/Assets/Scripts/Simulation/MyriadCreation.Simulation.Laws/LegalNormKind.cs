namespace MyriadCreation.Simulation.Laws
{
    /// <summary>法律/法令/特许的规范类型；冲突时按类型与来源排序。</summary>
    public enum LegalNormKind
    {
        Custom = 0,          // 习惯法/默认
        Decree = 1,          // 临时法令
        Statute = 2,         // 一般成文法
        Charter = 3,         // 特许/特权（有明确范围与覆盖声明）
        Constitutional = 4,  // 根本组织法
        Fundamental = 5,     // 最高法理/不可轻易变更
    }

    /// <summary>规范适用层级；不是“谁大谁小”，先看适用范围。</summary>
    public enum LegalScope
    {
        Realm = 0,
        Province = 1,
        Lordship = 2,
        Locality = 3,
        Personal = 4,
    }
}
