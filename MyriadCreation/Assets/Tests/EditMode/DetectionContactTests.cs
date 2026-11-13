using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// D37 探测/接触层：先有斥候可达范围，才有接敌；不做全知视野。
    /// 战斗只在同地块接触后结算，遭遇变量由地形/设防/侦察先手决定。
    /// </summary>
    public class DetectionContactTests
    {
        [Test]
        public void TileDistance_IsChebyshev()
        {
            Assert.AreEqual(0, DetectionContactService.TileDistance(0, 0, 10));
            Assert.AreEqual(1, DetectionContactService.TileDistance(0, 11, 10), "斜向相邻算 1");
            Assert.AreEqual(2, DetectionContactService.TileDistance(0, 22, 10));
            Assert.AreEqual(3, DetectionContactService.TileDistance(0, 30, 10));
        }

        [Test]
        public void CanContact_RequiresScoutReach()
        {
            var nearby = new Army { armyId = 1, currentTileIndex = 0, scoutRange = 1 };
            var far = new Army { armyId = 2, currentTileIndex = 22, scoutRange = 1 };
            var farWithScouts = new Army { armyId = 3, currentTileIndex = 22, scoutRange = 2 };

            Assert.IsFalse(DetectionContactService.CanContact(nearby, far, 10), "范围 1 看不到距离 2");
            Assert.IsTrue(DetectionContactService.CanContact(nearby, farWithScouts, 10), "任一侧够得到即接触");
            Assert.IsTrue(DetectionContactService.CanContact(nearby, new Army { armyId = 4, currentTileIndex = 0, scoutRange = 0 }, 10), "同地块始终接触");
        }

        [Test]
        public void ScoutRange_RisesWithCavalryShare()
        {
            var unitDefs = new Dictionary<int, UnitDef>
            {
                { 1, new UnitDef { unitId = 1, category = GameEnums.UnitArmType.Infantry } },
                { 2, new UnitDef { unitId = 2, category = GameEnums.UnitArmType.Cavalry } }
            };
            var tiles = new TileData[10];
            var infantry = new Army { armyId = 1, currentTileIndex = 0, supply = 100f, training = 50f };
            infantry.unitCounts[1] = 100f;
            var cavalry = new Army { armyId = 2, currentTileIndex = 0, supply = 100f, training = 50f };
            cavalry.unitCounts[2] = 100f;

            int infantryRange = DetectionContactService.ComputeScoutRange(infantry, tiles, unitDefs);
            int cavalryRange = DetectionContactService.ComputeScoutRange(cavalry, tiles, unitDefs);

            Assert.Greater(cavalryRange, infantryRange, "机动兵力占比更高应看得更远");
            Assert.GreaterOrEqual(infantryRange, 1);
            Assert.LessOrEqual(cavalryRange, DetectionContactService.MaxScoutRange);
        }

        [Test]
        public void DailyTick_DoesNotFightWithoutContact()
        {
            var tiles = new TileData[100];
            var unitDefs = new Dictionary<int, UnitDef>();
            var combat = new CombatManager(tiles, unitDefs, null, null, 10);
            var armies = new Dictionary<int, Army>
            {
                { 1, new Army { armyId = 1, ownerRealmId = 1, currentTileIndex = 0, supply = 100f, training = 50f } },
                { 2, new Army { armyId = 2, ownerRealmId = 2, currentTileIndex = 80, supply = 100f, training = 50f } }
            };
            var wars = new List<WarState> { new WarState(1, 1, 2, 0) };

            combat.DailyTick(armies, wars, WarRules.Default(), 1);

            Assert.AreEqual(0, combat.LastContacts.Count, "范围外不得产生接触");
            Assert.AreEqual(-1, wars[0].lastBattleDay, "未接触不得开战");
        }

        [Test]
        public void DailyTick_ContactsAtRange_ButFightsOnlyOnSameTile()
        {
            var tiles = new TileData[100];
            var unitDefs = new Dictionary<int, UnitDef>();
            var combat = new CombatManager(tiles, unitDefs, null, null, 10);
            var armies = new Dictionary<int, Army>
            {
                { 1, new Army { armyId = 1, ownerRealmId = 1, currentTileIndex = 0, supply = 100f, training = 50f } },
                { 2, new Army { armyId = 2, ownerRealmId = 2, currentTileIndex = 10, supply = 100f, training = 50f } }
            };
            var wars = new List<WarState> { new WarState(1, 1, 2, 0) };

            combat.DailyTick(armies, wars, WarRules.Default(), 1);

            Assert.Greater(combat.LastContacts.Count, 0, "进入斥候范围应产生接触");
            CollectionAssert.IsNotEmpty(combat.LastContacts);
            Assert.AreEqual(1, combat.LastContacts[0].distance, "相邻地块为遭遇距离");
            Assert.AreEqual(-1, wars[0].lastBattleDay, "仅接触、不同地块不得直接结算会战");

            armies[2].currentTileIndex = 0;
            combat.DailyTick(armies, wars, WarRules.Default(), 2);

            Assert.AreEqual(2, wars[0].lastBattleDay, "同地块接触后才结算战斗");
            Assert.AreEqual(EncounterKind.Battle, combat.LastEncounter.kind);
        }

        [Test]
        public void BuildEncounter_SiegeFavorsDefender()
        {
            var tiles = new TileData[4];
            tiles[0].buildingLevels = new int[5];
            tiles[0].buildingLevels[3] = 1;
            var attacker = new Army { armyId = 1, currentTileIndex = 1, scoutRange = 0 };
            var defender = new Army { armyId = 2, currentTileIndex = 0, scoutRange = 0 };

            EncounterContext context = DetectionContactService.BuildEncounter(attacker, defender, tiles, 0);

            Assert.AreEqual(EncounterKind.Siege, context.kind, "设防地块同格接触是攻坚");
            Assert.Greater(context.defenderPowerMultiplier, 1f, "设防应增强守方");
        }

        [Test]
        public void BuildEncounter_ForestSkirmishUsesScoutingFirstStrike()
        {
            var tiles = new TileData[4];
            tiles[2].biome = GameEnums.BiomeType.DeciduousForest;
            tiles[2].buildingLevels = new int[5];
            var attacker = new Army { armyId = 1, currentTileIndex = 3, scoutRange = 1 };
            var defender = new Army { armyId = 2, currentTileIndex = 2, scoutRange = 0 };

            EncounterContext context = DetectionContactService.BuildEncounter(attacker, defender, tiles, 1);

            Assert.AreEqual(EncounterKind.Skirmish, context.kind, "未同格的接触是遭遇战");
            Assert.Less(context.attackerPowerMultiplier, 1f, "林地遭遇双方战力都打折");
            Assert.Less(context.defenderPowerMultiplier, 1f);
        }
    }
}
