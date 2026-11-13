using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>旧 SocialClass 与新四轴的桥接：人口块解析、汇总、社会画像兼容。</summary>
    public class SocialClassBridgeTests
    {
        private static PopulationBlock Block(GameEnums.SocialClass cls, float count,
            EconomicClass? economicClass = null, SocialStratum? stratum = null,
            OccupationFlags occupations = OccupationFlags.None)
        {
            return new PopulationBlock
            {
                socialClass = cls,
                count = count,
                economicClass = economicClass ?? EconomicClass.Freeholder,
                stratum = stratum ?? SocialStratum.Middle,
                occupations = occupations,
                raceId = 0,
                cultureId = 0,
                faithId = 0,
                satisfaction = 55f,
            };
        }

        [Test]
        public void Resolve_LegacyBlock_UsesLegacyMapping()
        {
            var slave = Block(GameEnums.SocialClass.Slave, 10f);
            var profile = PopulationIdentity.Resolve(slave);
            Assert.AreEqual(EconomicClass.Slave, profile.economicClass);
            Assert.AreEqual(SocialStratum.Lower, profile.stratum);

            var peasant = Block(GameEnums.SocialClass.Peasant, 10f);
            Assert.AreEqual(EconomicClass.Freeholder, PopulationIdentity.Resolve(peasant).economicClass);

            var noble = Block(GameEnums.SocialClass.NobilityClergy, 10f);
            var nobleProfile = PopulationIdentity.Resolve(noble);
            Assert.AreEqual(EconomicClass.Lord, nobleProfile.economicClass);
            Assert.AreEqual(SocialStratum.Upper, nobleProfile.stratum);
        }

        [Test]
        public void Resolve_NewAxisBlock_UsesNewFields()
        {
            var tenant = Block(GameEnums.SocialClass.Peasant, 10f,
                EconomicClass.Tenant, SocialStratum.LowerMiddle, OccupationFlags.Farmer | OccupationFlags.Artisan);
            var profile = PopulationIdentity.Resolve(tenant);
            Assert.AreEqual(EconomicClass.Tenant, profile.economicClass);
            Assert.AreEqual(SocialStratum.LowerMiddle, profile.stratum);
            Assert.IsTrue(profile.HasOccupation(OccupationFlags.Artisan));
        }

        [Test]
        public void Add_AccumulatesByClassAndStratum()
        {
            var byClass = new Dictionary<EconomicClass, float>();
            var byStratum = new Dictionary<SocialStratum, float>();
            PopulationIdentity.Add(Block(GameEnums.SocialClass.Peasant, 60f), byClass, byStratum);
            PopulationIdentity.Add(Block(GameEnums.SocialClass.Slave, 30f), byClass, byStratum);
            PopulationIdentity.Add(Block(GameEnums.SocialClass.MerchantFreeman, 10f,
                EconomicClass.SelfEmployed, SocialStratum.Middle, OccupationFlags.Merchant), byClass, byStratum);

            Assert.AreEqual(60f, byClass[EconomicClass.Freeholder]);
            Assert.AreEqual(30f, byClass[EconomicClass.Slave]);
            Assert.AreEqual(10f, byClass[EconomicClass.SelfEmployed]);
            Assert.AreEqual(60f, byStratum[SocialStratum.LowerMiddle]);
            Assert.AreEqual(30f, byStratum[SocialStratum.Lower]);
            Assert.AreEqual(10f, byStratum[SocialStratum.Middle]);
        }

        [Test]
        public void ApplyLegacy_WritesNewAxes()
        {
            var block = Block(GameEnums.SocialClass.Slave, 5f);
            PopulationIdentity.ApplyLegacy(GameEnums.SocialClass.Slave, ref block);
            Assert.AreEqual(EconomicClass.Slave, block.economicClass);
            Assert.AreEqual(SocialStratum.Lower, block.stratum);
        }

        [Test]
        public void SocietyManager_AggregatesNewAxes()
        {
            var tiles = new[]
            {
                new TileData
                {
                    exists = true, isLand = true, ownerRealmId = 1,
                    populationBlocks = new List<PopulationBlock>
                    {
                        Block(GameEnums.SocialClass.Peasant, 100f),
                        Block(GameEnums.SocialClass.Slave, 20f),
                    }
                },
                new TileData
                {
                    exists = true, isLand = true, ownerRealmId = 1,
                    populationBlocks = new List<PopulationBlock>
                    {
                        Block(GameEnums.SocialClass.MerchantFreeman, 30f,
                            EconomicClass.SelfEmployed, SocialStratum.Middle, OccupationFlags.Merchant),
                    }
                },
            };
            var realm = new RealmData { realmId = 1 };
            realm.coreTiles.Add(0);
            realm.coreTiles.Add(1);
            var society = new SocietyManager().EvaluateRealm(realm, tiles, new RealmSituation());

            Assert.AreEqual(150f, society.totalPopulation, 0.01f);
            Assert.AreEqual(100f, society.populationByEconomicClass[EconomicClass.Freeholder], 0.01f);
            Assert.AreEqual(20f, society.populationByEconomicClass[EconomicClass.Slave], 0.01f);
            Assert.AreEqual(30f, society.populationByEconomicClass[EconomicClass.SelfEmployed], 0.01f);
            Assert.AreEqual(EconomicClass.Freeholder, society.dominantEconomicClass);
            Assert.AreEqual(100f, society.populationByStratum[SocialStratum.LowerMiddle], 0.01f);
        }

        [Test]
        public void ClassProfile_CarriesMappedAxes()
        {
            var tiles = new[]
            {
                new TileData
                {
                    exists = true, isLand = true, ownerRealmId = 1,
                    populationBlocks = new List<PopulationBlock>
                    {
                        Block(GameEnums.SocialClass.NobilityClergy, 10f),
                    }
                }
            };
            var realm = new RealmData { realmId = 1 };
            realm.coreTiles.Add(0);
            var society = new SocietyManager().EvaluateRealm(realm, tiles, new RealmSituation());
            Assert.IsTrue(society.classes.TryGetValue(GameEnums.SocialClass.NobilityClergy, out var profile));
            Assert.AreEqual(EconomicClass.Lord, profile.economicClass);
            Assert.AreEqual(SocialStratum.Upper, profile.stratum);
        }
    }
}
