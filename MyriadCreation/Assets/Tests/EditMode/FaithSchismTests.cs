using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.2 六层运行时：宗派分裂与挂靠走既有 Denomination/FaithSystem，不建平行教团。</summary>
    public class FaithSchismTests
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
            var go = new GameObject("FaithSchismTestWorld");
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
        public void CreateDenomination_MovesFaithAndRebuildsMembership()
        {
            GameWorld world = NewWorld();
            FaithSystem faith = world.FaithSystems.FirstOrDefault(f => f != null && f.religionId >= 0);
            Assert.IsNotNull(faith, "前置：内容表应有挂 religion 的 faith");

            int denominationId = world.CreateDenomination(faith.faithId, "测试宗");

            Assert.Greater(denominationId, 0);
            Assert.AreEqual(denominationId, faith.denominationId);
            Denomination denomination = world.GetDenomination(denominationId);
            Assert.IsNotNull(denomination);
            Assert.AreEqual(faith.religionId, denomination.religionId);
            Assert.IsFalse(denomination.isDefault);
            CollectionAssert.Contains(denomination.faithIds, faith.faithId);
        }

        [Test]
        public void AttachFaith_RejectsDifferentReligion()
        {
            GameWorld world = NewWorld();
            List<FaithSystem> faiths = world.FaithSystems.Where(f => f != null && f.religionId >= 0).ToList();
            FaithSystem first = faiths.FirstOrDefault();
            FaithSystem other = faiths.FirstOrDefault(f => f.religionId != first.religionId);
            if (other == null)
            {
                Assert.Ignore("当前内容没有两个不同 religion 的 faith，跳过跨教挂靠检查");
            }
            int denominationId = world.CreateDenomination(first.faithId, "测试宗");

            Assert.IsFalse(world.AttachFaithToDenomination(other.faithId, denominationId));
            Assert.AreNotEqual(denominationId, other.denominationId);
        }

        [Test]
        public void CreateDenomination_InvalidFaith_ReturnsMinusOne()
        {
            GameWorld world = NewWorld();

            Assert.AreEqual(-1, world.CreateDenomination(-999, "无此信仰"));
        }
    }
}
