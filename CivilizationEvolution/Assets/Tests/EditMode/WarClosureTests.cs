using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Diplomacy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.War;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// </summary>
    public class WarClosureTests
    {
        private TileData[] _tiles;
        private Dictionary<int, UnitDef> _unitDefs;
        private CombatManager _combat;

        [SetUp]
        public void Setup()
        {
            _tiles = new TileData[64];
            for (int i = 0; i < 64; i++)
                _tiles[i] = new TileData { tileIndex = i, ownerRealmId = -1, buildingLevels = new int[6] };

            _unitDefs = new Dictionary<int, UnitDef>
            {
                [100] = new UnitDef { unitId = 100, unitName = "轻装步兵", category = GameEnums.UnitCategory.Infantry,
                    tier = 1, meleeAttack = 10f, defense = 8f, morale = 60f, speed = 2f,
                    terrainModifiers = new Dictionary<GameEnums.TerrainTacticType, float>() }
            };
            _combat = new CombatManager(_tiles, _unitDefs, null);
        }

        private static Army MakeArmy(int armyId, int owner, int tile)
        {
            var army = new Army { armyId = armyId, armyName = "军" + owner, ownerRealmId = owner, currentTileIndex = tile };
            army.unitCounts[100] = 100f;
            return army;
        }

        // ===== WarState 基础 =====

        [Test]
        public void WarState_ScoreTracks()
        {
            var war = new WarState(1, 10, 20, 1000);
            war.AddScore(10, 15f);
            Assert.AreEqual(15f, war.GetScore(10));
            Assert.AreEqual(0f, war.GetScore(20));
            Assert.IsFalse(war.ended);
        }

        // ===== 战斗推进与分数 =====

        [Test]
        public void DailyTick_SameTileEnemies_BattleAndScore()
        {
            var armies = new Dictionary<int, Army>
            {
                [1] = MakeArmy(1, 10, 5),
                [2] = MakeArmy(2, 20, 5) // 同地块敌方
            };
            var wars = new List<WarState> { new WarState(1, 10, 20, 1000) };
            var rules = WarRules.Default();

            _combat.DailyTick(armies, wars, rules, 1001);

            // 交战发生：胜方分数 > 0（或至少一场战斗——总分数为一方增加）
            Assert.That(wars[0].attackerScore + wars[0].defenderScore, Is.GreaterThan(0f), "战斗后应有分数产生");
            Assert.AreEqual(1001, wars[0].lastBattleDay, "记录战斗日");
        }

        [Test]
        public void DailyTick_NoContact_NoScore()
        {
            var armies = new Dictionary<int, Army>
            {
                [1] = MakeArmy(1, 10, 5),
                [2] = MakeArmy(2, 20, 30) // 不同地块
            };
            var wars = new List<WarState> { new WarState(1, 10, 20, 1000) };

            _combat.DailyTick(armies, wars, WarRules.Default(), 1001);

            Assert.AreEqual(0f, wars[0].attackerScore + wars[0].defenderScore, "未接触无分数");
            Assert.AreEqual(-1, wars[0].lastBattleDay);
        }

        // ===== 胜负判定 =====

        [Test]
        public void WarOutcome_VictoryAtThreshold()
        {
            var wars = new List<WarState> { new WarState(1, 10, 20, 1000) };
            wars[0].AddScore(10, CombatManager.VictoryScore); // 攻方满 100

            var ended = CombatManager.UpdateWarOutcomes(wars, WarRules.Default(), 1500);

            Assert.AreEqual(1, ended.Count, "应结束一场");
            Assert.AreEqual(10, wars[0].winnerId, "攻方胜利");
            Assert.AreEqual("victory", wars[0].outcome);
        }

        [Test]
        public void WarOutcome_WhitePeace_AfterMinYears()
        {
            var wars = new List<WarState> { new WarState(1, 10, 20, 1000) };
            // 双方低分 + 超 peaceMinYears
            var ended = CombatManager.UpdateWarOutcomes(wars, WarRules.Default(), 1000 + 3 * 365);

            Assert.AreEqual(1, ended.Count, "低分长期战争应白和");
            Assert.AreEqual(-1, wars[0].winnerId, "白和无胜者");
            Assert.AreEqual("white_peace", wars[0].outcome);
        }

        [Test]
        public void WarOutcome_NotEnded_WhenFreshAndLowScore()
        {
            var wars = new List<WarState> { new WarState(1, 10, 20, 1000) };
            var ended = CombatManager.UpdateWarOutcomes(wars, WarRules.Default(), 1100);
            Assert.AreEqual(0, ended.Count, "新开战低分不应结束");
        }

        // ===== 停战（ForcePeace） =====

        [Test]
        public void ForcePeace_SetsTruce()
        {
            var realms = new Dictionary<int, RealmData>();
            for (int i = 0; i < 2; i++)
                realms[i] = new RealmData { realmId = i, realmName = "国" + i };
            var dm = new DiplomacyManager(realms);
            var chronicle = new Chronicle();
            dm.Chronicle = chronicle;

            Assert.IsTrue(dm.DeclareWar(0, 1, "开战"));
            dm.ForcePeace(0, 1, 2000, 5, "甲国胜利");

            var rel = dm.GetRelation(0, 1);
            Assert.IsFalse(rel.isAtWar, "战争结束");
            Assert.AreEqual(2000 + 5 * 365, rel.truceUntilDay, "停战 5 年");
            Assert.AreEqual(1, chronicle.GetEntriesByType("peace").Count, "编年史记录和平");
        }
    }
}
