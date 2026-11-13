namespace MyriadCreation.Core.Contracts
{
    /// <summary>
    /// UI 4b2/4b3 聚合只读契约：把地图统计、政权、聚落、角色、信仰、社会、人物、文化、革新查询接口组合为一个注入对象，
    /// App 创建/切换世界后只向 UI 注入一次；UI 面板按需取用，不直接引用 Simulation/GameWorld。
    /// 不新增成员（纯接口组合）。
    /// </summary>
    public interface IWorldReadModel : IMapStatsView, IRealmQuery, ISettlementQuery, ICharacterQuery,
        IFaithQuery, ISocietyQuery, ISettlementPeopleQuery, ICultureQuery, IInnovationQuery,
        IFactionQuery, IRegimeQuery, ITileInfoQuery, ICharacterDetailQuery
    {
    }
}
