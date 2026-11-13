using NUnit.Framework;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// B2 发现（核心规则）：发现人群（商人/工匠/学者）、三态中的“发现”、栖居点人群判定。
    /// </summary>
    public class DiscoveryTests
    {
        private InnovationTree _tree;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            _tree = new InnovationTree();
        }

        [Test]
        public void Content_EveryInnovationHasValidDiscoveryGroups()
        {
            int total = 0;
            foreach (var pair in ContentRegistry.Innovations)
            {
                var groups = pair.Value.discoveryGroups;
                Assert.IsNotNull(groups, $"革新 {pair.Key} 未配置发现人群");
                Assert.Greater(groups.Count, 0, $"革新 {pair.Key} 发现人群为空");
                foreach (var group in groups)
                    Assert.IsTrue(DiscoveryGroups.IsValid(group),
                        $"革新 {pair.Key} 发现人群非法：{group}");
                total++;
            }
            Assert.Greater(total, 150);
        }

        [Test]
        public void Discover_RequiresMatchingGroup()
        {
            // 201 青铜铸造属于冶金，应由工匠发现
            var bronze = _tree.GetInnovation(201);
            Assert.IsNotNull(bronze);
            CollectionAssert.Contains(bronze.discoveryGroups, DiscoveryGroups.Artisan);

            Assert.IsFalse(InnovationDiscovery.TryDiscover(_tree, 1, 201, DiscoveryGroups.Merchant),
                "商人不能发现青铜铸造");
            Assert.IsFalse(_tree.IsDiscovered(1, 201));

            Assert.IsTrue(InnovationDiscovery.TryDiscover(_tree, 1, 201, DiscoveryGroups.Artisan),
                "工匠可以发现青铜铸造");
            Assert.IsTrue(_tree.IsDiscovered(1, 201));
            Assert.IsFalse(InnovationDiscovery.TryDiscover(_tree, 1, 201, DiscoveryGroups.Artisan),
                "重复发现返回 false");
            Assert.AreEqual(1, _tree.GetDiscoveredCount(1));
        }

        [Test]
        public void MerchantAndScholarDiscoverTheirOwnFields()
        {
            var coinage = _tree.GetInnovation(701); // 铸币，Economic → 商人
            Assert.IsNotNull(coinage);
            CollectionAssert.Contains(coinage.discoveryGroups, DiscoveryGroups.Merchant);
            Assert.IsTrue(InnovationDiscovery.TryDiscover(_tree, 2, 701, DiscoveryGroups.Merchant));
            Assert.IsFalse(InnovationDiscovery.TryDiscover(_tree, 2, 701, DiscoveryGroups.Scholar));

            var writing = _tree.GetInnovation(600); // 文字，Script → 学者
            Assert.IsNotNull(writing);
            CollectionAssert.Contains(writing.discoveryGroups, DiscoveryGroups.Scholar);
            Assert.IsTrue(InnovationDiscovery.TryDiscover(_tree, 2, 600, DiscoveryGroups.Scholar));
        }

        [Test]
        public void DiscoveredList_ExcludesMastered()
        {
            InnovationDiscovery.TryDiscover(_tree, 3, 700, DiscoveryGroups.Merchant);
            Assert.AreEqual(1, _tree.GetDiscoveredInnovations(3).Count);

            Assert.IsTrue(_tree.StartResearch(3, 700));
            _tree.DailyTick(3, 100000f);
            Assert.IsTrue(_tree.HasInnovation(3, 700));
            Assert.AreEqual(0, _tree.GetDiscoveredInnovations(3).Count, "已掌握的不再出现在候选");
        }

        [Test]
        public void ActorGroups_FromSettlement()
        {
            var market = new SettlementData { hasMarket = true };
            var marketGroups = DiscoveryActors.FromSettlement(market);
            Assert.IsTrue(DiscoveryActors.Has(marketGroups, DiscoveryGroups.Merchant));
            Assert.IsFalse(DiscoveryActors.Has(marketGroups, DiscoveryGroups.Artisan));

            var craft = new SettlementData();
            craft.economicComposition[EconomicSector.Artisanal] = 0.4f;
            Assert.IsTrue(DiscoveryActors.Has(DiscoveryActors.FromSettlement(craft), DiscoveryGroups.Artisan));

            var university = new SettlementData { hasUniversity = true };
            Assert.IsTrue(DiscoveryActors.Has(DiscoveryActors.FromSettlement(university), DiscoveryGroups.Scholar));

            Assert.AreEqual(0, DiscoveryActors.Count(DiscoveryActors.FromSettlement(new SettlementData())));
        }

        [Test]
        public void ActorGroups_SettlementWithAllThree()
        {
            var settlement = new SettlementData
            {
                hasMarket = true,
                hasUniversity = true,
            };
            settlement.economicComposition[EconomicSector.Artisanal] = 0.3f;
            var groups = DiscoveryActors.FromSettlement(settlement);
            Assert.AreEqual(3, DiscoveryActors.Count(groups));
            Assert.IsTrue(DiscoveryActors.Has(groups, DiscoveryGroups.Merchant));
            Assert.IsTrue(DiscoveryActors.Has(groups, DiscoveryGroups.Artisan));
            Assert.IsTrue(DiscoveryActors.Has(groups, DiscoveryGroups.Scholar));
        }
    }
}
