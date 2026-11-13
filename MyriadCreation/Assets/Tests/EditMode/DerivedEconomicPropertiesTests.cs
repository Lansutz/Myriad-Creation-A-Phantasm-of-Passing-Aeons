using NUnit.Framework;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 社会/经济属性是“比较出来的”，不是物品标注的：
    /// 稀缺性、货币适度性、可分割/便携（由延展性与价值密度派生）、地区等价量、套利利润。
    /// </summary>
    public class DerivedEconomicPropertiesTests
    {
        [Test]
        public void Scarcity_DependsOnRegionComparison_NotOnlyOwnSupply()
        {
            float isolated = DerivedEconomicProperties.Scarcity(supply: 10f, demand: 10f, alternativeSupply: 0f);
            float abundantRegion = DerivedEconomicProperties.Scarcity(supply: 10f, demand: 10f, alternativeSupply: 40f);
            Assert.Greater(isolated, abundantRegion, "区域替代品越多，该物品越不稀缺");
            Assert.GreaterOrEqual(abundantRegion, 0f);
            Assert.LessOrEqual(isolated, 1f);
        }

        [Test]
        public void SameGood_CanBeScarceInOneMarket_AndAbundantInAnother()
        {
            float marketA = DerivedEconomicProperties.Scarcity(2f, 20f, 0f);
            float marketB = DerivedEconomicProperties.Scarcity(50f, 5f, 0f);
            Assert.Greater(marketA, marketB);
        }

        [Test]
        public void RelativeValue_CombinesUseValueAndScarcity()
        {
            float cheap = DerivedEconomicProperties.RelativeValue(useValue: 10f, scarcity: 0f);
            float scarce = DerivedEconomicProperties.RelativeValue(useValue: 10f, scarcity: 0.8f);
            Assert.AreEqual(10f, cheap, 0.01f);
            Assert.Greater(scarce, cheap);
        }

        [Test]
        public void MoneyScarcity_NeedsSweetSpot()
        {
            float tooScarce = DerivedEconomicProperties.MoneyScarcityFitness(0.02f);
            float sweetSpot = DerivedEconomicProperties.MoneyScarcityFitness(0.5f);
            float tooCommon = DerivedEconomicProperties.MoneyScarcityFitness(0.98f);

            Assert.Greater(sweetSpot, tooScarce, "太稀缺（本地没几件）不适合流通");
            Assert.Greater(sweetSpot, tooCommon, "太泛滥（失去等价物功能）也不适合");
            Assert.AreEqual(1f, sweetSpot, 0.001f);
        }

        [Test]
        public void DivisibilityAndPortability_AreDerivedFromPhysicalInputs()
        {
            Assert.Greater(DerivedEconomicProperties.Divisibility(9f),
                DerivedEconomicProperties.Divisibility(3f), "延展性越高，可分割性越高");
            Assert.Greater(DerivedEconomicProperties.Portability(20f),
                DerivedEconomicProperties.Portability(2f), "价值密度越高越便携");
        }

        [Test]
        public void CurrencySuitability_PhysicalSame_DiffersByInstitution()
        {
            float accepted = DerivedEconomicProperties.CurrencySuitability(
                durability: 0.8f, ductility: 7f, valuePerWeight: 8f, uniformity: 0.9f,
                localScarcity: 0.5f, acceptability: 0.9f, circulationShare: 0.7f);
            float rejected = DerivedEconomicProperties.CurrencySuitability(
                durability: 0.8f, ductility: 7f, valuePerWeight: 8f, uniformity: 0.9f,
                localScarcity: 0.5f, acceptability: 0.1f, circulationShare: 0.1f);

            Assert.Greater(accepted, rejected, "同一物品在不同制度接受度下货币性不同");
            Assert.LessOrEqual(accepted, 1f);
        }

        [Test]
        public void SameGoldUnit_BuysDifferentAmountsInDifferentRegions()
        {
            // 同一份黄金；A 地粮食便宜、B 地粮食贵（稀缺）
            float inRegionA = DerivedEconomicProperties.ExchangeEquivalent(moneyUnitValue: 10f, localGoodPrice: 1f);
            float inRegionB = DerivedEconomicProperties.ExchangeEquivalent(moneyUnitValue: 10f, localGoodPrice: 4f);
            Assert.AreEqual(10f, inRegionA, 0.01f);
            Assert.AreEqual(2.5f, inRegionB, 0.01f);
            Assert.Greater(inRegionA, inRegionB, "等价量是地区性的，不是固定数");
        }

        [Test]
        public void ArbitrageProfit_AllowsDumpingBetweenRegions()
        {
            // A 地便宜、B 地贵：把 A 的货运到 B 卖（倾销/套利）
            float profit = DerivedEconomicProperties.ArbitrageProfit(buyPrice: 2f, sellPrice: 8f, transportCostPerUnit: 1f);
            Assert.AreEqual(5f, profit, 0.01f);

            float noProfit = DerivedEconomicProperties.ArbitrageProfit(buyPrice: 2f, sellPrice: 8f, transportCostPerUnit: 10f);
            Assert.AreEqual(0f, noProfit, 0.01f, "运费吃掉差价时无套利");
        }
    }
}
