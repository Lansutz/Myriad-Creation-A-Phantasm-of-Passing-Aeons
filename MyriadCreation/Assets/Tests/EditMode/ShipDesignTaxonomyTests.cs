using NUnit.Framework;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>船名按动力/攻击机理拆轴，而不是把文化泛称当船型。</summary>
    public class ShipDesignTaxonomyTests
    {
        [Test]
        public void Longship_IsHullForm_WithOarSailAndBoarding()
        {
            var ship = new EquipmentDef
            {
                equipmentId = 9001,
                equipmentName = "长船",
                category = EquipmentCategory.Ship,
                hullForm = ShipHullForm.Long,
                propulsion = ShipPropulsion.OarSail,
                ramType = RamType.None,
                boardingPlatform = true,
            };
            Assert.AreEqual(ShipHullForm.Long, ship.hullForm);
            Assert.AreEqual(ShipPropulsion.OarSail, ship.propulsion);
            Assert.IsTrue(ship.boardingPlatform);
        }

        [Test]
        public void RamTypes_AreDistinct()
        {
            Assert.AreNotEqual(RamType.Sharp, RamType.Hammer);
            Assert.AreNotEqual(RamType.Hammer, RamType.Fin);
            Assert.AreEqual(RamType.Sharp, RamType.Sharp);
        }

        [Test]
        public void BatteryLayouts_AreDistinct()
        {
            Assert.AreNotEqual(BatteryLayout.BroadsideSingleDeck, BatteryLayout.BroadsideMultiDeck);
            Assert.AreNotEqual(BatteryLayout.GunPort, BatteryLayout.BroadsideMultiDeck);
        }
    }
}
