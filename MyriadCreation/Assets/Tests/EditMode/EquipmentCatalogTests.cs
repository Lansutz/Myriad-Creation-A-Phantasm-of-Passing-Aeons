using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Content.Modding;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>具体物品目录：物品由革新解锁，革新不是物品。</summary>
    public class EquipmentCatalogTests
    {
        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
        }

        [Test]
        public void EquipmentCatalog_LoadsItems()
        {
            Assert.IsTrue(ContentRegistry.TryGetEquipment(1, out var trebuchet));
            Assert.AreEqual("配重式投石机", trebuchet.equipmentName);
            Assert.AreEqual(EquipmentCategory.SiegeEngine, trebuchet.category);
            Assert.AreEqual(305, trebuchet.requiredInnovation);

            Assert.IsTrue(ContentRegistry.TryGetEquipment(2, out var matchlock));
            Assert.AreEqual("火绳枪", matchlock.equipmentName);
            Assert.AreEqual(EquipmentCategory.Firearm, matchlock.category);
            Assert.AreEqual(307, matchlock.requiredInnovation);
        }

        [Test]
        public void InnovationUnlockRefs_ResolveToRecipes()
        {
            Assert.IsTrue(ContentRegistry.Innovations.TryGetValue(305, out var mechanism));
            Assert.IsTrue(mechanism.unlockRefs.Exists(r => r.kind == InnovationUnlockKind.Recipe && r.key == "craft_trebuchet"));
            Assert.IsTrue(RecipeCatalog.TryGet("craft_trebuchet", out var trebuchet));
            Assert.AreEqual(1, trebuchet.outputGoodsId);

            Assert.IsTrue(ContentRegistry.Innovations.TryGetValue(307, out var matchlockMechanism));
            Assert.IsTrue(matchlockMechanism.unlockRefs.Exists(r => r.kind == InnovationUnlockKind.Recipe && r.key == "craft_matchlock"));
            Assert.IsTrue(RecipeCatalog.TryGet("craft_matchlock", out var matchlock));
            Assert.AreEqual(2, matchlock.outputGoodsId);

            Assert.IsTrue(ContentRegistry.Innovations.TryGetValue(308, out var bombardCasting));
            Assert.IsTrue(bombardCasting.unlockRefs.Exists(r => r.kind == InnovationUnlockKind.Recipe && r.key == "craft_bombard"));
            Assert.IsTrue(RecipeCatalog.TryGet("craft_bombard", out var bombard));
            Assert.AreEqual(3, bombard.outputGoodsId);
        }

        [Test]
        public void MilitaryEquipment_UnlocksFromInnovations()
        {
            Assert.IsTrue(ContentRegistry.TryGetEquipment(4, out var bronzeGe));
            Assert.AreEqual("青铜戈", bronzeGe.equipmentName);
            Assert.AreEqual(201, bronzeGe.requiredInnovation);
            Assert.AreEqual(WeaponFamily.Polearm, bronzeGe.weaponFamily);
            Assert.AreEqual(CraftProcess.Casting, bronzeGe.craftProcess);
            Assert.AreEqual("范铸", bronzeGe.processDetail);
            Assert.IsTrue(ContentRegistry.TryGetEquipment(10, out var bronzeSpear));
            Assert.AreEqual(WeaponFamily.Spear, bronzeSpear.weaponFamily);
            Assert.IsTrue(ContentRegistry.TryGetEquipment(107, out var ramShip));
            Assert.AreEqual(1007, ramShip.requiredInnovation);
            Assert.AreEqual(ShipHullForm.Long, ramShip.hullForm);
            Assert.AreEqual(RamType.Sharp, ramShip.ramType);
        }

        [Test]
        public void EquipmentRequiredInnovation_Exists()
        {
            foreach (var pair in ContentRegistry.Equipment)
            {
                Assert.IsTrue(ContentRegistry.Innovations.ContainsKey(pair.Value.requiredInnovation),
                    $"物品 {pair.Key} 引用的革新 {pair.Value.requiredInnovation} 不存在");
            }
        }
    }
}
