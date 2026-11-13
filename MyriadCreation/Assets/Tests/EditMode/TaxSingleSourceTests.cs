using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P1.2 财政单源：税率只看 `realm.taxSystem`，税基接人口与控制度（秩序代理），
    /// 不再让所有政权共用一份全局税率。
    /// </summary>
    public class TaxSingleSourceTests
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

        private GameWorld NewWorld()
        {
            var go = new GameObject("TaxWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(47);
            return world;
        }

        private static RealmData FirstRealm(GameWorld world)
        {
            foreach (var realm in world.realms.Values) return realm;
            return null;
        }

        private static void ZeroRates(TaxSystem tax)
        {
            tax.agriculturalTax = 0f;
            tax.headTax = 0f;
            tax.tradeTax = 0f;
            tax.miningTax = 0f;
            tax.craftTax = 0f;
            tax.livestockTax = 0f;
            tax.luxuryTax = 0f;
            tax.saltMonopolyTax = 0f;
            tax.wartimeSpecialTax = 0f;
            tax.taxExemptions?.Clear();
        }

        [Test]
        public void RealmTaxSystem_IsTheOnlyRateSource()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm, "前置：应有政权");
            ZeroRates(realm.taxSystem);

            float zeroed = world.GetEconomyManager().SettleTaxes(realm.realmId);

            Assert.AreEqual(0f, zeroed, 0.001f, "政权税率为 0 时不得回退到全局税率");

            realm.taxSystem.agriculturalTax = 0.5f;
            realm.taxSystem.headTax = 0.5f;
            float taxed = world.GetEconomyManager().SettleTaxes(realm.realmId);

            Assert.Greater(taxed, 0f, "政权自己的税率应生效");
        }

        [Test]
        public void TaxBase_RisesWithPopulation()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm, "前置：应有政权");
            ZeroRates(realm.taxSystem);
            realm.taxSystem.agriculturalTax = 0.3f;

            int sampleTile = -1;
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (world.tiles[i].isLand && world.tiles[i].ownerRealmId == realm.realmId)
                {
                    sampleTile = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(sampleTile, 0, "前置：应有政权地块");

            TileData tile = world.tiles[sampleTile];
            tile.populationBlocks = new List<PopulationBlock>();
            world.tiles[sampleTile] = tile;
            float emptyTax = world.GetEconomyManager().SettleTaxes(realm.realmId);

            tile = world.tiles[sampleTile];
            tile.populationBlocks = new List<PopulationBlock>
            {
                new PopulationBlock
                {
                    count = 1000f,
                    raceId = 0,
                    cultureId = -1,
                    faithId = -1,
                    socialClass = GameEnums.SocialClass.Peasant,
                    satisfaction = 60f
                }
            };
            world.tiles[sampleTile] = tile;
            float populatedTax = world.GetEconomyManager().SettleTaxes(realm.realmId);

            Assert.Greater(populatedTax, emptyTax, "人口是税基的一部分");
        }

        [Test]
        public void TaxBase_RisesWithOrder()
        {
            var world = NewWorld();
            RealmData realm = FirstRealm(world);
            Assert.IsNotNull(realm, "前置：应有政权");
            ZeroRates(realm.taxSystem);
            realm.taxSystem.agriculturalTax = 0.3f;

            int sampleTile = -1;
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (world.tiles[i].isLand && world.tiles[i].ownerRealmId == realm.realmId)
                {
                    sampleTile = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(sampleTile, 0, "前置：应有政权地块");

            TileData tile = world.tiles[sampleTile];
            tile.populationBlocks = new List<PopulationBlock>
            {
                new PopulationBlock { count = 500f, socialClass = GameEnums.SocialClass.Peasant, satisfaction = 60f }
            };
            tile.order = 0f;
            world.tiles[sampleTile] = tile;
            float lowOrderTax = world.GetEconomyManager().SettleTaxes(realm.realmId);

            tile = world.tiles[sampleTile];
            tile.order = 100f;
            world.tiles[sampleTile] = tile;
            float highOrderTax = world.GetEconomyManager().SettleTaxes(realm.realmId);

            Assert.Greater(highOrderTax, lowOrderTax, "秩序（控制度代理）应影响税基");
        }
    }
}
