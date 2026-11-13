using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Enums;
using MyriadCreation.Core.Data;
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
    /// UI 4b 第一批：IRealmQuery/ISettlementQuery/ICharacterQuery 的 host 适配器测试。
    /// 小世界手搭 GameWorld 数据，断言 DTO 与 GameWorld/CharacterManager 直接查询一致、空世界/越界安全。
    /// </summary>
    public class UiQueryContractsTests
    {
        private GameObject _go;
        private GameWorld _world;

        private RealmQueryAdapter _realmQuery;
        private SettlementQueryAdapter _settlementQuery;
        private CharacterQueryAdapter _characterQuery;

        private const int RealmA = 1;
        private const int RealmB = 2;
        private const int RulerA = 100;
        private const int NobleA = 101;
        private const int RulerB = 102;
        private const int CapitalAnchor = 7;
        private const int OtherAnchor = 8;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("UiQueryTestWorld");
            _world = _go.AddComponent<GameWorld>();
            _world.mapWidth = 8;
            _world.mapHeight = 6;
            _world.tiles = new TileData[48];
            for (int i = 0; i < _world.tiles.Length; i++)
            {
                _world.tiles[i] = new TileData
                {
                    tileIndex = i,
                    exists = true,
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
                primaryCultureId = 11,
                stateReligionId = 12,
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
                },
                new AdminDivision
                {
                    divisionId = 11,
                    realmId = RealmA,
                    level = 2,
                    name = "Barony A",
                    parentDivisionId = 10,
                    titleId = "barony",
                    holderCharacterId = RulerA,
                    tiles = new HashSet<int> { 5 }
                }
            };
            var realmB = new RealmData
            {
                realmId = RealmB,
                realmName = "Realm B",
                treasury = 50f,
                stability = 40f,
                realmForm = RealmForm.Nomadic,
                monarchId = RulerB
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
                isPort = false,
                ruinLevel = 0
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
                settlementLevel = SettlementLevel.LevelI,
                ruinLevel = 3
            };
            other.anchor = new AnchorData { anchorId = OtherAnchor, tileIndex = 6 };
            _world.settlements[CapitalAnchor] = capital;
            _world.settlements[OtherAnchor] = other;

            // 聚落 5 的人口构成：文化 3 占多数、信仰 5 占多数。
            // 首都地块归属 Realm A（初始按列分配时 5 落在 B，这里显式改回 A）。
            _world.tiles[5].ownerRealmId = RealmA;
            _world.tiles[5].populationBlocks = new List<PopulationBlock>
            {
                new PopulationBlock { count = 80f, cultureId = 3, faithId = 5 },
                new PopulationBlock { count = 20f, cultureId = 4, faithId = 6 }
            };
            _world.tiles[0].populationBlocks = new List<PopulationBlock>
            {
                new PopulationBlock { count = 10f, cultureId = 3, faithId = 5 }
            };

            InjectCharacterManager();
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
            chars[RulerB] = new CharacterData
            {
                characterId = RulerB,
                firstName = "Carol",
                lastName = "Khan",
                age = 50,
                isMale = false,
                realmId = RealmB,
                role = CharacterRole.Ruler,
                deathDay = -1
            };
        }

        private static float ExpectedPopulation(GameWorld world, int realmId)
        {
            float sum = 0f;
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (world.tiles[i].exists && world.tiles[i].ownerRealmId == realmId)
                {
                    sum += world.GetTilePopulation(i);
                }
            }
            return sum;
        }

        [Test]
        public void RealmQuery_MatchesGameWorld_ColorAndEnumMapping()
        {
            RealmSnapshotDto dto = _realmQuery.GetRealm(RealmA);
            RealmData realm = _world.realms[RealmA];

            Assert.IsNotNull(dto);
            Assert.AreEqual(realm.realmId, dto.realmId);
            Assert.AreEqual(realm.realmName, dto.realmName);
            Assert.AreEqual(realm.treasury, dto.treasury, 0.0001f);
            Assert.AreEqual(realm.stability, dto.stability, 0.0001f);
            Assert.AreEqual(realm.lawState.legitimacy, dto.legitimacy, 0.0001f);
            Assert.AreEqual(realm.centralization, dto.centralization, 0.0001f);
            Assert.AreEqual((int)realm.realmForm, dto.governmentForm);
            Assert.AreEqual(RulerA, dto.rulerId);
            Assert.AreEqual("Alice Ruler", dto.rulerName);
            Assert.AreEqual(CapitalAnchor, dto.capitalAnchorId);
            Assert.AreEqual(ExpectedPopulation(_world, RealmA), dto.population, 0.0001f);

            int expectedTiles = 0;
            foreach (TileData tile in _world.tiles)
            {
                if (tile.exists && tile.ownerRealmId == RealmA) expectedTiles++;
            }
            Assert.AreEqual(expectedTiles, dto.territoryCount);

            // 颜色只暴露 float，0..1 且同 id 稳定。
            Assert.GreaterOrEqual(dto.colorR, 0f);
            Assert.LessOrEqual(dto.colorR, 1f);
            Assert.AreEqual(dto.colorR, _realmQuery.GetRealm(RealmA).colorR, 0.000001f);

            IReadOnlyList<AdminDivisionNodeDto> divisions = _realmQuery.GetAdminDivisions(RealmA);
            Assert.AreEqual(2, divisions.Count);
            Assert.AreEqual(10, divisions[0].divisionId);
            Assert.AreEqual(3, divisions[0].tileCount);
            Assert.AreEqual(NobleA, divisions[0].holderCharacterId);
            Assert.AreEqual("Bob Noble", divisions[0].holderName);
            Assert.AreEqual(10, divisions[1].parentId);
            Assert.AreEqual("Barony A", divisions[1].name);
            Assert.AreEqual("county", divisions[0].titleId);

            Assert.IsNull(_realmQuery.GetRealm(999));
            Assert.AreEqual(0, _realmQuery.GetAdminDivisions(999).Count);
            Assert.AreEqual(2, _realmQuery.GetAllRealms().Count);

            RealmSnapshotDto nomadic = _realmQuery.GetRealm(RealmB);
            Assert.AreEqual(2, nomadic.governmentForm);
            Assert.AreEqual(-1, nomadic.capitalAnchorId);
        }

        [Test]
        public void SettlementQuery_MatchesGameWorld_AndFiltersByRealmAndRadius()
        {
            SettlementSnapshotDto dto = _settlementQuery.GetSettlement(CapitalAnchor);
            SettlementData data = _world.settlements[CapitalAnchor];

            Assert.IsNotNull(dto);
            Assert.AreEqual(data.anchorId, dto.anchorId);
            Assert.AreEqual(data.settlementName, dto.settlementName);
            Assert.AreEqual(data.tileIndex, dto.tileIndex);
            Assert.AreEqual(RealmA, dto.realmId);
            Assert.AreEqual(data.population, dto.population, 0.0001f);
            Assert.AreEqual(data.wealth, dto.wealth, 0.0001f);
            Assert.AreEqual(data.garrison, dto.garrison);
            Assert.AreEqual(data.garrisonQuantity, dto.garrisonQuantity);
            Assert.AreEqual(data.constructionTier, dto.constructionTier);
            Assert.AreEqual((int)data.siteMode, dto.siteMode);
            Assert.AreEqual((int)data.settlementForm, dto.settlementForm);
            Assert.AreEqual((int)data.settlementLevel, dto.settlementLevel);
            Assert.IsTrue(dto.isCapital);
            Assert.IsFalse(dto.isPort);
            Assert.IsFalse(dto.isRuined);
            Assert.AreEqual(3, dto.majorityCultureId);
            Assert.AreEqual(5, dto.stateReligionId);

            SettlementSnapshotDto ruined = _settlementQuery.GetSettlement(OtherAnchor);
            Assert.AreEqual(RealmB, ruined.realmId);
            Assert.AreEqual((int)SiteMode.FortifiedOutpost, ruined.siteMode);
            Assert.IsTrue(ruined.isRuined);

            Assert.AreEqual(1, _settlementQuery.GetSettlementsInRealm(RealmA).Count);
            Assert.AreEqual(1, _settlementQuery.GetSettlementsInRealm(RealmB).Count);
            Assert.AreEqual(0, _settlementQuery.GetSettlementsInRealm(999).Count);
            Assert.AreEqual(1, _settlementQuery.GetSettlementsNearTile(5, 0).Count);
            Assert.AreEqual(2, _settlementQuery.GetSettlementsNearTile(5, 1).Count);
            Assert.AreEqual(0, _settlementQuery.GetSettlementsNearTile(5, -1).Count);
            Assert.AreEqual(0, _settlementQuery.GetSettlementsNearTile(-1, 1).Count);
            Assert.IsNull(_settlementQuery.GetSettlement(999));
        }

        [Test]
        public void CharacterQuery_MatchesManager_RulerOfficeAndRealmFiltering()
        {
            CharacterRowDto ruler = _characterQuery.GetCharacter(RulerA);
            CharacterData data = _world.GetCharacterManager().GetCharacter(RulerA);

            Assert.IsNotNull(ruler);
            Assert.AreEqual(data.characterId, ruler.characterId);
            Assert.AreEqual(data.fullName, ruler.name);
            Assert.AreEqual(data.age, ruler.age);
            Assert.AreEqual(data.isMale, ruler.isMale);
            Assert.AreEqual(RealmA, ruler.realmId);
            Assert.AreEqual((int)CharacterRole.Ruler, ruler.role);
            Assert.AreEqual(-1, ruler.officeTitleId);
            Assert.AreNotEqual(0, ruler.flags & CharacterRowDto.FlagAlive);
            Assert.AreNotEqual(0, ruler.flags & CharacterRowDto.FlagRuler);
            Assert.AreNotEqual(0, ruler.flags & CharacterRowDto.FlagPublicFigure);

            CharacterRowDto noble = _characterQuery.GetCharacter(NobleA);
            Assert.AreEqual(77, noble.officeTitleId);
            Assert.AreEqual((int)CharacterRole.Noble, noble.role);

            CharacterRowDto realmARuler = _characterQuery.GetRuler(RealmA);
            Assert.IsNotNull(realmARuler);
            Assert.AreEqual(RulerA, realmARuler.characterId);
            CharacterRowDto realmBRuler = _characterQuery.GetRuler(RealmB);
            Assert.AreEqual(RulerB, realmBRuler.characterId);

            IReadOnlyList<CharacterRowDto> realmACharacters = _characterQuery.GetCharactersInRealm(RealmA);
            Assert.AreEqual(2, realmACharacters.Count);
            Assert.AreEqual(RulerA, realmACharacters[0].characterId);
            Assert.AreEqual(NobleA, realmACharacters[1].characterId);
            Assert.AreEqual(0, _characterQuery.GetCharactersInRealm(999).Count);
            Assert.IsNull(_characterQuery.GetCharacter(999));
            Assert.IsNull(_characterQuery.GetRuler(999));
        }

        [Test]
        public void Queries_EmptyWorld_AreNullSafe()
        {
            var go = new GameObject("EmptyUiQueryWorld");
            try
            {
                GameWorld empty = go.AddComponent<GameWorld>();
                empty.tiles = null;
                empty.realms = null;
                empty.settlements = null;

                var realmQuery = new RealmQueryAdapter(empty);
                var settlementQuery = new SettlementQueryAdapter(empty);
                var characterQuery = new CharacterQueryAdapter(empty);

                Assert.IsNull(realmQuery.GetRealm(0));
                Assert.AreEqual(0, realmQuery.GetAllRealms().Count);
                Assert.AreEqual(0, realmQuery.GetAdminDivisions(0).Count);
                Assert.IsNull(settlementQuery.GetSettlement(0));
                Assert.AreEqual(0, settlementQuery.GetSettlementsInRealm(0).Count);
                Assert.AreEqual(0, settlementQuery.GetSettlementsNearTile(0, 2).Count);
                Assert.IsNull(characterQuery.GetCharacter(0));
                Assert.AreEqual(0, characterQuery.GetCharactersInRealm(0).Count);
                Assert.IsNull(characterQuery.GetRuler(0));

                var nullRealmQuery = new RealmQueryAdapter(null);
                Assert.IsNull(nullRealmQuery.GetRealm(0));
                Assert.AreEqual(0, nullRealmQuery.GetAllRealms().Count);
                Assert.IsNull(new SettlementQueryAdapter(null).GetSettlement(0));
                Assert.IsNull(new CharacterQueryAdapter(null).GetRuler(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
