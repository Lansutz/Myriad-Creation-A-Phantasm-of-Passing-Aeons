using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Settlement;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// UI 4b2a：IWorldReadModel / WorldReadModel 聚合契约测试。
    /// 小世界手搭 GameWorld 数据；断言聚合结果与分别 new 适配器一致（MapStats/Realm/Settlement/Character 抽查）、
    /// null world 全安全、同实例重复查询一致。
    /// </summary>
    public class WorldReadModelTests
    {
        private const int RealmA = 1;
        private const int RealmB = 2;
        private const int RulerA = 100;
        private const int NobleA = 101;
        private const int CapitalAnchor = 7;
        private const int OtherAnchor = 8;

        private GameObject _go;
        private GameWorld _world;

        private WorldReadModel _model;
        private WorldReadModelAdapter _mapStats;
        private RealmQueryAdapter _realmQuery;
        private SettlementQueryAdapter _settlementQuery;
        private CharacterQueryAdapter _characterQuery;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("WorldReadModelTestWorld");
            _world = _go.AddComponent<GameWorld>();
            _world.mapWidth = 8;
            _world.mapHeight = 6;
            _world.currentYear = 12;
            _world.currentDay = 34;
            _world.startYear = 1;
            _world.startDay = 1;
            _world.tiles = new TileData[48];
            for (int i = 0; i < _world.tiles.Length; i++)
            {
                _world.tiles[i] = new TileData
                {
                    tileIndex = i,
                    exists = true,
                    isLand = (i % 2 == 0),
                    ownerRealmId = (i % 8 < 4) ? RealmA : RealmB,
                    populationBlocks = new List<PopulationBlock>()
                };
            }
            if (_world.realms == null) _world.realms = new Dictionary<int, RealmData>();
            if (_world.settlements == null) _world.settlements = new Dictionary<int, SettlementData>();
            if (_world.anchors == null) _world.anchors = new Dictionary<int, AnchorData>();

            var realmA = new RealmData
            {
                realmId = RealmA,
                realmName = "Realm A",
                treasury = 1234f,
                stability = 67f,
                centralization = 0.75f,
                realmForm = RealmForm.Sedentary,
                monarchId = RulerA,
                lawState = new RealmLawState { legitimacy = 78f },
                coreTiles = new HashSet<int> { 0, 1, 2, 3 }
            };
            realmA.officeHolders[77] = NobleA;
            realmA.adminDivisions = new List<AdminDivision>
            {
                new AdminDivision
                {
                    divisionId = 10,
                    realmId = RealmA,
                    level = 1,
                    name = "County A",
                    parentDivisionId = -1,
                    titleId = "county",
                    holderCharacterId = NobleA,
                    tiles = new HashSet<int> { 0, 1, 2 }
                }
            };
            var realmB = new RealmData
            {
                realmId = RealmB,
                realmName = "Realm B",
                treasury = 50f,
                stability = 40f,
                realmForm = RealmForm.Nomadic,
                monarchId = -1
            };
            _world.realms[RealmA] = realmA;
            _world.realms[RealmB] = realmB;

            SettlementData capital = new SettlementData
            {
                anchorId = CapitalAnchor,
                siteName = "Alpha",
                population = 500f,
                wealth = 900f,
                garrison = 12,
                garrisonQuality = 3.5f,
                garrisonQuantity = 12,
                constructionTier = 4,
                siteMode = SiteMode.SedentarySettlement,
                settlementForm = SettlementForm.City,
                settlementLevel = SettlementLevel.LevelIII,
                isCapital = true,
                isPort = false
            };
            capital.anchor = new AnchorData { anchorId = CapitalAnchor, tileIndex = 5 };
            SettlementData other = new SettlementData
            {
                anchorId = OtherAnchor,
                siteName = "Beta",
                population = 80f,
                wealth = 25f,
                garrison = 2,
                siteMode = SiteMode.FortifiedOutpost,
                settlementForm = SettlementForm.Village,
                settlementLevel = SettlementLevel.LevelI
            };
            other.anchor = new AnchorData { anchorId = OtherAnchor, tileIndex = 6 };
            _world.settlements[CapitalAnchor] = capital;
            _world.settlements[OtherAnchor] = other;

            InjectCharacterManager();

            _model = new WorldReadModel(_world);
            _mapStats = new WorldReadModelAdapter(_world);
            _realmQuery = new RealmQueryAdapter(_world);
            _settlementQuery = new SettlementQueryAdapter(_world);
            _characterQuery = new CharacterQueryAdapter(_world);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
        }

        private void InjectCharacterManager()
        {
            CharacterManager manager = new CharacterManager();
            var managerField = typeof(GameWorld).GetField("_characterManager",
                BindingFlags.Instance | BindingFlags.NonPublic);
            managerField.SetValue(_world, manager);

            var charsField = typeof(CharacterManager).GetField("_characters",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var chars = (Dictionary<int, CharacterData>)charsField.GetValue(manager);
            chars[RulerA] = new CharacterData
            {
                characterId = RulerA,
                firstName = "Alice",
                lastName = "Ruler",
                age = 41,
                isMale = false,
                realmId = RealmA,
                role = CharacterRole.Ruler,
                isPublicFigure = true,
                deathDay = -1
            };
            chars[NobleA] = new CharacterData
            {
                characterId = NobleA,
                firstName = "Bob",
                lastName = "Noble",
                age = 30,
                isMale = true,
                realmId = RealmA,
                role = CharacterRole.Noble,
                deathDay = -1
            };
        }

        // ===================== 聚合接口 =====================

        [Test]
        public void Aggregate_IsAssignableToFourUiContracts()
        {
            Assert.IsInstanceOf<IWorldReadModel>(_model);
            Assert.IsInstanceOf<IMapStatsView>(_model);
            Assert.IsInstanceOf<IRealmQuery>(_model);
            Assert.IsInstanceOf<ISettlementQuery>(_model);
            Assert.IsInstanceOf<ICharacterQuery>(_model);
        }

        // ===================== 与单独 new 适配器一致（抽查） =====================

        [Test]
        public void Aggregate_MapStats_MatchesStandaloneAdapter()
        {
            Assert.AreEqual(_mapStats.MapWidth, _model.MapWidth);
            Assert.AreEqual(_mapStats.MapHeight, _model.MapHeight);
            Assert.AreEqual(_mapStats.TotalTiles, _model.TotalTiles);
            Assert.AreEqual(_mapStats.LandTiles, _model.LandTiles);
            Assert.AreEqual(_mapStats.SeaTiles, _model.SeaTiles);
            Assert.AreEqual(_mapStats.ProvinceCount, _model.ProvinceCount);
            Assert.AreEqual(_mapStats.SettlementCount, _model.SettlementCount);
            Assert.AreEqual(_mapStats.RealmCount, _model.RealmCount);
            Assert.AreEqual(_mapStats.CurrentYear, _model.CurrentYear);
            Assert.AreEqual(_mapStats.CurrentDay, _model.CurrentDay);
            Assert.AreEqual(_mapStats.StartYear, _model.StartYear);
            Assert.AreEqual(_mapStats.StartDay, _model.StartDay);

            Assert.AreEqual(8, _model.MapWidth);
            Assert.AreEqual(48, _model.TotalTiles);
            Assert.AreEqual(2, _model.RealmCount);
            Assert.AreEqual(2, _model.SettlementCount);
        }

        [Test]
        public void Aggregate_RealmQuery_MatchesStandaloneAdapter()
        {
            RealmSnapshotDto expected = _realmQuery.GetRealm(RealmA);
            RealmSnapshotDto actual = _model.GetRealm(RealmA);
            Assert.IsNotNull(expected);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.realmId, actual.realmId);
            Assert.AreEqual(expected.realmName, actual.realmName);
            Assert.AreEqual(expected.treasury, actual.treasury, 0.0001f);
            Assert.AreEqual(expected.stability, actual.stability, 0.0001f);
            Assert.AreEqual(expected.legitimacy, actual.legitimacy, 0.0001f);
            Assert.AreEqual(expected.rulerId, actual.rulerId);
            Assert.AreEqual(expected.rulerName, actual.rulerName);
            Assert.AreEqual(expected.governmentForm, actual.governmentForm);
            Assert.AreEqual(expected.capitalAnchorId, actual.capitalAnchorId);
            Assert.AreEqual(expected.territoryCount, actual.territoryCount);

            Assert.AreEqual(_realmQuery.GetAllRealms().Count, _model.GetAllRealms().Count);
            Assert.AreEqual(_realmQuery.GetAdminDivisions(RealmA).Count, _model.GetAdminDivisions(RealmA).Count);
            Assert.IsNull(_model.GetRealm(999));
        }

        [Test]
        public void Aggregate_SettlementQuery_MatchesStandaloneAdapter()
        {
            SettlementSnapshotDto expected = _settlementQuery.GetSettlement(CapitalAnchor);
            SettlementSnapshotDto actual = _model.GetSettlement(CapitalAnchor);
            Assert.IsNotNull(expected);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.anchorId, actual.anchorId);
            Assert.AreEqual(expected.settlementName, actual.settlementName);
            Assert.AreEqual(expected.realmId, actual.realmId);
            Assert.AreEqual(expected.population, actual.population, 0.0001f);
            Assert.AreEqual(expected.wealth, actual.wealth, 0.0001f);
            Assert.AreEqual(expected.siteMode, actual.siteMode);
            Assert.AreEqual(expected.settlementForm, actual.settlementForm);
            Assert.AreEqual(expected.settlementLevel, actual.settlementLevel);
            Assert.AreEqual(expected.isCapital, actual.isCapital);

            Assert.AreEqual(_settlementQuery.GetSettlementsInRealm(RealmA).Count, _model.GetSettlementsInRealm(RealmA).Count);
            Assert.AreEqual(_settlementQuery.GetSettlementsNearTile(5, 1).Count, _model.GetSettlementsNearTile(5, 1).Count);
            Assert.IsNull(_model.GetSettlement(999));
        }

        [Test]
        public void Aggregate_CharacterQuery_MatchesStandaloneAdapter()
        {
            CharacterRowDto expected = _characterQuery.GetCharacter(RulerA);
            CharacterRowDto actual = _model.GetCharacter(RulerA);
            Assert.IsNotNull(expected);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.characterId, actual.characterId);
            Assert.AreEqual(expected.name, actual.name);
            Assert.AreEqual(expected.age, actual.age);
            Assert.AreEqual(expected.realmId, actual.realmId);
            Assert.AreEqual(expected.role, actual.role);
            Assert.AreEqual(expected.flags, actual.flags);

            Assert.AreEqual(_characterQuery.GetRuler(RealmA)?.characterId, _model.GetRuler(RealmA)?.characterId);
            Assert.AreEqual(_characterQuery.GetCharactersInRealm(RealmA).Count, _model.GetCharactersInRealm(RealmA).Count);
            Assert.IsNull(_model.GetCharacter(999));
        }

        // ===================== 同实例一致 =====================

        [Test]
        public void SameInstance_RepeatedQueries_AreConsistent()
        {
            RealmSnapshotDto first = _model.GetRealm(RealmA);
            RealmSnapshotDto second = _model.GetRealm(RealmA);
            Assert.AreEqual(first.realmId, second.realmId);
            Assert.AreEqual(first.realmName, second.realmName);
            Assert.AreEqual(first.treasury, second.treasury, 0.0001f);

            Assert.AreEqual(_model.GetAllRealms().Count, _model.GetAllRealms().Count);
            Assert.AreEqual(_model.GetSettlement(CapitalAnchor).anchorId, _model.GetSettlement(CapitalAnchor).anchorId);
            Assert.AreEqual(_model.GetCharacter(RulerA).characterId, _model.GetCharacter(RulerA).characterId);
            Assert.AreEqual(_model.MapWidth, _model.MapWidth);
            Assert.AreEqual(_model.CurrentDay, _model.CurrentDay);
        }

        // ===================== null world 安全 =====================

        [Test]
        public void NullWorld_AllMembersAreSafe()
        {
            var model = new WorldReadModel(null);

            Assert.AreEqual(0, model.MapWidth);
            Assert.AreEqual(0, model.MapHeight);
            Assert.AreEqual(0, model.TotalTiles);
            Assert.AreEqual(0, model.LandTiles);
            Assert.AreEqual(0, model.SeaTiles);
            Assert.AreEqual(0, model.ProvinceCount);
            Assert.AreEqual(0, model.SettlementCount);
            Assert.AreEqual(0, model.RealmCount);
            Assert.AreEqual(0, model.CurrentYear);
            Assert.AreEqual(0, model.CurrentDay);
            Assert.AreEqual(0, model.StartYear);
            Assert.AreEqual(0, model.StartDay);

            Assert.IsNull(model.GetRealm(RealmA));
            Assert.IsNotNull(model.GetAllRealms());
            Assert.AreEqual(0, model.GetAllRealms().Count);
            Assert.IsNotNull(model.GetAdminDivisions(RealmA));
            Assert.AreEqual(0, model.GetAdminDivisions(RealmA).Count);

            Assert.IsNull(model.GetSettlement(CapitalAnchor));
            Assert.IsNotNull(model.GetSettlementsInRealm(RealmA));
            Assert.AreEqual(0, model.GetSettlementsInRealm(RealmA).Count);
            Assert.IsNotNull(model.GetSettlementsNearTile(0, 1));
            Assert.AreEqual(0, model.GetSettlementsNearTile(0, 1).Count);

            Assert.IsNull(model.GetCharacter(RulerA));
            Assert.IsNotNull(model.GetCharactersInRealm(RealmA));
            Assert.AreEqual(0, model.GetCharactersInRealm(RealmA).Count);
            Assert.IsNull(model.GetRuler(RealmA));
        }
    }
}
