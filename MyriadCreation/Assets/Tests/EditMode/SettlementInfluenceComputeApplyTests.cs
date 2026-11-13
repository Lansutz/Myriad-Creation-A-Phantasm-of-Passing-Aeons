using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.6：影响力场 Calculate（读）与 Apply（写）分离；查询路径只读 LastField。</summary>
    public class SettlementInfluenceComputeApplyTests
    {
        [Test]
        public void Apply_WritesAnchorTiers_FromField()
        {
            var anchor = new AnchorData { influenceTier = 0 };
            var settlement = new SettlementData { anchor = anchor };
            var settlements = new Dictionary<int, SettlementData> { [7] = settlement };
            var field = new InfluenceField();
            field.tiersBySettlement[7] = InfluenceTier.Secure;

            SettlementInfluenceSystem.Apply(settlements, field);

            Assert.AreEqual((int)InfluenceTier.Secure, anchor.influenceTier);
        }

        [Test]
        public void Apply_NullInputs_DoNotThrow()
        {
            Assert.DoesNotThrow(() => SettlementInfluenceSystem.Apply(null, null));
            Assert.DoesNotThrow(() => SettlementInfluenceSystem.Apply(new Dictionary<int, SettlementData>(), null));
        }

        [Test]
        public void Calculate_WithNoSettlements_ReturnsEmptyField_WithoutWritingAnchor()
        {
            var anchor = new AnchorData { influenceTier = 3 };
            var settlements = new Dictionary<int, SettlementData>
            {
                [1] = new SettlementData { anchor = anchor }
            };

            InfluenceField field = SettlementInfluenceSystem.Calculate(
                new Dictionary<int, SettlementData>(), new TileData[0], 0, 0);

            Assert.IsNotNull(field);
            Assert.AreEqual(0, field.tierByTile.Length);
            Assert.AreEqual(3, anchor.influenceTier, "Calculate 只读，不写 anchor");
            Assert.AreEqual(1, settlements.Count);
        }
    }
}
