using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.WorldState
{
    /// <summary>
    /// B3-Step1：宿主 <see cref="GameWorld"/> 实现聚合只读契约 <see cref="IWorldReadModel"/>。
    /// 全部成员委托给懒创建的 <see cref="WorldReadModel"/>（一次创建、世界数据实时读取；
    /// 世界切换由 App 新建 WorldReadModel 并重新注入，宿主不自带可变 rebind）。
    /// 本文件只加只读接口实现，不新增/移动任何序列化字段。
    /// </summary>
    public partial class GameWorld
    {
        private WorldReadModel _readModel;

        private WorldReadModel ReadModel => _readModel ??= new WorldReadModel(this);

        // ===== IMapStatsView =====

        int IMapStatsView.MapWidth => ReadModel.MapWidth;

        int IMapStatsView.MapHeight => ReadModel.MapHeight;

        int IMapStatsView.TotalTiles => ReadModel.TotalTiles;

        int IMapStatsView.LandTiles => ReadModel.LandTiles;

        int IMapStatsView.SeaTiles => ReadModel.SeaTiles;

        int IMapStatsView.ProvinceCount => ReadModel.ProvinceCount;

        int IMapStatsView.SettlementCount => ReadModel.SettlementCount;

        int IMapStatsView.RealmCount => ReadModel.RealmCount;

        int IMapStatsView.CurrentYear => ReadModel.CurrentYear;

        int IMapStatsView.CurrentDay => ReadModel.CurrentDay;

        int IMapStatsView.StartYear => ReadModel.StartYear;

        int IMapStatsView.StartDay => ReadModel.StartDay;

        // ===== IRealmQuery =====

        RealmSnapshotDto IRealmQuery.GetRealm(int realmId) => ReadModel.GetRealm(realmId);

        IReadOnlyList<RealmSnapshotDto> IRealmQuery.GetAllRealms() => ReadModel.GetAllRealms();

        IReadOnlyList<AdminDivisionNodeDto> IRealmQuery.GetAdminDivisions(int realmId) => ReadModel.GetAdminDivisions(realmId);

        // ===== ISettlementQuery =====

        SettlementSnapshotDto ISettlementQuery.GetSettlement(int anchorId) => ReadModel.GetSettlement(anchorId);

        IReadOnlyList<SettlementBlockRowDto> ISettlementQuery.GetSettlementBlocks(int anchorId) => ReadModel.GetSettlementBlocks(anchorId);

        IReadOnlyList<SettlementSnapshotDto> ISettlementQuery.GetSettlementsInRealm(int realmId) => ReadModel.GetSettlementsInRealm(realmId);

        IReadOnlyList<SettlementSnapshotDto> ISettlementQuery.GetSettlementsNearTile(int tileIndex, int radius) => ReadModel.GetSettlementsNearTile(tileIndex, radius);

        // ===== ICharacterQuery =====

        CharacterRowDto ICharacterQuery.GetCharacter(int characterId) => ReadModel.GetCharacter(characterId);

        IReadOnlyList<CharacterRowDto> ICharacterQuery.GetCharactersInRealm(int realmId) => ReadModel.GetCharactersInRealm(realmId);

        CharacterRowDto ICharacterQuery.GetRuler(int realmId) => ReadModel.GetRuler(realmId);

        // ===== IFaithQuery =====

        int IFaithQuery.GetStateReligionId(int realmId) => ReadModel.GetStateReligionId(realmId);

        bool IFaithQuery.HasFaith(int faithId) => ReadModel.HasFaith(faithId);

        string IFaithQuery.GetFaithName(int faithId) => ReadModel.GetFaithName(faithId);

        float IFaithQuery.GetFervor(int faithId) => ReadModel.GetFervor(faithId);

        IReadOnlyList<int> IFaithQuery.GetHolySiteTiles(int faithId) => ReadModel.GetHolySiteTiles(faithId);

        bool IFaithQuery.TryGetFaithSnapshot(int faithId, out FaithSnapshotDto snapshot) => ReadModel.TryGetFaithSnapshot(faithId, out snapshot);

        bool IFaithQuery.CanDeclareGreatHolyWar(int realmId) => ReadModel.CanDeclareGreatHolyWar(realmId);

        IReadOnlyList<FaithSaintDto> IFaithQuery.GetSaints(int faithId) => ReadModel.GetSaints(faithId);

        IReadOnlyList<FaithDoctrineDto> IFaithQuery.GetDoctrines(int faithId) => ReadModel.GetDoctrines(faithId);

        // ===== ISocietyQuery =====

        SocietySnapshotDto ISocietyQuery.GetSociety(int realmId) => ReadModel.GetSociety(realmId);

        // ===== ISettlementPeopleQuery =====

        IReadOnlyList<CharacterRowDto> ISettlementPeopleQuery.GetKnownCharactersAtAnchor(int anchorId, int observerRealmId)
            => ReadModel.GetKnownCharactersAtAnchor(anchorId, observerRealmId);

        IReadOnlyList<CharacterRowDto> ISettlementPeopleQuery.GetPublicFiguresAtAnchor(int anchorId)
            => ReadModel.GetPublicFiguresAtAnchor(anchorId);

        // ===== ICultureQuery =====

        int ICultureQuery.GetPrimaryCultureId(int realmId) => ReadModel.GetPrimaryCultureId(realmId);

        string ICultureQuery.GetCultureName(int cultureId) => ReadModel.GetCultureName(cultureId);

        int ICultureQuery.GetCultureStage(int cultureId) => ReadModel.GetCultureStage(cultureId);

        string ICultureQuery.GetCultureStageKey(int stage) => ReadModel.GetCultureStageKey(stage);

        // ===== IInnovationQuery =====

        int IInnovationQuery.GetResearchedCount(int realmId) => ReadModel.GetResearchedCount(realmId);

        IReadOnlyList<int> IInnovationQuery.GetResearchedInnovationIds(int realmId) => ReadModel.GetResearchedInnovationIds(realmId);

        InnovationSnapshotDto IInnovationQuery.GetInnovationSnapshot(int realmId) => ReadModel.GetInnovationSnapshot(realmId);

        bool IInnovationQuery.HasInnovation(int realmId, int innovationId) => ReadModel.HasInnovation(realmId, innovationId);

        string IInnovationQuery.GetInnovationName(int innovationId) => ReadModel.GetInnovationName(innovationId);

        IReadOnlyList<InnovationAvailabilityDto> IInnovationQuery.GetAvailableInnovations(int realmId)
            => ReadModel.GetAvailableInnovations(realmId);

        IReadOnlyList<int> IInnovationQuery.GetPrerequisiteInnovationIds(int innovationId)
            => ReadModel.GetPrerequisiteInnovationIds(innovationId);

        bool IInnovationQuery.CanResearch(int realmId, int innovationId) => ReadModel.CanResearch(realmId, innovationId);

        bool IInnovationQuery.IsTechAccessibleForGoods(int goodsId, int realmId)
            => ReadModel.IsTechAccessibleForGoods(goodsId, realmId);

        bool IInnovationQuery.IsResourceDepthAccessible(int accessDepth, int realmId)
            => ReadModel.IsResourceDepthAccessible(accessDepth, realmId);

        bool IInnovationQuery.CanAccessResource(int goodsId, int accessDepth, int realmId)
            => ReadModel.CanAccessResource(goodsId, accessDepth, realmId);

        // ===== IFaithQuery（patron saint 扩展）=====

        int IFaithQuery.GetPatronSaintId(int realmId) => ReadModel.GetPatronSaintId(realmId);

        string IFaithQuery.GetPatronSaintName(int realmId) => ReadModel.GetPatronSaintName(realmId);

        // ===== IFactionQuery =====

        IReadOnlyList<FactionSnapshotDto> IFactionQuery.GetFactions(int realmId) => ReadModel.GetFactions(realmId);

        // ===== IRegimeQuery =====

        RegimeSnapshotDto IRegimeQuery.GetRegime(int realmId) => ReadModel.GetRegime(realmId);

        // ===== ITileInfoQuery / ICharacterDetailQuery（UI 4e）=====

        TileInfoDto ITileInfoQuery.GetTileInfo(int tileIndex) => ReadModel.GetTileInfo(tileIndex);

        CharacterDetailDto ICharacterDetailQuery.GetCharacterDetail(int characterId)
            => ReadModel.GetCharacterDetail(characterId);
    }
}
