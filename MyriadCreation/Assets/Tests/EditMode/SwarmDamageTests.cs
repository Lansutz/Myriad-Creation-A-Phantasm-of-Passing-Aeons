using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 蝗群损害只在“到达新地块”时结算一次。MapActor.Tick 每天都会调用 Act，
    /// 若按日扣肥力，一个蝗群 120 天就能把沿途土地啃到零（220 年诊断的实际故障）。
    /// </summary>
    public class SwarmDamageTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private static TileData NewTile()
        {
            return new TileData
            {
                exists = true,
                isLand = true,
                isCoast = false,
                biome = GameEnums.BiomeType.TemperateGrassland,
                fertility = 0.8f,
                vegetation = 0.8f,
                development = 0.3f
            };
        }

        [Test]
        public void Act_DamagesOncePerTileArrival()
        {
            _go = new GameObject("SwarmWorld");
            var world = _go.AddComponent<GameWorld>();
            world.tiles = new[] { NewTile(), NewTile() };

            var actor = new MapActor { currentTile = 0 };
            var swarm = new SwarmAI(120);

            swarm.Act(world, actor);
            float afterFirst = world.tiles[0].fertility;
            Assert.Less(afterFirst, 0.8f, "到达时应造成一次肥力损伤");

            swarm.Act(world, actor);
            swarm.Act(world, actor);
            Assert.AreEqual(afterFirst, world.tiles[0].fertility, 0.0001f, "同一地块停留期间不得重复扣肥力");

            actor.currentTile = 1;
            swarm.Act(world, actor);
            Assert.Less(world.tiles[1].fertility, 0.8f, "移动到新地块后应再次结算");
        }

        [Test]
        public void Act_DoesNotZeroFertilityInOneVisit()
        {
            _go = new GameObject("SwarmWorld");
            var world = _go.AddComponent<GameWorld>();
            world.tiles = new[] { NewTile() };

            var actor = new MapActor { currentTile = 0 };
            var swarm = new SwarmAI(120);
            swarm.Act(world, actor);

            Assert.Greater(world.tiles[0].fertility, 0.5f, "单次蝗群过境不能把肥力清零");
        }
    }
}
