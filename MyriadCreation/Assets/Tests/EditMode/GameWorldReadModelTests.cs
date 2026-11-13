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
    /// B3-Step1：GameWorld 宿主实现 IWorldReadModel 的测试。
    /// 断言 world is IWorldReadModel（9 个接口）、关键成员与 new WorldReadModel(world) 逐项一致、
    /// 未初始化 GameWorld / 直接 new GameWorld 的接口成员安全不抛。
    /// </summary>
    public class GameWorldReadModelTests
    {
        private const int RealmA = 1;
        private const int RulerA = 100;
        private const int CapitalAnchor = 7;

        private GameObject _go;
        private GameWorld _world;
        private IWorldReadModel _host;
        private WorldReadModel _standalone;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("GameWorldReadModelTest");
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
                    ownerRealmId = RealmA,
                    populationBlocks = new List<PopulationBlock>()
                };
            }
            if (_world.realms == null) _world.realms = new Dictionary<int, RealmData>();
            if (_world.settlements == null) _world.settlements = new Dictionary<int, SettlementData>();
            if (_world.anchors == null) _world.anchors = new Dictionary<int, AnchorData>();

            var realm = new RealmData
            {
                realmId = RealmA,
                realmName = "Realm A",
                treasury = 123f,
                stability = 70f,
                centralization = 0.5f,
                realmForm = RealmForm.Sedentary,
                monarchId = RulerA,
                primaryCultureId = 11,
                stateReligionId = 12,
                lawState = new RealmLawState { legitimacy = 60f },
                coreTiles = new HashSet<int> { 0, 1 }
            };
            _world.realms[RealmA] = realm;

            SettlementData capital = new SettlementData
            {
                anchorId = CapitalAnchor,
                siteName = "Alpha",
                population = 500f,
                wealth = 900f,
                garrison = 12,
                siteMode = SiteMode.SedentarySettlement,
                settlementForm = SettlementForm.City,
                settlementLevel = SettlementLevel.LevelIII,
                isCapital = true,
                isPort = false
            };
            capital.anchor = new AnchorData { anchorId = CapitalAnchor, tileIndex = 5 };
            _world.settlements[CapitalAnchor] = capital;

            InjectCharacterManager();

            _host = _world;
            _standalone = new WorldReadModel(_world);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
        }

        private void InjectCharacterManager()
        {
            CharacterManager manager = new CharacterManager();
            FieldInfo managerField = typeof(GameWorld).GetField("_characterManager",
                BindingFlags.Instance | BindingFlags.NonPublic);
            managerField.SetValue(_world, manager);

            FieldInfo charsField = typeof(CharacterManager).GetField("_characters",
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
        }

        [Test]
        public void HostWorld_ImplementsAllNineContracts()
        {
            Assert.IsInstanceOf<IWorldReadModel>(_world);
            Assert.IsInstanceOf<IMapStatsView>(_host);
            Assert.IsInstanceOf<IRealmQuery>(_host);
            Assert.IsInstanceOf<ISettlementQuery>(_host);
            Assert.IsInstanceOf<ICharacterQuery>(_host);
            Assert.IsInstanceOf<IFaithQuery>(_host);
            Assert.IsInstanceOf<ISocietyQuery>(_host);
            Assert.IsInstanceOf<ISettlementPeopleQuery>(_host);
            Assert.IsInstanceOf<ICultureQuery>(_host);
            Assert.IsInstanceOf<IInnovationQuery>(_host);
        }

        [Test]
        public void HostWorld_MapStats_MatchStandaloneWorldReadModel()
        {
            Assert.AreEqual(_standalone.MapWidth, _host.MapWidth);
            Assert.AreEqual(_standalone.MapHeight, _host.MapHeight);
            Assert.AreEqual(_standalone.TotalTiles, _host.TotalTiles);
            Assert.AreEqual(_standalone.LandTiles, _host.LandTiles);
            Assert.AreEqual(_standalone.SeaTiles, _host.SeaTiles);
            Assert.AreEqual(_standalone.ProvinceCount, _host.ProvinceCount);
            Assert.AreEqual(_standalone.SettlementCount, _host.SettlementCount);
            Assert.AreEqual(_standalone.RealmCount, _host.RealmCount);
            Assert.AreEqual(_standalone.CurrentYear, _host.CurrentYear);
            Assert.AreEqual(_standalone.CurrentDay, _host.CurrentDay);
            Assert.AreEqual(_standalone.StartYear, _host.StartYear);
            Assert.AreEqual(_standalone.StartDay, _host.StartDay);

            Assert.AreEqual(8, _host.MapWidth);
            Assert.AreEqual(48, _host.TotalTiles);
            Assert.AreEqual(1, _host.RealmCount);
            Assert.AreEqual(1, _host.SettlementCount);
        }

        [Test]
        public void HostWorld_RealmSettlementCharacter_MatchStandaloneWorldReadModel()
        {
            RealmSnapshotDto hostRealm = _host.GetRealm(RealmA);
            RealmSnapshotDto standaloneRealm = _standalone.GetRealm(RealmA);
            Assert.IsNotNull(hostRealm);
            Assert.IsNotNull(standaloneRealm);
            Assert.AreEqual(standaloneRealm.realmId, hostRealm.realmId);
            Assert.AreEqual(standaloneRealm.realmName, hostRealm.realmName);
            Assert.AreEqual(standaloneRealm.treasury, hostRealm.treasury, 0.0001f);
            Assert.AreEqual(standaloneRealm.rulerId, hostRealm.rulerId);
            Assert.AreEqual(standaloneRealm.rulerName, hostRealm.rulerName);
            Assert.AreEqual(_standalone.GetAllRealms().Count, _host.GetAllRealms().Count);
            Assert.AreEqual(_standalone.GetAdminDivisions(RealmA).Count, _host.GetAdminDivisions(RealmA).Count);

            SettlementSnapshotDto hostSettlement = _host.GetSettlement(CapitalAnchor);
            SettlementSnapshotDto standaloneSettlement = _standalone.GetSettlement(CapitalAnchor);
            Assert.IsNotNull(hostSettlement);
            Assert.IsNotNull(standaloneSettlement);
            Assert.AreEqual(standaloneSettlement.anchorId, hostSettlement.anchorId);
            Assert.AreEqual(standaloneSettlement.settlementName, hostSettlement.settlementName);
            Assert.AreEqual(standaloneSettlement.realmId, hostSettlement.realmId);
            Assert.AreEqual(standaloneSettlement.siteMode, hostSettlement.siteMode);
            Assert.AreEqual(standaloneSettlement.settlementForm, hostSettlement.settlementForm);
            Assert.AreEqual(_standalone.GetSettlementsInRealm(RealmA).Count, _host.GetSettlementsInRealm(RealmA).Count);

            CharacterRowDto hostRuler = _host.GetRuler(RealmA);
            CharacterRowDto standaloneRuler = _standalone.GetRuler(RealmA);
            Assert.IsNotNull(hostRuler);
            Assert.IsNotNull(standaloneRuler);
            Assert.AreEqual(standaloneRuler.characterId, hostRuler.characterId);
            Assert.AreEqual(standaloneRuler.name, hostRuler.name);
            Assert.AreEqual(standaloneRuler.role, hostRuler.role);
            Assert.AreEqual(_standalone.GetCharactersInRealm(RealmA).Count, _host.GetCharactersInRealm(RealmA).Count);
        }

        [Test]
        public void HostWorld_FaithCultureSocietyInnovation_MatchStandaloneWorldReadModel()
        {
            Assert.AreEqual(_standalone.GetStateReligionId(RealmA), _host.GetStateReligionId(RealmA));
            Assert.AreEqual(_standalone.GetFaithName(1), _host.GetFaithName(1));
            Assert.AreEqual(_standalone.GetFervor(1), _host.GetFervor(1), 0.0001f);
            Assert.AreEqual(_standalone.GetPrimaryCultureId(RealmA), _host.GetPrimaryCultureId(RealmA));
            Assert.AreEqual(_standalone.GetCultureName(11), _host.GetCultureName(11));

            SocietySnapshotDto standaloneSociety = _standalone.GetSociety(RealmA);
            SocietySnapshotDto hostSociety = _host.GetSociety(RealmA);
            Assert.AreEqual(standaloneSociety?.realmId, hostSociety?.realmId);
            Assert.AreEqual(standaloneSociety?.classes?.Count ?? 0, hostSociety?.classes?.Count ?? 0);

            InnovationSnapshotDto standaloneInnovation = _standalone.GetInnovationSnapshot(RealmA);
            InnovationSnapshotDto hostInnovation = _host.GetInnovationSnapshot(RealmA);
            Assert.AreEqual(standaloneInnovation?.realmId, hostInnovation?.realmId);
            Assert.AreEqual(standaloneInnovation?.researchedCount, hostInnovation?.researchedCount);
            Assert.AreEqual(_standalone.GetResearchedCount(RealmA), _host.GetResearchedCount(RealmA));
            Assert.AreEqual(_standalone.HasInnovation(RealmA, 1), _host.HasInnovation(RealmA, 1));

            Assert.AreEqual(_standalone.GetKnownCharactersAtAnchor(CapitalAnchor).Count,
                _host.GetKnownCharactersAtAnchor(CapitalAnchor).Count);
            Assert.AreEqual(_standalone.GetPublicFiguresAtAnchor(CapitalAnchor).Count,
                _host.GetPublicFiguresAtAnchor(CapitalAnchor).Count);
        }

        [Test]
        public void UninitializedWorld_MembersAreSafe()
        {
            var go = new GameObject("UninitializedGameWorld");
            var uninitialized = go.AddComponent<GameWorld>();
            var model = (IWorldReadModel)uninitialized;

            Assert.DoesNotThrow(() =>
            {
                int width = model.MapWidth;
                int tiles = model.TotalTiles;
                int realms = model.RealmCount;
                RealmSnapshotDto realm = model.GetRealm(RealmA);
                SettlementSnapshotDto settlement = model.GetSettlement(CapitalAnchor);
                CharacterRowDto ruler = model.GetRuler(RealmA);
                SocietySnapshotDto society = model.GetSociety(RealmA);
                InnovationSnapshotDto innovation = model.GetInnovationSnapshot(RealmA);
                int stateReligion = model.GetStateReligionId(RealmA);
                int culture = model.GetPrimaryCultureId(RealmA);
                Assert.GreaterOrEqual(width, 0);
                Assert.GreaterOrEqual(tiles, 0);
                Assert.GreaterOrEqual(realms, 0);
                Assert.IsNull(realm);
                Assert.IsNull(settlement);
                Assert.IsNull(ruler);
                Assert.IsNull(society);
                Assert.IsNotNull(innovation);
                Assert.AreEqual(0, innovation.researchedCount);
                Assert.AreEqual(0, model.GetResearchedCount(RealmA));
                Assert.AreEqual(-1, stateReligion);
                Assert.AreEqual(-1, culture);
            });

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void DirectNewGameWorld_MembersAreSafe()
        {
            var fake = new GameWorld();
            var model = (IWorldReadModel)fake;

            Assert.DoesNotThrow(() =>
            {
                int width = model.MapWidth;
                int realmCount = model.RealmCount;
                RealmSnapshotDto realm = model.GetRealm(RealmA);
                SettlementSnapshotDto settlement = model.GetSettlement(CapitalAnchor);
                CharacterRowDto ruler = model.GetRuler(RealmA);
                Assert.GreaterOrEqual(width, 0);
                Assert.GreaterOrEqual(realmCount, 0);
                Assert.IsNull(realm);
                Assert.IsNull(settlement);
                Assert.IsNull(ruler);
            });
        }
    }
}
