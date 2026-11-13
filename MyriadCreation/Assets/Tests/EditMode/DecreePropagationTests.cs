using System;
using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Settlement;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.LawHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>法令不是瞬移：沿行政区划父子链传递，并由官员执行/拖延/拒绝。</summary>
    public class DecreePropagationTests
    {
        private static List<AdminDivision> Hierarchy()
        {
            var root = new AdminDivision { divisionId = 1, realmId = 0, level = 1, parentDivisionId = -1 };
            root.tiles.Add(0);
            var child = new AdminDivision
            {
                divisionId = 2, realmId = 0, level = 2, parentDivisionId = 1,
                holderCharacterId = 10,
            };
            child.tiles.Add(100);
            root.childIds.Add(2);
            return new List<AdminDivision> { root, child };
        }

        [Test]
        public void IssueDownward_CreatesOrdersWithTravelTime()
        {
            var orders = DecreePropagationSystem.IssueDownward("decree_tax_relief", 0,
                Hierarchy(), 1, 10);
            Assert.AreEqual(1, orders.Count);
            Assert.GreaterOrEqual(orders[0].totalTravelDays, 1);
            Assert.AreEqual(10, orders[0].officialCharacterId);
            Assert.AreEqual(DecreeOrderState.Pending, orders[0].state);
        }

        [Test]
        public void Advance_DeliversOnlyAfterTravelDays()
        {
            var order = DecreePropagationSystem.IssueDownward("d", 0, Hierarchy(), 1, 10)[0];
            int days = order.totalTravelDays;
            for (int i = 0; i < days - 1; i++)
                DecreePropagationSystem.Advance(order, 1f);
            Assert.AreNotEqual(DecreeOrderState.Delivered, order.state, "不能瞬移");
            DecreePropagationSystem.Advance(order, 1f);
            Assert.AreEqual(DecreeOrderState.Delivered, order.state);
        }

        [Test]
        public void IssueToSubordinateSettlements_TargetsSettlementNotDivisionCenter()
        {
            var tiles = new TileData[10];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = new TileData { exists = true, isLand = true, ownerRealmId = 0 };
            var root = new SettlementData
            {
                anchorId = 1, siteMode = SiteMode.SedentarySettlement,
                anchor = new AnchorData { anchorId = 1, tileIndex = 0 },
            };
            var child = new SettlementData
            {
                anchorId = 2, siteMode = SiteMode.SedentarySettlement,
                anchor = new AnchorData { anchorId = 2, tileIndex = 5 },
            };
            root.anchor.controlledAnchorIds.Add(2);
            var settlements = new Dictionary<int, SettlementData> { [1] = root, [2] = child };
            var orders = DecreePropagationSystem.IssueToSubordinateSettlements("d", 0, settlements, tiles, 1);
            Assert.AreEqual(1, orders.Count);
            Assert.AreEqual(2, orders[0].targetAnchorId);
            Assert.AreEqual(5, orders[0].targetTile);
            Assert.AreEqual(1, orders[0].fromAnchorId);
        }

        [Test]
        public void FundingSource_FromLawGroupsNotCentralization()
        {
            var realm = new RealmData();
            realm.laws.localSuccession.primary = (int)MyriadCreation.Content.Definitions.LocalSuccession.CityCharter;
            realm.centralization = 0.1f;
            Assert.AreEqual(UnitFundingSource.CityTreasury, UnitProvisioning.ResolveFundingSource(realm));

            realm.laws.localSuccession.primary = (int)MyriadCreation.Content.Definitions.LocalSuccession.Appointed;
            realm.laws.nationalTax.primary = 0;
            Assert.AreEqual(UnitFundingSource.NationalTreasury, UnitProvisioning.ResolveFundingSource(realm));
        }

        [Test]
        public void Resolve_CompetentOfficialEnforces()
        {
            var order = DecreePropagationSystem.IssueDownward("d", 0, Hierarchy(), 1, 10)[0];
            var official = new CharacterData
            {
                characterId = 10, management = 90f, conspiracy = 10f,
            };
            var state = DecreePropagationSystem.Resolve(order, official, 0.9f, 0.1f, 0.1f, new Random(2));
            Assert.AreEqual(DecreeOrderState.Enforced, state);
        }

        [Test]
        public void Resolve_MissingOfficialRefuses()
        {
            var order = DecreePropagationSystem.IssueDownward("d", 0, Hierarchy(), 1, 10)[0];
            var state = DecreePropagationSystem.Resolve(order, null, 1f, 0f, 0f, new Random(1));
            Assert.AreEqual(DecreeOrderState.Refused, state);
        }
    }
}
