using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P5：世界生成不再自动造宫廷/填官职；税收由行政区划持有人从地方仓储征实物。
    /// </summary>
    public class CourtAndTaxGateTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
            TreasuryLedger.ClearHistory();
        }

        private GameWorld NewWorld()
        {
            var go = new GameObject("CourtWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(131);
            return world;
        }

        private static RealmData FirstRealm(GameWorld world)
        {
            foreach (RealmData realm in world.realms.Values) return realm;
            return null;
        }

        private static CharacterData SeedMatureCourt(GameWorld world, RealmData realm)
        {
            world.Characters.CreateInitialCourtiers(world.realms, world.currentYear);
            OfficeSystem.FillVacancies(realm, world.Characters, world.currentYear * 365 + world.currentDay);
            CharacterData official = null;
            foreach (CharacterData character in world.Characters.GetCharactersByRealm(realm.realmId))
            {
                if (character.role != CharacterRole.Ruler && character.role != CharacterRole.Spouse)
                {
                    official = character;
                    break;
                }
            }
            Assert.IsNotNull(official);
            foreach (AdminDivision division in realm.adminDivisions)
            {
                division.holderCharacterId = official.characterId;
            }
            return official;
        }

        private static TradeCenter CapitalCenter(GameWorld world, RealmData realm)
        {
            foreach (int tileIndex in realm.coreTiles)
            {
                int regionId = world.tiles[tileIndex].regionId;
                if (world.tradeCenters.TryGetValue(regionId, out TradeCenter center)) return center;
            }
            return null;
        }

        private static TaxCollectionResult Collect(GameWorld world, RealmData realm)
        {
            return world.GetEconomyManager().CollectTaxes(
                realm.realmId, world.Characters, world.currentYear * 365 + world.currentDay);
        }

        [Test]
        public void NewRealm_HasNoAutoCourtOrOffices()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm);
            int extra = 0;
            foreach (CharacterData character in world.Characters.GetCharactersByRealm(realm.realmId))
            {
                if (character.role != CharacterRole.Ruler && character.role != CharacterRole.Spouse) extra++;
            }
            Assert.AreEqual(0, extra, "世界生成不得再凭空造 3 名宫廷人物");
            Assert.IsTrue(realm.officeHolders == null || realm.officeHolders.Count == 0, "生成结束不得自动填官职");
        }

        [Test]
        public void MatureRealm_OfficesFilledIncludingTaxation()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm);
            SeedMatureCourt(world, realm);
            Assert.IsTrue(realm.officeHolders != null && realm.officeHolders.Count > 0, "显式播种宫廷后才应补缺");
            Assert.IsTrue(OfficeSystem.HasModuleHolder(realm, OfficeSystem.ModuleTaxation, world.Characters, out _),
                "有可任职人物后应出现持 taxation 职权的官员");
        }

        [Test]
        public void TaxCollection_RequiresAdministrativeHolder()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            SeedMatureCourt(world, realm);
            TradeCenter center = CapitalCenter(world, realm);
            Assert.IsNotNull(center);
            center.inventory.Clear();
            center.inventory[0] = 1000f;

            realm.adminDivisions.Clear();
            TaxCollectionResult none = Collect(world, realm);
            Assert.AreEqual(0f, none.goodsCollected, 0.001f, "没有行政区划持有人就没有征收资格");
            Assert.AreEqual(1000f, center.inventory[0], 0.001f);
        }

        [Test]
        public void TaxCollection_MovesRealGoodsFromLocalStorageToNationalVault()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            CharacterData official = SeedMatureCourt(world, realm);
            Assert.IsNotNull(official);
            TradeCenter center = CapitalCenter(world, realm);
            center.inventory.Clear();
            center.inventory[0] = 1000f;
            float before = center.inventory[0];
            TreasuryVault vault = TreasuryLedger.NationalVault(realm);
            float vaultBefore = vault.Get(0);

            TaxCollectionResult result = Collect(world, realm);

            Assert.Greater(result.goodsCollected, 0f);
            Assert.Less(center.inventory[0], before, "地方仓储必须真实下降");
            Assert.Greater(vault.Get(0), vaultBefore, "国家仓必须真实上升");
        }

        [Test]
        public void RemoveHolder_StopsCollection_ThenRestores()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            CharacterData official = SeedMatureCourt(world, realm);
            TradeCenter center = CapitalCenter(world, realm);
            center.inventory.Clear();
            center.inventory[0] = 1000f;

            var saved = new List<AdminDivision>(realm.adminDivisions);
            realm.adminDivisions.Clear();
            Assert.AreEqual(0f, Collect(world, realm).goodsCollected, 0.001f, "没有行政区划就没有征收资格");

            realm.adminDivisions.AddRange(saved);
            foreach (AdminDivision division in realm.adminDivisions) division.holderCharacterId = official.characterId;
            Assert.Greater(Collect(world, realm).goodsCollected, 0f, "恢复行政区划与持有人后应恢复征收");
        }
    }
}
