namespace MyriadCreation.Core.Contracts
{
    /// <summary>
    /// 法律系统的最小宿主上下文（B6 Laws 首步 / ASMDEF_SPLIT Phase 0）。
    /// 由 Simulation.WorldState 的 <c>LawRealmContext</c>（包装 RealmData）实现；
    /// Laws 核心逻辑只通过该接口读取"提案/资格"所需的宿主数值，不再直接引用 RealmData。
    /// </summary>
    public interface ILawContext
    {
        int RealmId { get; }
        int GetSupremeRulerId();
        float Treasury { get; }
        float Prestige { get; }
        float Legitimacy { get; }
        float Centralization { get; }
        float Authority { get; }
        bool IsCentralOfficeHolder(int characterId);
    }
}
