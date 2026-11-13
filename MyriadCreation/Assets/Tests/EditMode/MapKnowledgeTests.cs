using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// S19 地图知识：每政权独立位图、半径揭示、Base64 存档往返、坏档段安全。
    /// </summary>
    public class MapKnowledgeTests
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

        [Test]
        public void MarkKnown_IsPerRealm()
        {
            var knowledge = new MapKnowledge();
            knowledge.MarkKnown(1, 10);

            Assert.IsTrue(knowledge.IsKnown(1, 10), "写入方应知道该地块");
            Assert.IsFalse(knowledge.IsKnown(2, 10), "其他政权不得共享知识");
            Assert.IsFalse(knowledge.IsKnown(1, 11), "未写入的邻居不得自动已知");
        }

        [Test]
        public void RevealRadius_ClampsAtMapEdges()
        {
            var knowledge = new MapKnowledge();
            knowledge.RevealRadius(1, 0, 2, 10, 10);

            Assert.IsTrue(knowledge.IsKnown(1, 0), "角落中心应已知");
            Assert.IsTrue(knowledge.IsKnown(1, 2), "半径内应已知");
            Assert.IsFalse(knowledge.IsKnown(1, 3), "半径外不得已知");
            Assert.AreEqual(9, knowledge.KnownCount(1), "左上角 2 格半径应揭示 3×3");
        }

        [Test]
        public void CaptureRestore_RoundTripsKnowledge()
        {
            var source = new MapKnowledge();
            source.MarkKnown(1, 5);
            source.MarkKnown(1, 900);
            source.MarkKnown(2, 7);

            var restored = new MapKnowledge();
            restored.Restore(source.Capture(), 1000);

            Assert.IsTrue(restored.IsKnown(1, 5));
            Assert.IsTrue(restored.IsKnown(1, 900));
            Assert.IsTrue(restored.IsKnown(2, 7));
            Assert.IsFalse(restored.IsKnown(1, 6));
            Assert.AreEqual(2, restored.KnownCount(1));
        }

        [Test]
        public void Restore_IgnoresBrokenSegment()
        {
            var knowledge = new MapKnowledge();
            var data = new List<RealmKnowledgeSaveData>
            {
                new RealmKnowledgeSaveData { realmId = 1, bitsBase64 = "not-base64" },
                new RealmKnowledgeSaveData { realmId = 2, bitsBase64 = "" }
            };

            Assert.DoesNotThrow(() => knowledge.Restore(data, 100), "坏档段不得让整档读失败");
            Assert.IsFalse(knowledge.IsKnown(1, 0));
        }

        [Test]
        public void GeneratedWorld_SeedsOwnedTerritory()
        {
            var go = new GameObject("KnowledgeWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(71);

            int realmId = -1;
            foreach (var realm in world.realms.Values) { realmId = realm.realmId; break; }
            Assert.GreaterOrEqual(realmId, 0, "前置：应有政权");
            Assert.Greater(world.Knowledge.KnownCount(realmId), 0, "生成后政权至少应知道自己拥有的地块");
        }
    }
}
