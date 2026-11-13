using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P1.5 海军写侧：舰队每日消耗补给并影响士气/组织。</summary>
    public class NavyTests
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
            var go = new GameObject("NavyWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(83);
            return world;
        }

        [Test]
        public void Navy_ConsumesSupplyDaily()
        {
            var world = NewWorld();
            int realmId = -1;
            foreach (int key in world.realms.Keys) { realmId = key; break; }
            Assert.GreaterOrEqual(realmId, 0);
            int port = -1;
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (world.tiles[i].isCoast) { port = i; break; }
            }
            if (port < 0) Assert.Ignore("地图没有海岸格");

            Navy navy = world.CreateNavy(realmId, -1, port);
            navy.AddShips(1, 10f);
            navy.supply = 100f;

            world.RunDiagnosticDays(1);

            Assert.Less(navy.supply, 100f, "舰队应每日消耗补给");
            Assert.GreaterOrEqual(navy.organization, 0f);
        }
    }
}
