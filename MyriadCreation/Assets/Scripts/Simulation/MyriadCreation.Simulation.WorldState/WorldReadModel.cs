using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.WorldState
{
    /// <summary>
    /// <see cref="IWorldReadModel"/> 的 host 聚合实现（UI 4b2/4b3）：一次构造组合
    /// WorldReadModelAdapter / RealmQueryAdapter / SettlementQueryAdapter / CharacterQueryAdapter，
    /// 以及 4b3 的 SocietyQueryAdapter / SettlementPeopleQueryAdapter / InnovationQueryAdapter，
    /// 信仰/文化查询直接复用 GameWorld.FaithQuery/CultureQuery（Core.Contracts 现有适配器）。
    /// 构造传入 <see cref="GameWorld"/>；null 世界时各适配器自身返回 0/null/空列表，不抛异常。
    /// 世界切换时由 App 新建实例（不做可变 rebind）。
    /// </summary>
    public sealed class WorldReadModel : IWorldReadModel
    {
        private readonly IMapStatsView _mapStats;
        private readonly IRealmQuery _realmQuery;
        private readonly ISettlementQuery _settlementQuery;
        private readonly ICharacterQuery _characterQuery;
        private readonly IFaithQuery _faithQuery;
        private readonly ISocietyQuery _societyQuery;
        private readonly ISettlementPeopleQuery _peopleQuery;
        private readonly ICultureQuery _cultureQuery;
        private readonly IInnovationQuery _innovationQuery;
        private readonly IFactionQuery _factionQuery;
        private readonly IRegimeQuery _regimeQuery;
        private readonly ITileInfoQuery _tileInfoQuery;
        private readonly ICharacterDetailQuery _characterDetailQuery;

        public WorldReadModel(GameWorld world)
        {
            _mapStats = new WorldReadModelAdapter(world);
            _realmQuery = new RealmQueryAdapter(world);
            _settlementQuery = new SettlementQueryAdapter(world);
            _characterQuery = new CharacterQueryAdapter(world);
            _faithQuery = world != null ? world.FaithQuery : null;
            _societyQuery = new SocietyQueryAdapter(world);
            _peopleQuery = new SettlementPeopleQueryAdapter(world);
            _cultureQuery = world != null ? world.CultureQuery : null;
            _innovationQuery = new InnovationQueryAdapter(world);
            _factionQuery = new FactionQueryAdapter(world);
            _regimeQuery = new RegimeQueryAdapter(world);
            _tileInfoQuery = new TileInfoQueryAdapter(world);
            _characterDetailQuery = new CharacterDetailQueryAdapter(world);
        }

        // ===== IMapStatsView =====

        public int MapWidth => _mapStats.MapWidth;

        public int MapHeight => _mapStats.MapHeight;

        public int TotalTiles => _mapStats.TotalTiles;

        public int LandTiles => _mapStats.LandTiles;

        public int SeaTiles => _mapStats.SeaTiles;

        public int ProvinceCount => _mapStats.ProvinceCount;

        public int SettlementCount => _mapStats.SettlementCount;

        public int RealmCount => _mapStats.RealmCount;

        public int CurrentYear => _mapStats.CurrentYear;

        public int CurrentDay => _mapStats.CurrentDay;

        public int StartYear => _mapStats.StartYear;

        public int StartDay => _mapStats.StartDay;

        // ===== IRealmQuery =====

        public RealmSnapshotDto GetRealm(int realmId) => _realmQuery.GetRealm(realmId);

        public IReadOnlyList<RealmSnapshotDto> GetAllRealms() => _realmQuery.GetAllRealms();

        public IReadOnlyList<AdminDivisionNodeDto> GetAdminDivisions(int realmId) => _realmQuery.GetAdminDivisions(realmId);

        // ===== ISettlementQuery =====

        public SettlementSnapshotDto GetSettlement(int anchorId) => _settlementQuery.GetSettlement(anchorId);

        public IReadOnlyList<SettlementBlockRowDto> GetSettlementBlocks(int anchorId) => _settlementQuery.GetSettlementBlocks(anchorId);

        public IReadOnlyList<SettlementSnapshotDto> GetSettlementsInRealm(int realmId) => _settlementQuery.GetSettlementsInRealm(realmId);

        public IReadOnlyList<SettlementSnapshotDto> GetSettlementsNearTile(int tileIndex, int radius) => _settlementQuery.GetSettlementsNearTile(tileIndex, radius);

        // ===== ICharacterQuery =====

        public CharacterRowDto GetCharacter(int characterId) => _characterQuery.GetCharacter(characterId);

        public IReadOnlyList<CharacterRowDto> GetCharactersInRealm(int realmId) => _characterQuery.GetCharactersInRealm(realmId);

        public CharacterRowDto GetRuler(int realmId) => _characterQuery.GetRuler(realmId);

        // ===== IFaithQuery（复用现有 FaithContracts；null world 安全降级） =====

        public int GetStateReligionId(int realmId) => _faithQuery?.GetStateReligionId(realmId) ?? -1;

        public bool HasFaith(int faithId) => _faithQuery != null && _faithQuery.HasFaith(faithId);

        public string GetFaithName(int faithId) => _faithQuery?.GetFaithName(faithId) ?? "";

        public float GetFervor(int faithId) => _faithQuery?.GetFervor(faithId) ?? 0f;

        public IReadOnlyList<int> GetHolySiteTiles(int faithId)
        {
            return _faithQuery != null ? _faithQuery.GetHolySiteTiles(faithId) : Array.Empty<int>();
        }

        public bool TryGetFaithSnapshot(int faithId, out FaithSnapshotDto snapshot)
        {
            if (_faithQuery != null)
            {
                return _faithQuery.TryGetFaithSnapshot(faithId, out snapshot);
            }
            snapshot = null;
            return false;
        }

        public bool CanDeclareGreatHolyWar(int realmId)
        {
            return _faithQuery != null && _faithQuery.CanDeclareGreatHolyWar(realmId);
        }

        public IReadOnlyList<FaithSaintDto> GetSaints(int faithId)
        {
            return _faithQuery != null ? _faithQuery.GetSaints(faithId) : Array.Empty<FaithSaintDto>();
        }

        public IReadOnlyList<FaithDoctrineDto> GetDoctrines(int faithId)
        {
            return _faithQuery != null ? _faithQuery.GetDoctrines(faithId) : Array.Empty<FaithDoctrineDto>();
        }

        public int GetPatronSaintId(int realmId)
        {
            return _faithQuery?.GetPatronSaintId(realmId) ?? -1;
        }

        public string GetPatronSaintName(int realmId)
        {
            return _faithQuery?.GetPatronSaintName(realmId) ?? "";
        }

        // ===== ISocietyQuery =====

        public SocietySnapshotDto GetSociety(int realmId) => _societyQuery.GetSociety(realmId);

        // ===== ISettlementPeopleQuery =====

        public IReadOnlyList<CharacterRowDto> GetKnownCharactersAtAnchor(int anchorId, int observerRealmId = -1)
            => _peopleQuery.GetKnownCharactersAtAnchor(anchorId, observerRealmId);

        public IReadOnlyList<CharacterRowDto> GetPublicFiguresAtAnchor(int anchorId)
            => _peopleQuery.GetPublicFiguresAtAnchor(anchorId);

        // ===== ICultureQuery（复用现有 CultureContracts；null world 安全降级） =====

        public int GetPrimaryCultureId(int realmId) => _cultureQuery?.GetPrimaryCultureId(realmId) ?? -1;

        public string GetCultureName(int cultureId) => _cultureQuery?.GetCultureName(cultureId) ?? "";

        public int GetCultureStage(int cultureId) => _cultureQuery?.GetCultureStage(cultureId) ?? -1;

        public string GetCultureStageKey(int stage) => _cultureQuery?.GetCultureStageKey(stage) ?? "Unknown";

        // ===== IInnovationQuery =====

        public int GetResearchedCount(int realmId) => _innovationQuery.GetResearchedCount(realmId);

        public IReadOnlyList<int> GetResearchedInnovationIds(int realmId) => _innovationQuery.GetResearchedInnovationIds(realmId);

        public InnovationSnapshotDto GetInnovationSnapshot(int realmId) => _innovationQuery.GetInnovationSnapshot(realmId);

        public bool HasInnovation(int realmId, int innovationId) => _innovationQuery.HasInnovation(realmId, innovationId);

        public string GetInnovationName(int innovationId) => _innovationQuery.GetInnovationName(innovationId);

        public IReadOnlyList<InnovationAvailabilityDto> GetAvailableInnovations(int realmId)
            => _innovationQuery.GetAvailableInnovations(realmId);

        public IReadOnlyList<int> GetPrerequisiteInnovationIds(int innovationId)
            => _innovationQuery.GetPrerequisiteInnovationIds(innovationId);

        public bool CanResearch(int realmId, int innovationId)
            => _innovationQuery.CanResearch(realmId, innovationId);

        public bool IsTechAccessibleForGoods(int goodsId, int realmId)
            => _innovationQuery.IsTechAccessibleForGoods(goodsId, realmId);

        public bool IsResourceDepthAccessible(int accessDepth, int realmId)
            => _innovationQuery.IsResourceDepthAccessible(accessDepth, realmId);

        public bool CanAccessResource(int goodsId, int accessDepth, int realmId)
            => _innovationQuery.CanAccessResource(goodsId, accessDepth, realmId);

        // ===== IFactionQuery =====

        public IReadOnlyList<FactionSnapshotDto> GetFactions(int realmId)
            => _factionQuery.GetFactions(realmId);

        // ===== IRegimeQuery =====

        public RegimeSnapshotDto GetRegime(int realmId)
            => _regimeQuery.GetRegime(realmId);

        // ===== ITileInfoQuery =====

        public TileInfoDto GetTileInfo(int tileIndex) => _tileInfoQuery.GetTileInfo(tileIndex);

        // ===== ICharacterDetailQuery =====

        public CharacterDetailDto GetCharacterDetail(int characterId)
            => _characterDetailQuery.GetCharacterDetail(characterId);
    }
}
