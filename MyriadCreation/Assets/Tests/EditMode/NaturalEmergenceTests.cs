using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P5：默认世界只有地理/聚落/人口，不预置政权；政权由聚落人口、开发与仓储剩余自然形成。
    /// </summary>
    public class NaturalEmergenceTests
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
        }

        private GameWorld NewWorld(int seed = 131)
        {
            var go = new GameObject("NaturalWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateDefault();
            world.InitializeWorld();
            world.GenerateTerrain(seed);
            return world;
        }

        private static SettlementData FirstVillage(GameWorld world)
        {
            foreach (SettlementData settlement in world.settlements.Values)
            {
                if (!settlement.IsRuined && settlement.settlementCategory != SettlementCategory.Outpost) return settlement;
            }
            return null;
        }

        private static void MakeEligible(GameWorld world, SettlementData settlement, float population = 300f, int ageDays = 9 * 365)
        {
            TileData tile = world.tiles[settlement.tileIndex];
            tile.populationBlocks = new List<PopulationBlock>
            {
                new PopulationBlock { count = population, raceId = 0, cultureId = -1, faithId = -1, socialClass = GameEnums.SocialClass.Peasant, satisfaction = 60f }
            };
            world.tiles[settlement.tileIndex] = tile;
            settlement.development = 10f;
            settlement.foundingTick = world.currentDay - ageDays;

            int regionId = tile.regionId;
            if (world.tradeCenters.TryGetValue(regionId, out TradeCenter center))
            {
                center.inventory.Clear();
                center.inventory[0] = population; // 0 号小麦：足够 90 天以上口粮
            }
        }

        [Test]
        public void DefaultWorld_HasSettlementsButNoPolitiesCharactersOrOffices()
        {
            var world = NewWorld();

            Assert.AreEqual(0, world.realms.Count, "默认世界不得预置政权");
            Assert.AreEqual(0, world.Characters.GetTotalCharacterCount(), "默认世界不得预置君主/宫廷");
            Assert.Greater(world.settlements.Count, 0, "默认世界应有聚落（人群），只是没有国家");
            foreach (RealmData realm in world.realms.Values)
            {
                Assert.IsTrue(realm.officeHolders == null || realm.officeHolders.Count == 0);
            }
        }

        [Test]
        public void EligibleSettlement_FormsRealmWithFoundingHousehold()
        {
            var world = NewWorld();
            SettlementData settlement = FirstVillage(world);
            Assert.IsNotNull(settlement, "前置：应生成聚落");
            MakeEligible(world, settlement);

            bool formed = StateFormationSystem.MonthlyTick(world);

            Assert.IsTrue(formed, "人口/开发/口粮剩余满足时应形成政权");
            Assert.AreEqual(1, world.realms.Count);
            RealmData realm = null;
            foreach (RealmData value in world.realms.Values) realm = value;
            Assert.IsNotNull(realm);
            Assert.GreaterOrEqual(realm.monarchId, 0, "政权必须有真实首领");
            Assert.AreEqual(realm.realmId, world.tiles[settlement.tileIndex].ownerRealmId, "聚落地块应归新政权");
            Assert.AreEqual(0f, realm.treasury, 0.001f, "自然政权不得开局送国库货币");
            Assert.IsTrue(realm.officeHolders == null || realm.officeHolders.Count == 0, "官职应由后续制度门槛补，不在建国当天填满");
            Assert.GreaterOrEqual(world.Characters.GetTotalCharacterCount(), 2, "至少应有首领与家庭/地方显贵");
        }

        [Test]
        public void NoFoodSurplus_DoesNotFormRealm()
        {
            var world = NewWorld();
            SettlementData settlement = FirstVillage(world);
            Assert.IsNotNull(settlement);
            MakeEligible(world, settlement);
            TradeCenter center = world.tradeCenters[world.tiles[settlement.tileIndex].regionId];
            center.inventory.Clear();

            Assert.IsFalse(StateFormationSystem.MonthlyTick(world), "没有口粮剩余不得建国");
            Assert.AreEqual(0, world.realms.Count);
        }

        [Test]
        public void TooYoungSettlement_DoesNotFormRealm()
        {
            var world = NewWorld();
            SettlementData settlement = FirstVillage(world);
            Assert.IsNotNull(settlement);
            MakeEligible(world, settlement, ageDays: 365);

            Assert.IsFalse(StateFormationSystem.MonthlyTick(world), "聚落存在不足 8 年不得建国");
            Assert.AreEqual(0, world.realms.Count);
        }

        [Test]
        public void OfficeThresholds_RequireTerritoryAndPopulation()
        {
            OfficeDef governor = new OfficeDef { minTiles = 2, minPopulation = 150f, minCentralization = 0.05f };
            Assert.IsFalse(OfficeSystem.MeetsInstitutionalThreshold(governor, new OfficeRealmContext
            {
                ownedTiles = 1, population = 300f, centralization = 0.3f, cultureStage = 4
            }), "只有一块地时不设地方长官");
            Assert.IsTrue(OfficeSystem.MeetsInstitutionalThreshold(governor, new OfficeRealmContext
            {
                ownedTiles = 2, population = 300f, centralization = 0.3f, cultureStage = 4
            }), "领土与人口达标后才设官");
        }

        [Test]
        public void SpouseAndCommoner_AreNotOfficeCandidates()
        {
            var manager = new CharacterManager();
            CharacterData ruler = manager.CreateCharacter("君", "氏", 40, true, 0, 0, 0, CharacterRole.Ruler);
            CharacterData spouse = manager.CreateCharacter("后", "氏", 35, false, 0, 0, 0, CharacterRole.Spouse);
            CharacterData commoner = manager.CreateCharacter("民", "氏", 30, true, 0, 0, 0, CharacterRole.Commoner);
            OfficeDef def = new OfficeDef();

            Assert.IsFalse(OfficeSystem.IsEligible(spouse, ruler, def));
            Assert.IsFalse(OfficeSystem.IsEligible(commoner, ruler, def));
            Assert.IsFalse(OfficeSystem.IsEligible(ruler, ruler, def));
        }
    }
}
