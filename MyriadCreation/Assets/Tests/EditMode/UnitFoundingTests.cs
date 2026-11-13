using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>单位建立栖居点（材料+工期）与动物物产。</summary>
    public class UnitFoundingTests
    {
        private static TransportUnit SettlerUnit()
        {
            var unit = new TransportUnit { type = TransportUnitType.Settlers };
            unit.TryAddMember(1, UnitRole.Leader);
            unit.TryAddMember(2, UnitRole.Artisan);
            unit.animals.Add(PackAnimalKind.Ox);
            return unit;
        }

        [Test]
        public void Founding_NeedsMaterialsAndDays()
        {
            var unit = SettlerUnit();
            var cost = new Dictionary<int, float> { [30] = 20f, [40] = 10f };

            Assert.IsFalse(unit.IsFoundingReady(cost), "无材料/无工期时不可建点");
            unit.TryAddCargo(30, 20f, 1f);
            unit.TryAddCargo(40, 10f, 1f);
            Assert.IsFalse(unit.IsFoundingReady(cost), "材料齐但工期不足");

            Assert.IsFalse(unit.AdvanceFounding(5f));
            Assert.IsTrue(unit.AdvanceFounding(5f), "累计 10 天达到工期");
            Assert.IsTrue(unit.IsFoundingReady(cost));

            Assert.IsTrue(unit.TryConsumeFoundingMaterials(cost));
            Assert.AreEqual(0f, unit.GetCargo(30), 0.01f);
            Assert.AreEqual(0f, unit.GetCargo(40), 0.01f);
        }

        [Test]
        public void UnitFounding_WithoutWorld_ReturnsFalse()
        {
            var unit = SettlerUnit();
            var cost = new Dictionary<int, float> { [30] = 20f, [40] = 10f };
            unit.TryAddCargo(30, 20f, 1f);
            unit.TryAddCargo(40, 10f, 1f);
            unit.AdvanceFounding(10f);

            Assert.IsFalse(UnitFounding.TryFoundSettlement(null, unit, 1, 0, 1, out _));
            Assert.AreEqual(20f, unit.GetCargo(30), "失败不应扣材料");
        }

        [Test]
        public void AnimalGoods_AreRegisteredForTrade()
        {
            var world = new GameWorld();
            var method = typeof(GameWorld).GetMethod("InitializeGoodsDefs",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method);
            method.Invoke(world, null);

            foreach (int id in new[] { 23, 24, 25, 26, 27, 28 })
            {
                Assert.IsTrue(world.goodsDefs.ContainsKey(id), $"动物物产 {id} 应存在");
                Assert.IsFalse(string.IsNullOrEmpty(world.goodsDefs[id].goodsName));
            }
            Assert.AreEqual("骆驼", world.goodsDefs[23].goodsName);
        }
    }
}
