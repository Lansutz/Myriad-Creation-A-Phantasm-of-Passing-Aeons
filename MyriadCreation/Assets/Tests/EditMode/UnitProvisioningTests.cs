using NUnit.Framework;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Core.Enums;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>单位大类只有五类；小类决定 AI/互动；单位要补给、可拆分、要经费。</summary>
    public class UnitProvisioningTests
    {
        [Test]
        public void Category_HasExactlyFiveGivenClasses()
        {
            var values = (UnitCategory[])System.Enum.GetValues(typeof(UnitCategory));
            Assert.AreEqual(5, values.Length);
            CollectionAssert.AreEquivalent(new[]
            {
                UnitCategory.Military, UnitCategory.Trade, UnitCategory.Migration,
                UnitCategory.Bandit, UnitCategory.Wilderness
            }, values);
        }

        [Test]
        public void Subcategory_DecidesAIAndInteraction()
        {
            Assert.AreEqual(UnitInteraction.DeliverOrders, UnitSubcategoryRules.Interaction(UnitSubcategory.Courier));
            Assert.AreEqual(UnitInteraction.CropDamage, UnitSubcategoryRules.Interaction(UnitSubcategory.LocustSwarm));
            Assert.AreEqual(UnitInteraction.Predation, UnitSubcategoryRules.Interaction(UnitSubcategory.Predator));
        }

        [Test]
        public void TransportUnit_DetachesSubunitWithoutLeader()
        {
            var parent = new TransportUnit
            {
                type = TransportUnitType.Settlers,
                leaderCharacterId = 1,
            };
            parent.AddCargo(1, 90f); // 食物
            parent.AddCargo(2, 30f); // 货币货物
            parent.TryAddMember(1, UnitRole.Leader);
            parent.TryAddMember(2, UnitRole.Guide);
            parent.TryAddMember(3, UnitRole.Quartermaster);

            var child = parent.DetachSubunit(new[] { 2, 3 }, UnitSubcategory.OrdinaryMigrant, 2);
            Assert.IsNotNull(child);
            Assert.AreEqual(2, child.members.Count);
            Assert.AreEqual(2, child.leaderCharacterId);
            Assert.AreEqual(1, parent.members.Count, "领袖留在原队");
            Assert.Less(parent.GetCargoAmount(1), 90f);
            Assert.Greater(child.GetCargoAmount(1), 0f);
            Assert.Greater(child.GetCargoAmount(2), 0f);
        }

        [Test]
        public void Supplies_ConsumedAndShortageReported()
        {
            var unit = new TransportUnit();
            unit.TryAddMember(1);
            unit.AddCargo(1, 2f);
            var goods = new System.Collections.Generic.Dictionary<int, GoodsDef>
            {
                [1] = new GoodsDef { goodsId = 1, category = GameEnums.GoodsCategory.Food },
            };
            float shortage = unit.ConsumeFood(goods, 5f, out float reported);
            Assert.AreEqual(0f, unit.GetCargoAmount(1));
            Assert.Greater(reported, 0f, "补给不足必须报告");
        }

        [Test]
        public void Funding_FromNationalTreasuryDeductsAndFailsWhenPoor()
        {
            var realm = new RealmData { treasury = 100f, centralization = 0.8f };
            Assert.IsTrue(UnitProvisioning.TryFund(realm, null, UnitFundingSource.NationalTreasury,
                40f, out var funding, out string reason), reason);
            Assert.AreEqual(60f, realm.treasury, 0.01f);
            Assert.AreEqual(UnitFundingSource.NationalTreasury, funding.source);

            realm.treasury = 10f;
            Assert.IsFalse(UnitProvisioning.TryFund(realm, null, UnitFundingSource.NationalTreasury,
                40f, out _, out reason));
            StringAssert.Contains("国库", reason);
        }

        [Test]
        public void CourierCost_IncludesFoodMoneyAndMount()
        {
            var cost = UnitProvisioningCost.ForCourier(100f);
            Assert.Greater(cost.food, 0f);
            Assert.Greater(cost.money, 0f);
            Assert.GreaterOrEqual(cost.horses, 1);
        }
    }
}
