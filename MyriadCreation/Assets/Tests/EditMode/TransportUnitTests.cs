using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Simulation.Units;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>地图单位组件：成员/职务、载重（役畜+车）、移动、易碎品、建立栖居点材料。</summary>
    public class TransportUnitTests
    {
        private static TerrainConditions Plains => new TerrainConditions
        {
            IsLand = true, TemperatureC = 20f, HumidityPct = 50f,
            SoilHumidityPct = 30f, SlopeDegrees = 0f, Elevation01 = 0.1f
        };

        private static TransportUnit Unit(TransportUnitType type = TransportUnitType.Caravan)
            => new TransportUnit { type = type, unitName = "测试队" };

        private static void AddMembers(TransportUnit unit, int count)
        {
            for (int i = 0; i < count; i++)
                unit.TryAddMember(100 + i, i == 0 ? UnitRole.Leader : UnitRole.Soldier);
            unit.TrySetLeader(100);
        }

        [Test]
        public void Members_MaxTen_AndLeaderMustBeMember()
        {
            var unit = Unit();
            AddMembers(unit, 10);
            Assert.AreEqual(10, unit.members.Count);
            Assert.IsFalse(unit.TryAddMember(999), "最多 10 名主要成员");
            Assert.IsFalse(unit.TrySetLeader(999), "领袖必须是成员");

            var fresh = Unit();
            fresh.TryAddMember(1);
            fresh.TryAddMember(2);
            Assert.IsTrue(fresh.TrySetLeader(2));
            Assert.AreEqual(2, fresh.leaderCharacterId);
            Assert.AreEqual(UnitRole.Leader, fresh.members.Find(m => m.characterId == 2).role);
        }

        [Test]
        public void DefaultRoles_ByUnitType()
        {
            var caravanRoles = TransportUnit.DefaultRoles(TransportUnitType.Caravan);
            CollectionAssert.Contains(caravanRoles, UnitRole.Trader);
            var settlerRoles = TransportUnit.DefaultRoles(TransportUnitType.Settlers);
            CollectionAssert.Contains(settlerRoles, UnitRole.Artisan);
            var armyRoles = TransportUnit.DefaultRoles(TransportUnitType.Army);
            CollectionAssert.Contains(armyRoles, UnitRole.Scout);
        }

        [Test]
        public void Capacity_BaseAnimalsAndCarts()
        {
            var unit = Unit();
            AddMembers(unit, 2);
            Assert.AreEqual(40f, unit.TotalCapacity(), 0.01f);

            unit.animals.Add(PackAnimalKind.Horse);
            unit.animals.Add(PackAnimalKind.Horse);
            Assert.AreEqual(40f + 120f, unit.TotalCapacity(), 0.01f);

            // 大车需要 4 点役畜力，2 匹马只有 2 点 → 车不贡献容量
            unit.carts.Add(CartKind.Wagon);
            Assert.AreEqual(0f, unit.CartCapacity(), 0.01f);
            Assert.AreEqual(160f, unit.TotalCapacity(), 0.01f);

            // 再加 2 头牛（每头 2 点）= 6 点，足够大车
            unit.animals.Add(PackAnimalKind.Ox);
            unit.animals.Add(PackAnimalKind.Ox);
            Assert.AreEqual(500f, unit.CartCapacity(), 0.01f);
            Assert.Greater(unit.TotalCapacity(), 500f);
        }

        [Test]
        public void Handcart_CanBePulledByPeople_WithoutAnimals()
        {
            var unit = Unit(TransportUnitType.Settlers);
            AddMembers(unit, 1);
            unit.carts.Add(CartKind.Handcart);
            Assert.AreEqual(80f, unit.CartCapacity(), 0.01f);

            var cartOnly = Unit();
            cartOnly.carts.Add(CartKind.OxCart);
            Assert.AreEqual(0f, cartOnly.CartCapacity(), "光有车没有役畜，车发挥不了作用");
        }

        [Test]
        public void Cargo_RespectsCapacity()
        {
            var unit = Unit();
            AddMembers(unit, 1); // 20 容量
            Assert.IsTrue(unit.TryAddCargo(40, amount: 10f, unitWeight: 1f)); // 10
            Assert.IsFalse(unit.TryAddCargo(40, amount: 20f, unitWeight: 1f), "超出载重应拒绝");
            Assert.AreEqual(10f, unit.GetCargo(40), 0.01f);
            Assert.IsTrue(unit.TryRemoveCargo(40, 4f));
            Assert.AreEqual(6f, unit.GetCargo(40), 0.01f);
        }

        [Test]
        public void Speed_SlowsWithLoadAndFragileGoods()
        {
            var unit = Unit();
            AddMembers(unit, 2);
            unit.animals.Add(PackAnimalKind.Horse);
            float empty = unit.MoveSpeedMultiplier(Plains);

            unit.TryAddCargo(40, amount: 50f, unitWeight: 1f);
            float loaded = unit.MoveSpeedMultiplier(Plains);
            Assert.Less(loaded, empty, "负载越高越慢");

            var fragile = Unit();
            AddMembers(fragile, 2);
            fragile.animals.Add(PackAnimalKind.Horse);
            fragile.TryAddCargo(80, amount: 50f, unitWeight: 1f, fragile: true);
            Assert.Less(fragile.MoveSpeedMultiplier(Plains), loaded,
                "易碎品会进一步降低速度");
        }

        [Test]
        public void Speed_UsesFieldConditions()
        {
            var camel = Unit();
            AddMembers(camel, 1);
            camel.animals.Add(PackAnimalKind.Camel);
            var hotDry = new TerrainConditions
            {
                IsLand = true, TemperatureC = 40f, HumidityPct = 15f,
                SoilHumidityPct = 10f, SlopeDegrees = 0f, Elevation01 = 0.2f
            };
            var coldWet = new TerrainConditions
            {
                IsLand = true, TemperatureC = 0f, HumidityPct = 90f,
                SoilHumidityPct = 80f, SlopeDegrees = 5f, Elevation01 = 0.2f
            };
            Assert.Greater(camel.MoveSpeedMultiplier(hotDry), camel.MoveSpeedMultiplier(coldWet),
                "骆驼在热干环境更快，冷湿环境更慢");
        }

        [Test]
        public void UnitTypes_CanConvert_ArmyIsAUnitType()
        {
            var unit = Unit(TransportUnitType.Settlers);
            Assert.IsTrue(unit.ConvertTo(TransportUnitType.Army), "武装移民可转为军队");
            Assert.IsTrue(unit.ConvertTo(TransportUnitType.Warband), "军队/战团可互转");
            Assert.IsFalse(unit.ConvertTo(TransportUnitType.Settlers), "军队不能直接变移民队");

            var caravan = Unit(TransportUnitType.Caravan);
            Assert.IsTrue(caravan.ConvertTo(TransportUnitType.Settlers));
            var freshCaravan = Unit(TransportUnitType.Caravan);
            Assert.IsFalse(freshCaravan.ConvertTo(TransportUnitType.Army), "商队不能直接变军队");
        }

        [Test]
        public void ManagementBurden_SlowsOverloadedTeam_ArmyMoreDisciplined()
        {
            var team = Unit(TransportUnitType.Caravan);
            AddMembers(team, 4);
            team.animals.Add(PackAnimalKind.Horse);
            team.animals.Add(PackAnimalKind.Horse);
            team.animals.Add(PackAnimalKind.Horse);
            team.animals.Add(PackAnimalKind.Horse);
            team.carts.Add(CartKind.Wagon);
            team.carts.Add(CartKind.Wagon);
            team.carts.Add(CartKind.Wagon);
            team.carts.Add(CartKind.Wagon);

            float armyCapacity = Unit(TransportUnitType.Army).ManagementCapacity();
            float caravanCapacity = unitCapacity(TransportUnitType.Caravan);
            Assert.Greater(armyCapacity, caravanCapacity, "军队纪律性提高管理能力");
            Assert.Greater(team.ManagementBurden(), team.ManagementCapacity(), "队伍已超管理能力");
            Assert.Less(team.MoveSpeedMultiplier(Plains), 1f, "超编降低速度");
        }

        private static float unitCapacity(TransportUnitType type)
            => new TransportUnit { type = type }.ManagementCapacity();

        [Test]
        public void AnimalGoods_ConvertToPackAnimals()
        {
            var unit = Unit();
            AddMembers(unit, 1);
            unit.TryAddCargo(20, 2f, 1f); // 马 ×2
            Assert.IsTrue(unit.TryConvertCargoToAnimal(20, 2));
            Assert.AreEqual(2, unit.animals.Count);
            Assert.AreEqual(0f, unit.GetCargo(20), 0.001f);
            Assert.Greater(unit.TotalCapacity(), 40f, "转成役畜后载重增加");

            Assert.IsFalse(unit.TryConvertCargoToAnimal(53, 1), "铜不是役畜");
        }

        [Test]
        public void RiverAndOcean_NeedBoats()
        {
            var unit = Unit();
            AddMembers(unit, 2);
            unit.animals.Add(PackAnimalKind.Horse);

            var river = new TerrainConditions
            {
                IsLand = true, IsRiver = true, WaterAdjacentWeight = 0.8f,
                RiverWidthMeters = 120f, RiverDepthMeters = 3f, RiverCurrentMps = 1.5f,
                TemperatureC = 20f, HumidityPct = 50f, SoilHumidityPct = 30f,
                SlopeDegrees = 5f, Elevation01 = 0.2f
            };
            Assert.IsTrue(river.IsDeepRiver);
            float ford = unit.MoveSpeedMultiplier(river);
            unit.boats = 1;
            float byBoat = unit.MoveSpeedMultiplier(river);
            Assert.Greater(byBoat, ford, "有船过深河更快");

            var ocean = new TerrainConditions
            {
                IsLand = false, OceanDepth01 = 0.5f,
                TemperatureC = 20f, HumidityPct = 80f, SlopeDegrees = 0f
            };
            Assert.IsTrue(ocean.RequiresBoat);
            unit.boats = 0;
            float swim = unit.MoveSpeedMultiplier(ocean);
            unit.boats = 1;
            float sail = unit.MoveSpeedMultiplier(ocean);
            Assert.Greater(sail, swim, "有船才能有效跨海");
        }

        [Test]
        public void Founding_RequiresMaterials_AndConsumesThem()
        {
            var unit = Unit(TransportUnitType.Settlers);
            AddMembers(unit, 2);
            unit.animals.Add(PackAnimalKind.Ox);
            var cost = new Dictionary<int, float> { [30] = 20f, [40] = 10f };

            Assert.IsFalse(unit.CanFoundSettlement(cost));
            Assert.IsFalse(unit.TryConsumeFoundingMaterials(cost));

            unit.TryAddCargo(30, 20f, 1f);
            unit.TryAddCargo(40, 10f, 1f);
            Assert.IsTrue(unit.CanFoundSettlement(cost));
            Assert.IsTrue(unit.TryConsumeFoundingMaterials(cost));
            Assert.AreEqual(0f, unit.GetCargo(30), 0.01f);
            Assert.AreEqual(0f, unit.GetCargo(40), 0.01f);
            Assert.AreEqual(10, unit.FoundingDaysRequired);
        }
    }
}
