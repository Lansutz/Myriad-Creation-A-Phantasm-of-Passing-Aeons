using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Settlement;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P10.3：默认民间信仰、国教多数确立、宗教革新门槛。</summary>
    public class ReligionAdoptionTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private GameWorld NewWorld()
        {
            _go = new GameObject("FaithAdoptionWorld");
            var world = _go.AddComponent<GameWorld>();
            world.mapWidth = 32;
            world.mapHeight = 16;
            world.SetupOptions = WorldSetupOptions.CreateDefault();
            world.InitializeWorld();
            world.GenerateTerrain(131);
            return world;
        }

        [Test]
        public void FolkFaith_IsContentDrivenAndSeeded()
        {
            int folk = ReligionCatalog.GetDefaultFolkReligionId();
            Assert.AreEqual(601, folk, "默认民间信仰应由内容标记决定");
            var world = NewWorld();
            bool found = false;
            foreach (TileData tile in world.tiles)
            {
                if (tile.populationBlocks == null) continue;
                foreach (PopulationBlock block in tile.populationBlocks)
                {
                    if (block.count <= 0f) continue;
                    Assert.AreEqual(folk, block.faithId, "生成期人口应持有民间信仰");
                    found = true;
                }
            }
            Assert.IsTrue(found, "默认世界应有已持有信仰的人口");
        }

        [Test]
        public void Adoption_PicksMajorityFaithWithOpenReligion()
        {
            var world = NewWorld();
            SettlementData settlement = null;
            foreach (SettlementData s in world.settlements.Values) { settlement = s; break; }
            Assert.IsNotNull(settlement);

            var realm = new RealmData { realmId = 99, realmName = "测试政权" };
            world.realms[99] = realm;
            realm.coreTiles.Add(settlement.tileIndex);
            world.tiles[settlement.tileIndex].populationBlocks = new List<PopulationBlock>
            {
                new PopulationBlock { count = 1000f, raceId = 0, faithId = 601 }
            };

            Assert.IsTrue(world.TryAdoptMajorityFaithForRealm(99));
            Assert.AreEqual(601, realm.stateReligionId);
        }

        [Test]
        public void SetStateReligion_RejectsLockedReligionUntilInnovation()
        {
            var world = NewWorld();
            var realm = new RealmData { realmId = 98, realmName = "测试政权" };
            world.realms[98] = realm;

            Assert.IsFalse(world.FaithCommand.SetStateReligion(98, 101, out string reason), "缺少神学革新时不应能立为国教");
            StringAssert.Contains("革新", reason);

            world.Innovations.GrantInnovation(98, 2057);
            Assert.IsTrue(world.FaithCommand.SetStateReligion(98, 101, out reason), reason);
            Assert.AreEqual(101, realm.stateReligionId);
        }
    }
}
