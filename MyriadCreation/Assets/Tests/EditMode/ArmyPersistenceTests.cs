using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Enums;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>存读档 v13：军队实体入档（编制/士气/补给/行军路径/队伍货物），地图视图由军队重新派生。</summary>
    public class ArmyPersistenceTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            MapUnitRegistry.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            MapUnitRegistry.Clear();
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
        }

        private GameWorld NewWorld()
        {
            var go = new GameObject("ArmyPersistenceWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 2;
            world.mapHeight = 1;
            world.tiles = new[]
            {
                new MyriadCreation.Core.Data.TileData { tileIndex = 0, exists = true, isLand = true },
                new MyriadCreation.Core.Data.TileData { tileIndex = 1, exists = true, isLand = true }
            };
            world.armies = new Dictionary<int, Army>();
            return world;
        }

        private static Army BuildArmy()
        {
            var army = new Army
            {
                armyId = 4,
                armyName = "测试国军",
                ownerRealmId = 3,
                commanderId = 12,
                currentTileIndex = 1,
                state = GameEnums.CombatState.Idle,
                organization = 78f,
                morale = 61f,
                supply = 44f,
                training = 55f,
                moveTargetTile = 0,
                moveProgress = 0.35f,
                scoutDetached = true,
                scoutTileIndex = 0
            };
            army.unitCounts[50] = 500f;
            army.unitCounts[51] = 120f;
            army.movePath.Add(0);
            army.transport = new TransportUnit
            {
                type = TransportUnitType.Army,
                unitName = "军需队",
                leaderCharacterId = 12,
                boats = 2
            };
            army.transport.animals.Add(PackAnimalKind.Horse);
            army.transport.carts.Add(CartKind.Wagon);
            army.transport.AddCargo(10, 240f);
            army.transport.AddCargo(70, 30f);
            return army;
        }

        [Test]
        public void RoundTrip_ThroughJson_PreservesArmyState()
        {
            var world = NewWorld();
            world.armies[4] = BuildArmy();

            var captured = ArmyPersistence.CaptureArmies(world);
            Assert.AreEqual(1, captured.Count, "军队应入档");

            var json = JsonUtility.ToJson(new SaveData { armies = captured });
            var reloaded = JsonUtility.FromJson<SaveData>(json);
            Assert.IsNotNull(reloaded.armies);

            var target = NewWorld();
            int nextArmyId = ArmyPersistence.RestoreArmies(target, reloaded.armies);

            Assert.AreEqual(5, nextArmyId, "自增 armyId 应推进到存档最大值之后");
            Assert.IsTrue(target.armies.ContainsKey(4));
            var army = target.armies[4];
            Assert.AreEqual("测试国军", army.armyName);
            Assert.AreEqual(3, army.ownerRealmId);
            Assert.AreEqual(12, army.commanderId);
            Assert.AreEqual(1, army.currentTileIndex);
            Assert.AreEqual(78f, army.organization, 0.01f);
            Assert.AreEqual(61f, army.morale, 0.01f);
            Assert.AreEqual(44f, army.supply, 0.01f);
            Assert.IsTrue(army.scoutDetached, "斥候分遣状态应入档");
            Assert.AreEqual(0, army.scoutTileIndex, "斥候地块应入档");
            Assert.AreEqual(55f, army.training, 0.01f);
            Assert.AreEqual(500f, army.unitCounts[50], 0.01f);
            Assert.AreEqual(120f, army.unitCounts[51], 0.01f);
            CollectionAssert.AreEqual(new[] { 0 }, army.movePath);
            Assert.AreEqual(0, army.moveTargetTile);
            Assert.AreEqual(0.35f, army.moveProgress, 0.001f);

            Assert.IsNotNull(army.transport, "军队队伍/货物不应读档蒸发");
            Assert.AreEqual(240f, army.transport.GetCargoAmount(10), 0.01f);
            Assert.AreEqual(30f, army.transport.GetCargoAmount(70), 0.01f);
            Assert.AreEqual(2, army.transport.boats, "船舶编成应入档");
            Assert.AreEqual(1, army.transport.animals.Count, "驮兽编成应入档");
            Assert.AreEqual(PackAnimalKind.Horse, army.transport.animals[0]);
            Assert.AreEqual(1, army.transport.carts.Count, "车辆编成应入档");
            Assert.AreEqual(CartKind.Wagon, army.transport.carts[0]);
            Assert.AreEqual(12, army.transport.leaderCharacterId);
        }

        [Test]
        public void RegisterArmyViews_DerivesMapUnitFromArmy()
        {
            var world = NewWorld();
            ArmyPersistence.RestoreArmies(world, new List<ArmySaveData> { ArmySaveData.FromArmy(BuildArmy()) });

            ArmyPersistence.RegisterArmyViews(world);

            MapUnit view = null;
            foreach (var unit in MapUnitRegistry.All)
            {
                if (unit.source == UnitSourceKind.Army && unit.sourceId == 4) view = unit;
            }

            Assert.IsNotNull(view, "军队应有地图单位视图");
            Assert.AreEqual(UnitCategory.Military, view.category);
            Assert.AreEqual(1, view.currentTile);
            Assert.AreEqual(3, view.ownerRealmId);
            Assert.AreEqual(12, view.leaderCharacterId);
        }

        [Test]
        public void LegacySave_WithoutArmies_YieldsEmptyArmyTable()
        {
            var world = NewWorld();
            world.armies[9] = BuildArmy();

            int nextArmyId = ArmyPersistence.RestoreArmies(world, null);

            Assert.AreEqual(1, nextArmyId);
            Assert.AreEqual(0, world.armies.Count, "旧档应按零军队处理，不残留上一局军队");
        }
    }
}
