using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Save.Contracts;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.2：合一写运行时教义覆盖、妥协写共融期限，两者都可入档。</summary>
    public class FaithUnityTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
        }

        private GameWorld NewWorld()
        {
            var go = new GameObject("FaithUnityTestWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 24;
            world.mapHeight = 12;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(11);
            return world;
        }

        [Test]
        public void UnifyReligion_GivesAllFaithsSameDoctrineOverride()
        {
            GameWorld world = NewWorld();
            var group = world.FaithSystems
                .Where(f => f != null && f.religionId >= 0)
                .GroupBy(f => f.religionId)
                .FirstOrDefault(g => g.Count() >= 2);
            if (group == null)
            {
                Assert.Ignore("当前内容没有同一 religion 下的两条 faith");
            }

            Assert.IsTrue(world.TryUnifyReligion(group.Key));

            var members = world.FaithSystems.Where(f => f != null && f.religionId == group.Key).ToList();
            string first = string.Join("|", members[0].doctrineIds);
            foreach (FaithSystem faith in members)
            {
                Assert.AreEqual(first, string.Join("|", faith.doctrineIds), "合一后教义集合应一致");
                Assert.AreEqual(-1, faith.communionUntilDay);
            }
        }

        [Test]
        public void Compromise_SetsCommunionDeadline()
        {
            GameWorld world = NewWorld();
            var group = world.FaithSystems
                .Where(f => f != null && f.religionId >= 0)
                .GroupBy(f => f.religionId)
                .FirstOrDefault(g => g.Count() >= 2);
            if (group == null)
            {
                Assert.Ignore("当前内容没有同一 religion 下的两条 faith");
            }

            Assert.IsTrue(world.TryCompromiseReligion(group.Key, 365));
            foreach (FaithSystem faith in world.FaithSystems.Where(f => f != null && f.religionId == group.Key))
            {
                Assert.Greater(faith.communionUntilDay, world.currentDay);
            }
        }

        [Test]
        public void FaithSaveData_RoundTripsDoctrineAndCommunion()
        {
            var faith = new FaithSystem
            {
                faithId = 1,
                doctrineIds = new List<string> { "doctrine_monotheism", "doctrine_pacifism" },
                communionUntilDay = 123
            };

            FaithSaveData dto = FaithSaveMapping.FromFaith(faith);
            var restored = new FaithSystem();
            FaithSaveMapping.ApplyTo(dto, restored);

            CollectionAssert.AreEqual(faith.doctrineIds, restored.doctrineIds);
            Assert.AreEqual(123, restored.communionUntilDay);
        }
    }
}
