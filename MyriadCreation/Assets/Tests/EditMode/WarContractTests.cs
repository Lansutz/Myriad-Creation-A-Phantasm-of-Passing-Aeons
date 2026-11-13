using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Enums;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// D38 战争三层契约：WarState 是战略层真源，承载宣战理由与战争目标；
    /// 外交关系上的 CB/目标是投影；战争结束必须落到 Treaty，而不是只改 isAtWar。
    /// </summary>
    public class WarContractTests
    {
        private const string SaveName = "__war_contract_test";
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
            SaveSystem.DeleteSave(SaveName);
        }

        private GameWorld NewWorld()
        {
            var go = new GameObject("WarContractWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(23);
            return world;
        }

        private static void PickTwoRealms(GameWorld world, out int attacker, out int defender)
        {
            attacker = -1;
            defender = -1;
            foreach (int key in world.realms.Keys)
            {
                if (attacker < 0) attacker = key;
                else { defender = key; break; }
            }
        }

        /// <summary>找一个守方所有、且八邻接攻方领土的地块；目标选择必须通过接壤约束。</summary>
        private static int FindBorderTile(GameWorld world, int attacker, int defender)
        {
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (world.tiles[i].ownerRealmId != defender) continue;
                int x = i % world.mapWidth;
                int y = i / world.mapWidth;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= world.mapWidth || ny >= world.mapHeight) continue;
                        if (world.tiles[ny * world.mapWidth + nx].ownerRealmId == attacker) return i;
                    }
                }
            }
            return -1;
        }

        private static int FindNonBorderTileOwnedBy(GameWorld world, int attacker, int defender)
        {
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (world.tiles[i].ownerRealmId != defender) continue;
                int x = i % world.mapWidth;
                int y = i / world.mapWidth;
                bool adjacent = false;
                for (int dy = -1; dy <= 1 && !adjacent; dy++)
                {
                    for (int dx = -1; dx <= 1 && !adjacent; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= world.mapWidth || ny >= world.mapHeight) continue;
                        if (world.tiles[ny * world.mapWidth + nx].ownerRealmId == attacker) adjacent = true;
                    }
                }
                if (!adjacent) return i;
            }
            return -1;
        }

        private static WarState LastOpenWar(GameWorld world)
        {
            WarState found = null;
            foreach (var war in world.GetWars())
            {
                if (!war.ended) found = war;
            }
            return found;
        }

        [Test]
        public void DeclareWarWithJustification_AttachesCasusBelliAndGoalToWarState()
        {
            var world = NewWorld();
            PickTwoRealms(world, out int attacker, out int defender);
            Assert.GreaterOrEqual(defender, 0, "前置：应有两个政权");

            int borderTile = FindBorderTile(world, attacker, defender);
            Assert.GreaterOrEqual(borderTile, 0, "前置：应有接壤地块");
            var casusBelli = new CasusBelli
            {
                cbId = 1,
                type = GameEnums.CasusBelliType.TerritorialDispute,
                holderRealmId = attacker,
                targetRealmId = defender,
                expiryDay = -1
            };
            var goal = new WarGoal
            {
                goalId = 1,
                type = GameEnums.WarGoalType.ConquerTerritory,
                targetTileIndex = borderTile
            };

            bool declared = world.DeclareWarWithJustification(attacker, defender, casusBelli, goal, "测试宣战", WarDecisionMode.SkipConsultation);

            Assert.IsTrue(declared, "带理由宣战应成功");
            WarState war = LastOpenWar(world);
            Assert.IsNotNull(war, "战争运行态应存在");
            Assert.AreEqual(attacker, war.attackerId);
            Assert.AreEqual(defender, war.defenderId);
            Assert.IsNotNull(war.casusBelli, "WarState 必须承载宣战理由");
            Assert.AreEqual(GameEnums.CasusBelliType.TerritorialDispute, war.casusBelli.type);
            Assert.AreEqual(1, war.warGoals.Count, "WarState 必须承载战争目标");
            Assert.AreEqual(attacker, war.warGoals[0].attackerRealmId);
            Assert.AreEqual(defender, war.warGoals[0].defenderRealmId);

            var relation = world.GetDiplomacyManager().GetRelation(attacker, defender);
            Assert.IsTrue(relation.isAtWar, "外交投影应同步");
            Assert.AreEqual(1, relation.activeWarGoals.Count, "外交投影保留目标供界面查询");
        }

        [Test]
        public void DeclareWarWithoutCasusBelli_StillCreatesWarStateWithReason()
        {
            var world = NewWorld();
            PickTwoRealms(world, out int attacker, out int defender);
            Assert.GreaterOrEqual(defender, 0, "前置：应有两个政权");

            bool declared = world.DeclareWar(attacker, defender, "无借口突袭", WarDecisionMode.SkipConsultation);

            Assert.IsTrue(declared, "无 CB 突袭也应允许");
            WarState war = LastOpenWar(world);
            Assert.IsNotNull(war, "无 CB 不得绕过战争运行态");
            Assert.AreEqual("无借口突袭", war.reason);
            Assert.IsNull(war.casusBelli);
            Assert.AreEqual(0, war.warGoals.Count);
        }

        [Test]
        public void WarVictory_GeneratesPeaceTreatyFromWarGoals()
        {
            var world = NewWorld();
            PickTwoRealms(world, out int attacker, out int defender);
            Assert.GreaterOrEqual(defender, 0, "前置：应有两个政权");

            int borderTile = FindBorderTile(world, attacker, defender);
            Assert.GreaterOrEqual(borderTile, 0, "前置：应有接壤地块");
            var goal = new WarGoal
            {
                goalId = 2,
                type = GameEnums.WarGoalType.ConquerTerritory,
                targetTileIndex = borderTile
            };
            Assert.IsTrue(world.DeclareWarWithJustification(attacker, defender, null, goal, "征服", WarDecisionMode.SkipConsultation));
            WarState war = LastOpenWar(world);
            Assert.IsNotNull(war);
            war.attackerScore = 100f;

            world.RunDiagnosticDays(2);

            Assert.IsTrue(war.ended, "分数到位后战争应结束");
            Assert.AreEqual(attacker, war.winnerId);
            var relation = world.GetDiplomacyManager().GetRelation(attacker, defender);
            Assert.IsFalse(relation.isAtWar, "战争结束应回到非战争态");
            Treaty treaty = relation.activeTreaties.Find(t => t.isPeaceTreaty);
            Assert.IsNotNull(treaty, "战争结束必须生成和平条约");
            CollectionAssert.Contains(treaty.originalWarGoals, GameEnums.WarGoalType.ConquerTerritory);
            Assert.IsTrue(treaty.clauses.Exists(c => c.type == TreatyClauseType.Truce), "条约必须含停战条款");
            Assert.Greater(relation.truceUntilDay, world.currentDay, "停战期应生效");
        }

        [Test]
        public void ReviseWarGoal_WithinCasusBelliScope_IsBoundedByCooldown()
        {
            var world = NewWorld();
            PickTwoRealms(world, out int attacker, out int defender);
            Assert.GreaterOrEqual(defender, 0, "前置：应有两个政权");

            var casusBelli = new CasusBelli
            {
                cbId = 3,
                type = GameEnums.CasusBelliType.BorderIncident,
                holderRealmId = attacker,
                targetRealmId = defender,
                expiryDay = -1
            };
            int borderTile = FindBorderTile(world, attacker, defender);
            Assert.GreaterOrEqual(borderTile, 0, "前置：应有接壤地块");
            var initialGoal = new WarGoal { goalId = 3, type = GameEnums.WarGoalType.BorderAdjustment, targetTileIndex = borderTile };
            Assert.IsTrue(world.DeclareWarWithJustification(attacker, defender, casusBelli, initialGoal, "边境", WarDecisionMode.SkipConsultation));
            WarState war = LastOpenWar(world);

            var downgradedGoal = new WarGoal { goalId = 4, type = GameEnums.WarGoalType.Humiliation, targetTileIndex = borderTile };
            Assert.IsTrue(world.ReviseWarGoal(war.warId, downgradedGoal, WarDecisionMode.SkipConsultation), "CB 授权范围内的降级应允许");
            Assert.AreEqual(GameEnums.WarGoalType.Humiliation, war.warGoals[0].type);
            Assert.AreEqual(world.currentDay, war.lastWarGoalChangeDay);
            Assert.AreEqual(1, war.warGoalRevisionCount);
            Assert.AreEqual(war.casusBelli, casusBelli, "理由不随目标变更");

            var secondRevision = new WarGoal { goalId = 5, type = GameEnums.WarGoalType.Indemnity, targetTileIndex = 1 };
            Assert.IsFalse(world.ReviseWarGoal(war.warId, secondRevision, WarDecisionMode.SkipConsultation), "冷却期内不得再次改目标");
            Assert.AreEqual(GameEnums.WarGoalType.Humiliation, war.warGoals[0].type);
        }

        [Test]
        public void ReviseWarGoal_OutsideCasusBelliScope_IsRejected()
        {
            var world = NewWorld();
            PickTwoRealms(world, out int attacker, out int defender);
            Assert.GreaterOrEqual(defender, 0, "前置：应有两个政权");

            var casusBelli = new CasusBelli
            {
                cbId = 6,
                type = GameEnums.CasusBelliType.BorderIncident,
                holderRealmId = attacker,
                targetRealmId = defender,
                expiryDay = -1
            };
            int borderTile = FindBorderTile(world, attacker, defender);
            Assert.GreaterOrEqual(borderTile, 0, "前置：应有接壤地块");
            var initialGoal = new WarGoal { goalId = 6, type = GameEnums.WarGoalType.BorderAdjustment, targetTileIndex = borderTile };
            Assert.IsTrue(world.DeclareWarWithJustification(attacker, defender, casusBelli, initialGoal, "边境", WarDecisionMode.SkipConsultation));
            WarState war = LastOpenWar(world);

            var overreach = new WarGoal { goalId = 7, type = GameEnums.WarGoalType.ConvertReligion };
            Assert.IsFalse(world.ReviseWarGoal(war.warId, overreach, WarDecisionMode.SkipConsultation), "越出 CB 授权范围的目标不得改");
            Assert.AreEqual(GameEnums.WarGoalType.BorderAdjustment, war.warGoals[0].type);
            Assert.AreEqual(0, war.warGoalRevisionCount);
        }

        [Test]
        public void ReviseWarGoal_WithoutCasusBelli_IsRejected()
        {
            var world = NewWorld();
            PickTwoRealms(world, out int attacker, out int defender);
            Assert.GreaterOrEqual(defender, 0, "前置：应有两个政权");
            Assert.IsTrue(world.DeclareWar(attacker, defender, "无借口突袭", WarDecisionMode.SkipConsultation));
            WarState war = LastOpenWar(world);

            var revised = new WarGoal { goalId = 8, type = GameEnums.WarGoalType.Indemnity };
            Assert.IsFalse(world.ReviseWarGoal(war.warId, revised, WarDecisionMode.SkipConsultation), "无 CB 的突袭没有扩张依据");
            Assert.AreEqual(0, war.warGoals.Count);
        }

        [Test]
        public void DeclareWarWithGoal_RejectsUnsupportedCasusBelli()
        {
            var world = NewWorld();
            PickTwoRealms(world, out int attacker, out int defender);
            Assert.GreaterOrEqual(defender, 0, "前置：应有两个政权");
            int borderTile = FindBorderTile(world, attacker, defender);
            Assert.GreaterOrEqual(borderTile, 0, "前置：应有接壤地块");

            var casusBelli = new CasusBelli
            {
                cbId = 9,
                type = GameEnums.CasusBelliType.BorderIncident,
                holderRealmId = attacker,
                targetRealmId = defender,
                expiryDay = -1
            };
            var overreach = new WarGoal
            {
                goalId = 9,
                type = GameEnums.WarGoalType.ConquerTerritory,
                targetTileIndex = borderTile
            };

            Assert.IsFalse(world.DeclareWarWithJustification(attacker, defender, casusBelli, overreach, "越权", WarDecisionMode.SkipConsultation),
                "边境事件不足以支撑吞并领土");
            Assert.IsNull(LastOpenWar(world), "失败宣战不得留下战争运行态");
        }

        [Test]
        public void ReviseWarGoal_NonAdjacentTerritory_IsRejected()
        {
            var world = NewWorld();
            PickTwoRealms(world, out int attacker, out int defender);
            Assert.GreaterOrEqual(defender, 0, "前置：应有两个政权");
            int borderTile = FindBorderTile(world, attacker, defender);
            int interiorTile = FindNonBorderTileOwnedBy(world, attacker, defender);
            Assert.GreaterOrEqual(borderTile, 0, "前置：应有接壤地块");
            if (interiorTile < 0) Assert.Ignore("该地图没有非接壤的守方地块");

            var casusBelli = new CasusBelli
            {
                cbId = 10,
                type = GameEnums.CasusBelliType.TerritorialDispute,
                holderRealmId = attacker,
                targetRealmId = defender,
                expiryDay = -1
            };
            var initialGoal = new WarGoal { goalId = 10, type = GameEnums.WarGoalType.BorderAdjustment, targetTileIndex = borderTile };
            Assert.IsTrue(world.DeclareWarWithJustification(attacker, defender, casusBelli, initialGoal, "边境", WarDecisionMode.SkipConsultation));
            WarState war = LastOpenWar(world);
            war.lastWarGoalChangeDay = -10000;

            var deepStrike = new WarGoal { goalId = 11, type = GameEnums.WarGoalType.ConquerTerritory, targetTileIndex = interiorTile };
            Assert.IsFalse(world.ReviseWarGoal(war.warId, deepStrike, WarDecisionMode.SkipConsultation), "不接壤的领土目标不得成为战争目标");
            Assert.AreEqual(GameEnums.WarGoalType.BorderAdjustment, war.warGoals[0].type);
        }

        [Test]
        public void ReviseWarGoal_TierShortfall_CostsStabilityNotPrestige()
        {
            var world = NewWorld();
            PickTwoRealms(world, out int attacker, out int defender);
            Assert.GreaterOrEqual(defender, 0, "前置：应有两个政权");
            int borderTile = FindBorderTile(world, attacker, defender);
            Assert.GreaterOrEqual(borderTile, 0, "前置：应有接壤地块");
            RealmData realm = world.realms[attacker];
            realm.prestige = 50f;
            realm.stability = 70f;
            realm.centralization = 0.5f;

            var casusBelli = new CasusBelli
            {
                cbId = 12,
                type = GameEnums.CasusBelliType.TerritorialDispute,
                holderRealmId = attacker,
                targetRealmId = defender,
                expiryDay = -1
            };
            var initialGoal = new WarGoal { goalId = 12, type = GameEnums.WarGoalType.BorderAdjustment, targetTileIndex = borderTile };
            Assert.IsTrue(world.DeclareWarWithJustification(attacker, defender, casusBelli, initialGoal, "边境", WarDecisionMode.SkipConsultation));
            WarState war = LastOpenWar(world);
            float prestigeBefore = realm.prestige;
            float stabilityBefore = realm.stability;
            war.lastWarGoalChangeDay = -10000;

            var escalate = new WarGoal { goalId = 13, type = GameEnums.WarGoalType.ConquerTerritory, targetTileIndex = borderTile };
            Assert.IsTrue(world.ReviseWarGoal(war.warId, escalate, WarDecisionMode.SkipConsultation), "威望等级不足但政权仍稳时可以强推");
            Assert.AreEqual(prestigeBefore, realm.prestige, "威望等级是门槛，不是可花费点数");
            Assert.Less(realm.stability, stabilityBefore, "强推目标的代价落在稳定度上");
            Assert.Greater(realm.unrest, 0f, "强推目标应推高不安");
            Assert.AreEqual(GameEnums.WarGoalType.ConquerTerritory, war.warGoals[0].type);
        }

        [Test]
        public void ReviseWarGoal_TierShortfall_RejectedWhenRealmUnstable()
        {
            var world = NewWorld();
            PickTwoRealms(world, out int attacker, out int defender);
            Assert.GreaterOrEqual(defender, 0, "前置：应有两个政权");
            int borderTile = FindBorderTile(world, attacker, defender);
            Assert.GreaterOrEqual(borderTile, 0, "前置：应有接壤地块");
            RealmData realm = world.realms[attacker];
            realm.prestige = 50f;
            realm.stability = 70f;
            realm.centralization = 0.5f;

            var casusBelli = new CasusBelli
            {
                cbId = 14,
                type = GameEnums.CasusBelliType.TerritorialDispute,
                holderRealmId = attacker,
                targetRealmId = defender,
                expiryDay = -1
            };
            var initialGoal = new WarGoal { goalId = 14, type = GameEnums.WarGoalType.BorderAdjustment, targetTileIndex = borderTile };
            Assert.IsTrue(world.DeclareWarWithJustification(attacker, defender, casusBelli, initialGoal, "边境", WarDecisionMode.SkipConsultation));
            WarState war = LastOpenWar(world);
            war.lastWarGoalChangeDay = -10000;
            realm.stability = 30f;

            var escalate = new WarGoal { goalId = 15, type = GameEnums.WarGoalType.ConquerTerritory, targetTileIndex = borderTile };
            Assert.IsFalse(world.ReviseWarGoal(war.warId, escalate, WarDecisionMode.SkipConsultation), "政权不稳时不得强推超越威望等级的目标");
            Assert.AreEqual(GameEnums.WarGoalType.BorderAdjustment, war.warGoals[0].type);
        }

        [Test]
        public void WarState_JustificationAndGoals_RoundTripThroughSave()
        {
            var world = NewWorld();
            PickTwoRealms(world, out int attacker, out int defender);
            Assert.GreaterOrEqual(defender, 0, "前置：应有两个政权");

            var casusBelli = new CasusBelli
            {
                cbId = 7,
                type = GameEnums.CasusBelliType.TerritorialDispute,
                holderRealmId = attacker,
                targetRealmId = defender,
                expiryDay = -1,
                description = "存读往返"
            };
            int borderTile = FindBorderTile(world, attacker, defender);
            Assert.GreaterOrEqual(borderTile, 0, "前置：应有接壤地块");
            var goal = new WarGoal
            {
                goalId = 7,
                type = GameEnums.WarGoalType.BorderAdjustment,
                targetTileIndex = borderTile
            };
            Assert.IsTrue(world.DeclareWarWithJustification(attacker, defender, casusBelli, goal, "存读", WarDecisionMode.SkipConsultation));
            int warId = LastOpenWar(world).warId;

            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "存盘应成功");
            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "读档应返回世界");
            _objects.Add(loaded.gameObject);

            WarState loadedWar = loaded.GetWars().Find(w => w.warId == warId);
            Assert.IsNotNull(loadedWar, "战争运行态应还原");
            Assert.AreEqual("存读", loadedWar.reason);
            Assert.IsNotNull(loadedWar.casusBelli, "宣战理由应还原");
            Assert.AreEqual(GameEnums.CasusBelliType.TerritorialDispute, loadedWar.casusBelli.type);
            Assert.AreEqual(1, loadedWar.warGoals.Count, "战争目标应还原");
            Assert.AreEqual(GameEnums.WarGoalType.BorderAdjustment, loadedWar.warGoals[0].type);
        }
    }
}
