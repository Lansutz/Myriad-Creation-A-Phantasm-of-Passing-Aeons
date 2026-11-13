namespace MyriadCreation.Content.Definitions
{
    /// <summary>
    /// 内容只读查询契约（B5）：由 host 侧（Simulation/CultureHost 的 ContentLookupAdapter）实现，
    /// Culture 程序集只依赖本接口与 Content 类型，不再直接引用 Modding/ContentResolvers。
    /// </summary>
    public interface IContentLookup
    {
        /// <summary>取某传统的因子数值（缺失返回 false）。</summary>
        bool TryGetTraditionFactor(string traditionId, TraditionFactorKey key, out float value);

        /// <summary>取某文化关联民族组的全部传统 id（合并去重由实现决定；无结果返回 false）。</summary>
        bool TryGetEthnicGroupTraditionIds(int cultureId, out string[] traditionIds);
    }
}
