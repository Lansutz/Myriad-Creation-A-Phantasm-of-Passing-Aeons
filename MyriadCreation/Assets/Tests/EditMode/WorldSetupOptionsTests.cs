using NUnit.Framework;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>新世界初始内容档位与自然生成开关。</summary>
    public class WorldSetupOptionsTests
    {
        [Test]
        public void Default_HasSettlementsButNoPolitiesOrUnits()
        {
            var options = WorldSetupOptions.CreateDefault();
            Assert.AreEqual(0, options.initialPolityCount);
            Assert.AreEqual(-1, options.initialSettlementCount, "默认按省生成聚落：有人群，但没有国家");
            Assert.AreEqual(0, options.initialUnitCount);
            Assert.IsFalse(options.generatePolities, "默认世界不得预置政权");
            Assert.IsTrue(options.generateSettlements, "聚落是人群，不是国家");
            Assert.IsFalse(options.generateInitialUnits);
            Assert.IsTrue(options.spawnRates.enableNaturalSpawning);
        }

        [Test]
        public void Standard_HasThreePolitiesAndSettlements()
        {
            var options = WorldSetupOptions.CreateStandard();
            Assert.AreEqual(InitialContentMode.Standard, options.initialContent);
            Assert.IsTrue(options.generatePolities);
            Assert.IsTrue(options.generateSettlements);
            Assert.AreEqual(3, options.initialPolityCount);
            Assert.IsTrue(options.spawnRates.enableNaturalSpawning);
        }

        [Test]
        public void Empty_DisablesEverythingExceptGeography()
        {
            var options = WorldSetupOptions.Create(InitialContentMode.Empty);
            Assert.IsFalse(options.generatePolities);
            Assert.IsFalse(options.generateSettlements);
            Assert.IsFalse(options.generateInitialUnits);
            Assert.AreEqual(0, options.initialPolityCount);
            Assert.IsFalse(options.spawnRates.enableNaturalSpawning);
        }

        [Test]
        public void Minimal_OnePolityAndSmallPopulation()
        {
            var options = WorldSetupOptions.Create(InitialContentMode.Minimal);
            Assert.AreEqual(1, options.initialPolityCount);
            Assert.AreEqual(0.25f, options.initialPopulationScale);
        }

        [Test]
        public void Custom_PreservesManualFlags()
        {
            var options = WorldSetupOptions.Create(InitialContentMode.Custom);
            options.generatePolities = false;
            options.initialPolityCount = 7;
            options.Normalize();
            Assert.IsFalse(options.generatePolities, "Custom 不应被 Normalize 覆盖");
            Assert.AreEqual(7, options.initialPolityCount);
        }

        [Test]
        public void Normalize_ClampsRanges()
        {
            var options = new WorldSetupOptions
            {
                initialContent = InitialContentMode.Custom,
                initialPolityCount = 99,
                initialPopulationScale = 9f,
            };
            options.spawnRates.mapActorMultiplier = 9f;
            options.Normalize();
            Assert.AreEqual(12, options.initialPolityCount);
            Assert.AreEqual(3f, options.initialPopulationScale);
            Assert.AreEqual(3f, options.spawnRates.mapActorMultiplier);
        }
    }
}
