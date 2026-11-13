using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Settlement;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 驻军统计空引用回归（审计 C-P1-7）：`SettlementControlSystem` 每日结算里
    /// `army.GetTotalManpower(null)` 会把 unitDefs 当非空解引用 → 每日 NRE。
    /// 现在 unitDefs 由 SettlementControlRuntime 传入，且 Army 侧对 null 做成"返回 0"。
    /// </summary>
    public class ArmyAndGarrisonSafetyTests
    {
        private static Dictionary<int, UnitDef> UnitDefs()
        {
            return new Dictionary<int, UnitDef>
            {
                { 100, new UnitDef { unitId = 100, unitName = "轻装步兵", category = GameEnums.UnitArmType.Infantry, manpowerCost = 10f } }
            };
        }

        [Test]
        public void GetTotalManpower_NullDefs_ReturnsZeroWithoutThrow()
        {
            var army = new Army { armyId = 1 };
            army.unitCounts[100] = 5f;

            Assert.AreEqual(0f, army.GetTotalManpower(null), 0.001f, "unitDefs 未初始化时应返回 0，不抛 NRE");
            Assert.AreEqual(50f, army.GetTotalManpower(UnitDefs()), 0.001f, "5 单位 × 10 人力 = 50");
        }

        [Test]
        public void SettlementControlDailyTick_WithArmy_DoesNotThrowAndAddsGarrison()
        {
            var tiles = new[]
            {
                new TileData { tileIndex = 0, exists = true, isLand = true, ownerRealmId = 1 },
                new TileData { tileIndex = 1, exists = true, isLand = true, ownerRealmId = -1 }
            };
            var settlements = new Dictionary<int, SettlementData>();
            var settlement = new SettlementData
            {
                anchor = new AnchorData { anchorId = 1, provinceId = 0, tileIndex = 0 },
                anchorId = 1,
                siteMode = SiteMode.SedentarySettlement,
                settlementName = "测试城"
            };
            settlements[1] = settlement;

            var armies = new Dictionary<int, Army>();
            var army = new Army { armyId = 2, ownerRealmId = 1, currentTileIndex = 0, state = GameEnums.CombatState.Idle };
            army.unitCounts[100] = 5f;
            armies[2] = army;

            Assert.DoesNotThrow(() => SettlementControlSystem.DailyTick(settlements, tiles, 2, 1, armies, UnitDefs()),
                "驻军结算不应再因 unitDefs 为空而 NRE");

            Assert.GreaterOrEqual(settlement.garrisonQuantity, 50, "驻军人数应包含军队人力");
        }
    }
}
