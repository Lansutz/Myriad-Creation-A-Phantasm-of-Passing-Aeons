using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.UI.Common;
using MyriadCreation.World;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// UI 4e：ITileInfoQuery / ICharacterDetailQuery 适配器 + UIManager 地块/角色面板接线测试。
    /// 覆盖 DTO 与 GameWorld/CharacterManager 一致、越界/空世界安全、UI 无 view 显示 "-" 且不抛。
    /// </summary>
    public class UiTileCharacterPanelsTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();
        private GameWorld _world;
        private WorldReadModel _model;
        private TileInfoQueryAdapter _tileQuery;
        private CharacterDetailQueryAdapter _characterQuery;

        private const int RealmId = 1;
        private const int TargetTile = 2;
        private const int CharacterId = 100;
        private const int FatherId = 200;
        private const int MotherId = 201;
        private const int SpouseId = 202;
        private const int ChildId = 203;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("UiTileCharacterWorld");
            _created.Add(go);
            _world = go.AddComponent<GameWorld>();
            _world.mapWidth = 3;
            _world.mapHeight = 2;
            _world.tiles = new TileData[6];
            for (int i = 0; i < _world.tiles.Length; i++)
            {
                _world.tiles[i] = new TileData
                {
                    tileIndex = i,
                    exists = true,
                    isLand = i != 5,
                    elevation01 = i * 0.1f,
                    slopeDegree = i,
                    biome = GameEnums.BiomeType.Savanna,
                    climateZone = GameEnums.ClimateZone.TemperateMild,
                    annualTemp = 12f,
                    annualPrecipMm = 800f,
                    fertility = 0.7f,
                    ownerRealmId = RealmId,
                    provinceId = 0,
                    development = 0.4f,
                    stability = 60f,
                    populationBlocks = new List<PopulationBlock>
                    {
                        new PopulationBlock { count = 10f, cultureId = 3, faithId = 5 }
                    },
                    resources = new List<TileResource>()
                };
            }
            _world.tiles[TargetTile].resources.Add(new TileResource
            {
                goodsId = 42,
                abundance = 0.8f,
                discovered = true,
                developed = false
            });

            _world.realms = new Dictionary<int, RealmData>
            {
                [RealmId] = new RealmData { realmId = RealmId, realmName = "Test Realm" }
            };
            _world.provinces = new Dictionary<int, Province>
            {
                [0] = new Province { provinceId = 0, provinceName = "Test Province" }
            };
            if (_world.cultures == null) _world.cultures = new Dictionary<int, CultureData>();
            _world.cultures[3] = new CultureData { cultureId = 3, cultureName = "Test Culture" };

            AnchorData anchor = new AnchorData { anchorId = 9, tileIndex = TargetTile };
            SettlementData settlement = new SettlementData { anchorId = 9, siteName = "Test Town" };
            settlement.anchor = anchor;
            _world.anchors = new Dictionary<int, AnchorData> { [9] = anchor };
            _world.settlements = new Dictionary<int, SettlementData> { [9] = settlement };
            if (_world.goodsDefs == null) _world.goodsDefs = new Dictionary<int, GoodsDef>();
            _world.goodsDefs[42] = new GoodsDef { goodsId = 42, goodsName = "Iron" };

            InjectCharacters();
            _tileQuery = new TileInfoQueryAdapter(_world);
            _characterQuery = new CharacterDetailQueryAdapter(_world);
            _model = new WorldReadModel(_world);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
            _created.Clear();
        }

        private void InjectCharacters()
        {
            CharacterManager manager = new CharacterManager();
            typeof(GameWorld).GetField("_characterManager", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_world, manager);
            var charsField = typeof(CharacterManager).GetField("_characters",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var chars = (Dictionary<int, CharacterData>)charsField.GetValue(manager);
            chars[FatherId] = new CharacterData
            {
                characterId = FatherId,
                firstName = "Father",
                lastName = "Test",
                age = 60,
                isMale = true,
                realmId = RealmId,
                deathDay = -1
            };
            chars[MotherId] = new CharacterData
            {
                characterId = MotherId,
                firstName = "Mother",
                lastName = "Test",
                age = 58,
                isMale = false,
                realmId = RealmId,
                deathDay = -1
            };
            chars[SpouseId] = new CharacterData
            {
                characterId = SpouseId,
                firstName = "Spouse",
                lastName = "Test",
                age = 35,
                isMale = false,
                realmId = RealmId,
                deathDay = -1
            };
            chars[ChildId] = new CharacterData
            {
                characterId = ChildId,
                firstName = "Child",
                lastName = "Test",
                age = 10,
                isMale = true,
                realmId = RealmId,
                fatherId = CharacterId,
                motherId = SpouseId,
                deathDay = -1
            };
            CharacterData target = new CharacterData
            {
                characterId = CharacterId,
                firstName = "Target",
                lastName = "Hero",
                age = 40,
                isMale = true,
                realmId = RealmId,
                role = CharacterRole.Ruler,
                fatherId = FatherId,
                motherId = MotherId,
                spouseId = SpouseId,
                cultureId = 3,
                faithId = 5,
                boldness = 80f,
                compassion = 60f,
                traits = new List<PersonalityTrait>
                {
                    new PersonalityTrait { traitId = "brave", traitName = "勇敢" }
                },
                prowess = 70f,
                social = 60f,
                military = 55f,
                management = 50f,
                conspiracy = 45f,
                scholarship = 40f,
                prestige = 80f,
                health = 90f,
                stress = 12f,
                spiritualFulfillment = 66f,
                deathDay = -1
            };
            chars[CharacterId] = target;
            manager.CreateFamily("Test Dynasty", CharacterId, 1, RealmId);
            _world.realms[RealmId].officeHolders[77] = CharacterId;
        }

        private static void SetField(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private static void Invoke(object target, string method)
        {
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(target, null);
        }

        private TMP_Text CreateText()
        {
            var go = new GameObject("UiText");
            _created.Add(go);
            return go.AddComponent<TextMeshProUGUI>();
        }

        [Test]
        public void TileInfoDto_MatchesGameWorld_AndBoundsSafe()
        {
            TileInfoDto dto = _tileQuery.GetTileInfo(TargetTile);
            Assert.IsNotNull(dto);
            Assert.AreEqual(TargetTile, dto.tileIndex);
            Assert.AreEqual(TargetTile % 3, dto.x);
            Assert.AreEqual(TargetTile / 3, dto.y);
            Assert.AreEqual("Test Realm", dto.ownerRealmName);
            Assert.AreEqual("Test Province", dto.provinceName);
            Assert.AreEqual("Test Town", dto.settlementName);
            Assert.AreEqual(9, dto.settlementAnchorId);
            Assert.AreEqual(10f, dto.population, 0.0001f);
            Assert.AreEqual(1, dto.resources.Count);
            Assert.AreEqual("Iron", dto.resources[0].goodsName);
            Assert.AreEqual(GameEnums.BiomeType.Savanna.ToString(), dto.biomeName);

            Assert.IsNull(_tileQuery.GetTileInfo(-1));
            Assert.IsNull(_tileQuery.GetTileInfo(999));
            Assert.IsNull(new TileInfoQueryAdapter(null).GetTileInfo(0));
        }

        [Test]
        public void CharacterDetailDto_MatchesManager_AndUnknownSafe()
        {
            CharacterDetailDto dto = _characterQuery.GetCharacterDetail(CharacterId);
            Assert.IsNotNull(dto);
            Assert.AreEqual("Target Hero", dto.name);
            Assert.AreEqual(40, dto.age);
            Assert.AreEqual(RealmId, dto.realmId);
            Assert.AreEqual("Test Realm", dto.realmName);
            Assert.AreEqual((int)CharacterRole.Ruler, dto.roleId);
            Assert.AreEqual("统治者", dto.roleName);
            Assert.AreEqual(77, dto.officeTitleId);
            Assert.AreEqual("Test Culture", dto.cultureName);
            Assert.AreEqual(70f, dto.prowess, 0.0001f);
            Assert.AreEqual(66f, dto.piety, 0.0001f);
            Assert.AreEqual(1, dto.familyId);
            Assert.AreEqual("Test Dynasty", dto.dynastyName);
            Assert.AreEqual("Test Dynasty", dto.familyName);
            Assert.IsTrue(dto.isRuler);
            Assert.IsFalse(dto.isHeir);
            Assert.IsFalse(string.IsNullOrWhiteSpace(dto.personality));
            CollectionAssert.Contains(dto.traits, "勇敢");
            Assert.IsNotNull(dto.siblings);
            Assert.AreEqual("Father Test", dto.father.name);
            Assert.AreEqual("Mother Test", dto.mother.name);
            Assert.AreEqual("Spouse Test", dto.spouse.name);
            Assert.AreEqual(1, dto.children.Count);
            Assert.AreEqual("Child Test", dto.children[0].name);

            Assert.IsNull(_characterQuery.GetCharacterDetail(999));
            Assert.IsNull(new CharacterDetailQueryAdapter(null).GetCharacterDetail(0));
        }

        [Test]
        public void WorldReadModel_ExposesTileAndCharacterDetail_AndNullSafe()
        {
            Assert.IsInstanceOf<ITileInfoQuery>(_model);
            Assert.IsInstanceOf<ICharacterDetailQuery>(_model);
            Assert.AreEqual(TargetTile, _model.GetTileInfo(TargetTile).tileIndex);
            Assert.AreEqual("Target Hero", _model.GetCharacterDetail(CharacterId).name);
            Assert.IsNull(_model.GetTileInfo(-1));
            Assert.IsNull(_model.GetCharacterDetail(999));

            var nullModel = new WorldReadModel(null);
            Assert.IsNull(nullModel.GetTileInfo(0));
            Assert.IsNull(nullModel.GetCharacterDetail(0));
        }

        [Test]
        public void UIManager_WithoutView_ShowsDash_AndDoesNotThrow()
        {
            var uiGo = new GameObject("UiTileCharacterUIManager");
            _created.Add(uiGo);
            var ui = uiGo.AddComponent<UIManager>();
            var tileName = CreateText();
            SetField(ui, "tileNameText", tileName);
            ui.SetSelectedTile(TargetTile);

            Assert.DoesNotThrow(() => Invoke(ui, "UpdateTileInfo"));
            Assert.AreEqual("-", tileName.text);
            Assert.DoesNotThrow(() => Invoke(ui, "OpenCharacterPanel"));
        }

        [Test]
        public void UIManager_WithView_UsesTileDto()
        {
            var uiGo = new GameObject("UiTileCharacterUIManagerView");
            _created.Add(uiGo);
            var ui = uiGo.AddComponent<UIManager>();
            var tileName = CreateText();
            var tileTerrain = CreateText();
            var tileEconomy = CreateText();
            SetField(ui, "tileNameText", tileName);
            SetField(ui, "tileTerrainText", tileTerrain);
            SetField(ui, "tileEconomyText", tileEconomy);
            SetField(ui, "_viewRealmId", RealmId);
            ui.SetWorldReadModel(_model);
            ui.SetSelectedTile(TargetTile);

            Assert.DoesNotThrow(() => Invoke(ui, "UpdateTileInfo"));
            StringAssert.Contains("Test Realm", tileName.text);
            StringAssert.Contains("Test Town", tileEconomy.text);
        }
    }
}
