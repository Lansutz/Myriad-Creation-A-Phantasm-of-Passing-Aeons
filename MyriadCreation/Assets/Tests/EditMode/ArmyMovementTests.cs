using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P1.5 军队日结：行军推进；进入交战方领土后标记军事占领。</summary>
    public class ArmyMovementTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
        }

        private GameWorld NewWorld()
        {
            var go = new GameObject("ArmyMovementWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(73);
            return world;
        }

        private static (int attacker, int defender) PickTwoRealms(GameWorld world)
        {
            int attacker = -1;
            int defender = -1;
            foreach (int key in world.realms.Keys)
            {
                if (attacker < 0) attacker = key;
                else { defender = key; break; }
            }
            return (attacker, defender);
        }

        private static (int from, int to) FindBorderPair(GameWorld world, int attacker, int defender)
        {
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (world.tiles[i].ownerRealmId != attacker || !world.tiles[i].isLand) continue;
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
                        int target = ny * world.mapWidth + nx;
                        if (world.tiles[target].ownerRealmId == defender && world.tiles[target].isLand) return (i, target);
                    }
                }
            }
            return (-1, -1);
        }

        [Test]
        public void ArmyMovesAndOccupiesEnemyTile()
        {
            var world = NewWorld();
            var (attacker, defender) = PickTwoRealms(world);
            Assert.GreaterOrEqual(defender, 0, "前置：应有两个政权");
            var (from, to) = FindBorderPair(world, attacker, defender);
            Assert.GreaterOrEqual(from, 0, "前置：应有接壤地块");

            int unitId = -1;
            foreach (int key in world.unitDefs.Keys) { unitId = key; break; }
            Assert.GreaterOrEqual(unitId, 0, "前置：应有兵种");
            UnitDef def = world.unitDefs[unitId];
            def.speed = 100f;
            world.unitDefs[unitId] = def;

            Army army = world.CreateArmy(attacker, -1, from);
            army.unitCounts[unitId] = 10f;
            Assert.IsTrue(world.DeclareWar(attacker, defender, "行军测试"));
            army.SetMoveTarget(to, world.tiles, world.GetSeaLandGenerator(), world.mapWidth);

            world.RunDiagnosticDays(3);

            Assert.AreEqual(to, army.currentTileIndex, "军队应抵达目标地块");
            Assert.AreEqual(attacker, world.tiles[to].occupyingRealmId, "敌境应标记军事占领");
        }
    }
}
